using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.Systems
{
    public class MovementSystem
    {
        private readonly GameWorld _world;
        private readonly float _pathUpdateInterval;

        public MovementSystem(GameWorld world, float pathUpdateInterval)
        {
            _world = world;
            _pathUpdateInterval = pathUpdateInterval;
        }

        public void Execute(float dt)
        {
            for (int i = _world.Movables.Count - 1; i >= 0; i--)
            {
                var movable = _world.Movables[i];

                if (movable == null || (movable is GameEntity entity && !entity.IsAlive))
                    continue;

                var moveData = _world.MovementComponents[movable];
                Vector3 originPos = movable.Transform.position;
                Vector3 targetPos = Vector3.zero;
                bool shouldMove = false;

                if (movable is IWeapon weaponUser && weaponUser.Target != null)
                {
                    targetPos = weaponUser.Target.Transform.position;

                    Vector3 toTarget = targetPos - originPos;
                    toTarget.y = 0f;
                    float distance = toTarget.magnitude;
                    float attackRange = weaponUser.Weapon != null ? weaponUser.Weapon.AttackRange : 1f;

                    if (distance <= attackRange)
                    {
                        SmoothLookAt(movable, toTarget.normalized, dt);
                        continue;
                    }

                    shouldMove = true;
                }
                else if (moveData.ExplicitDestination.HasValue)
                {
                    targetPos = moveData.ExplicitDestination.Value;
                    if (Vector3.SqrMagnitude(targetPos - originPos) < 0.04f)
                    {
                        moveData.ExplicitDestination = null;
                        continue;
                    }
                    shouldMove = true;
                }

                if (shouldMove)
                {
                    UpdateUnitPath(movable, moveData, originPos, targetPos);
                    MoveUnitAlongPath(movable, moveData, originPos, dt);
                }
            }
        }

        private void UpdateUnitPath(IMovable movable, MovementData moveData, Vector3 originPos, Vector3 targetPos)
        {
            if (Time.time < moveData.nextPathUpdateTime) return;
            moveData.nextPathUpdateTime = Time.time + _pathUpdateInterval;

            moveData.hasValidPath = false;

            bool sampleStart = NavMesh.SamplePosition(originPos, out var startHit, 3.0f, NavMesh.AllAreas);
            bool sampleTarget = NavMesh.SamplePosition(targetPos, out var targetHit, 3.0f, NavMesh.AllAreas);

            if (!sampleStart || !sampleTarget) return;

            if (NavMesh.CalculatePath(startHit.position, targetHit.position, NavMesh.AllAreas, moveData.path))
            {
                if (moveData.path.status is NavMeshPathStatus.PathComplete or NavMeshPathStatus.PathPartial)
                {
                    moveData.currentWaypointIndex = moveData.path.corners.Length > 1 ? 1 : 0;
                    moveData.hasValidPath = true;
                }
            }
        }

        private void MoveUnitAlongPath(IMovable movable, MovementData moveData, Vector3 originPos, float dt)
        {
            if (!moveData.hasValidPath || moveData.path.corners.Length <= moveData.currentWaypointIndex) return;

            Vector3 waypoint = moveData.path.corners[moveData.currentWaypointIndex];
            Vector3 toWaypoint = waypoint - originPos;
            toWaypoint.y = 0f;

            if (toWaypoint.sqrMagnitude < 0.04f)
            {
                moveData.currentWaypointIndex++;
                return;
            }

            Vector3 moveDir = toWaypoint.normalized;
            SmoothLookAt(movable, moveDir, dt);

            float speed = movable.Config != null ? movable.Config.MoveSpeed : 3f;
            movable.MovePosition(moveDir * speed * dt);
        }

        private void SmoothLookAt(IMovable movable, Vector3 direction, float dt)
        {
            if (direction.sqrMagnitude < 0.001f) return;
            Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
            float rotSpeed = movable.RotationSpeed > 0 ? movable.RotationSpeed : 10f;
            movable.SetRotation(Quaternion.Slerp(movable.Transform.rotation, targetRot, rotSpeed * dt));
        }
    }
}

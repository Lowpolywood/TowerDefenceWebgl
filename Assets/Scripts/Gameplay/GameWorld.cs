using System.Collections.Generic;

namespace Gameplay 
{
    /// <summary>
    /// Kind of ECS world wich contains entity pools
    /// </summary>
    public class GameWorld
    {
        public Dictionary<int, List<GameEntity>> Entities { get; } = new();
        public List<IWeapon> Weapons { get; } = new(256);
        public List<IMovable> Movables { get; } = new(256);
        public Dictionary<IMovable, MovementData> MovementComponents { get; } = new();

        public GameWorld()
        {
            Entities.Add(0, new List<GameEntity>());
            Entities.Add(1, new List<GameEntity>());
        }

        public void AddEntity(GameEntity entity)
        {
            if (entity == null) return;

            entity.Init();

            var target = entity.GetComponent<ITarget>();
            var weapon = entity.GetComponent<IWeapon>();
            var movable = entity.GetComponent<IMovable>();

            if (target != null)
            {
                if (!Entities.ContainsKey(target.Team))
                {
                    Entities.Add(target.Team, new List<GameEntity>());
                }
                Entities[target.Team].Add(entity);
            }

            if (weapon != null)
            {
                Weapons.Add(weapon);
            }

            if (movable != null)
            {
                if (!MovementComponents.ContainsKey(movable))
                {
                    Movables.Add(movable);
                    MovementComponents.Add(movable, new MovementData());
                }
            }
        }

        public void RemoveEntity(GameEntity entity)
        {
            if (entity == null) return;

            var target = entity.GetComponent<ITarget>();
            if (target != null && Entities.ContainsKey(target.Team))
            {
                Entities[target.Team].Remove(entity);
            }

            var weapon = entity.GetComponent<IWeapon>();
            if (weapon != null)
            {
                Weapons.Remove(weapon);
            }

            var movable = entity.GetComponent<IMovable>();
            if (movable != null)
            {
                Movables.Remove(movable);
                MovementComponents.Remove(movable);
            }
        }
    }
}
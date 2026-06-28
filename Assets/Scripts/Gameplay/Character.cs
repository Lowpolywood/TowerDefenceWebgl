using Configs;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// In real case ECS based contains Weapon, Target, Movable, Team components etc
    /// </summary>
    public class Character : GameEntity, IWeapon, IMovable
    {
        [field: SerializeField] public CharacterConfig Config { get; private set; }
        [field: SerializeField] public WeaponConfig Weapon { get; private set; }
        [field: SerializeField] public Transform ShootPoint { get; private set; }

        [SerializeField] private float rotationSpeed = 10f;

        public Transform Transform => transform;
        public ITarget Target { get; set; }
        public int Team { get; set; }
        public float Cooldown { get; set; }

        public float RotationSpeed => Config.RotationSpeed;

        public override void Init()
        {
            base.Init();

            MaxHealth = Config.Health;
            Health = Config.Health;

            if (ShootPoint == null) ShootPoint = transform;
        }

        public void MovePosition(Vector3 delta) => transform.position += delta;
        public void SetRotation(Quaternion rotation) => transform.rotation = rotation;
    }
}

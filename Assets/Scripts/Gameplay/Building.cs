using UnityEngine;
using Configs;

namespace Gameplay
{
    /// <summary>
    /// In real case ECS based contains Weapon, Target, Movable, Team components etc
    /// </summary>
    public class Building : GameEntity, IWeapon
    {
        [field: SerializeField] public BuildingConfig Config { get; private set; }
        [field: SerializeField] public WeaponConfig Weapon { get; set; }
        [field: SerializeField] public Transform ShootPoint { get; private set; }

        public Transform Transform => transform;
        public ITarget Target { get; set; }
        public int Team { get; set; }
        public float Cooldown { get; set; }

        public override void Init()
        {
            base.Init();

            MaxHealth = Config.Health;
            Health = Config.Health;

            if (ShootPoint == null) ShootPoint = transform;
        }
    }
}

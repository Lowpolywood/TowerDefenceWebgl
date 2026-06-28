using System;
using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "New Weapon Config", menuName = "Config/Weapon", order = 100)]
    public class WeaponConfig : Config
    {
        [field: SerializeField] public float AttackRange { get; private set; } = 1f;
        [field: SerializeField] public float Rate { get; private set; } = 1f;

        [Header("Delivery Method")]
        [SerializeField] private bool isProjectile;
        public bool IsProjectile => isProjectile;
        [SerializeField] private ProjectileSettings[] projectiles;
        public ProjectileSettings[] Projectiles => projectiles;
        [field: SerializeField] public int MaxTargetsPerShot { get; private set; } = 1;

        [Header("Impact Effect (Melee Damage or Area Splash)")]
        [SerializeField] private EffectConfig impactEffect;
        public EffectConfig ImpactEffect => impactEffect;

        [Serializable]
        public struct ProjectileSettings
        {
            public ProjecticleConfig projecticle;
            public float speed;
            public AnimationCurve speedOverPath;
            public float parabolaHeight;
        }
    }
}

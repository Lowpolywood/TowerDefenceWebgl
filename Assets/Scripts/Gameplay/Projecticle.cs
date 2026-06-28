using Configs;
using Gameplay;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// In real case struct ECS componenets emmited over shared particle emitters
    /// </summary>
    public class Projecticle : MonoBehaviour
    {
        [field: SerializeField] public ProjecticleConfig Config { get; private set; }      
    }

    public struct ProjecticleData
    {
        public GameObject instance;
        public string poolName;
        public Vector3 origin;
        public ITarget target;
        public Vector3 lastTargetPos;
        public WeaponConfig.ProjectileSettings settings;
        public EffectConfig impactEffect;
        public float time;
        public int team;
        public float totalDistance;
    }

    public struct EffectData
    {
        public Vector3 position;
        public float radius;
        public float duration;
        public float tickRate;
        public int damagePerTick;
        public int team;
        public float timeRemaining;
        public float nextTickTime;
    }
}

using System;
using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "New Level", menuName = "Config/Level", order = 100)]
    public class LevelConfig : Config
    {
        [field: SerializeField] public SpawnSettings[] Spawn { get; private set; }

        [Serializable]
        public struct SpawnSettings 
        {
            public Config config;
            public AnimationCurve rate;
        }
    }
}
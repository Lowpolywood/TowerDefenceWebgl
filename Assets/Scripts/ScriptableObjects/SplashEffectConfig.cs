using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "New Spawn Zone Effect", menuName = "Config/Effect/SpawnZone")]
    public class SplashEffectConfig : EffectConfig
    {
        [SerializeField] private float radius = 3f;
        [SerializeField] private float duration = 5f;
        [SerializeField] private float tickRate = 0.5f;
        [SerializeField] private int damagePerTick = 10;

        public float Radius => radius;
        public float Duration => duration;
        public float TickRate => tickRate;
        public int DamagePerTick => damagePerTick;
    }
}

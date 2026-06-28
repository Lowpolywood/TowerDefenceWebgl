using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "New Direct Damage", menuName = "Config/Effect/DirectDamage")]
    public class DirectEffectConfig : EffectConfig
    {
        [SerializeField] private int damage;
        public int Damage => damage;
    }
}
using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "New Building", menuName = "Config/Building", order = 100)]
    public class BuildingConfig : Config
    {
        [field: SerializeField] public int Health { get; private set; } = 100;
        [field: SerializeField] public WeaponConfig Weapon { get; private set; }
    }
}

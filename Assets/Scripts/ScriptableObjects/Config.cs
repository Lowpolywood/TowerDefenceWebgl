using UnityEngine;

namespace Configs
{
    /// <summary>
    /// Use AssetReference instead of dirrect links to avoid default asset database
    /// </summary>
    public abstract class Config : ScriptableObject
    {
        [field: SerializeField] public int Order { get; private set; }
        [field: SerializeField] public int InitPoolSize { get; private set; }
        [field: SerializeField] public int MaxPoolSize { get; private set; }
    }
}
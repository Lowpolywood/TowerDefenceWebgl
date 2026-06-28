using Configs;
using System;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// In real ECS based, does not require solid entity
    /// </summary>
    public class GameEntity : MonoBehaviour
    {
        [SerializeField] private int health;

        public int Health
        {
            get => health;
            set
            {
                value = Mathf.Clamp(value, 0, MaxHealth);

                if (health != value)
                {
                    health = value;

                    if (health <= 0)
                        OnDeath?.Invoke(this);
                }
            }
        }

        public int MaxHealth { get; protected set; }

        public bool IsAlive => Health > 0;

        public event Action<GameEntity> OnDeath;

        public virtual void Init()
        {
            
        }
    }
}

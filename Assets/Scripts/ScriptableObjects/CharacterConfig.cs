using UnityEngine;

namespace Configs 
{
    [CreateAssetMenu(fileName = "New Character", menuName = "Config/Character", order = 100)]
    public class CharacterConfig : Config
    {
        [field: SerializeField] public int Health { get; private set; } = 100;

        [SerializeField][Range(0, 5)] float moveSpeed = 1.0f;
        public float MoveSpeed => moveSpeed;

        [SerializeField][Range(0, 5)] float rotationSpeed = 1.0f;
        public float RotationSpeed => rotationSpeed;
    }
}

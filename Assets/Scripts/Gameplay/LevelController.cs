using UnityEngine;
using UnityEngine.AI;
using Zenject;
using UI;
using UnityEngine.UI; // Separate UIController for UI logic
using Configs;
using Gameplay.Systems;

namespace Gameplay
{
    /// <summary>
    /// Root game context. Hadles shared and game context transition, connects with base logic, ECS, networking etc
    /// UI simply included here without separate UI controller and full UI base code
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        [Inject] private GameResources resources;

        [SerializeField] private HPBarView hp;
        [SerializeField] private Button switchSpell;

        [SerializeField] private Transform spawnCenter;
        [SerializeField] private float spawnRadius = 18f;
        [SerializeField] private float pathUpdateInterval = 0.2f;
        [SerializeField] private float projectileHitRadius = 0.8f;

        private GameWorld _world;
        private UnitSpawnSystem _spawnSystem;
        private MovementSystem _movementSystem;
        private CombatSystem _combatSystem;

        private float _elapsedTime;
        private Building _tower;
        private bool _isBarrageSpell;

        private void Start()
        {
            _world = new GameWorld();

            _tower = resources.Spawn("Tower", Vector3.zero, Quaternion.identity).GetComponent<Building>();
            if (_tower is ITarget target)
                target.Team = 0;

            _world.AddEntity(_tower);

            // In real case init over main ECS controller wich is incapsulate and manage logic
            // Using zenject or custom ECS injections if available
            _spawnSystem = new UnitSpawnSystem(resources, _world, spawnCenter, spawnRadius);
            _movementSystem = new MovementSystem(_world, pathUpdateInterval);
            _combatSystem = new CombatSystem(_world, resources, _tower, projectileHitRadius);

            switchSpell.onClick.AddListener(SwitchSpell);
        }

        private void Update()
        {
            if (!_tower) return;

            float dt = Time.deltaTime;
            _elapsedTime += dt;

            // Kind of run ECS systems
            _spawnSystem.Execute(_elapsedTime);

            hp.Value = _tower.Health / (float)_tower.MaxHealth;
        }

        private void FixedUpdate()
        {
            // Kind of run ECS systems
            _movementSystem.Execute(Time.fixedDeltaTime);
            _combatSystem.Execute(Time.fixedDeltaTime);
            _combatSystem.FixedExecute(Time.fixedDeltaTime);
        }

        private void SwitchSpell()
        {
            if (!_tower) return;

            _isBarrageSpell = !_isBarrageSpell;

            _tower.Weapon = _isBarrageSpell
                ? resources.GetConfig<WeaponConfig>("BarrageSpell")
                : resources.GetConfig<WeaponConfig>("FireballSpell");
        }

        private void OnDestroy()
        {
            switchSpell.onClick.RemoveListener(SwitchSpell);
        }
    }

    public class MovementData
    {
        public NavMeshPath path = new();
        public int currentWaypointIndex = 1;
        public float nextPathUpdateTime;
        public bool hasValidPath;
        public Vector3? ExplicitDestination;
    }
}

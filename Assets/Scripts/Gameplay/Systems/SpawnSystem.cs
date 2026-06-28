using UnityEngine;

namespace Gameplay.Systems
{
    public class UnitSpawnSystem
    {
        private readonly GameResources _resources;
        private readonly GameWorld _world;
        private readonly Transform _spawnCenter;
        private readonly float _spawnRadius;
        private float[] _spawnProgress;

        public UnitSpawnSystem(GameResources resources, GameWorld world, Transform spawnCenter, float spawnRadius)
        {
            _resources = resources;
            _world = world;
            _spawnCenter = spawnCenter;
            _spawnRadius = spawnRadius;

            _spawnProgress = new float[_resources.Level.Spawn.Length];
        }

        public void Execute(float elapsedTime)
        {
            var spawnCount = _resources.Level.Spawn.Length;

            if (_spawnProgress == null || _spawnProgress.Length != spawnCount)
            {
                _spawnProgress = new float[spawnCount];
            }

            for (var i = 0; i < spawnCount; i++)
            {
                var settings = _resources.Level.Spawn[i];
                if (!settings.config || settings.rate == null) continue;

                _spawnProgress[i] += Mathf.Max(0f, settings.rate.Evaluate(elapsedTime)) * Time.deltaTime;

                while (_spawnProgress[i] >= 1f)
                {
                    _spawnProgress[i] -= 1f;

                    var spawnPos = GetSpawnPosition();
                    var entityObject = _resources.Spawn(settings.config.name, spawnPos, Quaternion.identity);
                    var entity = entityObject.GetComponent<GameEntity>();

                    if (entity is ITarget target)
                    {
                        target.Team = 1;
                    }

                    _world.AddEntity(entity);
                    entity.OnDeath += OnEntityDeath;
                }
            }
        }

        private void OnEntityDeath(GameEntity entity)
        {
            if (entity == null) return;

            entity.OnDeath -= OnEntityDeath;
            _world.RemoveEntity(entity);
            _resources.Despawn(entity.gameObject);
        }

        private Vector3 GetSpawnPosition()
        {
            var center = _spawnCenter ? _spawnCenter.position : Vector3.zero;
            var direction = Random.insideUnitCircle.normalized;
            return center + new Vector3(direction.x, 0f, direction.y) * _spawnRadius;
        }
    }
}

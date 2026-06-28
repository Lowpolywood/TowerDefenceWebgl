using System.Collections.Generic;
using UnityEngine;
using Configs;

namespace Gameplay.Systems
{
    public class CombatSystem
    {
        private readonly GameWorld _world;
        private readonly GameResources _resources;
        private readonly Building _centralTower;
        private readonly float _projectileHitRadius;

        private readonly List<ITarget> _targetsBuffer = new(128);
        private readonly List<ProjecticleData> _projectiles = new(512);
        private readonly List<EffectData> _activeZones = new(64);

        public CombatSystem(GameWorld world, GameResources resources, Building centralTower, float projectileHitRadius)
        {
            _world = world;
            _resources = resources;
            _centralTower = centralTower;
            _projectileHitRadius = projectileHitRadius;
        }

        public void Execute(float dt)
        {
            ProcessTargeting();
            ProcessWeapons(dt);
        }

        public void FixedExecute(float fixedDt)
        {
            ProcessProjectiles(fixedDt);
            ProcessActiveZones(fixedDt);
        }

        private void ProcessTargeting()
        {
            for (int i = _world.Weapons.Count - 1; i >= 0; i--)
            {
                var weapon = _world.Weapons[i];
                if (weapon == null || !weapon.IsAlive) continue;

                float attackRange = weapon.Weapon != null ? weapon.Weapon.AttackRange : 1f;

                if (weapon is not IMovable)
                {
                    weapon.Target = FindClosestTarget(weapon.Transform.position, attackRange, weapon.Team);
                    continue;
                }

                var immediateEnemy = FindClosestTarget(weapon.Transform.position, attackRange, weapon.Team);
                if (immediateEnemy != null)
                {
                    weapon.Target = immediateEnemy;
                    continue;
                }

                if (weapon.Target == null || !weapon.Target.IsAlive)
                {
                    weapon.Target = FindClosestTarget(weapon.Transform.position, float.MaxValue, weapon.Team);

                    if (weapon.Target == null && weapon.Team == 1 && _centralTower != null && _centralTower.IsAlive)
                    {
                        weapon.Target = _centralTower;
                    }
                }
            }
        }

        private void ProcessWeapons(float dt)
        {
            for (int i = _world.Weapons.Count - 1; i >= 0; i--)
            {
                var weapon = _world.Weapons[i];
                if (weapon == null || !weapon.IsAlive) continue;

                var config = weapon.Weapon as WeaponConfig;
                if (config == null) continue;

                weapon.Cooldown -= dt;
                if (weapon.Cooldown > 0f) continue;

                if (weapon.Target == null || !weapon.Target.IsAlive) continue;

                Vector3 posA = weapon.Transform.position;
                Vector3 posB = weapon.Target.Transform.position;

                posA.y = 0f;
                posB.y = 0f;

                if (Vector3.Distance(posA, posB) > config.AttackRange) continue;

                weapon.Cooldown = 1f / Mathf.Max(0.01f, config.Rate);
                Vector3 origin = weapon.ShootPoint != null ? weapon.ShootPoint.position : weapon.Transform.position;

                _targetsBuffer.Clear();
                if (config.MaxTargetsPerShot <= 1)
                {
                    _targetsBuffer.Add(weapon.Target);
                }
                else
                {
                    GetTargetsInRangeNonAlloc(origin, config.AttackRange, weapon.Team, _targetsBuffer);

                    if (_targetsBuffer.Count > config.MaxTargetsPerShot)
                        _targetsBuffer.RemoveRange(config.MaxTargetsPerShot, _targetsBuffer.Count - config.MaxTargetsPerShot);
                }

                if (_targetsBuffer.Count == 0) continue;

                if (!config.IsProjectile)
                {
                    for (int j = 0; j < _targetsBuffer.Count; j++)
                        EvaluateEffect(config.ImpactEffect, _targetsBuffer[j].Transform.position, _targetsBuffer[j], weapon.Team);
                }
                else
                {
                    int projectileSettingsCount = config.Projectiles.Length;
                    if (projectileSettingsCount == 0) continue;

                    for (int j = 0; j < _targetsBuffer.Count; j++)
                    {
                        var settings = config.Projectiles[j % projectileSettingsCount];
                        SpawnProjectile(weapon.ShootPoint, settings, config.ImpactEffect, _targetsBuffer[j], weapon.Team);
                    }
                }
            }
        }

        private void ProcessProjectiles(float dt)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                var pr = _projectiles[i];

                if (!pr.instance)
                {
                    _projectiles.RemoveAt(i);
                    continue;
                }

                float curveModifier = 1f;

                if (pr.settings.speedOverPath != null)
                {
                    try
                    {
                        if (pr.settings.speedOverPath.length > 0)
                        {
                            float currentT = Mathf.Clamp01(pr.time / Mathf.Max(0.01f, pr.totalDistance / Mathf.Max(0.1f, pr.settings.speed)));
                            curveModifier = pr.settings.speedOverPath.Evaluate(currentT);
                        }
                    }
                    catch
                    {
                        curveModifier = 1f;
                    }
                }

                float currentSpeed = pr.settings.speed * curveModifier;
                pr.time += dt;

                // Безопасная проверка цели
                if (pr.target != null && pr.target.IsAlive && pr.target.Transform)
                {
                    pr.lastTargetPos = pr.target.Transform.position;
                }

                float currentSpeedClamped = Mathf.Max(0.1f, currentSpeed);
                float totalEstimatedTime = Mathf.Max(0.01f, pr.totalDistance / currentSpeedClamped);
                float t = Mathf.Clamp01(pr.time / totalEstimatedTime);

                Vector3 baselinePos = Vector3.Lerp(pr.origin, pr.lastTargetPos, t);
                float heightOffset = Mathf.Sin(t * Mathf.PI) * pr.settings.parabolaHeight;
                Vector3 finalPos = baselinePos + Vector3.up * heightOffset;

                // Безопасное обновление позиции (еще раз жестко проверяем объект)
                if (pr.instance)
                {
                    Transform prTransform = pr.instance.transform;
                    Vector3 moveDirection = (finalPos - prTransform.position).normalized;
                    if (moveDirection.sqrMagnitude > 0.001f) prTransform.forward = moveDirection;
                    prTransform.position = finalPos;
                }
                else
                {
                    _projectiles.RemoveAt(i);
                    continue;
                }

                _projectiles[i] = pr;

                bool hitRegistered = false;
                if (pr.target != null && pr.target.IsAlive && pr.target.Transform)
                {
                    if (Vector3.SqrMagnitude(finalPos - pr.target.Transform.position) <= _projectileHitRadius * _projectileHitRadius)
                        hitRegistered = true;
                }
                if (!hitRegistered && t >= 1f) hitRegistered = true;

                if (hitRegistered)
                {
                    EvaluateEffect(pr.impactEffect, finalPos, pr.target, pr.team);

                    // На всякий случай проверяем перед возвратом в пул
                    if (pr.instance)
                    {
                        _resources.Despawn(pr.instance, pr.poolName);
                    }

                    _projectiles.RemoveAt(i);
                }
            }
        }

        private void ProcessActiveZones(float dt)
        {
            for (int i = _activeZones.Count - 1; i >= 0; i--)
            {
                var zone = _activeZones[i];
                zone.timeRemaining -= dt;
                zone.nextTickTime -= dt;

                if (zone.nextTickTime <= 0f)
                {
                    zone.nextTickTime = zone.tickRate;

                    int enemyTeam = zone.team == 0 ? 1 : 0;
                    if (_world.Entities.TryGetValue(enemyTeam, out var enemyList))
                    {
                        float radiusSqr = zone.radius * zone.radius;
                        for (int j = 0; j < enemyList.Count; j++)
                        {
                            var enemy = enemyList[j];
                            if (enemy == null || !enemy.IsAlive) continue;

                            if (Vector3.SqrMagnitude(enemy.transform.position - zone.position) <= radiusSqr)
                            {
                                enemy.Health = Mathf.Max(0, enemy.Health - zone.damagePerTick);
                            }
                        }
                    }
                }

                if (zone.timeRemaining <= 0f)
                {
                    _activeZones.RemoveAt(i);
                }
                else
                {
                    _activeZones[i] = zone;
                }
            }
        }

        public ITarget FindClosestTarget(Vector3 origin, float range, int myTeam)
        {
            ITarget closest = null;
            float closestDistSqr = range * range;
            Vector3 originXZ = new Vector3(origin.x, 0f, origin.z);

            foreach (var pair in _world.Entities)
            {
                if (pair.Key == myTeam) continue;

                var enemyList = pair.Value;
                for (int i = 0; i < enemyList.Count; i++)
                {
                    var entity = enemyList[i];
                    if (entity == null || !entity.IsAlive) continue;

                    if (entity is ITarget target)
                    {
                        Vector3 targetXZ = entity.transform.position;
                        targetXZ.y = 0f;

                        float distSqr = (targetXZ - originXZ).sqrMagnitude;
                        if (distSqr <= closestDistSqr)
                        {
                            closest = target;
                            closestDistSqr = distSqr;
                        }
                    }
                }
            }
            return closest;
        }

        public void GetTargetsInRangeNonAlloc(Vector3 origin, float range, int myTeam, List<ITarget> resultsBuffer)
        {
            resultsBuffer.Clear();
            float rangeSqr = range * range;
            Vector3 originXZ = new Vector3(origin.x, 0f, origin.z);

            foreach (var pair in _world.Entities)
            {
                if (pair.Key == myTeam) continue;

                var enemyList = pair.Value;
                for (int i = 0; i < enemyList.Count; i++)
                {
                    var entity = enemyList[i];
                    if (entity == null || !entity.IsAlive) continue;

                    if (entity is ITarget target)
                    {
                        Vector3 targetXZ = entity.transform.position;
                        targetXZ.y = 0f;

                        if ((targetXZ - originXZ).sqrMagnitude <= rangeSqr)
                        {
                            resultsBuffer.Add(target);
                        }
                    }
                }
            }
        }

        private void SpawnProjectile(Transform startPoint, WeaponConfig.ProjectileSettings settings, EffectConfig impactEffect, ITarget target, int launcherTeam)
        {
            if (!settings.projecticle || target == null) return;

            var origin = startPoint ? startPoint.position : _centralTower.Transform.position;
            Vector3 targetPos = target.Transform.position;

            float dist = Vector3.Distance(origin, targetPos);
            Vector3 direction = (targetPos - origin).normalized;

            string poolName = settings.projecticle.name;
            var instance = _resources.SpawnInPool(poolName, origin, Quaternion.LookRotation(direction));

            if (instance != null)
            {
                _projectiles.Add(new ProjecticleData
                {
                    instance = instance,
                    poolName = poolName,
                    origin = origin,
                    target = target,
                    lastTargetPos = targetPos,
                    settings = settings,
                    impactEffect = impactEffect,
                    time = 0f,
                    team = launcherTeam,
                    totalDistance = dist
                });
            }
        }

        private void EvaluateEffect(EffectConfig effect, Vector3 position, ITarget primaryTarget, int shooterTeam)
        {
            if (effect == null) return;

            switch (effect)
            {
                case DirectEffectConfig directDamage:
                    if (primaryTarget is GameEntity entity && entity.IsAlive)
                    {
                        entity.Health = Mathf.Max(0, entity.Health - directDamage.Damage);
                    }
                    break;

                case SplashEffectConfig zoneEffect:
                    _activeZones.Add(new EffectData
                    {
                        position = position,
                        radius = zoneEffect.Radius,
                        duration = zoneEffect.Duration,
                        tickRate = zoneEffect.TickRate,
                        damagePerTick = zoneEffect.DamagePerTick,
                        team = shooterTeam,
                        timeRemaining = zoneEffect.Duration,
                        nextTickTime = 0f
                    });
                    break;
            }
        }
    }
}

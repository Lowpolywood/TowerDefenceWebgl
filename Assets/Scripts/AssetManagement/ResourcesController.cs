using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using AssetManagement.Providers;
using Configs;

#if ZENJECT
using Zenject;
#endif

namespace AssetManagement 
{
    /// <summary>
    /// Sample base resources management with addressable/legacy resources providers
    /// </summary>
    public class ResourcesController : MonoBehaviour
    {
        private Dictionary<Type, Dictionary<string, IAssetProvider>> providers = new();
        private Dictionary<string, ISceneHandle> sceneHandles = new();
        private Dictionary<string, ObjectPool<GameObject>> pools = new();

        private IAssetProvider configProvider;

        private SynchronizationContext context;
        private Config[] configs;

        public bool DebugProviders = false;

        public bool IsInitializing { get; private set; }
        public bool IsInitialized { get; private set; }
        public Action<bool> OnInitialize { get; private set; }

#if ZENJECT
        [Inject] DiContainer di;
#endif

        public IAssetProvider GetProvider<T>(string key)
        {
            AddProvider<T>(key);
            return GetProviders<T>()[key];
        }

        public Dictionary<string, IAssetProvider> GetProviders<T>()
        {
            var type = typeof(T);
            if (!providers.ContainsKey(type))
                providers.Add(type, new Dictionary<string, IAssetProvider>());

            return providers[type];
        }

        public void AddProvider<T>(params string[] key)
        {
            var type = typeof(T);
            foreach (var k in key)
            {
                if (string.IsNullOrEmpty(k))
                {
                    Debug.LogError("Provider has no key!");
                    return;
                }

                if (!providers.ContainsKey(type))
                    providers.Add(type, new Dictionary<string, IAssetProvider>());

                if (!providers[type].ContainsKey(k))
                    providers[type].Add(k, new AddressablesProvider(k));
            }
        }

        public void AddSceneProvider(params string[] key)
        {
            AddProvider<ISceneHandle>(key);
        }

        public bool IsLoaded<T>(string key) => GetProvider<T>(key).IsDone;
        public T Get<T>(string key) where T : class => GetProvider<T>(key).Get<T>();

        public T GetConfig<T>(string key = null) where T : Config
        {
            var list = GetConfigs<T>();
            return string.IsNullOrEmpty(key) ? list.FirstOrDefault() : list.FirstOrDefault(x => x.name == key);
        }

        public T[] GetConfigs<T>() => configs == null ? Array.Empty<T>() : configs.OfType<T>().ToArray();

        public async Task<bool> Initialize()
        {
            if (IsInitialized) return true;

            configProvider = new AddressablesProvider("config");
            configs = await configProvider.LoadAll<Config>();

            IsInitializing = false;
            IsInitialized = true;
            OnInitialize?.Invoke(true);

            return IsInitialized;
        }

        public void LoadAll<T>(Action<bool> result) where T : class
        {
            LoadAllAsync<T>().ContinueWith((task) =>
            {
                context.Post(_ => result?.Invoke(task.Result), null);
            });
        }

        public void Load<T>(Action<bool> result, params string[] key) where T : class
        {
            LoadAsync<T>(key).ContinueWith((task) =>
            {
                context.Post(_ => result?.Invoke(task.Result), null);
            });
        }

        public async Task<bool> LoadAllAsync<T>() where T : class
        {
            var typeProviders = GetProviders<T>();
            if (typeProviders.Count == 0) return true;
            return await LoadProviderAsync<T>(typeProviders.Keys.ToArray());
        }

        public async Task<bool> LoadAsync<T>(params string[] key) where T : class
        {
            return await LoadProviderAsync<T>(key);
        }

        private async Task<bool> LoadProviderAsync<T>(string[] key) where T : class
        {
            if(!IsInitialized)
            {
                Debug.LogError("Not initialized yet!");
                return false;
            }

            var tasks = key.Select(k =>
            {
                AddProvider<T>(k);
                return providers[typeof(T)][k].Load<T>();
            }).ToList();

            var results = await Task.WhenAll(tasks);
            return results.All(r => r != null);
        }

        public async void LoadScene(LoadSceneMode mode, bool activateOnLoad, Action<bool> onComplete, params string[] key)
        {
            try
            {
                bool result = await LoadSceneInternalAsync(key, mode, activateOnLoad);
                onComplete?.Invoke(result);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                onComplete?.Invoke(false);
            }
        }

        public void LoadScene(Action<bool> onComplete, params string[] key)
        {
            LoadScene(LoadSceneMode.Single, true, onComplete, key);
        }

        public async Task<bool> LoadSceneAsync(params string[] key)
        {
            return await LoadSceneInternalAsync(key, LoadSceneMode.Single, true);
        }

        public async Task<bool> LoadSceneAsync(LoadSceneMode mode, params string[] key)
        {
            return await LoadSceneInternalAsync(key, mode, true);
        }

        private async Task<bool> LoadSceneInternalAsync(string[] keys, LoadSceneMode mode, bool activateOnLoad)
        {
            if (!IsInitialized) 
            {
                Debug.LogError("Not initialized yet!");
                return false;
            }

            var success = true;
            foreach (var k in keys)
            {
                AddSceneProvider(k);

                var provider = providers[typeof(ISceneHandle)][k];

                var handle = await provider.LoadScene(mode, activateOnLoad);
                if (handle != null) sceneHandles[k] = handle;
                else success = false;
            }
            return success;
        }

        public void ActivateScene(string key, Action onComplete = null)
        {
            if (sceneHandles.TryGetValue(key, out var handle)) handle.Activate(onComplete);
            else Debug.LogError($"Scene handle for {key} not found!");
        }

        public void Unload<T>(params string[] key)
        {
            var type = typeof(T);
            if (!providers.ContainsKey(type)) return;

            foreach (var k in key)
            {
                if (providers[type].TryGetValue(k, out var p)) p.Unload();
            }
        }

        public void UnloadScene(params string[] key)
        {
            foreach (var k in key)
            {
                var type = typeof(ISceneHandle);
                if (providers.TryGetValue(type, out var dict) && dict.TryGetValue(k, out var p))
                {
                    p.Unload();
                    sceneHandles.Remove(k);
                }
            }
        }

        public GameObject Spawn(string key, Vector3 position = default, Quaternion rotation = default, Transform parent = null, float lifeTime = -1f)
        {
            var resource = Get<GameObject>(key);
            if (resource == null)
            {
                Debug.LogError($"Resource {key} not loaded! Load resource before instantiate.");
                return null;
            }

            var obj = Instantiate(resource, position, rotation, parent);
            obj.name = obj.name.Replace("(Clone)", "");

            if (lifeTime > 0) StartLifeTimeTimer(obj, lifeTime);
#if ZENJECT
            di.InjectGameObject(obj);
#endif
            return obj;
        }

        public GameObject SpawnInPool(string key, Vector3 position = default, Quaternion rotation = default, Transform parent = null, float lifeTime = -1f)
        {
            if (!pools.ContainsKey(key))
            {
                Debug.LogError($"Pool {key} not exist!");
                return null;
            }

            var obj = pools[key].Get();
            if (obj == null)
            {
                Debug.LogError($"Pool {key} returned destroyed instance. Re-instantiating.");
                var resource = Get<GameObject>(key);
                if (resource == null)
                {
                    Debug.LogError($"Resource {key} not loaded! Load resource before instantiate.");
                    return null;
                }
                obj = Instantiate(resource);
            }

            if (parent != null && parent) obj.transform.SetParent(parent);

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            if (lifeTime > 0) StartLifeTimeTimer(obj, lifeTime, key);
            return obj;
        }

        public void Despawn(GameObject obj, string pool = null)
        {
            if (string.IsNullOrEmpty(pool) || !pools.ContainsKey(pool)) Destroy(obj);
            else pools[pool].Release(obj);
        }

        public async Task AddPool(string key, int defaultCapacity, int maxSize)
        {
            var provider = GetProvider<GameObject>(key);
            var resource = Get<GameObject>(key);

            if (resource == null)
            {
                Debug.LogError($"Resource {key} not loaded!");
                return;
            }

            if (pools.ContainsKey(key)) return;

            if (maxSize <= 0) 
            {
                maxSize = 1;
                Debug.LogWarning("Max size fallback to 1. Configure configs!");
            }

            var pool = new ObjectPool<GameObject>(() =>
            {
                var obj = Instantiate(resource);
#if ZENJECT
                di.InjectGameObject(obj);
#endif
                return obj;
            },
            null,
            (release) =>
            {
                release.SetActive(false);

                if (this && transform) release.transform.SetParent(transform);
            },
            (destroy) => Destroy(destroy),
            false, 0, maxSize);

            pools.Add(key, pool);

            for (int i = 0; i < defaultCapacity; i++)
            {
                var obj = await provider.Instantiate();
                pool.Release(obj);
            }
        }

        private async void StartLifeTimeTimer(GameObject obj, float duration, string poolKey = null)
        {
            if (obj == null) return;
            await Task.Delay((int)(duration * 1000));
            if (obj != null) Despawn(obj, poolKey);
        }
    }

    public enum LoadSceneMode { Single = 0, Additive = 1 }

    public interface ISceneHandle
    {
        void Activate(Action onComplete = null);
    }

    public interface IAssetProvider
    {
        string Key { get; }
        float Progress { get; }
        bool IsDone { get; }

        Task<T> Load<T>(bool activateOnLoad = true) where T : class;
        Task<T[]> LoadAll<T>() where T : class;
        Task<ISceneHandle> LoadScene(LoadSceneMode mode, bool activateOnLoad = true);

        T Get<T>() where T : class;
        Task<GameObject> Instantiate(Transform parent = null);
        void Unload();

        Task<bool> Download();
        Task<bool> ClearCache();
    }
}

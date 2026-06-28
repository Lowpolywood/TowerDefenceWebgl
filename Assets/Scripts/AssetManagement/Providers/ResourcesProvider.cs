using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AssetManagement.Providers
{
    [Serializable]
    public class ResourcesProvider : IAssetProvider
    {
        [field: SerializeField] public string Key { get; private set; }
        [field: SerializeField] public float Progress { get; private set; }
        [field: SerializeField] public bool IsDone { get; private set; }

        private object _loadedAsset;
        private bool _isScene;

        private class ResourcesSceneHandle : ISceneHandle
        {
            private AsyncOperation _op;
            public ResourcesSceneHandle(AsyncOperation op) => _op = op;

            public void Activate(Action onComplete = null)
            {
                if (_op == null)
                {
                    onComplete?.Invoke();
                    return;
                }
                _op.allowSceneActivation = true;
                _op.completed += _ => onComplete?.Invoke();
            }
        }

        public ResourcesProvider(string key) => Key = key;

        public T Get<T>() where T : class => _loadedAsset as T;

        public async Task<T> Load<T>(bool activateOnLoad = true) where T : class
        {
            if (IsDone && _loadedAsset != null)
                return _loadedAsset as T;

            if (!typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
            {
                Debug.LogError($"Resources.Load cannot load type {typeof(T).Name} because it's not a UnityEngine.Object");
                return null;
            }

            Debug.Log($"Resources load: {Key} (Type: {typeof(T).Name})");
            IsDone = false;
            _isScene = false;

            ResourceRequest request = LoadAsyncInternal<T>(Key);

            while (!request.isDone)
            {
                Progress = request.progress;
                await Task.Yield();
            }

            _loadedAsset = request.asset;
            IsDone = true;
            Progress = 1f;

            if (_loadedAsset == null)
                Debug.LogError($"ResourcesProvider: Asset with key '{Key}' not found in Resources!");

            return _loadedAsset as T;
        }

        private ResourceRequest LoadAsyncInternal<T>(string path) where T : class
        {
            return Resources.LoadAsync(path, typeof(T));
        }

        public async Task<T[]> LoadAll<T>() where T : class
        {
            Debug.Log($"Resources load all from folder: {Key} (Type: {typeof(T).Name})");

            if (!typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
            {
                return Array.Empty<T>();
            }

            IsDone = false;
            var assets = Resources.LoadAll(Key, typeof(T)).Cast<T>().ToArray();

            IsDone = true;
            Progress = 1f;

            return await Task.FromResult(assets);
        }

        public async Task<ISceneHandle> LoadScene(LoadSceneMode mode, bool activateOnLoad = true)
        {
            Debug.Log($"Resources load scene: {Key}");
            IsDone = false;
            _isScene = true;
            var unityMode = (UnityEngine.SceneManagement.LoadSceneMode)mode;
            AsyncOperation op = SceneManager.LoadSceneAsync(Key, unityMode);
            op.allowSceneActivation = activateOnLoad;

            while (op.progress < 0.9f)
            {
                Progress = op.progress;
                await Task.Yield();
            }

            IsDone = true;
            Progress = 1f;
            return new ResourcesSceneHandle(op);
        }

        public async Task<GameObject> Instantiate(Transform parent = null)
        {
            if (_loadedAsset == null) await Load<GameObject>();
            if (_loadedAsset is GameObject prefab) return UnityEngine.Object.Instantiate(prefab, parent);
            return null;
        }

        public void Unload()
        {
            if (_isScene)
            {
                SceneManager.UnloadSceneAsync(Key);
            }
            else if (_loadedAsset != null && !(_loadedAsset is GameObject))
            {
                Resources.UnloadAsset(_loadedAsset as UnityEngine.Object);
            }
            _loadedAsset = null;
            IsDone = false;
            Progress = 0;
        }

        public Task<bool> Download() { Progress = 1f; IsDone = true; return Task.FromResult(true); }
        public Task<bool> ClearCache() => Task.FromResult(true);
    }
}
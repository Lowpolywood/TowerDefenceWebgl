using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Exceptions;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace AssetManagement.Providers 
{
    [Serializable]
    public class AddressablesProvider : IAssetProvider
    {
        [field: SerializeField] public string Key { get; private set; }
        [field: SerializeField] public float Progress { get; private set; }
        [field: SerializeField] public bool IsDone { get; private set; }
        [field: SerializeField] public bool IsDownloaded { get; private set; }
        [field: SerializeField] public long DownloadSize { get; private set; }
        [field: SerializeField] public long Downloaded { get; private set; }

        private List<AsyncOperationHandle> _allAssetsHandles = new();
        private AsyncOperationHandle _handle;
        private bool _isScene;

        private class AddressableSceneHandle : ISceneHandle
        {
            private SceneInstance _instance;
            public AddressableSceneHandle(SceneInstance instance) => _instance = instance;

            public void Activate(Action onComplete = null)
            {
                var op = _instance.ActivateAsync();
                op.completed += _ => onComplete?.Invoke();
            }
        }

        public AddressablesProvider(string key)
        {
            Key = key;

            Addressables.WebRequestOverride += DebugUrl;
        }

        private void DebugUrl(UnityWebRequest request) 
        {
            Debug.Log($"Addressable {Key} URL: {request.url}");
        }

        private string GetDownloadError(AsyncOperationHandle fromHandle)
        {
            if (fromHandle.IsValid() && fromHandle.Status != AsyncOperationStatus.Failed)
                return null;

            Exception e = fromHandle.OperationException;
            while (e != null)
            {
                if (e is RemoteProviderException remoteException)
                    return remoteException.WebRequestResult.Error;
                e = e.InnerException;
            }

            return fromHandle.OperationException?.Message ?? "Unknown Addressables Error";
        }

        public T Get<T>() where T : class
        {
            try
            {
                return _handle.Result as T;
            }
            catch (Exception e)
            {
                throw new Exception($"Addressable: {Key} Error: {e}");
            }
        }

        public async Task<bool> Download()
        {
            Debug.Log($"Addressables download: {Key}");

            if (IsDownloaded)
            {
                Progress = 1;
                IsDone = true;
                return true;
            }

            IsDone = false;
            var getDownloadSize = Addressables.GetDownloadSizeAsync(Key);
            await getDownloadSize.Task;

            DownloadSize = getDownloadSize.Result;

            if (DownloadSize > 0)
            {
                var download = Addressables.DownloadDependenciesAsync(Key, Addressables.MergeMode.Union);

                while (!download.IsDone)
                {
                    if (!Application.isPlaying) break;
                    Progress = download.GetDownloadStatus().Percent;
                    Downloaded = download.GetDownloadStatus().DownloadedBytes;
                    await Task.Yield();
                }

                string error = GetDownloadError(download);
                if (!string.IsNullOrEmpty(error))
                    Debug.LogError($"Addressables {Key} download error: {error}");

                IsDone = true;
                bool success = download.Status == AsyncOperationStatus.Succeeded;
                Addressables.Release(download);

                if (success)
                {
                    IsDownloaded = true;
                    Progress = 1;
                    return true;
                }
                return false;
            }

            Progress = 1;
            IsDownloaded = true;
            IsDone = true;
            return true;
        }

        public async Task<T> Load<T>(bool activateOnLoad = true) where T : class
        {
            if (_handle.IsValid() && _handle.IsDone && _handle.Status == AsyncOperationStatus.Succeeded)
                return _handle.Result as T;

            Debug.Log($"Addressables load: {Key} (Type: {typeof(T).Name})");
            IsDone = false;
            _isScene = false;

            _handle = Addressables.LoadAssetAsync<T>(Key);

            await WaitForHandle(_handle);

            string error = GetDownloadError(_handle);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"Addressable {Key} load error: {error}");
                IsDone = true;
                return default;
            }

            IsDone = true;
            return _handle.Status == AsyncOperationStatus.Succeeded ? _handle.Result as T : default;
        }

        public async Task<ISceneHandle> LoadScene(LoadSceneMode mode, bool activateOnLoad = true)
        {
            Debug.Log($"Addressables load scene: {Key}");
            IsDone = false;
            _isScene = true;

            var unityMode = (UnityEngine.SceneManagement.LoadSceneMode)mode;
            _handle = Addressables.LoadSceneAsync(Key, unityMode, activateOnLoad);

            if (activateOnLoad)
            {
                await WaitForHandle(_handle);
            }
            else
            {
                await WaitForSceneLoadReady(_handle);
            }

            var isSceneReadyForActivation =
                _handle.Result is SceneInstance loadedScene &&
                loadedScene.Scene.IsValid() &&
                loadedScene.Scene.isLoaded;

            if (_handle.Status == AsyncOperationStatus.Succeeded || (!activateOnLoad && isSceneReadyForActivation))
            {
                IsDone = true;
                var sceneInstance = (SceneInstance)_handle.Result;
                return new AddressableSceneHandle(sceneInstance);
            }

            Debug.LogError($"Addressable Scene {Key} load error: {GetDownloadError(_handle)}");

            IsDone = true;
            return null;
        }

        public async Task<T[]> LoadAll<T>() where T : class
        {
            IsDone = false;
            var multipleHandle = Addressables.LoadAssetsAsync<T>(Key, null);
            await WaitForHandle(multipleHandle);

            if (multipleHandle.Status == AsyncOperationStatus.Succeeded)
            {
                _allAssetsHandles.Add(multipleHandle);
                IsDone = true;
                return multipleHandle.Result.ToArray();
            }

            Addressables.Release(multipleHandle);
            IsDone = true;
            return Array.Empty<T>();
        }

        public async Task<GameObject> Instantiate(Transform parent = null)
        {
            var op = Addressables.InstantiateAsync(Key, parent);
            await op.Task;
            return op.Result;
        }

        public async Task<bool> ClearCache()
        {
            Debug.Log($"Addressables clear cache: {Key}");
            var clearCache = Addressables.ClearDependencyCacheAsync(Key, false);
            await clearCache.Task;
            bool result = clearCache.Status == AsyncOperationStatus.Succeeded;
            Addressables.Release(clearCache);
            return result;
        }

        public void Unload()
        {
            if (!_handle.IsValid()) return;

            Debug.Log($"Addressables unload: {Key}");
            if (_isScene)
                Addressables.UnloadSceneAsync(_handle, UnloadSceneOptions.None);
            else
                Addressables.Release(_handle);

            foreach (var h in _allAssetsHandles)
            {
                if (h.IsValid()) Addressables.Release(h);
            }
            _allAssetsHandles.Clear();
        }

        private async Task WaitForHandle(AsyncOperationHandle op)
        {
            while (!op.IsDone)
            {
                if (!Application.isPlaying) break;
                Progress = op.PercentComplete;
                await Task.Yield();
            }
            Progress = 1f;
        }

        private async Task WaitForSceneLoadReady(AsyncOperationHandle op)
        {
            while (true)
            {
                if (!Application.isPlaying) break;

                Progress = op.PercentComplete;

                if (!op.IsValid())
                    break;

                if (op.IsDone)
                    break;

                if (op.Result is SceneInstance sceneInstance && sceneInstance.Scene.IsValid() && sceneInstance.Scene.isLoaded)
                    break;

                await Task.Yield();
            }

            Progress = 1f;
        }
    }
}

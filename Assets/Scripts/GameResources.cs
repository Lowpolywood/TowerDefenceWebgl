using AssetManagement;
using Configs;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Some shared context with resource management
/// </summary>
public class GameResources : ResourcesController
{
    [field: SerializeField] public LevelConfig Level { get; private set; }

    const string BOOTSTRAPSCENE = "Bootstrap";
    const string GAMESCENE = "Game";

    public async void LoadGame()
    {
        bool isLoaded = await LoadGameAsync();
    }

    /// <summary>
    /// Request providers, warm up pools, unload
    /// </summary>
    /// <returns></returns>
    private async Task<bool> LoadGameAsync()
    {
        try
        {
            await Initialize();

            Level = GetConfig<LevelConfig>("TestLevel");

            var buildings = GetConfigs<BuildingConfig>();
            var characters = GetConfigs<CharacterConfig>();
            var weapons = GetConfigs<WeaponConfig>();
            var projecticles = GetConfigs<ProjecticleConfig>();

            foreach (var c in buildings)
                AddProvider<GameObject>(c.name);

            foreach (var c in characters)
                AddProvider<GameObject>(c.name);

            foreach (var c in projecticles)
                AddProvider<GameObject>(c.name);

            await LoadAllAsync<GameObject>();

            foreach (var c in projecticles)
            {
                await AddPool(c.name, c.InitPoolSize, c.MaxPoolSize);
                Unload<GameObject>(c.name);
            }

            AddSceneProvider(GAMESCENE);

            await LoadSceneAsync(GAMESCENE);
            UnloadScene(BOOTSTRAPSCENE);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }
}
using UnityEngine;
using Zenject;
using Gameplay;

public class LevelInstaller : MonoInstaller
{
    [SerializeField] LevelController levelController;

    public override void InstallBindings()
    {
        Container.Bind<LevelController>().FromInstance(levelController).AsSingle().NonLazy();
    }
}
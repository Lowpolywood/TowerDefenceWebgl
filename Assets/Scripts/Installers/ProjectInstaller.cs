using UnityEngine;
using Zenject;

public class ProjectInstaller : MonoInstaller
{
    [SerializeField] GameResources resources;

    public override void InstallBindings()
    {
        Container.Bind<GameResources>().FromInstance(resources).AsSingle();
    }
}
using System.Collections;
using UnityEngine;
using Zenject;

/// <summary>
/// Entry point, instantly open game UI, avoid asset database heavy load
/// start empty scene, async preload menu/game assets
/// </summary>
public class Bootstraper : MonoBehaviour
{
    [Inject] GameResources resources;

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        //Application.targetFrameRate = 60;

        // Other settings or bindings
#if UNITY_STANDALONE
        Screen.SetResolution(640, 380, false);
#endif

        // Inreal case preload menu only. During player is in menu game assets loading in background
        // reducing  time of first run
        resources.LoadGame();
    }
}

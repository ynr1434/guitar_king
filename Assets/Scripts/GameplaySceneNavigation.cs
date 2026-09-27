using UnityEngine;

public sealed class GameplaySceneNavigation : MonoBehaviour
{
    private void Awake()
    {
        if (FindFirstObjectByType<GameplayVisualController>() == null)
            gameObject.AddComponent<GameplayVisualController>();
        if (FindFirstObjectByType<PauseController>() == null)
            gameObject.AddComponent<PauseController>();
        if (FindFirstObjectByType<GameplayVFXController>() == null)
            gameObject.AddComponent<GameplayVFXController>();
    }
}

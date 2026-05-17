using UnityEngine;

/// <summary>
/// После загрузки сцены создаёт runtime-объекты для дополнительных систем:
/// DrillHud, DrillOverheatSystem, PauseMenu. Это позволяет включать новый функционал
/// без правки сцены — аналогично подходу <see cref="InstallationHudController"/>.
/// </summary>
static class SimulatorBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SpawnRuntimeSystems()
    {
        EnsureSingleton<DrillOverheatSystem>("DrillOverheatSystem");
        EnsureSingleton<DrillHud>("DrillHud");
        EnsureSingleton<PauseMenu>("PauseMenu");
    }

    static void EnsureSingleton<T>(string objectName) where T : MonoBehaviour
    {
#if UNITY_2023_1_OR_NEWER
        var existing = Object.FindAnyObjectByType<T>();
#else
        var existing = Object.FindObjectOfType<T>();
#endif
        if (existing != null)
            return;

        var go = new GameObject(objectName);
        Object.DontDestroyOnLoad(go);
        go.AddComponent<T>();
    }
}

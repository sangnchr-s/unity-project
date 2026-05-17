using UnityEngine;

/// <summary>
/// После загрузки сцены создаёт runtime-объекты для дополнительных игровых систем:
/// перегрева бура, динамики звука, маркеров глубины и тряски камеры.
/// Никаких UI-оверлеев — все системы тихие и интегрированы в существующую сцену
/// в духе <see cref="InstallationHudController"/> (создание из кода, без правок сцены).
/// </summary>
static class SimulatorBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SpawnRuntimeSystems()
    {
        EnsureSingleton<DrillOverheatSystem>("DrillOverheatSystem");
        EnsureSingleton<DrillAudioDynamics>("DrillAudioDynamics");
        EnsureSingleton<DepthMarkerTrail>("DepthMarkerTrail");
        EnsureSingleton<DrillingCameraShake>("DrillingCameraShake");
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

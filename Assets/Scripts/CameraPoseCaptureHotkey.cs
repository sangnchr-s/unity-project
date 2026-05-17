using UnityEngine;

/// <summary>
/// По нажатию клавиши P выводит текущую мировую позицию и поворот камеры.
/// Удобно для снятия ракурса перед настройкой автопереключения у тоннеля.
/// </summary>
[DisallowMultipleComponent]
public sealed class CameraPoseCaptureHotkey : MonoBehaviour
{
    [SerializeField] KeyCode captureKey = KeyCode.P;
    static bool _autoAttachAttempted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttachToMainCamera()
    {
        if (_autoAttachAttempted)
            return;
        _autoAttachAttempted = true;

        if (Camera.main == null)
        {
            Debug.LogWarning("[CameraPose] Main Camera не найдена. Назначьте тег MainCamera активной камере.");
            return;
        }

        if (Camera.main.GetComponent<CameraPoseCaptureHotkey>() == null)
            Camera.main.gameObject.AddComponent<CameraPoseCaptureHotkey>();
    }

    void Awake()
    {
        Debug.Log("[CameraPose] Захват ракурса активен. Нажмите P в окне Game.");
    }

    void Update()
    {
        if (!Input.GetKeyDown(captureKey))
            return;

        var pos = transform.position;
        var rot = transform.eulerAngles;

        Debug.Log(
            $"[CameraPose] position=({pos.x:F3}, {pos.y:F3}, {pos.z:F3}) rotation=({rot.x:F3}, {rot.y:F3}, {rot.z:F3})");
    }
}

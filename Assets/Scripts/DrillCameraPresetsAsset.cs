using UnityEngine;

/// <summary>
/// Хранит ракурсы камеры. Создайте: ПКМ в Project → Create → Simulator → Пресеты камеры буровой.
/// Назначьте ассет на Main Camera → Lecture Preset Camera Controller → Preset Source.
/// В Play Mode: Shift+1…8 записывают текущую позицию камеры в слот (изменения в ассете сохраняются — Ctrl+S).
/// </summary>
[CreateAssetMenu(menuName = "Simulator/Пресеты камеры буровой", fileName = "DrillCameraPresets")]
public sealed class DrillCameraPresetsAsset : ScriptableObject
{
    public LecturePresetCameraController.CameraPreset[] presets;

    void OnValidate()
    {
        EnsureLengthAndLabels();
    }

    void Reset()
    {
        EnsureLengthAndLabels();
    }

    void EnsureLengthAndLabels()
    {
        if (presets != null && presets.Length == 8)
            return;

        var labels = new[]
        {
            "Буровая голова", "Малый бур L01", "Малый бур L02", "Малый бур R01",
            "Малый бур R02", "Прожектор левый", "Прожектор правый", "Корпус и ходовая"
        };
        var next = new LecturePresetCameraController.CameraPreset[8];
        for (var i = 0; i < 8; i++)
        {
            if (presets != null && i < presets.Length)
                next[i] = presets[i];
            if (string.IsNullOrEmpty(next[i].label))
                next[i].label = labels[i];
        }
        presets = next;
    }
}

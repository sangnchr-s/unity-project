using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LecturePresetCameraController))]
public sealed class LecturePresetCameraControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "Захват ракурса в Play Mode:\n" +
            "• Удерживайте Shift и нажмите 1…8 (верхний ряд или NumPad).\n" +
            "• Слот 1 = буровая голова, … 8 = корпус (как кнопки в UI).\n" +
            "• Назначьте Preset Source (ассет), иначе после выхода из Play значения сбросятся.\n" +
            "• После записи: Ctrl+S (Cmd+S) — сохранить ассет на диске.",
            MessageType.Info);
        DrawDefaultInspector();
    }
}

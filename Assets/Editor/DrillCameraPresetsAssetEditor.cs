using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DrillCameraPresetsAsset))]
public sealed class DrillCameraPresetsAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox(
            "В Play Mode наведите камеру и нажмите Shift+1 … Shift+8 на клавиатуре (не NumPad), " +
            "либо кнопки ниже. После захвата сохраните проект (Ctrl+S / Cmd+S).",
            MessageType.Info);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Кнопки захвата доступны во время воспроизведения сцены.", MessageType.None);
            return;
        }

        var cam = Camera.main;
        if (cam == null)
        {
            EditorGUILayout.HelpBox("Main Camera не найдена.", MessageType.Warning);
            return;
        }

        var asset = (DrillCameraPresetsAsset)target;
        if (asset.presets == null || asset.presets.Length == 0)
            return;

        EditorGUILayout.LabelField("Захват текущей Main Camera", EditorStyles.boldLabel);
        for (var i = 0; i < asset.presets.Length; i++)
        {
            var label = string.IsNullOrEmpty(asset.presets[i].label) ? $"Слот {i}" : asset.presets[i].label;
            if (GUILayout.Button($"Записать в «{label}» (как Shift+{i + 1})"))
                CaptureFromCamera(asset, i, cam.transform);
        }
    }

    static void CaptureFromCamera(DrillCameraPresetsAsset asset, int index, Transform cam)
    {
        var p = asset.presets[index];
        p.position = cam.position;
        p.rotation = cam.eulerAngles;
        asset.presets[index] = p;
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Пресеты камеры] Записан слот {index} ({p.label}): pos={p.position}, rot={p.rotation}");
    }
}

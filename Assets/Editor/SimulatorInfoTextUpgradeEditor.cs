using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Центральный блок «Симулятор…» в сцене создавался со старым UI Text — он даёт размытие при Scale With Screen Size.
/// Один раз заменяет его на TextMeshProUGUI и отключает Pixel Perfect на Canvas.
/// </summary>
public static class SimulatorInfoTextUpgradeEditor
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/TextMesh Pro/Upgrade Info_Text to TMP (SampleScene)")]
    public static void UpgradeInfoTextInSampleScene()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var canvasGo = GameObject.Find("Canvas_SimulatorInfo");
        if (canvasGo == null)
        {
            EditorUtility.DisplayDialog("Info_Text → TMP", "Не найден Canvas_SimulatorInfo.", "OK");
            return;
        }

        var canvas = canvasGo.GetComponent<Canvas>();
        if (canvas != null)
        {
            Undo.RecordObject(canvas, "Canvas UI quality");
            canvas.pixelPerfect = false;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                                               | AdditionalCanvasShaderChannels.Normal
                                               | AdditionalCanvasShaderChannels.Tangent;
            EditorUtility.SetDirty(canvas);
        }

        var infoTf = canvasGo.transform.Find("Info_Text");
        if (infoTf == null)
        {
            EditorUtility.DisplayDialog("Info_Text → TMP", "Не найден дочерний объект Info_Text.", "OK");
            EditorSceneManager.SaveScene(scene);
            return;
        }

        var go = infoTf.gameObject;
        if (go.GetComponent<TextMeshProUGUI>() != null)
        {
            EditorUtility.DisplayDialog("Info_Text → TMP", "Info_Text уже использует TextMeshProUGUI.", "OK");
            EditorSceneManager.SaveScene(scene);
            return;
        }

        var legacy = go.GetComponent<Text>();
        string body = legacy != null ? legacy.text : string.Empty;
        Color col = legacy != null ? legacy.color : new Color(0.95f, 0.95f, 0.95f, 1f);
        if (legacy != null)
            Undo.DestroyObjectImmediate(legacy);

        var tmp = Undo.AddComponent<TextMeshProUGUI>(go);
        tmp.text = body;
        tmp.fontSize = 30f;
        tmp.color = col;
        tmp.alignment = TextAlignmentOptions.Midline;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = true;
        tmp.extraPadding = true;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;

        EditorUtility.SetDirty(go);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SimulatorInfoTextUpgrade] Info_Text переведён на TMP, Canvas: Pixel Perfect выключен.");
    }

    [MenuItem("Tools/TextMesh Pro/Upgrade Rules_Text to TMP (SampleScene)")]
    public static void UpgradeRulesTextInSampleScene()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var canvasGo = GameObject.Find("Canvas_SimulatorInfo");
        if (canvasGo == null)
        {
            EditorUtility.DisplayDialog("Rules_Text → TMP", "Не найден Canvas_SimulatorInfo.", "OK");
            return;
        }

        var rulesTf = canvasGo.transform.Find("RulesPopup_Panel/Rules_Text");
        if (rulesTf == null)
        {
            EditorUtility.DisplayDialog("Rules_Text → TMP", "Не найден RulesPopup_Panel/Rules_Text.", "OK");
            EditorSceneManager.SaveScene(scene);
            return;
        }

        var go = rulesTf.gameObject;
        if (go.GetComponent<TextMeshProUGUI>() != null)
        {
            EditorUtility.DisplayDialog("Rules_Text → TMP", "Rules_Text уже использует TextMeshProUGUI.", "OK");
            EditorSceneManager.SaveScene(scene);
            return;
        }

        var legacy = go.GetComponent<Text>();
        string body = legacy != null ? legacy.text : string.Empty;
        Color col = legacy != null ? legacy.color : new Color(0.95f, 0.95f, 0.95f, 1f);
        if (legacy != null)
            Undo.DestroyObjectImmediate(legacy);

        var tmp = Undo.AddComponent<TextMeshProUGUI>(go);
        tmp.text = body;
        tmp.fontSize = 22f;
        tmp.color = col;
        tmp.alignment = TextAlignmentOptions.Midline;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        tmp.extraPadding = true;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;

        EditorUtility.SetDirty(go);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SimulatorInfoTextUpgrade] Rules_Text переведён на TMP.");
    }
}

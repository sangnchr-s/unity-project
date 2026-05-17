using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Создаёт на сцене Canvas со справкой: фон (Image) под текстом, UI Text по центру с полями.
/// Повторный запуск удаляет предыдущий Canvas_SimulatorInfo.
/// </summary>
public static class SimulatorInfoCanvasBuilder
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string CanvasName = "Canvas_SimulatorInfo";

    const string InfoRu =
        "Симулятор подземной буровой установки\n\n" +
        "Назначение: отработка перемещения машины по подземным выработкам и процесса бурения. " +
        "Установка ведёт себя как самоходная машина: вы управляете перемещением в пространстве тоннеля, " +
        "подводите бур к забою и выполняете бурение.\n\n" +
        "Следующие этапы: сюда добавятся сведения о правилах работы с симулятором и организации безопасных операций.";

    [MenuItem("Tools/Create Simulator Info Canvas")]
    public static void CreateFromMenu()
    {
        BuildIntoSampleScene();
    }

    /// <summary>Точка входа для batchmode: Unity -executeMethod SimulatorInfoCanvasBuilder.BuildFromCommandLine</summary>
    public static void BuildFromCommandLine()
    {
        BuildIntoSampleScene();
        EditorApplication.Exit(0);
    }

    static void BuildIntoSampleScene()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var old = GameObject.Find(CanvasName);
        if (old != null)
            Object.DestroyImmediate(old);

        var canvasGO = new GameObject(CanvasName);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvas.pixelPerfect = false;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                                            | AdditionalCanvasShaderChannels.Normal
                                            | AdditionalCanvasShaderChannels.Tangent;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var canvasRt = canvasGO.GetComponent<RectTransform>();
        canvasRt.anchorMin = Vector2.zero;
        canvasRt.anchorMax = Vector2.one;
        canvasRt.sizeDelta = Vector2.zero;
        canvasRt.anchoredPosition = Vector2.zero;

        // Фон: первый по иерархии — рисуется под текстом
        var bgGO = new GameObject("Background_Image");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgRt = bgGO.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = new Vector2(48f, 48f);
        bgRt.offsetMax = new Vector2(-48f, -48f);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = BuiltinUiSprite();
        bgImg.type = Image.Type.Sliced;
        bgImg.color = new Color(0.07f, 0.09f, 0.12f, 0.93f);

        // Текст поверх фона
        var textGO = new GameObject("Info_Text");
        textGO.transform.SetParent(canvasGO.transform, false);
        var textRt = textGO.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(64f, 64f);
        textRt.offsetMax = new Vector2(-64f, -64f);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = InfoRu;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = 30f;
        tmp.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        tmp.alignment = TextAlignmentOptions.Midline;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = true;
        tmp.extraPadding = true;

        EnsureEventSystem();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SimulatorInfo] Canvas создан и сцена сохранена: " + ScenePath);
    }

    static Sprite BuiltinUiSprite()
    {
        var s = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (s != null)
            return s;
        var t = Texture2D.whiteTexture;
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }
}

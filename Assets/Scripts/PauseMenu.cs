using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Меню паузы (Esc): Resume / Restart / Master Volume / Mouse Sensitivity.
/// При открытии ставит Time.timeScale = 0 и показывает курсор; настройки сохраняются
/// в <see cref="GameSettings"/>.
/// Чужие скрипты (Movement, FlyCamera и т.д.) могут читать <see cref="IsPaused"/> чтобы
/// игнорировать ввод во время паузы.
/// </summary>
public sealed class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [SerializeField] KeyCode toggleKey = KeyCode.Escape;

    static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.55f);
    static readonly Color PanelColor = new Color(0.08f, 0.09f, 0.12f, 0.96f);
    static readonly Color BorderColor = new Color(0.95f, 0.55f, 0.18f, 0.9f);
    static readonly Color TextColor = new Color(0.96f, 0.97f, 1f, 1f);
    static readonly Color SubTextColor = new Color(0.78f, 0.82f, 0.9f, 1f);
    static readonly Color ButtonColor = new Color(0.18f, 0.4f, 0.78f, 1f);
    static readonly Color ButtonHoverColor = new Color(0.24f, 0.5f, 0.92f, 1f);
    static readonly Color RestartColor = new Color(0.78f, 0.32f, 0.28f, 1f);

    GameObject _canvasGo;
    GameObject _rootPanel;
    Slider _volumeSlider;
    Slider _mouseSlider;
    TMP_Text _volumeValueLabel;
    TMP_Text _mouseValueLabel;
    CursorLockMode _previousLockMode = CursorLockMode.None;
    bool _previousCursorVisible = true;
    float _previousTimeScale = 1f;

    void Start()
    {
        BuildUi();
        SetPaused(false);
    }

    void OnDestroy()
    {
        if (IsPaused)
        {
            Time.timeScale = _previousTimeScale;
            IsPaused = false;
        }
    }

    void Update()
    {
        if (SimulatorPracticePanel.IsTypingInPracticeInput)
            return;

        if (Input.GetKeyDown(toggleKey))
            SetPaused(!IsPaused);
    }

    void SetPaused(bool paused)
    {
        IsPaused = paused;
        if (_rootPanel != null)
            _rootPanel.SetActive(paused);

        if (paused)
        {
            _previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            _previousLockMode = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SyncSlidersFromSettings();
        }
        else
        {
            Time.timeScale = _previousTimeScale > 0f ? _previousTimeScale : 1f;
            Cursor.lockState = _previousLockMode;
            Cursor.visible = _previousCursorVisible;
            GameSettings.Save();
        }
    }

    void SyncSlidersFromSettings()
    {
        if (_volumeSlider != null)
            _volumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
        if (_mouseSlider != null)
            _mouseSlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
        RefreshValueLabels();
    }

    void RefreshValueLabels()
    {
        if (_volumeValueLabel != null)
            _volumeValueLabel.text = $"{Mathf.RoundToInt(GameSettings.MasterVolume * 100f)}%";
        if (_mouseValueLabel != null)
            _mouseValueLabel.text = GameSettings.MouseSensitivity.ToString("F2", CultureInfo.InvariantCulture);
    }

    void BuildUi()
    {
        _canvasGo = new GameObject("PauseMenu_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvasGo.transform.SetParent(transform, false);

        var canvas = _canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = _canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var canvasRt = _canvasGo.GetComponent<RectTransform>();

        var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(canvasRt, false);
        var backdropRt = backdrop.GetComponent<RectTransform>();
        backdropRt.anchorMin = Vector2.zero;
        backdropRt.anchorMax = Vector2.one;
        backdropRt.offsetMin = Vector2.zero;
        backdropRt.offsetMax = Vector2.zero;
        var backdropImg = backdrop.GetComponent<Image>();
        backdropImg.sprite = MakeWhiteSprite();
        backdropImg.color = BackdropColor;

        _rootPanel = backdrop;

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(backdrop.transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(540f, 420f);
        var panelImg = panel.GetComponent<Image>();
        panelImg.sprite = MakeWhiteSprite();
        panelImg.color = PanelColor;
        var outline = panel.AddComponent<Outline>();
        outline.effectColor = BorderColor;
        outline.effectDistance = new Vector2(2f, -2f);

        var title = new GameObject("Title", typeof(RectTransform));
        title.transform.SetParent(panel.transform, false);
        var titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -16f);
        titleRt.sizeDelta = new Vector2(-32f, 40f);
        var titleTmp = title.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(titleTmp, 30f, TextAlignmentOptions.Center, TextColor);
        titleTmp.text = "ПАУЗА";

        var subtitle = new GameObject("Subtitle", typeof(RectTransform));
        subtitle.transform.SetParent(panel.transform, false);
        var subtitleRt = subtitle.GetComponent<RectTransform>();
        subtitleRt.anchorMin = new Vector2(0f, 1f);
        subtitleRt.anchorMax = new Vector2(1f, 1f);
        subtitleRt.pivot = new Vector2(0.5f, 1f);
        subtitleRt.anchoredPosition = new Vector2(0f, -56f);
        subtitleRt.sizeDelta = new Vector2(-32f, 24f);
        var subtitleTmp = subtitle.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(subtitleTmp, 16f, TextAlignmentOptions.Center, SubTextColor);
        subtitleTmp.text = "Esc — продолжить · настройки сохраняются автоматически";

        BuildSlider(panel.transform, "Громкость",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(32f, -120f), new Vector2(480f, 40f),
            GameSettings.MinMasterVolume, GameSettings.MaxMasterVolume, GameSettings.MasterVolume,
            out _volumeSlider, out _volumeValueLabel,
            v =>
            {
                GameSettings.MasterVolume = v;
                RefreshValueLabels();
            });

        BuildSlider(panel.transform, "Чувствительность мыши",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(32f, -200f), new Vector2(480f, 40f),
            GameSettings.MinMouseSensitivity, GameSettings.MaxMouseSensitivity, GameSettings.MouseSensitivity,
            out _mouseSlider, out _mouseValueLabel,
            v =>
            {
                GameSettings.MouseSensitivity = v;
                RefreshValueLabels();
            });

        BuildButton(panel.transform, "Продолжить",
            new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(220f, 50f),
            ButtonColor, ButtonHoverColor, TextColor,
            () => SetPaused(false));

        BuildButton(panel.transform, "Заново",
            new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(220f, 40f),
            RestartColor, ButtonHoverColor, TextColor,
            RestartScene);
    }

    void BuildSlider(Transform parent, string label,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 size,
        float min, float max, float value,
        out Slider slider, out TMP_Text valueLabel,
        System.Action<float> onChanged)
    {
        var row = new GameObject(label + " Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        var rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = anchorMin;
        rowRt.anchorMax = anchorMax;
        rowRt.pivot = new Vector2(0f, 1f);
        rowRt.anchoredPosition = anchoredPos;
        rowRt.sizeDelta = size;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(row.transform, false);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 1f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.pivot = new Vector2(0f, 1f);
        labelRt.anchoredPosition = new Vector2(0f, 0f);
        labelRt.sizeDelta = new Vector2(0f, 18f);
        var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(labelTmp, 16f, TextAlignmentOptions.MidlineLeft, TextColor);
        labelTmp.text = label;

        var valueGo = new GameObject("Value", typeof(RectTransform));
        valueGo.transform.SetParent(row.transform, false);
        var valueRt = valueGo.GetComponent<RectTransform>();
        valueRt.anchorMin = new Vector2(0f, 1f);
        valueRt.anchorMax = new Vector2(1f, 1f);
        valueRt.pivot = new Vector2(1f, 1f);
        valueRt.anchoredPosition = new Vector2(0f, 0f);
        valueRt.sizeDelta = new Vector2(0f, 18f);
        var valueTmp = valueGo.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(valueTmp, 16f, TextAlignmentOptions.MidlineRight, SubTextColor);
        valueTmp.text = "";
        valueLabel = valueTmp;

        var sliderGo = new GameObject("Slider", typeof(RectTransform));
        sliderGo.transform.SetParent(row.transform, false);
        var sliderRt = sliderGo.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0f, 0f);
        sliderRt.anchorMax = new Vector2(1f, 0f);
        sliderRt.pivot = new Vector2(0f, 0f);
        sliderRt.anchoredPosition = new Vector2(0f, 0f);
        sliderRt.sizeDelta = new Vector2(0f, 18f);

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(sliderGo.transform, false);
        var bgRt = background.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.5f);
        bgRt.anchorMax = new Vector2(1f, 0.5f);
        bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.sizeDelta = new Vector2(0f, 6f);
        var bgImg = background.GetComponent<Image>();
        bgImg.sprite = MakeWhiteSprite();
        bgImg.color = new Color(0.2f, 0.22f, 0.27f, 1f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);
        var fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRt.pivot = new Vector2(0.5f, 0.5f);
        fillAreaRt.sizeDelta = new Vector2(-12f, 6f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        var fillImg = fill.GetComponent<Image>();
        fillImg.sprite = MakeWhiteSprite();
        fillImg.color = ButtonColor;

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderGo.transform, false);
        var handleAreaRt = handleArea.GetComponent<RectTransform>();
        handleAreaRt.anchorMin = new Vector2(0f, 0f);
        handleAreaRt.anchorMax = new Vector2(1f, 1f);
        handleAreaRt.sizeDelta = new Vector2(-20f, 0f);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        var handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(18f, 18f);
        var handleImg = handle.GetComponent<Image>();
        handleImg.sprite = MakeWhiteSprite();
        handleImg.color = TextColor;

        var sld = sliderGo.AddComponent<Slider>();
        sld.fillRect = fillRt;
        sld.handleRect = handleRt;
        sld.targetGraphic = handleImg;
        sld.direction = Slider.Direction.LeftToRight;
        sld.minValue = min;
        sld.maxValue = max;
        sld.SetValueWithoutNotify(Mathf.Clamp(value, min, max));
        sld.onValueChanged.AddListener(v => onChanged(v));
        slider = sld;
    }

    void BuildButton(Transform parent, string label,
        Vector2 anchor, Vector2 anchoredPos, Vector2 size,
        Color normalColor, Color hoverColor, Color textColor,
        System.Action onClick)
    {
        var go = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.sprite = MakeWhiteSprite();
        img.color = normalColor;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = hoverColor;
        colors.pressedColor = hoverColor;
        colors.selectedColor = hoverColor;
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick());

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(tmp, 20f, TextAlignmentOptions.Center, textColor);
        tmp.text = label;
    }

    void RestartScene()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        Cursor.lockState = _previousLockMode;
        Cursor.visible = _previousCursorVisible;
        GameSettings.Save();
        var active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.buildIndex);
    }

    static void ApplyTextStyle(TMP_Text tmp, float size, TextAlignmentOptions alignment, Color color)
    {
        var font = TmpCyrillicFontWarmup.PreferredUiFont != null
            ? TmpCyrillicFontWarmup.PreferredUiFont
            : TMP_Settings.defaultFontAsset;
        if (font != null)
            tmp.font = font;
        tmp.fontSize = size;
        tmp.enableAutoSizing = false;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        tmp.richText = false;
    }

    static Sprite MakeWhiteSprite()
    {
        return Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
    }
}

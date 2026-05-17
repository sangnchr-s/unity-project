using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Игровой HUD-оверлей с телеметрией бура: глубина, пройдено метров, скорость, передача,
/// текущая порода и требуемая мощность, статус головы (вращение/выдвижение) и шкала перегрева.
///
/// Создаёт собственный Canvas в верхнем левом углу, чтобы не мешать существующему UI.
/// Включается/выключается клавишей H (по умолчанию).
/// </summary>
public sealed class DrillHud : MonoBehaviour
{
    [SerializeField] KeyCode toggleKey = KeyCode.H;
    [SerializeField] bool visibleAtStart = true;
    [SerializeField] float panelWidth = 320f;
    [SerializeField] float panelHeight = 230f;
    [SerializeField] float margin = 16f;

    static readonly Color BackgroundColor = new Color(0.04f, 0.05f, 0.07f, 0.78f);
    static readonly Color BorderColor = new Color(0.95f, 0.55f, 0.18f, 0.9f);
    static readonly Color TextColor = new Color(0.96f, 0.97f, 1f, 1f);
    static readonly Color SubTextColor = new Color(0.78f, 0.82f, 0.9f, 1f);
    static readonly Color HeatNormal = new Color(0.35f, 0.78f, 0.45f, 1f);
    static readonly Color HeatWarning = new Color(0.95f, 0.78f, 0.25f, 1f);
    static readonly Color HeatCritical = new Color(0.95f, 0.28f, 0.22f, 1f);

    GameObject _canvasGo;
    GameObject _rootPanel;
    TMP_Text _depthLine;
    TMP_Text _distanceLine;
    TMP_Text _speedLine;
    TMP_Text _gearLine;
    TMP_Text _rockLine;
    TMP_Text _headLine;
    TMP_Text _heatLabel;
    Image _heatFill;

    Movement _movement;
    Transform _drillTransform;
    float _startZ;
    float _totalDistance;
    Vector3 _lastDrillPos;
    bool _hasLastPos;
    string _currentRockName = "неизвестно";
    int _currentRockRequiredPower = 10;

    void Start()
    {
        BuildUi();
        ResolveSceneRefs();
        if (_drillTransform != null)
        {
            _startZ = _drillTransform.position.z;
            _lastDrillPos = _drillTransform.position;
            _hasLastPos = true;
        }
        SetVisible(visibleAtStart);
    }

    void OnEnable()
    {
        Movement.OnRockContextChanged += OnRockContextChanged;
    }

    void OnDisable()
    {
        Movement.OnRockContextChanged -= OnRockContextChanged;
    }

    void Update()
    {
        if (!SimulatorPracticePanel.IsTypingInPracticeInput && !PauseMenu.IsPaused && Input.GetKeyDown(toggleKey))
            SetVisible(_rootPanel != null && !_rootPanel.activeSelf);

        if (_rootPanel == null || !_rootPanel.activeSelf)
            return;

        ResolveSceneRefs();
        UpdateDistance();
        RefreshLabels();
    }

    void SetVisible(bool visible)
    {
        if (_rootPanel != null)
            _rootPanel.SetActive(visible);
    }

    void ResolveSceneRefs()
    {
        if (_movement == null)
            _movement = FindAnyObjectByType<Movement>();
        if (_drillTransform == null && _movement != null)
            _drillTransform = _movement.transform;
    }

    void UpdateDistance()
    {
        if (_drillTransform == null)
            return;

        Vector3 pos = _drillTransform.position;
        if (!_hasLastPos)
        {
            _lastDrillPos = pos;
            _hasLastPos = true;
            return;
        }

        Vector3 delta = pos - _lastDrillPos;
        _totalDistance += delta.magnitude;
        _lastDrillPos = pos;
    }

    void RefreshLabels()
    {
        var inv = CultureInfo.InvariantCulture;

        if (_drillTransform != null)
        {
            float depth = _drillTransform.position.z - _startZ;
            _depthLine.text = $"Глубина: {depth.ToString("F1", inv)} м";
        }
        else
        {
            _depthLine.text = "Глубина: —";
        }

        _distanceLine.text = $"Пройдено: {_totalDistance.ToString("F1", inv)} м";

        float speed = _movement != null ? Mathf.Abs(_movement.currentSpeed) : 0f;
        bool moving = _movement != null && _movement.moving;
        _speedLine.text = $"Скорость: {speed.ToString("F1", inv)} м/с {(moving ? "" : "(стоп)")}";

        int power = DrillPracticeTelemetry.DrillPower;
        int gear = power switch
        {
            >= 30 => 3,
            >= 20 => 2,
            _ => 1
        };
        _gearLine.text = $"Передача: {gear} (мощность {power})";

        string requiredHint = _currentRockRequiredPower > 0
            ? $" (треб. {_currentRockRequiredPower})"
            : "";
        _rockLine.text = $"Порода: {_currentRockName}{requiredHint}";

        bool rotating = DrillPracticeTelemetry.HeadIsRotating;
        bool extended = DrillPracticeTelemetry.HeadIsExtended;
        _headLine.text = $"Голова: {(rotating ? "вращается" : "стоит")}, {(extended ? "выдвинута" : "втянута")}";

        UpdateHeatBar();
    }

    void UpdateHeatBar()
    {
        var overheat = DrillOverheatSystem.Instance;
        float heat = overheat != null ? overheat.Heat : 0f;
        _heatLabel.text = $"Температура: {Mathf.RoundToInt(heat * 100f)}%{(overheat != null && overheat.IsCritical ? " КРИТ." : "")}";

        if (_heatFill != null)
        {
            _heatFill.fillAmount = Mathf.Clamp01(heat);
            if (overheat != null && heat >= overheat.CriticalThreshold)
                _heatFill.color = HeatCritical;
            else if (overheat != null && heat >= overheat.WarningThreshold)
                _heatFill.color = HeatWarning;
            else
                _heatFill.color = HeatNormal;
        }
    }

    void OnRockContextChanged(string rockName, int requiredPower)
    {
        _currentRockName = string.IsNullOrWhiteSpace(rockName) ? "неизвестно" : rockName;
        _currentRockRequiredPower = Mathf.Max(10, requiredPower);
    }

    void BuildUi()
    {
        _canvasGo = new GameObject("DrillHud_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvasGo.transform.SetParent(transform, false);

        var canvas = _canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                                          | AdditionalCanvasShaderChannels.Normal
                                          | AdditionalCanvasShaderChannels.Tangent;

        var scaler = _canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var raycaster = _canvasGo.GetComponent<GraphicRaycaster>();
        raycaster.ignoreReversedGraphics = true;

        var canvasRt = _canvasGo.GetComponent<RectTransform>();

        _rootPanel = new GameObject("HUD_Panel", typeof(RectTransform), typeof(Image));
        _rootPanel.transform.SetParent(canvasRt, false);
        var panelRt = _rootPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 1f);
        panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.anchoredPosition = new Vector2(margin, -margin);
        panelRt.sizeDelta = new Vector2(panelWidth, panelHeight);

        var panelImg = _rootPanel.GetComponent<Image>();
        panelImg.sprite = MakeWhiteSprite();
        panelImg.color = BackgroundColor;
        panelImg.raycastTarget = false;

        var outline = _rootPanel.AddComponent<Outline>();
        outline.effectColor = BorderColor;
        outline.effectDistance = new Vector2(2f, -2f);

        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(_rootPanel.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -8f);
        titleRt.sizeDelta = new Vector2(-12f, 28f);
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(titleTmp, 22f, TextAlignmentOptions.Center, TextColor);
        titleTmp.text = "ТЕЛЕМЕТРИЯ БУРА";

        var stack = new GameObject("Stack", typeof(RectTransform), typeof(VerticalLayoutGroup));
        stack.transform.SetParent(_rootPanel.transform, false);
        var stackRt = stack.GetComponent<RectTransform>();
        stackRt.anchorMin = new Vector2(0f, 0f);
        stackRt.anchorMax = new Vector2(1f, 1f);
        stackRt.offsetMin = new Vector2(12f, 32f);
        stackRt.offsetMax = new Vector2(-12f, -40f);
        var vlg = stack.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 3f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.padding = new RectOffset(0, 0, 0, 0);

        _depthLine = AddTextLine(stack.transform, "Depth");
        _distanceLine = AddTextLine(stack.transform, "Distance");
        _speedLine = AddTextLine(stack.transform, "Speed");
        _gearLine = AddTextLine(stack.transform, "Gear");
        _rockLine = AddTextLine(stack.transform, "Rock");
        _headLine = AddTextLine(stack.transform, "Head");
        _heatLabel = AddTextLine(stack.transform, "Heat");
        BuildHeatBar(stack.transform);

        var hintGo = new GameObject("Hint", typeof(RectTransform));
        hintGo.transform.SetParent(_rootPanel.transform, false);
        var hintRt = hintGo.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0f, 0f);
        hintRt.anchorMax = new Vector2(1f, 0f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.anchoredPosition = new Vector2(0f, 6f);
        hintRt.sizeDelta = new Vector2(-12f, 18f);
        var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(hintTmp, 14f, TextAlignmentOptions.MidlineRight, SubTextColor);
        hintTmp.text = $"скрыть: {toggleKey}";
    }

    TMP_Text AddTextLine(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 22f;
        le.minHeight = 22f;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(tmp, 18f, TextAlignmentOptions.MidlineLeft, TextColor);
        tmp.text = "—";
        return tmp;
    }

    void BuildHeatBar(Transform parent)
    {
        var barGo = new GameObject("HeatBar", typeof(RectTransform), typeof(Image));
        barGo.transform.SetParent(parent, false);
        var le = barGo.AddComponent<LayoutElement>();
        le.preferredHeight = 12f;
        le.minHeight = 12f;
        var bg = barGo.GetComponent<Image>();
        bg.sprite = MakeWhiteSprite();
        bg.color = new Color(0.18f, 0.2f, 0.24f, 1f);
        bg.raycastTarget = false;

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(barGo.transform, false);
        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.offsetMin = new Vector2(1f, 1f);
        fillRt.offsetMax = new Vector2(-1f, -1f);
        _heatFill = fillGo.GetComponent<Image>();
        _heatFill.sprite = MakeWhiteSprite();
        _heatFill.type = Image.Type.Filled;
        _heatFill.fillMethod = Image.FillMethod.Horizontal;
        _heatFill.fillAmount = 0f;
        _heatFill.color = HeatNormal;
        _heatFill.raycastTarget = false;
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

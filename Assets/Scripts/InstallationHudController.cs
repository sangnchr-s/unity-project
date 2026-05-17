using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class InstallationHudController : MonoBehaviour
{
    static readonly Color HighlightTint = new Color(1f, 0.22f, 0.2f, 1f);

    [SerializeField] string drillRootName = "Drill_Prefab";
    [SerializeField] float panelWidth = 280f;
    [SerializeField] float statusBarHeight = 118f;
    [SerializeField] float bottomMargin = 20f;
    [SerializeField] float sideMargin = 40f;

    OrbitCameraAroundTarget _orbit;
    LecturePresetCameraController _presetCam;
    Transform _drillRoot;
    TMP_Text _statusLine;
    GameObject _statusBarRoot;
    ElementBinding _hovered;
    static TMP_FontAsset _cachedTmpFont;

    RectTransform _sideMenuRoot;
    GameObject _controlPanelGo;
    RectTransform _menuToggleRt;
    TMP_Text _menuToggleLabel;
    bool _sideMenuExpanded = true;
    RectTransform _sideMenuCanvasRt;

    SimulatorPracticePanel _practicePanel;
    RectTransform _practiceTasksRoot;
    readonly List<Button> _practiceTaskButtons = new List<Button>();
    bool _taskHintSubscribed;
    bool _showTasksPanelRequested;
    /// <summary>Корень списка кнопок (VLG + ContentSizeFitter внутри ScrollRect).</summary>
    RectTransform _menuListRoot;
    ScrollRect _menuScroll;
    const float MenuToggleWidth = 52f;

    float ComputeSidePanelScrollHeight()
    {
        float h = Screen.height;
        if (_sideMenuCanvasRt != null && _sideMenuCanvasRt.rect.height > 2f)
            h = _sideMenuCanvasRt.rect.height;
        return Mathf.Clamp(h * 0.92f, 340f, Mathf.Max(400f, h - 24f));
    }

    RectTransform EnsureSideMenuCanvas(RectTransform fallbackParent)
    {
        const string sideMenuCanvasName = "Canvas_SideMenuOverlay";
        var hostCanvas = GetComponentInParent<Canvas>();
        var parent = hostCanvas != null ? hostCanvas.transform : fallbackParent;

        var existing = parent.Find(sideMenuCanvasName);
        GameObject go;
        if (existing != null)
            go = existing.gameObject;
        else
            go = new GameObject(sideMenuCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        go.layer = fallbackParent.gameObject.layer;
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var sideCanvas = go.GetComponent<Canvas>();
        if (hostCanvas != null)
        {
            sideCanvas.renderMode = hostCanvas.renderMode;
            sideCanvas.worldCamera = hostCanvas.worldCamera;
            sideCanvas.planeDistance = hostCanvas.planeDistance;
            sideCanvas.sortingLayerID = hostCanvas.sortingLayerID;
            sideCanvas.sortingOrder = hostCanvas.sortingOrder + 10;
        }
        else
        {
            sideCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            sideCanvas.sortingOrder = 100;
        }
        sideCanvas.overrideSorting = true;
        sideCanvas.pixelPerfect = false;

        var hostScaler = hostCanvas != null ? hostCanvas.GetComponent<CanvasScaler>() : null;
        var sideScaler = go.GetComponent<CanvasScaler>();
        if (hostScaler != null)
        {
            sideScaler.uiScaleMode = hostScaler.uiScaleMode;
            sideScaler.referenceResolution = hostScaler.referenceResolution;
            sideScaler.screenMatchMode = hostScaler.screenMatchMode;
            sideScaler.matchWidthOrHeight = hostScaler.matchWidthOrHeight;
            sideScaler.referencePixelsPerUnit = hostScaler.referencePixelsPerUnit;
        }
        else
        {
            sideScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sideScaler.referenceResolution = new Vector2(1920f, 1080f);
            sideScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            sideScaler.matchWidthOrHeight = 0.5f;
        }

        _sideMenuCanvasRt = rt;
        return rt;
    }
    // LiberationSans SDF не содержит ◀▶ (U+25C0/U+25B6) — только ASCII в атласе.
    const string MenuGlyphHidePanel = "<";
    const string MenuGlyphShowPanel = ">";
    const string HintListDefault =
        "Наведите курсор на элемент в списке «УСТАНОВКА» для подсказки.";
    const string HintPracticeHover = "Открыть окно практики (наблюдения, таблица результатов).";

    readonly List<(Renderer renderer, Color[] originalColors)> _highlighted
        = new List<(Renderer, Color[])>();

    enum ResolveMode { DrillRoot, ByName }

    sealed class ElementBinding
    {
        public string Title;
        public string Description;
        public ResolveMode Mode;
        public string ObjectName;
        public int PresetIndex;
    }

    readonly struct PracticeTaskUiDef
    {
        public readonly int Index1Based;
        public readonly TaskId Id;
        public readonly string Title;
        public readonly string Hint;

        public PracticeTaskUiDef(int index1Based, TaskId id, string title, string hint)
        {
            Index1Based = index1Based;
            Id = id;
            Title = title;
            Hint = hint;
        }
    }

    static readonly PracticeTaskUiDef[] PracticeTasks =
    {
        new PracticeTaskUiDef(
            1,
            TaskId.StartMachine,
            "задание 1",
            "Запустите машину (клавиша пробел)."
        ),
        new PracticeTaskUiDef(
            2,
            TaskId.StartRotation,
            "задание 2",
            "Запустите вращение бура (клавиша R)."
        ),
        new PracticeTaskUiDef(
            3,
            TaskId.Run,
            "задание 3",
            "Доедьте до горной породы (клавиша F)."
        ),
        new PracticeTaskUiDef(
            4,
            TaskId.ExtendDrill,
            "задание 4",
            "Выдвиньте головку бура (клавиша E)."
        ),
        new PracticeTaskUiDef(
            5,
            TaskId.ChangePower,
            "задание 5",
            "Доедьте до гранита и смените мощность на 2."
        ),
        new PracticeTaskUiDef(
            6,
            TaskId.ChangePowerAgain,
            "задание 6",
            "Доедьте до диорита и смените мощность на 3."
        ),
        new PracticeTaskUiDef(
            7,
            TaskId.Finish,
            "задание 7",
            "Закончите тоннель."
        )
    };

    public void SetStatusLine(string message)
    {
        if (_statusLine != null)
            _statusLine.text = message;
    }

    void ShowStatusBar(string message)
    {
        if (_statusLine != null)
            _statusLine.text = message;
        if (_statusBarRoot != null)
            _statusBarRoot.SetActive(true);
    }

    void HideStatusBar()
    {
        if (_statusBarRoot != null)
            _statusBarRoot.SetActive(false);
    }

    void WireButtonHoverStatusBar(Button btn, string hoverText)
    {
        var trigger = btn.gameObject.GetComponent<EventTrigger>()
                      ?? btn.gameObject.AddComponent<EventTrigger>();
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => ShowStatusBar(hoverText));
        trigger.triggers.Add(enter);
        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideStatusBar());
        trigger.triggers.Add(exit);
    }

    void Awake()
    {
        EnsureCameraRefs();
        var drillGo = GameObject.Find(drillRootName);
        _drillRoot = drillGo != null ? drillGo.transform : null;

        var selfRt = GetComponent<RectTransform>();
        selfRt.anchorMin = Vector2.zero;
        selfRt.anchorMax = Vector2.one;
        selfRt.pivot = new Vector2(0.5f, 0.5f);
        selfRt.offsetMin = Vector2.zero;
        selfRt.offsetMax = Vector2.zero;

        EnsureCanvasSupportsTextMeshPro();

        BuildStatusBar(selfRt);

        var canvasRt = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        if (canvasRt != null)
        {
            var reservedRightWidth = EffectivePanelWidth() + MenuToggleWidth + sideMargin;
            _practicePanel = SimulatorPracticePanel.Build(canvasRt, GetTmpFont(), reservedRightWidth);
        }

        // Окно практики создаётся раньше бокового меню, иначе нижняя широкая панель перекрывает кнопку «<» справа.
        var sideMenuParent = EnsureSideMenuCanvas(selfRt);
        BuildControlPanel(sideMenuParent);
        HideIntroductoryInfoWindows();

        transform.SetAsLastSibling();
    }

    void OnEnable()
    {
        EnsureCameraRefs();
        TrySubscribeTaskHintEvents();
    }

    void OnDisable()
    {
        UnsubscribeTaskHintEvents();
    }

    void Start()
    {
        // После CanvasScaler пересчитать размер бокового меню.
        ApplySideMenuRootSize();
    }

    void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled)
            return;
        ApplySideMenuRootSize();
    }

    void EnsureCameraRefs()
    {
        if (Camera.main == null)
            return;
        if (_presetCam == null)
            _presetCam = Camera.main.GetComponent<LecturePresetCameraController>();
        if (_orbit == null)
            _orbit = Camera.main.GetComponent<OrbitCameraAroundTarget>();
    }

    void EnsureCanvasSupportsTextMeshPro()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return;
        canvas.pixelPerfect = false;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                                           | AdditionalCanvasShaderChannels.Normal
                                           | AdditionalCanvasShaderChannels.Tangent;
    }

    static TMP_FontAsset GetTmpFont()
    {
        var preferred = TmpCyrillicFontWarmup.PreferredUiFont;
        if (IsFontUsable(preferred))
            return preferred;
        if (IsFontUsable(_cachedTmpFont))
            return _cachedTmpFont;
        _cachedTmpFont = TMP_Settings.defaultFontAsset;
        return _cachedTmpFont;
    }

    static bool IsFontUsable(TMP_FontAsset font)
    {
        if (font == null)
            return false;
        try
        {
            if (font.atlasTexture == null && (font.atlasTextures == null || font.atlasTextures.Length == 0))
                return false;
            return true;
        }
        catch
        {
            return false;
        }
    }

    static void ApplyCommonTmpText(TMP_Text tx, float fontSize, TextAlignmentOptions alignment)
    {
        var font = GetTmpFont();
        if (font != null)
            tx.font = font;
        tx.fontSize = fontSize;
        tx.enableAutoSizing = false;
        tx.color = new Color(0.97f, 0.98f, 1f, 1f);
        tx.alignment = alignment;
        tx.richText = false;
        tx.raycastTarget = false;
    }

    void BuildStatusBar(RectTransform parent)
    {
        var root = new GameObject("StatusBar_Root", typeof(RectTransform));
        var rt = root.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.SetAsLastSibling();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, bottomMargin);
        rt.sizeDelta = new Vector2(-sideMargin * 2f, statusBarHeight);

        var bgGo = new GameObject("StatusBar_Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.SetParent(rt, false);
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg = bgGo.GetComponent<Image>();
        bgImg.sprite = MakeWhiteSprite();
        bgImg.type = Image.Type.Simple;
        bgImg.color = new Color(0.06f, 0.07f, 0.1f, 0.92f);

        var txtGo = new GameObject("StatusBar_Text", typeof(RectTransform));
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.SetParent(rt, false);
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(20f, 14f);
        txtRt.offsetMax = new Vector2(-20f, -14f);
        _statusLine = txtGo.AddComponent<TextMeshProUGUI>();
        ApplyCommonTmpText(_statusLine, 30f, TextAlignmentOptions.MidlineLeft);
        _statusLine.enableWordWrapping = true;
        _statusLine.overflowMode = TextOverflowModes.Ellipsis;
        _statusLine.text = HintListDefault;
        _statusBarRoot = root;
        root.SetActive(false);
    }

    void BuildControlPanel(RectTransform parent)
    {
        var rootGo = new GameObject("SideMenu_Root", typeof(RectTransform));
        rootGo.layer = parent.gameObject.layer;
        _sideMenuRoot = rootGo.GetComponent<RectTransform>();

        _sideMenuRoot.SetParent(parent, false);
        _sideMenuRoot.SetAsLastSibling();
        _sideMenuRoot.anchorMin = new Vector2(1f, 1f);
        _sideMenuRoot.anchorMax = new Vector2(1f, 1f);
        _sideMenuRoot.pivot = new Vector2(1f, 1f);
        _sideMenuRoot.anchoredPosition = new Vector2(-10f, -44f);
        var initW = EffectivePanelWidth() + MenuToggleWidth;
        var initH = ComputeSidePanelScrollHeight();
        _sideMenuRoot.sizeDelta = new Vector2(initW, initH);

        var panelGo = new GameObject("Control_Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _controlPanelGo = panelGo;
        var prt = panelGo.GetComponent<RectTransform>();
        prt.SetParent(_sideMenuRoot, false);
        prt.SetAsFirstSibling();
        prt.anchorMin = new Vector2(1f, 1f);
        prt.anchorMax = new Vector2(1f, 1f);
        prt.pivot = new Vector2(1f, 1f);
        prt.sizeDelta = new Vector2(EffectivePanelWidth(), initH);
        prt.anchoredPosition = new Vector2(-MenuToggleWidth, 0f);

        var pBg = panelGo.GetComponent<Image>();
        pBg.sprite = MakeWhiteSprite();
        pBg.type = Image.Type.Simple;
        pBg.color = new Color(0.08f, 0.09f, 0.12f, 0.94f);

        var scrollGo = new GameObject("MenuScroll", typeof(RectTransform), typeof(ScrollRect));
        var scrollRootRt = scrollGo.GetComponent<RectTransform>();
        scrollRootRt.SetParent(prt, false);
        scrollRootRt.anchorMin = Vector2.zero;
        scrollRootRt.anchorMax = Vector2.one;
        scrollRootRt.offsetMin = Vector2.zero;
        scrollRootRt.offsetMax = Vector2.zero;

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        var viewportRt = viewportGo.GetComponent<RectTransform>();
        viewportRt.SetParent(scrollRootRt, false);
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        var vpImg = viewportGo.GetComponent<Image>();
        vpImg.sprite = MakeWhiteSprite();
        vpImg.color = new Color(1f, 1f, 1f, 0.001f);
        vpImg.raycastTarget = true;

        var menuListGo = new GameObject("MenuList", typeof(RectTransform), typeof(VerticalLayoutGroup));
        menuListGo.transform.SetParent(viewportRt, false);
        var menuListRt = menuListGo.GetComponent<RectTransform>();
        menuListRt.anchorMin = new Vector2(0f, 1f);
        menuListRt.anchorMax = new Vector2(1f, 1f);
        menuListRt.pivot = new Vector2(0.5f, 1f);
        menuListRt.anchoredPosition = Vector2.zero;
        menuListRt.sizeDelta = new Vector2(0f, 0f);
        _menuListRoot = menuListRt;

        var vlg = menuListGo.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.padding = new RectOffset(10, 10, 10, 10);

        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.content = menuListRt;
        scroll.viewport = viewportRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 36f;
        scroll.inertia = false;
        _menuScroll = scroll;

        var contentRt = menuListRt;

        AddInstallationTitleButton(contentRt);

        foreach (var def in BuildElementList())
        {
            var btn = CreatePanelButton(contentRt, def.Title, 20f, 44f, 48f);
            ApplyElementButtonRedTheme(btn);
            WireElementButton(btn, def);
        }

        var practiceBtn = CreatePanelButton(contentRt, "ПРАКТИКА", 24f, 48f, 52f);
        var pColors = practiceBtn.colors;
        pColors.normalColor = new Color(0.35f, 0.55f, 0.85f, 1f);
        pColors.highlightedColor = new Color(0.45f, 0.65f, 0.95f, 1f);
        pColors.pressedColor = new Color(0.28f, 0.45f, 0.75f, 1f);
        practiceBtn.colors = pColors;
        practiceBtn.onClick.AddListener(OnPracticeClicked);
        WireButtonHoverStatusBar(practiceBtn, HintPracticeHover);
        BuildPracticeTaskButtons(contentRt);
        SetPracticeTaskButtonsVisible(false);

        var toggleBtn = CreateMenuToggleButton(_sideMenuRoot);
        _menuToggleRt = toggleBtn.GetComponent<RectTransform>();
        toggleBtn.onClick.AddListener(ToggleSideMenu);

        ApplySideMenuRootSize();
        RefreshMenuScrollContentSize();
    }

    void RefreshSideMenuListLayout()
    {
        RefreshMenuScrollContentSize();
    }

    void RefreshMenuScrollContentSize()
    {
        if (_menuListRoot == null || _menuScroll == null || _menuScroll.viewport == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(_menuScroll.viewport);

        var viewportWidth = Mathf.Max(80f, _menuScroll.viewport.rect.width);
        _menuListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, viewportWidth);

        LayoutRebuilder.ForceRebuildLayoutImmediate(_menuListRoot);
        var preferredHeight = LayoutUtility.GetPreferredHeight(_menuListRoot);
        preferredHeight = Mathf.Max(preferredHeight, 8f);
        _menuListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);

        // Контент к верхнему краю viewport (иначе ScrollRect при «коротком» контенте даёт пустоту сверху).
        _menuListRoot.anchorMin = new Vector2(0f, 1f);
        _menuListRoot.anchorMax = new Vector2(1f, 1f);
        _menuListRoot.pivot = new Vector2(0.5f, 1f);
        _menuListRoot.anchoredPosition = Vector2.zero;

        var vh = _menuScroll.viewport.rect.height;
        var needsScroll = preferredHeight > vh + 0.5f;
        _menuScroll.vertical = needsScroll;

        Canvas.ForceUpdateCanvases();
        if (needsScroll)
            _menuScroll.verticalNormalizedPosition = 1f;
    }

    void ShowIntroductoryInfoWindows()
    {
        var canvas = transform.parent;
        if (canvas == null)
            return;
        var bg = canvas.Find("Background_Image");
        var inf = canvas.Find("Info_Text");
        if (bg != null)
            bg.gameObject.SetActive(true);
        if (inf != null)
            inf.gameObject.SetActive(true);
    }

    void HideIntroductoryInfoWindows()
    {
        var canvas = transform.parent;
        if (canvas == null)
            return;
        var bg = canvas.Find("Background_Image");
        var inf = canvas.Find("Info_Text");
        if (bg != null)
            bg.gameObject.SetActive(false);
        if (inf != null)
            inf.gameObject.SetActive(false);
    }

    float EffectivePanelWidth()
    {
        var preferred = Mathf.Max(200f, panelWidth);
        var canvasW = _sideMenuCanvasRt != null && _sideMenuCanvasRt.rect.width > 2f
            ? _sideMenuCanvasRt.rect.width
            : Screen.width;
        var adaptiveMax = Mathf.Clamp(canvasW * 0.4f, 220f, 420f);
        return Mathf.Clamp(preferred, 200f, adaptiveMax);
    }

    void ApplySideMenuRootSize()
    {
        if (_sideMenuRoot == null || _controlPanelGo == null || _menuToggleRt == null)
            return;

        var prt = _controlPanelGo.GetComponent<RectTransform>();
        var expanded = _controlPanelGo.activeSelf;
        var panelH = expanded ? ComputeSidePanelScrollHeight() : 96f;
        var panelW = expanded ? EffectivePanelWidth() : 0f;
        var w = Mathf.Max(MenuToggleWidth, panelW + MenuToggleWidth);
        var h = Mathf.Max(96f, panelH);
        _sideMenuRoot.sizeDelta = new Vector2(w, h);

        if (expanded)
        {
            prt.anchorMin = new Vector2(1f, 1f);
            prt.anchorMax = new Vector2(1f, 1f);
            prt.pivot = new Vector2(1f, 1f);
            prt.sizeDelta = new Vector2(panelW, panelH);
            prt.anchoredPosition = new Vector2(-MenuToggleWidth, 0f);
        }

        _menuToggleRt.anchorMin = new Vector2(1f, 1f);
        _menuToggleRt.anchorMax = new Vector2(1f, 1f);
        _menuToggleRt.pivot = new Vector2(1f, 1f);
        _menuToggleRt.sizeDelta = new Vector2(MenuToggleWidth, h);
        _menuToggleRt.anchoredPosition = Vector2.zero;

        if (expanded)
            LayoutRebuilder.ForceRebuildLayoutImmediate(prt);

        RefreshSideMenuListLayout();
        Canvas.ForceUpdateCanvases();
    }

    Button CreateMenuToggleButton(RectTransform parent)
    {
        var go = new GameObject("MenuToggle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.sprite = MakeWhiteSprite();
        img.type = Image.Type.Simple;
        img.color = new Color(0.12f, 0.13f, 0.17f, 0.96f);

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var c = btn.colors;
        c.highlightedColor = new Color(0.35f, 0.18f, 0.18f, 1f);
        c.pressedColor = new Color(0.22f, 0.12f, 0.12f, 1f);
        btn.colors = c;

        var txtGo = new GameObject("Text", typeof(RectTransform));
        var trt = txtGo.GetComponent<RectTransform>();
        trt.SetParent(rt, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(6f, 10f);
        trt.offsetMax = new Vector2(-6f, -10f);
        _menuToggleLabel = txtGo.AddComponent<TextMeshProUGUI>();
        ApplyCommonTmpText(_menuToggleLabel, 28f, TextAlignmentOptions.Midline);
        _menuToggleLabel.enableWordWrapping = false;
        _menuToggleLabel.overflowMode = TextOverflowModes.Overflow;
        _menuToggleLabel.text = MenuGlyphHidePanel;

        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(MenuToggleWidth, ComputeSidePanelScrollHeight());
        rt.anchoredPosition = Vector2.zero;

        rt.SetAsLastSibling();
        return btn;
    }

    void ToggleSideMenu()
    {
        _sideMenuExpanded = !_sideMenuExpanded;
        if (_controlPanelGo != null)
            _controlPanelGo.SetActive(_sideMenuExpanded);
        ApplyTasksPanelVisibility();
        if (_menuToggleLabel != null)
            _menuToggleLabel.text = _sideMenuExpanded ? MenuGlyphHidePanel : MenuGlyphShowPanel;
        ApplySideMenuRootSize();
    }

    static void ApplyElementButtonRedTheme(Button btn)
    {
        var c = btn.colors;
        c.highlightedColor = new Color(0.72f, 0.22f, 0.22f, 1f);
        c.pressedColor = new Color(0.52f, 0.14f, 0.14f, 1f);
        c.selectedColor = c.highlightedColor;
        btn.colors = c;
    }

    void AddInstallationTitleButton(RectTransform parent)
    {
        const string title = "УСТАНОВКА";
        var go = new GameObject("Title_Installation", typeof(RectTransform), typeof(LayoutElement),
            typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        var le = go.GetComponent<LayoutElement>();
        le.minHeight = 40f;
        le.preferredHeight = 44f;

        var img = go.GetComponent<Image>();
        img.sprite = MakeWhiteSprite();
        img.type = Image.Type.Simple;
        img.color = new Color(0.14f, 0.15f, 0.2f, 1f);

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var c = btn.colors;
        c.highlightedColor = new Color(0.55f, 0.18f, 0.18f, 1f);
        c.pressedColor = new Color(0.4f, 0.12f, 0.12f, 1f);
        btn.colors = c;
        btn.onClick.AddListener(OnInstallationTitleClicked);
        WireButtonHoverStatusBar(btn, HintListDefault);

        var txtGo = new GameObject("Text", typeof(RectTransform));
        var trt = txtGo.GetComponent<RectTransform>();
        trt.SetParent(rt, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(10f, 6f);
        trt.offsetMax = new Vector2(-10f, -6f);
        var tx = txtGo.AddComponent<TextMeshProUGUI>();
        ApplyCommonTmpText(tx, 26f, TextAlignmentOptions.Midline);
        tx.fontStyle = FontStyles.Bold;
        tx.color = new Color(0.95f, 0.95f, 1f, 1f);
        tx.enableWordWrapping = false;
        tx.overflowMode = TextOverflowModes.Overflow;
        tx.text = title;
    }

    void OnInstallationTitleClicked()
    {
        EnsureCameraRefs();
        _presetCam?.MoveView_Home();
        ClearHighlightVisuals();
        _hovered = null;

        HideStatusBar();
        _showTasksPanelRequested = false;
        ApplyTasksPanelVisibility();
    }

    static Button CreatePanelButton(RectTransform parent, string label, float fontSize = 16f,
        float minHeight = 34f, float preferredHeight = 38f)
    {
        var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button), typeof(LayoutElement));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        var le = go.GetComponent<LayoutElement>();
        le.minHeight = minHeight;
        le.preferredHeight = preferredHeight;

        var img = go.GetComponent<Image>();
        img.sprite = MakeWhiteSprite();
        img.type = Image.Type.Simple;
        img.color = new Color(0.22f, 0.24f, 0.3f, 1f);

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var txtGo = new GameObject("Text", typeof(RectTransform));
        var trt = txtGo.GetComponent<RectTransform>();
        trt.SetParent(rt, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(10f, 6f);
        trt.offsetMax = new Vector2(-10f, -6f);
        var tx = txtGo.AddComponent<TextMeshProUGUI>();
        ApplyCommonTmpText(tx, fontSize, TextAlignmentOptions.Midline);
        tx.enableWordWrapping = true;
        tx.overflowMode = TextOverflowModes.Overflow;
        tx.text = label;

        return btn;
    }

    void WireElementButton(Button btn, ElementBinding def)
    {
        var trigger = btn.gameObject.GetComponent<EventTrigger>()
                      ?? btn.gameObject.AddComponent<EventTrigger>();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => OnElementPointerEnter(def));
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => OnElementPointerExit(def));
        trigger.triggers.Add(exit);

        btn.onClick.AddListener(() => OnElementClicked(def));
    }

    List<ElementBinding> BuildElementList()
    {
        return new List<ElementBinding>
        {
            new ElementBinding
            {
                Title = "Буровая голова",
                Mode = ResolveMode.ByName,
                ObjectName = "DrillHead",
                Description = "Основной рабочий орган: выдвижение (E), задвижение (Q), вращение бура (R).",
                PresetIndex = 0
            },
            new ElementBinding
            {
                Title = "Малый бур L01",
                Mode = ResolveMode.ByName,
                ObjectName = "DrillSmalldrillL01",
                Description = "Левый передний малый бур. Клавиша 1 — пуск/стоп анимации бурения.",
                PresetIndex = 1
            },
            new ElementBinding
            {
                Title = "Малый бур L02",
                Mode = ResolveMode.ByName,
                ObjectName = "DrillSmalldrillL02",
                Description = "Левый задний малый бур. Клавиша 2 — пуск/стоп.",
                PresetIndex = 2
            },
            new ElementBinding
            {
                Title = "Малый бур R01",
                Mode = ResolveMode.ByName,
                ObjectName = "DrillSmalldrillR01",
                Description = "Правый передний малый бур. Клавиша 3 — пуск/стоп.",
                PresetIndex = 3
            },
            new ElementBinding
            {
                Title = "Малый бур R02",
                Mode = ResolveMode.ByName,
                ObjectName = "DrillSmalldrillR02",
                Description = "Правый задний малый бур. Клавиша 4 — пуск/стоп.",
                PresetIndex = 4
            },
            new ElementBinding
            {
                Title = "Прожектор передний левый",
                Mode = ResolveMode.ByName,
                ObjectName = "DrillFrontLight1_Dummy",
                Description = "Передний левый прожектор освещения забоя. Клавиша Z — вкл/выкл.",
                PresetIndex = 5
            },
            new ElementBinding
            {
                Title = "Прожектор передний правый",
                Mode = ResolveMode.ByName,
                ObjectName = "DrillFrontLight2_Dummy",
                Description = "Передний правый прожектор. Клавиша X — вкл/выкл.",
                PresetIndex = 6
            },
            new ElementBinding
            {
                Title = "Корпус и ходовая",
                Mode = ResolveMode.DrillRoot,
                Description = "Несущий корпус, гусеничная ходовая и каркас самоходной установки. Движение: F/B, поворот: K/L.",
                PresetIndex = 7
            },
        };
    }

    void OnElementPointerEnter(ElementBinding def)
    {
        _hovered = def;
        ShowStatusBar(def.Description);
        ClearHighlightVisuals();
        var t = ResolveTransform(def);
        if (t != null)
            ApplyHighlightVisuals(t);
    }

    void OnElementPointerExit(ElementBinding def)
    {
        if (_hovered != def)
            return;
        _hovered = null;
        HideStatusBar();
        ClearHighlightVisuals();
    }

    void OnElementClicked(ElementBinding def)
    {
        EnsureCameraRefs();
        if (_presetCam == null)
        {
            ShowStatusBar("На Main Camera нужен скрипт LecturePresetCameraController.");
            return;
        }

        var t = ResolveTransform(def);
        if (t == null)
        {
            ShowStatusBar("Объект «" + (def.ObjectName ?? drillRootName)
                          + "» не найден в иерархии Drill_Prefab.");
            return;
        }

        _presetCam.MoveToPreset(def.PresetIndex);
    }

[SerializeField] private GameObject tasksPanel;
    void OnPracticeClicked()
    {
            EnsureCameraRefs();
            TrySubscribeTaskHintEvents();
            _presetCam?.MoveView_Home();
            ClearHighlightVisuals();
            _hovered = null;
            HideStatusBar();
            HideIntroductoryInfoWindows();
            DrillPracticeTelemetry.ResetSession();
            // Показываем рабочую панель практики с полем ввода и кнопкой "Запись".
            if (_practicePanel != null)
            {
                _practicePanel.SetCurrentTaskIndex(0);
                var taskHint = GetCurrentTaskHintText();
                _practicePanel.SetHint(taskHint);
                _practicePanel.Show();
            }

            _showTasksPanelRequested = true;
            ApplyTasksPanelVisibility();

    }

    void ApplyTasksPanelVisibility()
    {
        var visible = _showTasksPanelRequested && _sideMenuExpanded;

        if (tasksPanel != null)
            tasksPanel.SetActive(false);

        SetPracticeTaskButtonsVisible(visible);
    }

    void TrySubscribeTaskHintEvents()
    {
        if (_taskHintSubscribed || Main.taskManager == null)
            return;

        Main.taskManager.OnTaskChanged += OnTaskManagerTaskChanged;
        Main.taskManager.OnTaskCompleted += OnTaskManagerTaskCompleted;
        _taskHintSubscribed = true;
    }

    void UnsubscribeTaskHintEvents()
    {
        if (!_taskHintSubscribed || Main.taskManager == null)
            return;

        Main.taskManager.OnTaskChanged -= OnTaskManagerTaskChanged;
        Main.taskManager.OnTaskCompleted -= OnTaskManagerTaskCompleted;
        _taskHintSubscribed = false;
    }

    void OnTaskManagerTaskChanged(TaskId id)
    {
        if (_practicePanel == null || !_practicePanel.IsVisible)
            return;

        _practicePanel.SetHint("задание: " + id.ToCustomString());
    }

    void OnTaskManagerTaskCompleted(TaskId id)
    {
        if (_practicePanel == null || !_practicePanel.IsVisible)
            return;

        _practicePanel.SetHint("Задание выполнено. Переходите к следующему этапу.");
    }

    static string GetCurrentTaskHintText()
    {
        if (Main.taskManager == null)
            return "Окно практики открыто. Введите наблюдение по породе/мощности и нажмите «Запись», затем «Таблица».";

        return "задание: " + Main.taskManager.current.ToCustomString();
    }

    void BuildPracticeTaskButtons(RectTransform parent)
    {
        var tasksGo = new GameObject("Practice_Tasks_Group", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        tasksGo.transform.SetParent(parent, false);
        _practiceTasksRoot = tasksGo.GetComponent<RectTransform>();

        var le = tasksGo.GetComponent<LayoutElement>();
        le.minHeight = 0f;
        le.preferredHeight = -1f;

        var v = tasksGo.GetComponent<VerticalLayoutGroup>();
        v.spacing = 5f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlHeight = true;
        v.childControlWidth = true;
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = true;
        v.padding = new RectOffset(0, 0, 2, 2);

        _practiceTaskButtons.Clear();
        for (var i = 0; i < PracticeTasks.Length; i++)
        {
            var def = PracticeTasks[i];
            var btn = CreatePanelButton(_practiceTasksRoot, def.Title, 21f, 42f, 46f);
            var colors = btn.colors;
            colors.normalColor = new Color(0.16f, 0.26f, 0.43f, 1f);
            colors.highlightedColor = new Color(0.23f, 0.36f, 0.58f, 1f);
            colors.pressedColor = new Color(0.12f, 0.2f, 0.34f, 1f);
            colors.selectedColor = colors.highlightedColor;
            btn.colors = colors;
            var tx = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tx != null)
            {
                tx.text = def.Title;
                tx.color = new Color(1f, 1f, 1f, 1f);
                tx.fontStyle = FontStyles.Bold;
                tx.enableWordWrapping = false;
                tx.overflowMode = TextOverflowModes.Ellipsis;
                tx.alignment = TextAlignmentOptions.Midline;
            }

            var index = def.Index1Based;
            btn.onClick.AddListener(() => OnPracticeTaskClicked(index));
            WireButtonHoverStatusBar(btn, def.Hint);
            _practiceTaskButtons.Add(btn);
        }
    }

    void SetPracticeTaskButtonsVisible(bool visible)
    {
        if (_practiceTasksRoot == null)
            return;
        _practiceTasksRoot.gameObject.SetActive(visible);
        RefreshSideMenuListLayout();
    }

    void OnPracticeTaskClicked(int taskIndex1Based)
    {
        if (_practicePanel == null)
            return;

        if (taskIndex1Based <= 0 || taskIndex1Based > PracticeTasks.Length)
            return;

        var def = PracticeTasks[taskIndex1Based - 1];
        Main.taskManager?.Select(def.Id);
        _practicePanel.Show();
        _practicePanel.ShowTaskHint(def.Index1Based, def.Hint);
        ShowStatusBar(def.Hint);
    }

    Transform ResolveTransform(ElementBinding def)
    {
        switch (def.Mode)
        {
            case ResolveMode.DrillRoot:
                return _drillRoot;
            case ResolveMode.ByName:
                return FindDeepChild(_drillRoot, def.ObjectName);
            default:
                return null;
        }
    }

    static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name))
            return null;
        if (root.name == name)
            return root;
        for (var i = 0; i < root.childCount; i++)
        {
            var c = FindDeepChild(root.GetChild(i), name);
            if (c != null)
                return c;
        }
        return null;
    }

    void ApplyHighlightVisuals(Transform root)
    {
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.materials;
            var orig = new Color[mats.Length];
            for (var i = 0; i < mats.Length; i++)
            {
                orig[i] = mats[i].color;
                mats[i].color = Color.Lerp(orig[i], HighlightTint, 0.62f);
            }
            _highlighted.Add((r, orig));
        }
    }

    void ClearHighlightVisuals()
    {
        foreach (var pair in _highlighted)
        {
            if (pair.renderer == null)
                continue;
            var mats = pair.renderer.materials;
            for (var i = 0; i < pair.originalColors.Length && i < mats.Length; i++)
                mats[i].color = pair.originalColors[i];
        }
        _highlighted.Clear();
    }

    static Sprite MakeWhiteSprite()
    {
        var t = Texture2D.whiteTexture;
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
    }
}

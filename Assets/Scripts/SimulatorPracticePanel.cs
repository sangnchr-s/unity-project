using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Нижнее информационное окно практики: подсказка слева, TMP Input справа от неё, три кнопки — Запись / Таблица / Очистка.
/// Таблица реализована как Image-подложка и именованные текстовые ячейки.
/// </summary>
public sealed class SimulatorPracticePanel : MonoBehaviour
{
    const int TableColumnCount = 5;
    const int MaxTableRows = 12;
    static readonly int[] DrillPowerValues = { 10, 20, 30 };

    [SerializeField] GameObject root;
    [SerializeField] TextMeshProUGUI hintText;
    [SerializeField] TMP_InputField resultInput;
    [SerializeField] Button powerButton;
    [SerializeField] TextMeshProUGUI powerButtonText;
    [SerializeField] Button recordButton;
    [SerializeField] Button tableButton;
    [SerializeField] Button clearButton;
    [SerializeField] Button nextTaskButton;
    [SerializeField] GameObject tableOverlayRoot;

    readonly List<TableRecord> _records = new List<TableRecord>();
    TMP_Text[] _headerCells;
    TMP_Text[,] _dataCells;
    int _currentTaskIndex;
    string _baseHintText = "";
    int _powerIndex;
    string _currentRockName = "мягкая порода";
    int _currentRockRequiredPower = 10;

    struct TableRecord
    {
        public int Number;
        public string Rock;
        public string Power;
        public string Time;
        public string Note;
        public string Result;
    }

    public bool IsVisible => root != null && root.activeSelf;
    public static bool IsTypingInPracticeInput { get; private set; }

    public static SimulatorPracticePanel Build(RectTransform canvasParent, TMP_FontAsset font, float reservedRightWidth = 0f)
    {
        const float BottomPanelFontScale = 1.5f;
        const float ButtonFontScale = 1.25f;

        var white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        var res = new TMP_DefaultControls.Resources
        {
            standard = white,
            background = white,
            inputField = white,
            knob = white,
            checkmark = white,
            dropdown = white,
            mask = white
        };

        var go = new GameObject("Practice_Window", typeof(RectTransform));
        go.layer = canvasParent.gameObject.layer;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(canvasParent, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        reservedRightWidth = Mathf.Max(0f, reservedRightWidth);
        rt.anchoredPosition = new Vector2(-reservedRightWidth * 0.5f, 12f);
        rt.sizeDelta = new Vector2(-48f - reservedRightWidth, 300f);

        var rootImg = go.AddComponent<Image>();
        rootImg.sprite = white;
        rootImg.type = Image.Type.Sliced;
        rootImg.color = new Color(0.98f, 0.98f, 1f, 0.98f);

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.45f, 0.1f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(go.transform, false);
        var rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = Vector2.zero;
        rowRt.anchorMax = Vector2.one;
        rowRt.offsetMin = new Vector2(16f, 16f);
        rowRt.offsetMax = new Vector2(-16f, -16f);

        var h = row.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 12f;
        h.childAlignment = TextAnchor.UpperLeft;
        h.childControlHeight = true;
        h.childControlWidth = true;
        h.childForceExpandHeight = true;
        h.childForceExpandWidth = false;
        h.padding = new RectOffset(0, 0, 0, 0);

        var hintGo = new GameObject("Hint_Text", typeof(RectTransform));
        hintGo.transform.SetParent(row.transform, false);
        var hintLe = hintGo.AddComponent<LayoutElement>();
        hintLe.flexibleWidth = 1.6f;
        hintLe.minWidth = 220f;
        var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
        if (font != null)
            hintTmp.font = font;
        hintTmp.fontSize = 22f * BottomPanelFontScale;
        hintTmp.color = new Color(0.12f, 0.12f, 0.14f, 1f);
        hintTmp.alignment = TextAlignmentOptions.TopLeft;
        hintTmp.enableWordWrapping = true;
        hintTmp.text = "Выберите этап практики в правом меню.";

        var inputRoot = TMP_DefaultControls.CreateInputField(res);
        inputRoot.name = "Input_Result";
        inputRoot.transform.SetParent(row.transform, false);
        var inputLe = inputRoot.AddComponent<LayoutElement>();
        inputLe.flexibleWidth = 0.9f;
        inputLe.minWidth = 160f;
        inputLe.preferredHeight = 120f;
        inputLe.minHeight = 120f;
        var inputRt = inputRoot.GetComponent<RectTransform>();
        inputRt.sizeDelta = new Vector2(280f, 120f);
        var inputField = inputRoot.GetComponent<TMP_InputField>();
        var placeholder = inputField.placeholder as Graphic;
        if (placeholder != null)
        {
            var ptmp = placeholder.GetComponent<TextMeshProUGUI>();
            if (ptmp != null)
            {
                ptmp.text = "Введите наблюдаемые результаты";
                if (font != null)
                    ptmp.font = font;
                ptmp.fontSize = 26f * BottomPanelFontScale;
            }
        }

        var tc = inputField.textComponent;
        if (font != null)
        {
            inputField.fontAsset = font;
            if (tc != null)
                tc.font = font;
        }
        if (tc != null)
            tc.fontSize = 30f * BottomPanelFontScale;

        var btnCol = new GameObject("Buttons_Column", typeof(RectTransform), typeof(VerticalLayoutGroup));
        btnCol.transform.SetParent(row.transform, false);
        var btnLe = btnCol.AddComponent<LayoutElement>();
        btnLe.minWidth = 230f;
        btnLe.preferredWidth = 245f;
        var v = btnCol.GetComponent<VerticalLayoutGroup>();
        v.spacing = 8f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlHeight = true;
        v.childControlWidth = true;
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = true;

        Button MakeBtn(string name, string label)
        {
            var bgo = TMP_DefaultControls.CreateButton(res);
            bgo.name = name;
            bgo.transform.SetParent(btnCol.transform, false);
            var le = bgo.AddComponent<LayoutElement>();
            le.minHeight = 46f;
            le.preferredHeight = 50f;
            var btnImage = bgo.GetComponent<Image>();
            if (btnImage != null)
                btnImage.color = new Color(0.96f, 0.97f, 1f, 1f);
            var btnOutline = bgo.GetComponent<Outline>();
            if (btnOutline == null)
                btnOutline = bgo.AddComponent<Outline>();
            btnOutline.effectColor = new Color(0.2f, 0.28f, 0.45f, 0.55f);
            btnOutline.effectDistance = new Vector2(1f, -1f);
            var txt = bgo.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = label;
                if (font != null)
                    txt.font = font;
                txt.fontSize = 18f * ButtonFontScale;
                txt.alignment = TextAlignmentOptions.Center;
                txt.enableWordWrapping = true;
            }

            return bgo.GetComponent<Button>();
        }

        var powerBtn = MakeBtn("Btn_Power", "Мощность: 10");
        var powerText = powerBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (powerText != null)
            powerText.fontSize = 18f * ButtonFontScale;
        var rec = MakeBtn("Btn_Record", "Запись");
        var tab = MakeBtn("Btn_Table", "Таблица");
        var clr = MakeBtn("Btn_Clear", "Очистка");
        var next = MakeBtn("Btn_NextTask", "Следующее задание");

        var recText = rec.GetComponentInChildren<TextMeshProUGUI>();
        if (recText != null)
            recText.fontSize = 22f * ButtonFontScale;
        var tabText = tab.GetComponentInChildren<TextMeshProUGUI>();
        if (tabText != null)
            tabText.fontSize = 24f * ButtonFontScale;
        var clrText = clr.GetComponentInChildren<TextMeshProUGUI>();
        if (clrText != null)
            clrText.fontSize = 22f * ButtonFontScale;
        var nextText = next.GetComponentInChildren<TextMeshProUGUI>();
        if (nextText != null)
            nextText.fontSize = 20f * ButtonFontScale;

        var overlay = new GameObject("Table_Overlay", typeof(RectTransform), typeof(Image));
        overlay.layer = canvasParent.gameObject.layer;
        overlay.transform.SetParent(canvasParent, false);
        var oRt = overlay.GetComponent<RectTransform>();
        oRt.anchorMin = Vector2.zero;
        oRt.anchorMax = Vector2.one;
        oRt.offsetMin = Vector2.zero;
        oRt.offsetMax = Vector2.zero;
        var oImg = overlay.GetComponent<Image>();
        oImg.sprite = white;
        oImg.color = new Color(0f, 0f, 0f, 0.55f);
        oImg.raycastTarget = true;

        var panel = new GameObject("Table_Image", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay.transform, false);
        var pRt = panel.GetComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.5f, 0.5f);
        pRt.anchorMax = new Vector2(0.5f, 0.5f);
        pRt.sizeDelta = new Vector2(1360f, 860f);
        var pImg = panel.GetComponent<Image>();
        pImg.sprite = white;
        pImg.color = new Color(1f, 1f, 1f, 0.98f);

        var titleGo = new GameObject("Table_Title", typeof(RectTransform));
        titleGo.transform.SetParent(panel.transform, false);
        var ttRt = titleGo.GetComponent<RectTransform>();
        ttRt.anchorMin = new Vector2(0f, 1f);
        ttRt.anchorMax = new Vector2(1f, 1f);
        ttRt.pivot = new Vector2(0.5f, 1f);
        ttRt.anchoredPosition = new Vector2(0f, -16f);
        ttRt.sizeDelta = new Vector2(-24f, 36f);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        if (font != null)
            titleText.font = font;
        titleText.fontSize = 24f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.1f, 0.1f, 0.12f, 1f);
        titleText.text = "Таблица результатов";

        var gridRoot = new GameObject("Table_Grid", typeof(RectTransform), typeof(GridLayoutGroup));
        gridRoot.transform.SetParent(panel.transform, false);
        var gRt = gridRoot.GetComponent<RectTransform>();
        gRt.anchorMin = new Vector2(0.5f, 0.5f);
        gRt.anchorMax = new Vector2(0.5f, 0.5f);
        gRt.sizeDelta = new Vector2(1260f, 430f);
        gRt.anchoredPosition = new Vector2(0f, -18f);
        var grid = gridRoot.GetComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = TableColumnCount;
        grid.cellSize = new Vector2(249f, 30f);
        grid.spacing = new Vector2(3f, 3f);
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.padding = new RectOffset(0, 0, 0, 0);

        var headerCells = new TMP_Text[TableColumnCount];
        var dataCells = new TMP_Text[MaxTableRows, TableColumnCount];
        var headerTitles = new[] { "Порода", "Мощность", "Заметка", "Результат", "Время" };

        for (var c = 0; c < TableColumnCount; c++)
        {
            var header = CreateTableCell(gridRoot.transform, $"Cell_Header_{c + 1}", font, true);
            header.text = headerTitles[c];
            headerCells[c] = header;
        }

        for (var r = 0; r < MaxTableRows; r++)
        for (var c = 0; c < TableColumnCount; c++)
        {
            var cell = CreateTableCell(gridRoot.transform, $"Cell_R{r + 1}_C{c + 1}", font, false);
            cell.text = "";
            dataCells[r, c] = cell;
        }

        var closeGo = TMP_DefaultControls.CreateButton(res);
        closeGo.name = "Btn_CloseTable";
        closeGo.transform.SetParent(panel.transform, false);
        var closeBg = closeGo.GetComponent<Image>();
        if (closeBg != null)
            closeBg.color = new Color(0.19f, 0.28f, 0.48f, 1f);
        var cRt = closeGo.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.5f, 0f);
        cRt.anchorMax = new Vector2(0.5f, 0f);
        cRt.pivot = new Vector2(0.5f, 0f);
        cRt.anchoredPosition = new Vector2(0f, 16f);
        cRt.sizeDelta = new Vector2(220f, 40f);
        var cTxt = closeGo.GetComponentInChildren<TextMeshProUGUI>();
        if (cTxt != null)
        {
            cTxt.text = "Закрыть";
            if (font != null)
                cTxt.font = font;
            cTxt.color = Color.white;
            cTxt.fontSize = 20f;
        }

        var closeBtn = closeGo.GetComponent<Button>();

        var comp = go.AddComponent<SimulatorPracticePanel>();
        comp.root = go;
        comp.hintText = hintTmp;
        comp.resultInput = inputField;
        comp.powerButton = powerBtn;
        comp.powerButtonText = powerText;
        comp.recordButton = rec;
        comp.tableButton = tab;
        comp.clearButton = clr;
        comp.nextTaskButton = next;
        comp.tableOverlayRoot = overlay;
        comp._headerCells = headerCells;
        comp._dataCells = dataCells;
        overlay.SetActive(false);

        comp.powerButton.onClick.AddListener(comp.OnPowerClicked);
        comp.recordButton.onClick.AddListener(comp.OnRecord);
        comp.tableButton.onClick.AddListener(comp.OnShowTable);
        comp.clearButton.onClick.AddListener(comp.OnClearTable);
        comp.nextTaskButton.onClick.AddListener(comp.OnNextTaskClicked);
        comp.resultInput.onSelect.AddListener(comp.OnInputSelected);
        comp.resultInput.onDeselect.AddListener(comp.OnInputDeselected);
        comp.resultInput.onEndEdit.AddListener(comp.OnInputEndEdit);
        closeBtn.onClick.AddListener(() => overlay.SetActive(false));
        comp.ApplyDrillPowerSelection();
        comp.RefreshTableCells();

        go.SetActive(false);
        return comp;
    }

    static TMP_Text CreateTableCell(Transform parent, string name, TMP_FontAsset font, bool header)
    {
        var cellGo = new GameObject(name, typeof(RectTransform), typeof(Image));
        cellGo.transform.SetParent(parent, false);
        var img = cellGo.GetComponent<Image>();
        img.color = header
            ? new Color(0.24f, 0.27f, 0.34f, 1f)
            : new Color(0.93f, 0.94f, 0.97f, 1f);
        img.raycastTarget = false;

        var txtGo = new GameObject($"{name}_Text", typeof(RectTransform));
        txtGo.transform.SetParent(cellGo.transform, false);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(3f, 2f);
        txtRt.offsetMax = new Vector2(-3f, -2f);
        var txt = txtGo.AddComponent<TextMeshProUGUI>();
        if (font != null)
            txt.font = font;
        txt.fontSize = header ? 23f : 21f;
        txt.enableWordWrapping = false;
        txt.overflowMode = TextOverflowModes.Ellipsis;
        txt.alignment = TextAlignmentOptions.Left;
        txt.color = header ? Color.white : new Color(0.1f, 0.1f, 0.12f, 1f);
        txt.raycastTarget = false;
        return txt;
    }

    public void Show()
    {
        if (root != null)
            root.SetActive(true);
        IsTypingInPracticeInput = false;
        SyncPowerIndexFromTelemetry();
        UpdateHintFromState();
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
        if (tableOverlayRoot != null)
            tableOverlayRoot.SetActive(false);
        IsTypingInPracticeInput = false;
    }

    public void SetCurrentTaskIndex(int taskIndex1Based)
    {
        _currentTaskIndex = taskIndex1Based;
        UpdateHintFromState();
    }

    void OnEnable()
    {
        Movement.OnPowerLevelChanged += OnPowerLevelChanged;
        Movement.OnRockContextChanged += OnRockContextChanged;
        IsTypingInPracticeInput = false;
        SyncPowerIndexFromTelemetry();
        UpdateHintFromState();
    }

    void OnDisable()
    {
        Movement.OnPowerLevelChanged -= OnPowerLevelChanged;
        Movement.OnRockContextChanged -= OnRockContextChanged;
        IsTypingInPracticeInput = false;
    }

    public void SetHint(string text)
    {
        _baseHintText = text ?? "";
        UpdateHintFromState();
    }

    public void ShowTaskHint(int taskIndex1Based, string taskHint)
    {
        _currentTaskIndex = taskIndex1Based;
        _baseHintText = taskHint ?? "";
        UpdateHintFromState();
    }

    void UpdateHintFromState()
    {
        if (hintText == null)
            return;
        if (_currentTaskIndex <= 0)
        {
            var defaultHint = string.IsNullOrWhiteSpace(_baseHintText)
                ? "Выберите этап практики в правом меню."
                : _baseHintText;
            hintText.text = $"{defaultHint}\n\n{BuildRockAndPowerHint()}";
            return;
        }

        var stageState = DrillPracticeTelemetry.DescribeTaskState(_currentTaskIndex);
        hintText.text = $"{_baseHintText}\n\n{stageState}\n\n{BuildRockAndPowerHint()}";
    }

    void OnRecord()
    {
        if (resultInput == null)
            return;

        ApplyDrillPowerSelection();
        var userValue = resultInput.text != null ? resultInput.text.Trim() : "";
        var powerValue = DrillPowerValues[_powerIndex];
        var note = string.IsNullOrEmpty(userValue)
            ? "без комментария"
            : userValue;
        var result = BuildPassabilityText(powerValue);

        var rec = new TableRecord
        {
            Number = _records.Count + 1,
            Rock = _currentRockName,
            Power = powerValue.ToString(),
            Time = DateTime.Now.ToString("HH:mm:ss"),
            Note = note,
            Result = result
        };

        _records.Add(rec);
        if (_records.Count > MaxTableRows)
        {
            _records.RemoveAt(0);
            for (var i = 0; i < _records.Count; i++)
            {
                var item = _records[i];
                item.Number = i + 1;
                _records[i] = item;
            }
        }

        resultInput.text = "";
        RefreshTableCells();
        UpdateHintFromState();
    }

    void OnPowerClicked()
    {
        _powerIndex++;
        if (_powerIndex >= DrillPowerValues.Length)
            _powerIndex = 0;
        ApplyDrillPowerSelection();
        UpdateHintFromState();
    }

    void ApplyDrillPowerSelection()
    {
        var powerValue = DrillPowerValues[Mathf.Clamp(_powerIndex, 0, DrillPowerValues.Length - 1)];
        DrillPracticeTelemetry.SetDrillPower(powerValue);
        if (powerButtonText != null)
            powerButtonText.text = $"Мощность: {powerValue}";
    }

    void OnShowTable()
    {
        if (tableOverlayRoot == null)
            return;
        RefreshTableCells();
        tableOverlayRoot.SetActive(true);
    }

    void OnClearTable()
    {
        _records.Clear();
        RefreshTableCells();
    }

    void RefreshTableCells()
    {
        if (_dataCells == null)
            return;

        for (var r = 0; r < MaxTableRows; r++)
        for (var c = 0; c < TableColumnCount; c++)
            _dataCells[r, c].text = "";

        for (var r = 0; r < _records.Count && r < MaxTableRows; r++)
        {
            var rec = _records[r];
            _dataCells[r, 0].text = rec.Rock;
            _dataCells[r, 1].text = rec.Power;
            _dataCells[r, 2].text = rec.Note;
            _dataCells[r, 3].text = rec.Result;
            _dataCells[r, 4].text = rec.Time;
        }

        if (_records.Count == 0)
        {
            _dataCells[0, 0].text = "нет записей";
            _dataCells[0, 1].text = "-";
            _dataCells[0, 2].text = "Введите текст и нажмите «Запись»";
            _dataCells[0, 3].text = "после этого откройте «Таблица»";
            _dataCells[0, 4].text = DateTime.Now.ToString("HH:mm:ss");
        }
    }

    void OnPowerLevelChanged(int power)
    {
        for (var i = 0; i < DrillPowerValues.Length; i++)
        {
            if (DrillPowerValues[i] != power)
                continue;
            _powerIndex = i;
            break;
        }

        ApplyDrillPowerSelection();
        UpdateHintFromState();
    }

    void OnRockContextChanged(string rockName, int requiredPower)
    {
        _currentRockName = string.IsNullOrWhiteSpace(rockName) ? "неизвестно" : rockName;
        _currentRockRequiredPower = Mathf.Max(10, requiredPower);
        UpdateHintFromState();
    }

    void SyncPowerIndexFromTelemetry()
    {
        var currentPower = DrillPracticeTelemetry.DrillPower;
        for (var i = 0; i < DrillPowerValues.Length; i++)
        {
            if (DrillPowerValues[i] != currentPower)
                continue;
            _powerIndex = i;
            break;
        }

        ApplyDrillPowerSelection();
    }

    string BuildRockAndPowerHint()
    {
        var power = DrillPowerValues[Mathf.Clamp(_powerIndex, 0, DrillPowerValues.Length - 1)];
        var result = BuildPassabilityText(power);
        return $"Порода: {_currentRockName}. Текущая мощность: {power}. {result}\nЗапишите вручную наблюдение в поле справа и нажмите «Запись».";
    }

    string BuildPassabilityText(int power)
    {
        if (power < _currentRockRequiredPower)
            return $"Проходимость затруднена (рекомендуется от {_currentRockRequiredPower}).";
        return "Проходимость достаточная, бур проходит породу.";
    }

    void OnNextTaskClicked()
    {
        if (Main.taskManager == null)
            return;

        Main.taskManager.SelectNext();
    }

    void OnInputSelected(string _)
    {
        IsTypingInPracticeInput = true;
    }

    void OnInputDeselected(string _)
    {
        IsTypingInPracticeInput = false;
    }

    void OnInputEndEdit(string _)
    {
        IsTypingInPracticeInput = false;
    }
}

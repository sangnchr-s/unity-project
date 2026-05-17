using UnityEngine;

/// <summary>
/// Перемещение камеры по пресетам (Position + Rotation).
/// Рекомендуется назначить <see cref="presetSource"/> — тогда ракурсы сохраняются между сессиями.
/// Захват ракурса в Play Mode: удерживайте Shift и нажмите 1…8 (верхний ряд клавиатуры) — в слот 0…7.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public sealed class LecturePresetCameraController : MonoBehaviour
{
    [System.Serializable]
    public struct CameraPreset
    {
        [Tooltip("Подпись в Inspector")]
        public string label;
        public Vector3 position;
        public Vector3 rotation;
    }

    [Header("Скорость перемещения")]
    [SerializeField] float moveDuration = 1.15f;

    [Header("Хранение пресетов")]
    [Tooltip("Create → Simulator → Пресеты камеры буровой. Shift+1…8 в Play записывают в этот файл — потом Ctrl+S.")]
    [SerializeField] DrillCameraPresetsAsset presetSource;

    [Tooltip("В Play Mode: Shift + 1…8 (не NumPad) — записать текущую позицию Main Camera в пресет.")]
    [SerializeField] bool enableShiftDigitCapture = true;

    [Header("Старт сцены")]
    [Tooltip("Если включено — при входе в Play сразу эта позиция и угол (мир). Имеет приоритет над «пресет 0 на старте». Орбита подстраивается под ракурс.")]
    [SerializeField] bool useBootstrapWorldPose;

    [SerializeField] Vector3 bootstrapWorldPosition = new Vector3(-10.36f, 1f, -34.31f);
    [SerializeField] Vector3 bootstrapWorldEuler = new Vector3(17.224f, 47.029f, 0f);

    [Tooltip("Если включено и пресет 0 заполнен — при старте ракурс из ассета (если выключен старт из bootstrap).")]
    [SerializeField] bool applyFirstPresetOnPlay = true;

    [Header("Запасной список (если Preset Source не назначен)")]
    public CameraPreset[] presets = new CameraPreset[]
    {
        new CameraPreset { label = "Буровая голова" },
        new CameraPreset { label = "Малый бур L01" },
        new CameraPreset { label = "Малый бур L02" },
        new CameraPreset { label = "Малый бур R01" },
        new CameraPreset { label = "Малый бур R02" },
        new CameraPreset { label = "Прожектор левый" },
        new CameraPreset { label = "Прожектор правый" },
        new CameraPreset { label = "Корпус и ходовая" },
    };

    OrbitCameraAroundTarget _orbit;

    bool _move;
    bool _syncOrbitToDrillOnArrival;
    Vector3 _startPosition;
    Quaternion _startRotation;
    Vector3 _needPosition;
    Quaternion _needRotation;
    float _moveTime;

    /// <summary>Камера в момент первого кадра после инициализации (пресет 0 или орбита по умолчанию).</summary>
    Vector3 _sceneStartPosition;
    Quaternion _sceneStartRotation;

    bool _adoptOrbitFromCurrentPoseAfterMove;

    public bool IsMovingToPreset => _move;

    void Awake()
    {
        _orbit = GetComponent<OrbitCameraAroundTarget>();
    }

    void Start()
    {
        if (useBootstrapWorldPose)
            TryApplyBootstrapWorldPose();
        else if (applyFirstPresetOnPlay)
            TryApplyFirstPresetAsInitialView();

        _sceneStartPosition = transform.position;
        _sceneStartRotation = transform.rotation;
    }

    void TryApplyBootstrapWorldPose()
    {
        transform.SetPositionAndRotation(bootstrapWorldPosition, Quaternion.Euler(bootstrapWorldEuler));

        if (_orbit == null)
            return;

        var pivot = _orbit.GetDefaultOrbitPivot();
        if (pivot != null)
            _orbit.AdoptWorldPoseIntoOrbit(pivot);
        else
            _orbit.LockPresetUntilUserAdjusts();
    }

    bool TryApplyFirstPresetAsInitialView()
    {
        var arr = GetActivePresetsArray();
        if (arr == null || arr.Length == 0)
            return false;

        var p = arr[0];
        if (p.position == Vector3.zero && p.rotation == Vector3.zero)
            return false;

        transform.SetPositionAndRotation(p.position, Quaternion.Euler(p.rotation));

        if (_orbit == null)
            return true;

        var pivot = _orbit.GetDefaultOrbitPivot();
        if (pivot != null)
            _orbit.AdoptWorldPoseIntoOrbit(pivot);
        else
            _orbit.LockPresetUntilUserAdjusts();

        return true;
    }

    void Update()
    {
        if (_move)
        {
            _moveTime += Time.deltaTime / Mathf.Max(0.05f, moveDuration);
            float a = _moveTime >= 1f ? 1f : Mathf.SmoothStep(0f, 1f, _moveTime);
            transform.position = Vector3.Lerp(_startPosition, _needPosition, a);
            transform.rotation = Quaternion.Slerp(_startRotation, _needRotation, a);

            if (_moveTime >= 1f)
            {
                _move = false;
                SyncOrbitAfterMove();
            }

            return;
        }

        TryPresetCaptureInput();
    }

    CameraPreset[] GetActivePresetsArray()
    {
        if (presetSource != null && presetSource.presets != null && presetSource.presets.Length > 0)
            return presetSource.presets;
        return presets;
    }

    void TryPresetCaptureInput()
    {
        if (SimulatorPracticePanel.IsTypingInPracticeInput)
            return;

        if (!enableShiftDigitCapture || !Application.isPlaying)
            return;
        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            return;

        var arr = GetActivePresetsArray();
        for (var i = 0; i < 8 && i < arr.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                CaptureCurrentViewToPreset(i);
                return;
            }
        }
    }

    /// <summary>Записать текущую позицию и поворот этой камеры в пресет с индексом index.</summary>
    public void CaptureCurrentViewToPreset(int index)
    {
        var arr = GetActivePresetsArray();
        if (arr == null || index < 0 || index >= arr.Length)
            return;

        var p = arr[index];
        p.position = transform.position;
        p.rotation = transform.eulerAngles;
        arr[index] = p;

#if UNITY_EDITOR
        if (presetSource != null)
            UnityEditor.EditorUtility.SetDirty(presetSource);
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        Debug.Log(
            $"[Камера] Пресет {index} «{p.label}»: Position {p.position}, Rotation {p.rotation}. " +
            (presetSource != null
                ? "Ассет помечен как изменённый — сохраните проект (Ctrl+S / Cmd+S)."
                : "Назначьте Preset Source (ScriptableObject), иначе после выхода из Play данные сбросятся."));
    }

    void SyncOrbitAfterMove()
    {
        if (_orbit == null)
            return;
        if (_adoptOrbitFromCurrentPoseAfterMove)
        {
            _adoptOrbitFromCurrentPoseAfterMove = false;
            var pivot = _orbit.GetDefaultOrbitPivot();
            if (pivot != null)
                _orbit.AdoptWorldPoseIntoOrbit(pivot);
            else
                _orbit.LockPresetUntilUserAdjusts();
            return;
        }

        if (_syncOrbitToDrillOnArrival)
            _orbit.RestoreDefaultOrbitTarget();
        else
            _orbit.LockPresetUntilUserAdjusts();
    }

    void BeginMove(Vector3 pos, Quaternion rot, bool syncOrbitToDrillOnArrival,
        bool adoptOrbitFromCurrentPoseAfterMove = false)
    {
        _move = true;
        _syncOrbitToDrillOnArrival = syncOrbitToDrillOnArrival;
        _adoptOrbitFromCurrentPoseAfterMove = adoptOrbitFromCurrentPoseAfterMove;
        _startPosition = transform.position;
        _startRotation = transform.rotation;
        _needPosition = pos;
        _needRotation = rot;
        _moveTime = 0f;
    }

    /// <summary>Вернуть камеру в тот же ракурс, что сразу после старта сцены (кнопка «ПРАКТИКА»).</summary>
    public void MoveView_Home()
    {
        BeginMove(_sceneStartPosition, _sceneStartRotation, syncOrbitToDrillOnArrival: false,
            adoptOrbitFromCurrentPoseAfterMove: true);
    }

    public void MoveToPreset(int index)
    {
        var arr = GetActivePresetsArray();
        if (arr == null || index < 0 || index >= arr.Length)
            return;

        var p = arr[index];
        if (p.position == Vector3.zero && p.rotation == Vector3.zero)
        {
            Debug.LogWarning($"[Камера] Ракурс «{p.label}» пустой — наведите камеру в Play и нажмите Shift+{index + 1}.");
            return;
        }

        BeginMove(p.position, Quaternion.Euler(p.rotation), syncOrbitToDrillOnArrival: false);
    }

    /// <summary>
    /// Плавно перевести камеру в заданную мировую позицию/поворот.
    /// </summary>
    public void MoveToWorldPose(Vector3 worldPosition, Vector3 worldEuler, bool adoptOrbitFromCurrentPoseAfterMove = true)
    {
        BeginMove(
            worldPosition,
            Quaternion.Euler(worldEuler),
            syncOrbitToDrillOnArrival: false,
            adoptOrbitFromCurrentPoseAfterMove: adoptOrbitFromCurrentPoseAfterMove);
    }
}

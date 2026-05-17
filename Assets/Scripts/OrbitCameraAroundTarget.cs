using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Орбитальная камера вокруг точки: ПКМ (и опционально СКМ) — вращение, колесо — зум, WASD — сдвиг центра.
/// Если <see cref="target"/> не задан, ищется объект <see cref="autoOrbitTargetName"/> (по умолчанию бур).
/// </summary>
[DefaultExecutionOrder(-50)]
public sealed class OrbitCameraAroundTarget : MonoBehaviour
{
    [Header("Центр орбиты")]
    [Tooltip("Если задан — орбита вокруг этого объекта. Иначе при старте ищется объект по имени ниже.")]
    public Transform target;

    [SerializeField] string autoOrbitTargetName = "Drill_Prefab";

    [Header("Старт рядом с центром")]
    [Tooltip("Дистанция при первом кадре (если не подменяется пресетом 0 на старте). Чуть дальше — удобнее обзор.")]
    [SerializeField] float startDistance = 9f;
    [SerializeField] float startYaw = 35f;
    [SerializeField] float startPitch = 22f;

    [Header("Скорости")]
    public float rotationSpeed = 300f;
    public float sideMoveSpeed = 7f;

    [Header("Зум")]
    [Tooltip("Множитель за один щелчок колеса (чем ближе к 1, тем плавнее).")]
    [SerializeField] float zoomStepPerTick = 0.14f;
    public float minDistance = 1.2f;
    [Tooltip("Максимальная дистанция до центра орбиты. Мало — камера быстро «упирается» при отъезде.")]
    public float maxDistance = 160f;

    [Header("Ограничения сдвига (локально относительно бура, WASD)")]
    [Tooltip("Границы смещения центра орбиты по осям объекта (метры).")]
    public float minSideX = -45f;
    public float maxSideX = 45f;
    public float minSideZ = -45f;
    public float maxSideZ = 45f;

    [Tooltip("При отдалении колесом лимиты WASD умножаются (1 = при maxDistance в 1+раз больше коробки). 0 = без расширения.")]
    [SerializeField][Range(0f, 4f)] float panLimitExtraAtFullZoom = 1.35f;

    [Header("Ограничения по углу наклона")]
    [Tooltip("Слишком узкий диапазон — вращение «упирается» вверх/вниз раньше времени.")]
    public float minPitch = -88f;
    public float maxPitch = 88f;

    [Header("Синхронизация после пресета камеры")]
    [Tooltip("На каком расстоянии по лучу взгляда ставится виртуальная точка орбиты после прилёта к пресету (метры). Не должно быть равно старому «расстоянию орбиты» — иначе была ошибка скачка.")]
    [SerializeField] float adoptFocusDepth = 14f;

    [Header("Удобство ввода")]
    [Tooltip("СКМ — то же, что ПКМ: вращение орбиты (удобно, если ПКМ занят или неудобен на тачпаде).")]
    [SerializeField] bool useMiddleMouseForOrbit = true;

    [Tooltip("Кадров без зума и WASD-сдвига сразу после синхронизации орбиты (защита от шумного тачпада). 0 — полный контроль с первого кадра.")]
    [SerializeField] int suppressZoomPanFramesAfterSync = 2;

    float _yaw;
    float _pitch = 20f;
    float _distance = 5f;
    Vector2 _sideOffsetXZ;

    /// <summary>Мировая точка орбиты, когда target не задан и объект не найден.</summary>
    Vector3 _freePivot;

    Transform _orbitCenter;
    Transform _pivotOverride;
    LecturePresetCameraController _lecturePreset;

    /// <summary>После пресета игнорируем зум/WASD — иначе тачпад Mac даёт скачок расстояния до maxDistance.</summary>
    int _skipZoomAndPanFrames;

    int SuppressFramesAfterSync => Mathf.Max(0, suppressZoomPanFramesAfterSync);

    /// <summary>На полной дистанции зума расширяем «коробку» WASD, чтобы не упираться сразу.</summary>
    float PanLimitScale()
    {
        if (panLimitExtraAtFullZoom <= 0f || maxDistance <= minDistance + 0.01f)
            return 1f;
        var t = Mathf.Clamp01((_distance - minDistance) / (maxDistance - minDistance));
        return 1f + panLimitExtraAtFullZoom * t;
    }

    bool OrbitRotateMouseHeld()
    {
        if (Input.GetMouseButton(1))
            return true;
        return useMiddleMouseForOrbit && Input.GetMouseButton(2);
    }

    /// <summary>
    /// После прилёта к пресету не вызываем <see cref="ApplyOrbit"/> — иначе модель yaw/pitch не совпадает с вашим Quaternion пресета и камера «улетает».
    /// Орбита включается снова после ПКМ / колеса / WASD (подбор углов под текущий кадр).
    /// </summary>
    bool _presetPoseLocked;

    /// <summary>Точка, вокруг которой строится орбита (корень установки или выбранный элемент).</summary>
    public Transform OrbitPivot => _pivotOverride != null ? _pivotOverride : _orbitCenter;

    /// <summary>Корень буровой для <see cref="AdoptWorldPoseIntoOrbit"/> (Inspector target или поиск по имени).</summary>
    public Transform GetDefaultOrbitPivot()
    {
        if (target != null)
            return target;
        if (!string.IsNullOrEmpty(autoOrbitTargetName))
        {
            var go = GameObject.Find(autoOrbitTargetName);
            if (go != null)
                return go.transform;
        }

        return _orbitCenter;
    }

    void Reset()
    {
        if (Camera.main != null)
        {
            transform.position = Camera.main.transform.position;
            transform.rotation = Camera.main.transform.rotation;
        }
    }

    void Start()
    {
        _lecturePreset = GetComponent<LecturePresetCameraController>();
        _pivotOverride = null;
        _orbitCenter = target;
        if (_orbitCenter == null && !string.IsNullOrEmpty(autoOrbitTargetName))
        {
            var go = GameObject.Find(autoOrbitTargetName);
            if (go != null)
                _orbitCenter = go.transform;
        }

        if (_orbitCenter != null)
        {
            _distance = Mathf.Clamp(startDistance, minDistance, maxDistance);
            _yaw = startYaw;
            _pitch = Mathf.Clamp(startPitch, minPitch, maxPitch);
            _sideOffsetXZ = Vector2.zero;
            ApplyOrbit();
            return;
        }

        _distance = Mathf.Clamp(startDistance, minDistance, maxDistance);
        _freePivot = transform.position + transform.forward * _distance;

        var toCam = transform.position - _freePivot;
        if (toCam.sqrMagnitude > 0.0001f)
            _distance = Mathf.Clamp(toCam.magnitude, minDistance, maxDistance);

        var flat = new Vector3(toCam.x, 0f, toCam.z);
        if (flat.sqrMagnitude > 0.0001f)
            _yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;

        _pitch = Mathf.Clamp(
            Mathf.Asin(Mathf.Clamp(toCam.normalized.y, -1f, 1f)) * Mathf.Rad2Deg,
            minPitch, maxPitch);

        ApplyOrbit();
    }

    void LateUpdate()
    {
        if (SimulatorPracticePanel.IsTypingInPracticeInput)
            return;

        if (_lecturePreset != null && _lecturePreset.IsMovingToPreset)
            return;

        if (_presetPoseLocked)
        {
            if (HasOrbitUserIntent())
            {
                _presetPoseLocked = false;
                _skipZoomAndPanFrames = SuppressFramesAfterSync;
                FitFreeOrbitAnglesToCurrentCamera();
            }
            return;
        }

        var skipZoomPan = _skipZoomAndPanFrames > 0;
        if (skipZoomPan)
            _skipZoomAndPanFrames--;

        HandleOrbit();
        if (!skipZoomPan)
        {
            HandleZoom();
            HandleSideMove();
        }

        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        _distance = Mathf.Clamp(_distance, minDistance, maxDistance);

        var panK = PanLimitScale();
        _sideOffsetXZ.x = Mathf.Clamp(_sideOffsetXZ.x, minSideX * panK, maxSideX * panK);
        _sideOffsetXZ.y = Mathf.Clamp(_sideOffsetXZ.y, minSideZ * panK, maxSideZ * panK);

        ApplyOrbit();
    }

    bool HasOrbitUserIntent()
    {
        // Клик/скролл по UI (в т.ч. трекпад при «тапе» по кнопке) не должен снимать блок пресета —
        // иначе в том же LateUpdate вызывается ApplyOrbit() и камера «отскакивает» к старой орбите.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1))
            return false;

        return OrbitRotateMouseHeld()
               || Mathf.Abs(Input.mouseScrollDelta.y) > 0.02f
               || GetArrowPanInput().sqrMagnitude > 0.0004f;
    }

    Vector2 GetArrowPanInput()
    {
        float h = 0f;
        float v = 0f;
        if (Input.GetKey(KeyCode.LeftArrow))
            h -= 1f;
        if (Input.GetKey(KeyCode.RightArrow))
            h += 1f;
        if (Input.GetKey(KeyCode.DownArrow))
            v -= 1f;
        if (Input.GetKey(KeyCode.UpArrow))
            v += 1f;
        return new Vector2(h, v);
    }

    /// <summary>После UI-пресета держим ракурс, пока пользователь не тронет управление.</summary>
    public void LockPresetUntilUserAdjusts()
    {
        _presetPoseLocked = true;
    }

    /// <summary>Подбираем yaw/pitch так, чтобы формула орбиты совпала с текущей позицией камеры (без скачка).</summary>
    void FitFreeOrbitAnglesToCurrentCamera()
    {
        Vector3 savedPos = transform.position;
        Quaternion savedRot = transform.rotation;
        Vector3 fwd = savedRot * Vector3.forward;

        float depth = Mathf.Clamp(adoptFocusDepth, minDistance, maxDistance);
        _orbitCenter = null;
        _pivotOverride = null;
        _freePivot = savedPos + fwd * depth;
        _distance = depth;
        _sideOffsetXZ = Vector2.zero;

        float bestErr = float.MaxValue;
        float bestY = _yaw;
        float bestP = _pitch;
        const float coarse = 3f;
        for (float p = minPitch; p <= maxPitch; p += coarse)
        {
            for (float y = -180f; y < 180f; y += coarse)
            {
                float err = OrbitPositionErrorSq(_freePivot, p, y, _distance, savedPos);
                if (err < bestErr)
                {
                    bestErr = err;
                    bestY = y;
                    bestP = p;
                }
            }
        }

        for (float p = bestP - 4f; p <= bestP + 4f; p += 0.5f)
        {
            float pc = Mathf.Clamp(p, minPitch, maxPitch);
            for (float y = bestY - 4f; y <= bestY + 4f; y += 0.5f)
            {
                float err = OrbitPositionErrorSq(_freePivot, pc, y, _distance, savedPos);
                if (err < bestErr)
                {
                    bestErr = err;
                    bestY = y;
                    bestP = pc;
                }
            }
        }

        _yaw = bestY;
        _pitch = Mathf.Clamp(bestP, minPitch, maxPitch);
        ApplyOrbit();

        if (bestErr > 0.0004f)
            Debug.LogWarning($"[OrbitCamera] Подгонка ракурса: остаточная ошибка позиции²={bestErr:F6}. Проверьте Min/Max Pitch на камере.");
    }

    static float OrbitPositionErrorSq(Vector3 freePivot, float pitch, float yaw, float dist, Vector3 targetPos)
    {
        var pos = freePivot + Quaternion.Euler(pitch, yaw, 0f) * (Vector3.back * dist);
        return (pos - targetPos).sqrMagnitude;
    }

    static float DrillPivotOrbitErrorSq(Vector3 pivotWorld, float pitch, float yaw, float dist, Vector3 targetCamPos)
    {
        var pos = pivotWorld + Quaternion.Euler(pitch, yaw, 0f) * (Vector3.back * dist);
        return (pos - targetCamPos).sqrMagnitude;
    }

    /// <summary>
    /// Подобрать yaw/pitch по той же формуле, что <see cref="ApplyOrbit"/> (поиск по сетке), чтобы позиция не «прыгала».
    /// Старый вариант через Atan2(dir) не совпадал с Euler(pitch,yaw) и давал телепорт после UI.
    /// </summary>
    public void RecalculateOrbitAnglesFromCurrentTransform(Transform pivotRoot)
    {
        if (pivotRoot == null)
            return;

        _pivotOverride = null;
        _orbitCenter = pivotRoot;

        Vector3 savedPos = transform.position;
        Vector3 pivotPos = pivotRoot.position;
        Vector3 delta = savedPos - pivotPos;
        float rawMag = delta.magnitude;
        if (rawMag < 0.001f)
        {
            ApplyOrbit();
            _skipZoomAndPanFrames = SuppressFramesAfterSync;
            return;
        }

        // Цель на орбите с уже ограниченной дистанцией; иначе после Clamp(distance) точка пресета не лежит на сфере и ошибка остаётся большой.
        Vector3 dir = delta / rawMag;
        _distance = Mathf.Clamp(rawMag, minDistance, maxDistance);
        Vector3 targetCamPos = pivotPos + dir * _distance;
        _sideOffsetXZ = Vector2.zero;

        float bestErr = float.MaxValue;
        float bestY = _yaw;
        float bestP = _pitch;
        const float coarse = 3f;
        for (float p = minPitch; p <= maxPitch; p += coarse)
        {
            for (float y = -180f; y < 180f; y += coarse)
            {
                float err = DrillPivotOrbitErrorSq(pivotPos, p, y, _distance, targetCamPos);
                if (err < bestErr)
                {
                    bestErr = err;
                    bestY = y;
                    bestP = p;
                }
            }
        }

        for (float p = bestP - 4f; p <= bestP + 4f; p += 0.5f)
        {
            float pc = Mathf.Clamp(p, minPitch, maxPitch);
            for (float y = bestY - 4f; y <= bestY + 4f; y += 0.5f)
            {
                float err = DrillPivotOrbitErrorSq(pivotPos, pc, y, _distance, targetCamPos);
                if (err < bestErr)
                {
                    bestErr = err;
                    bestY = y;
                    bestP = pc;
                }
            }
        }

        _yaw = bestY;
        _pitch = Mathf.Clamp(bestP, minPitch, maxPitch);
        ApplyOrbit();

        if (bestErr > 0.0004f)
            Debug.LogWarning($"[OrbitCamera] Подгонка орбиты к буру: остаточная ошибка позиции²={bestErr:F6}.");

        _skipZoomAndPanFrames = SuppressFramesAfterSync;
    }

    void ApplyOrbit()
    {
        Vector3 pivot = GetPivot();

        var orbitRot = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.position = pivot + orbitRot * (Vector3.back * _distance);
        transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
    }

    Vector3 GetPivot()
    {
        var pivot = OrbitPivot;
        if (pivot != null)
        {
            return pivot.position
                   + pivot.right * _sideOffsetXZ.x
                   + pivot.forward * _sideOffsetXZ.y;
        }

        var yawOnly = Quaternion.Euler(0f, _yaw, 0f);
        return _freePivot + yawOnly * new Vector3(_sideOffsetXZ.x, 0f, _sideOffsetXZ.y);
    }

    /// <summary>Временно смотреть на выбранный узел установки (щелчок по списку).</summary>
    public void FocusOnElement(Transform elementPivot, float yawDeg, float pitchDeg, float distance)
    {
        if (elementPivot == null)
            return;
        _pivotOverride = elementPivot;
        SnapOrbitAngles(yawDeg, pitchDeg, distance);
    }

    public void SnapOrbitAngles(float yawDeg, float pitchDeg, float distance)
    {
        _yaw = yawDeg;
        _pitch = Mathf.Clamp(pitchDeg, minPitch, maxPitch);
        _distance = Mathf.Clamp(distance, minDistance, maxDistance);
        _sideOffsetXZ = Vector2.zero;
        ApplyOrbit();
    }

    /// <summary>Вернуть центр орбиты к корню установки.</summary>
    public void ClearOrbitPivotOverride()
    {
        _pivotOverride = null;
        ApplyOrbit();
    }

    /// <summary>После перемещения камеры по заданным мировым Position/Rotation (лекция) — подстроить yaw/pitch/distance орбиты.</summary>
    public void AdoptWorldPoseIntoOrbit(Transform pivotRoot)
    {
        RecalculateOrbitAnglesFromCurrentTransform(pivotRoot);
    }

    /// <summary>
    /// Подстроить орбиту под текущий ракурс. Та же геометрия, что в <see cref="Start"/> для режима без центра —
    /// иначе углы из Atan2(dir) не совпадают с <see cref="ApplyOrbit"/> (Euler pitch,yaw) и камера «улетает».
    /// </summary>
    public void AdoptCurrentCameraPose()
    {
        _pivotOverride = null;
        _orbitCenter = null;

        float depth = Mathf.Clamp(adoptFocusDepth, minDistance, maxDistance);
        _freePivot = transform.position + transform.forward * depth;

        var toCam = transform.position - _freePivot;
        if (toCam.sqrMagnitude > 0.0001f)
            _distance = Mathf.Clamp(toCam.magnitude, minDistance, maxDistance);

        _sideOffsetXZ = Vector2.zero;

        var flat = new Vector3(toCam.x, 0f, toCam.z);
        if (flat.sqrMagnitude > 0.0001f)
            _yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;

        _pitch = Mathf.Clamp(
            Mathf.Asin(Mathf.Clamp(toCam.normalized.y, -1f, 1f)) * Mathf.Rad2Deg,
            minPitch, maxPitch);

        _skipZoomAndPanFrames = SuppressFramesAfterSync;
        ApplyOrbit();
    }

    /// <summary>Восстановить орбиту вокруг корня установки (после «ПРАКТИКА» / домашнего ракурса).</summary>
    public void RestoreDefaultOrbitTarget()
    {
        _presetPoseLocked = false;
        _pivotOverride = null;
        if (!string.IsNullOrEmpty(autoOrbitTargetName))
        {
            var go = GameObject.Find(autoOrbitTargetName);
            if (go != null)
                _orbitCenter = go.transform;
        }

        if (_orbitCenter != null)
            AdoptWorldPoseIntoOrbit(_orbitCenter);
        else
            AdoptCurrentCameraPose();
    }

    void HandleOrbit()
    {
        if (!OrbitRotateMouseHeld())
            return;

        float mx = Input.GetAxisRaw("Mouse X");
        float my = Input.GetAxisRaw("Mouse Y");

        _yaw += mx * rotationSpeed * Time.unscaledDeltaTime;
        _pitch -= my * rotationSpeed * Time.unscaledDeltaTime;
    }

    void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.0001f)
            return;

        float factor = 1f - Mathf.Sign(scroll) * zoomStepPerTick;
        factor = Mathf.Clamp(factor, 0.5f, 1.5f);
        _distance *= factor;
    }

    void HandleSideMove()
    {
        Vector2 panInput = GetArrowPanInput();
        float h = panInput.x;
        float v = panInput.y;

        if (Mathf.Abs(h) < 0.0001f && Mathf.Abs(v) < 0.0001f)
            return;

        // Чуть быстрее сдвиг при отдалении — иначе та же «коробка» ощущается тугой.
        var distMul = 1f + 0.35f * Mathf.Clamp01((_distance - minDistance) / Mathf.Max(0.01f, maxDistance - minDistance));
        var delta = new Vector2(h, v) * (sideMoveSpeed * distMul) * Time.unscaledDeltaTime;
        _sideOffsetXZ += delta;
    }
}

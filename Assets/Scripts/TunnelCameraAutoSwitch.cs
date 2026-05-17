using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Автоматически и плавно переводит камеру на ракурс тоннеля,
/// когда буровая достигает заданной Z-границы.
/// </summary>
[DisallowMultipleComponent]
public sealed class TunnelCameraAutoSwitch : MonoBehaviour
{
    [Header("Отслеживаемый объект")]
    [SerializeField] Transform drillTransform;
    [SerializeField] Movement drillMovement;

    [Header("Порог входа в тоннель")]
    [SerializeField] bool useMovementTaskBorderIfAvailable = true;
    [SerializeField] float tunnelEntryZ = -28f;
    [SerializeField] bool triggerOnlyWhenMovingForward = true;
    [SerializeField] bool oneShot = true;
    [SerializeField] float resetHysteresis = 1.5f;

    [Header("Ракурс тоннеля (из твоего замера)")]
    [SerializeField] Vector3 tunnelCameraPosition = new Vector3(-7.323f, 1.028f, -31.248f);
    [SerializeField] Vector3 tunnelCameraEuler = new Vector3(15.251f, 6.694f, 0f);

    [Header("Сопровождение бура в тоннеле")]
    [SerializeField] bool followDrillAfterSwitch = true;
    [SerializeField] bool allowManualBreakFollow = true;

    [Header("Финал: пролет камеры назад")]
    [SerializeField] bool flyBackAtTunnelFinish = true;
    [SerializeField] bool useMovementFinishIfAvailable = true;
    [SerializeField] float tunnelFinishZ = -5.23f;
    [SerializeField] bool useCustomThreePointFlyback = true;
    [SerializeField] float customFlybackDuration = 2.2f;
    [SerializeField] Vector3 flybackStartPoint = new Vector3(-7.395f, 0.727f, 2.233f);
    [SerializeField] Vector3 flybackMidPoint = new Vector3(-28.562f, 9.279f, -5.328f);
    [SerializeField] Vector3 flybackEndPoint = new Vector3(-7.167f, 0.74f, -29.904f);
    [SerializeField] Vector3 flybackStartEuler = new Vector3(10.031f, 189.38f, 0f);
    [SerializeField] Vector3 flybackMidEuler = new Vector3(31.166f, 111.938f, 0f);
    [SerializeField] Vector3 flybackEndEuler = new Vector3(7.355f, 6.764f, 0f);

    LecturePresetCameraController _presetCam;
    bool _switched;
    bool _followActive;
    bool _finishFlyTriggered;
    bool _customFlybackRunning;
    bool _hasPrevZ;
    float _prevZ;
    Vector3 _followOffset;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttachToMainCamera()
    {
        if (Camera.main == null)
            return;
        if (Camera.main.GetComponent<TunnelCameraAutoSwitch>() != null)
            return;

        Camera.main.gameObject.AddComponent<TunnelCameraAutoSwitch>();
    }

    void Awake()
    {
        _presetCam = GetComponent<LecturePresetCameraController>();
        if (_presetCam == null && Camera.main != null)
            _presetCam = Camera.main.GetComponent<LecturePresetCameraController>();

        if (drillMovement == null)
            drillMovement = FindObjectOfType<Movement>();

        if (drillTransform == null && drillMovement != null)
            drillTransform = drillMovement.transform;

        if (useMovementTaskBorderIfAvailable && drillMovement != null)
            tunnelEntryZ = drillMovement.taskBorderZ;
        if (useMovementFinishIfAvailable && drillMovement != null)
            tunnelFinishZ = drillMovement.taskFinishZ;

        svet_v_konce_tonela = GameObject.Find("svet");
        if (svet_v_konce_tonela != null)
            svet_v_konce_tonela.SetActive(false);
        Debug.Log($"[TunnelCamera] Активно. Порог Z={tunnelEntryZ:F3}, oneShot={oneShot}.");
    }

    void Update()
    {
        if (_presetCam == null || drillTransform == null)
            return;

        var currentZ = drillTransform.position.z;
        TryStartFinishFlyBack(currentZ);
        var movingForward = !_hasPrevZ || currentZ > _prevZ + 0.0005f;
        _prevZ = currentZ;
        _hasPrevZ = true;

        if (!oneShot && _switched && currentZ < tunnelEntryZ - Mathf.Abs(resetHysteresis))
        {
            _switched = false;
            _followActive = false;
            _finishFlyTriggered = false;
        }

        if (_switched && oneShot)
            return;

        if (currentZ < tunnelEntryZ)
            return;

        if (triggerOnlyWhenMovingForward && !movingForward)
            return;

        _presetCam.MoveToWorldPose(tunnelCameraPosition, tunnelCameraEuler, adoptOrbitFromCurrentPoseAfterMove: false);
        _followOffset = tunnelCameraPosition - drillTransform.position;
        _followActive = followDrillAfterSwitch;
        _switched = true;
        Debug.Log($"[TunnelCamera] Переключение ракурса у тоннеля. DrillZ={currentZ:F3}.");

        TryStartFinishFlyBack(currentZ);
    }

    void LateUpdate()
    {
        if (_customFlybackRunning)
            return;

        if (drillTransform == null)
            return;

        TryStartFinishFlyBack(drillTransform.position.z);

        if (!_followActive)
            return;

        if (_presetCam != null && _presetCam.IsMovingToPreset)
            return;

        if (allowManualBreakFollow && HasManualCameraIntent())
        {
            _followActive = false;
            return;
        }

        transform.SetPositionAndRotation(
            drillTransform.position + _followOffset,
            Quaternion.Euler(tunnelCameraEuler));
    }

    bool HasManualCameraIntent()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1))
            return false;

        // Даем пользователю возможность перехватить камеру мышью во время бурения.
        return Input.GetMouseButton(1)
               || Input.GetMouseButton(2)
               || Mathf.Abs(Input.mouseScrollDelta.y) > 0.02f;
    }
    [SerializeField] private GameObject svet_v_konce_tonela;
    void TryStartFinishFlyBack(float currentZ)
    {
        if (!flyBackAtTunnelFinish || _finishFlyTriggered || _presetCam == null)
            return;
        if (!_switched)
            return;
        if (currentZ < tunnelFinishZ)
            return;

        _followActive = false;
        _finishFlyTriggered = true;
        if (useCustomThreePointFlyback)
            StartCoroutine(FlyBackAlongThreePointCurve());
        else
            _presetCam.MoveView_Home();
        Debug.Log($"[TunnelCamera] Финальный пролет назад запущен. DrillZ={currentZ:F3}.");
        StartCoroutine(ShowTunnelEndLightAfterDelay());

    }

    IEnumerator ShowTunnelEndLightAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        if (svet_v_konce_tonela != null)
            svet_v_konce_tonela.SetActive(true);
    }

    IEnumerator FlyBackAlongThreePointCurve()
    {
        _customFlybackRunning = true;
        if (_presetCam != null)
            _presetCam.enabled = false;

        var currentPos = transform.position;
        var currentRot = transform.rotation;

        var startPoint = flybackStartPoint;
        var midPoint = flybackMidPoint;
        var endPoint = flybackEndPoint;
        var startRot = Quaternion.Euler(flybackStartEuler);
        var midRot = Quaternion.Euler(flybackMidEuler);
        var endRot = Quaternion.Euler(flybackEndEuler);

        // Если точки заданы в обратном порядке — разворачиваем траекторию, чтобы старт был ближе к текущей камере.
        if ((currentPos - endPoint).sqrMagnitude < (currentPos - startPoint).sqrMagnitude)
        {
            var tmpPoint = startPoint;
            startPoint = endPoint;
            endPoint = tmpPoint;

            var tmpRot = startRot;
            startRot = endRot;
            endRot = tmpRot;
        }

        // Если сохраненная стартовая точка слишком далеко, начинаем с реального положения камеры — без резкого «провала».
        const float startSnapThreshold = 4f;
        if ((currentPos - startPoint).sqrMagnitude > startSnapThreshold * startSnapThreshold)
        {
            startPoint = currentPos;
            startRot = currentRot;
        }

        var duration = Mathf.Max(0.05f, customFlybackDuration);
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var smoothT = Mathf.SmoothStep(0f, 1f, t);

            var p01 = Vector3.Lerp(startPoint, midPoint, smoothT);
            var p12 = Vector3.Lerp(midPoint, endPoint, smoothT);
            var pos = Vector3.Lerp(p01, p12, smoothT);
            var q01 = Quaternion.Slerp(startRot, midRot, smoothT);
            var q12 = Quaternion.Slerp(midRot, endRot, smoothT);
            var rot = Quaternion.Slerp(q01, q12, smoothT);
            transform.SetPositionAndRotation(pos, rot);

            yield return null;
        }

        transform.SetPositionAndRotation(endPoint, endRot);
        if (_presetCam != null)
            _presetCam.enabled = true;
        _customFlybackRunning = false;
    }
}

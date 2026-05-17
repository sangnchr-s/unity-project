using System;
using UnityEngine;

public class DrillHeadController : MonoBehaviour
{
    [Header("Клавиши — выдвижение")]
    public KeyCode extendKey = KeyCode.E;
    public KeyCode retractKey = KeyCode.Q;

    [Header("Клавиши — вращение")]
    [Tooltip("Нажать ещё раз — остановить")]
    public KeyCode rotateKey = KeyCode.R;

    [Header("Настройки вращения")]
    [Tooltip("Объект который вращается (дочерний drill bit). Если пусто — вращается этот объект")]
    public Transform rotationTarget;
    [Tooltip("Ось вращения в локальных координатах объекта")]
    public Vector3 rotationAxis = Vector3.forward;
    [Tooltip("Градусов в секунду")]
    public float rotationSpeed = 360f;

    [Header("Аниматор выдвижения")]
    [Tooltip("Если пусто — берётся bool из контроллера: IsExtended (после Tools/Setup) или turn (стандартный DrillHead.controller).")]
    [SerializeField] string extendRetractBoolParameter = "";

    Animator _animator;
    bool _isRotating;
    bool _isExtended;
    int _extendRetractHash;
    bool _extendParamResolved;

    public static event Action<DrillTaskAction> DrillRotateStarted;
    public static event Action<DrillTaskAction> DrillHeadExtended;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            Debug.LogError("[DrillHead] Animator не найден на объекте " + gameObject.name);
            return;
        }

        if (_animator.runtimeAnimatorController == null)
        {
            Debug.LogError(
                "[DrillHead] У Animator нет Runtime Animator Controller — назначьте контроллер " +
                "(например Assets/anim/DrillHead.controller) на объект «" + gameObject.name + "».",
                this);
            return;
        }

        ResolveExtendRetractParameter();

        if (rotationTarget == null)
            rotationTarget = transform;

        _isExtended = false;
        DrillPracticeTelemetry.UpdateHeadExtension(_isExtended);
        DrillPracticeTelemetry.UpdateFromDrillHead(rotationSpeed, false);
    }

    void ResolveExtendRetractParameter()
    {
        if (!string.IsNullOrEmpty(extendRetractBoolParameter))
        {
            if (HasBoolParameter(extendRetractBoolParameter))
            {
                _extendRetractHash = Animator.StringToHash(extendRetractBoolParameter);
                _extendParamResolved = true;
                return;
            }

            Debug.LogWarning(
                "[DrillHead] В контроллере нет bool-параметра «" + extendRetractBoolParameter +
                "». Пробую IsExtended / turn.", this);
        }

        if (HasBoolParameter("IsExtended"))
            _extendRetractHash = Animator.StringToHash("IsExtended");
        else if (HasBoolParameter("turn"))
            _extendRetractHash = Animator.StringToHash("turn");
        else
        {
            Debug.LogError(
                "[DrillHead] В Animator Controller нет bool IsExtended или turn — Q/E не смогут " +
                "управлять выдвижением. Выполните Tools → Setup DrillHead Animator или настройте переходы вручную.",
                this);
            return;
        }

        _extendParamResolved = true;
    }

    bool HasBoolParameter(string name)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
            return false;
        foreach (var p in _animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Bool && p.name == name)
                return true;
        }

        return false;
    }

    void Update()
    {
        if (SimulatorPracticePanel.IsTypingInPracticeInput)
        {
            DrillPracticeTelemetry.UpdateFromDrillHead(rotationSpeed, false);
            DrillPracticeTelemetry.UpdateHeadExtension(_isExtended);
            return;
        }

        if (_animator == null || !_extendParamResolved || _animator.runtimeAnimatorController == null)
            return;

        if (Input.GetKeyDown(extendKey))
        {
            _animator.SetBool(_extendRetractHash, true);
            _isExtended = true;
            DrillHeadExtended?.Invoke(DrillTaskAction.DrillHeadExtended);
        }

        if (Input.GetKeyDown(retractKey))
        {
            _animator.SetBool(_extendRetractHash, false);
            _isExtended = false;
        }

        if (Input.GetKeyDown(rotateKey))
        {

            _isRotating = !_isRotating;
            DrillRotateStarted?.Invoke(DrillTaskAction.RotationStarted);
        }


        if (_isRotating && rotationTarget != null)
            rotationTarget.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);

        DrillPracticeTelemetry.UpdateFromDrillHead(rotationSpeed, _isRotating && rotationTarget != null);
        DrillPracticeTelemetry.UpdateHeadExtension(_isExtended);
    }
}

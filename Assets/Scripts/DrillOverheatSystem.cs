using UnityEngine;

/// <summary>
/// Игровая механика перегрева бура.
/// Если оператор бурит с мощностью ниже требуемой для текущей породы (например, 1‑я передача на граните),
/// бур постепенно нагревается. При критической температуре эффективность бурения временно падает,
/// пока бур не остынет. Это учит подбирать корректную передачу под породу.
///
/// Подписывается на <see cref="Movement.OnRockContextChanged"/> для актуальной требуемой мощности.
/// Используется <see cref="Drill"/> через <see cref="EffectivePowerMultiplier"/>.
/// </summary>
public sealed class DrillOverheatSystem : MonoBehaviour
{
    public static DrillOverheatSystem Instance { get; private set; }

    [Header("Накопление тепла")]
    [Tooltip("Скорость прироста температуры (ед./сек), когда мощность недостаточна для породы.")]
    [SerializeField] float heatRatePerSecond = 0.18f;

    [Tooltip("Скорость остывания (ед./сек), когда мощность достаточна или машина стоит.")]
    [SerializeField] float coolRatePerSecond = 0.32f;

    [Header("Пороги")]
    [Tooltip("Температура, при которой эффективность бурения начинает падать (0..1).")]
    [Range(0.5f, 0.99f)]
    [SerializeField] float warningThreshold = 0.7f;

    [Tooltip("Температура, при которой бур переходит в критический режим.")]
    [Range(0.6f, 1f)]
    [SerializeField] float criticalThreshold = 0.95f;

    [Tooltip("Минимальный множитель мощности в критическом режиме (0..1).")]
    [Range(0f, 1f)]
    [SerializeField] float minPowerMultiplier = 0.25f;

    /// <summary>Текущая температура, 0..1.</summary>
    public float Heat { get; private set; }

    /// <summary>Истина, когда перегрев активен и эффективность бурения снижена.</summary>
    public bool IsCritical { get; private set; }

    /// <summary>Множитель эффективной мощности для <see cref="Drill"/> (1 = норма, &lt;1 = перегрев).</summary>
    public float EffectivePowerMultiplier
    {
        get
        {
            if (Heat < warningThreshold)
                return 1f;
            float t = Mathf.InverseLerp(warningThreshold, criticalThreshold, Heat);
            return Mathf.Lerp(1f, minPowerMultiplier, t);
        }
    }

    public float WarningThreshold => warningThreshold;
    public float CriticalThreshold => criticalThreshold;

    int _requiredPower = 10;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        Movement.OnRockContextChanged += HandleRockContextChanged;
    }

    void OnDisable()
    {
        Movement.OnRockContextChanged -= HandleRockContextChanged;
        if (Instance == this)
            Instance = null;
    }

    void HandleRockContextChanged(string rockName, int requiredPower)
    {
        _requiredPower = Mathf.Max(10, requiredPower);
    }

    void Update()
    {
        bool drilling = DrillStateManager.instance != null && DrillStateManager.instance.isDrilling;
        int currentPower = DrillPracticeTelemetry.DrillPower;
        bool underpowered = drilling && currentPower < _requiredPower;

        if (underpowered)
            Heat = Mathf.Min(1f, Heat + heatRatePerSecond * Time.deltaTime);
        else
            Heat = Mathf.Max(0f, Heat - coolRatePerSecond * Time.deltaTime);

        if (!IsCritical && Heat >= criticalThreshold)
            IsCritical = true;
        else if (IsCritical && Heat <= warningThreshold * 0.6f)
            IsCritical = false;
    }
}

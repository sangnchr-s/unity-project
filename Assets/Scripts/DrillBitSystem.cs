using System;
using UnityEngine;

/// <summary>
/// Система износа и смены долота бура.
///
/// Долото изнашивается во время бурения. Скорость износа зависит от мощности
/// (=породы) и типа установленного долота. Когда износ достигает 1.0, долото
/// «сломано»: эффективная мощность падает до <see cref="brokenPowerMultiplier"/>,
/// пока игрок не вернётся к поверхности (Z &le; <see cref="surfaceRepairZ"/>) и
/// не будет некоторое время стоять — долото автоматически чинится.
///
/// Переключение типа долота — клавиша <see cref="cycleBitKey"/> (по умолчанию <c>T</c>).
/// Доступны три типа:
/// <list type="bullet">
///   <item>Standard — баланс износа и скорости.</item>
///   <item>Carbide — быстрее на мягкой породе, изнашивается быстрее на твёрдой.</item>
///   <item>Diamond — медленнее на мягкой, но почти не изнашивается на твёрдой.</item>
/// </list>
///
/// Подсказку об износе игрок слышит через <see cref="DrillAudioDynamics"/> (питч
/// падает по мере износа) и видит через тряску камеры (резкий пик при поломке).
/// </summary>
public sealed class DrillBitSystem : MonoBehaviour
{
    public static DrillBitSystem Instance { get; private set; }

    public enum BitType { Standard, Carbide, Diamond }

    [Header("Управление")]
    [Tooltip("Клавиша циклической смены типа долота. Не должна совпадать с уже занятыми клавишами (F/B/K/L/1‑3/Esc/Q).")]
    [SerializeField] KeyCode cycleBitKey = KeyCode.T;

    [Header("Износ")]
    [Tooltip("Базовый прирост износа за секунду на 10 ед. мощности (передача 1, мягкая порода).")]
    [SerializeField] float baseWearPerSecondPerPower10 = 0.02f;

    [Tooltip("Дополнительный множитель износа, если бурим мощностью ниже требуемой для породы.")]
    [SerializeField] float underpoweredWearBonus = 1.5f;

    [Header("Поломка и ремонт")]
    [Tooltip("Множитель эффективной мощности при сломанном долоте (0..1).")]
    [Range(0f, 1f)]
    [SerializeField] float brokenPowerMultiplier = 0.25f;

    [Tooltip("Множитель эффективной мощности при сильном износе (50‑100%).")]
    [Range(0f, 1f)]
    [SerializeField] float wornPowerMultiplierAtMax = 0.7f;

    [Tooltip("Z‑координата, ниже которой считается, что бур у поверхности (для авто‑ремонта).")]
    [SerializeField] float surfaceRepairZ = -28f;

    [Tooltip("Скорость авто‑ремонта (1/секунд), пока бур стоит у поверхности.")]
    [SerializeField] float repairRatePerSecond = 0.18f;

    public BitType CurrentBit { get; private set; } = BitType.Standard;

    /// <summary>0..1, где 0 = новое, 1 = сломано.</summary>
    public float Wear { get; private set; }

    public bool IsBroken => Wear >= 1f;

    /// <summary>Множитель мощности по совокупности износа и типа долота.</summary>
    public float EffectivePowerMultiplier
    {
        get
        {
            if (IsBroken)
                return brokenPowerMultiplier;
            float wearMul = Mathf.Lerp(1f, wornPowerMultiplierAtMax, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 1f, Wear)));
            return wearMul * GetSpeedBonusForCurrentBit();
        }
    }

    public event Action<BitType> OnBitChanged;
    public event Action OnBroken;
    public event Action OnRepaired;

    int _requiredPower = 10;
    bool _wasBroken;

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
        HandleInput();
        TickWear();
    }

    void HandleInput()
    {
        if (SimulatorPracticePanel.IsTypingInPracticeInput)
            return;

        if (Input.GetKeyDown(cycleBitKey))
            CycleBit();
    }

    void CycleBit()
    {
        CurrentBit = CurrentBit switch
        {
            BitType.Standard => BitType.Carbide,
            BitType.Carbide => BitType.Diamond,
            _ => BitType.Standard,
        };
        OnBitChanged?.Invoke(CurrentBit);
    }

    void TickWear()
    {
        var dsm = DrillStateManager.instance;
        bool drilling = dsm != null && dsm.isDrilling;
        float dt = Time.deltaTime;

        if (drilling)
        {
            int currentPower = DrillPracticeTelemetry.DrillPower <= 0 ? 10 : DrillPracticeTelemetry.DrillPower;
            float powerFactor = currentPower / 10f;
            float wearMul = GetWearMultiplierForCurrentBit(currentPower, _requiredPower);
            float wearRate = baseWearPerSecondPerPower10 * powerFactor * wearMul;

            if (currentPower < _requiredPower)
                wearRate *= underpoweredWearBonus;

            Wear = Mathf.Clamp01(Wear + wearRate * dt);
        }
        else
        {
            float z = dsm != null && dsm.movement != null ? dsm.movement.transform.position.z : 0f;
            if (z <= surfaceRepairZ)
                Wear = Mathf.Max(0f, Wear - repairRatePerSecond * dt);
        }

        if (!_wasBroken && IsBroken)
        {
            _wasBroken = true;
            OnBroken?.Invoke();
        }
        else if (_wasBroken && Wear < 0.95f)
        {
            _wasBroken = false;
            OnRepaired?.Invoke();
        }
    }

    float GetSpeedBonusForCurrentBit()
    {
        // Разная производительность по типам — для текущей породы.
        // _requiredPower: 10 = мягкая, 20 = гранит, 30 = диорит.
        return CurrentBit switch
        {
            BitType.Standard => 1.0f,
            BitType.Carbide => _requiredPower <= 10 ? 1.15f : 0.95f,
            BitType.Diamond => _requiredPower >= 20 ? 1.10f : 0.9f,
            _ => 1.0f,
        };
    }

    float GetWearMultiplierForCurrentBit(int currentPower, int requiredPower)
    {
        return CurrentBit switch
        {
            BitType.Standard => 1.0f,
            BitType.Carbide => requiredPower >= 20 ? 1.6f : 0.9f,
            BitType.Diamond => requiredPower >= 20 ? 0.5f : 0.8f,
            _ => 1.0f,
        };
    }
}

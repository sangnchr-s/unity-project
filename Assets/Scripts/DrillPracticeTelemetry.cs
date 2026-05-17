using UnityEngine;

/// <summary>
/// Телеметрия для практики КП: связь скорости вращения бура с условной «интенсивностью бурения» и метки времени для таблицы.
/// чатгпт даже сюда добавило телеметрию
/// саша кажется не знает об этом
/// </summary>
public static class DrillPracticeTelemetry
{
    public static float HeadRotationSpeedDegPerSec { get; private set; }
    public static bool HeadIsRotating { get; private set; }
    public static bool HeadIsExtended { get; private set; }
    public static int DrillPower { get; private set; } = 10;
    /// <summary>0…1 — нормировка относительно 720°/с (можно менять в инспекторе при расширении).</summary>
    public static float RelativeDrillingIntensity { get; private set; }

    static readonly bool[] SmallDrillsRunning = new bool[4];
    static readonly bool[] FrontLightsOn = new bool[2];

    const float ReferenceSpeedDegPerSec = 720f;

    public static void ResetSession()
    {
        HeadRotationSpeedDegPerSec = 0f;
        HeadIsRotating = false;
        HeadIsExtended = false;
        DrillPower = 10;
        RelativeDrillingIntensity = 0f;
        for (var i = 0; i < SmallDrillsRunning.Length; i++)
            SmallDrillsRunning[i] = false;
        for (var i = 0; i < FrontLightsOn.Length; i++)
            FrontLightsOn[i] = false;
    }

    public static void SetDrillPower(int power)
    {
        if (power <= 15)
            DrillPower = 10;
        else if (power <= 25)
            DrillPower = 20;
        else
            DrillPower = 30;
    }

    public static string DescribeRockByPower(int power)
    {
        if (power <= 10)
            return "легкая порода";
        if (power <= 20)
            return "плотная порода";
        return "гранит";
    }

    public static void UpdateFromDrillHead(float rotationSpeedDegPerSec, bool isRotating)
    {
        HeadRotationSpeedDegPerSec = isRotating ? rotationSpeedDegPerSec : 0f;
        HeadIsRotating = isRotating;
        RelativeDrillingIntensity = isRotating
            ? Mathf.Clamp01(rotationSpeedDegPerSec / Mathf.Max(1f, ReferenceSpeedDegPerSec))
            : 0f;
    }

    public static void UpdateHeadExtension(bool isExtended)
    {
        HeadIsExtended = isExtended;
    }

    public static void UpdateSmallDrillState(int index, bool isRunning)
    {
        if (index < 0 || index >= SmallDrillsRunning.Length)
            return;
        SmallDrillsRunning[index] = isRunning;
    }

    public static void UpdateLightState(int index, bool isOn)
    {
        if (index < 0 || index >= FrontLightsOn.Length)
            return;
        FrontLightsOn[index] = isOn;
    }

    static int ActiveSmallDrills()
    {
        var active = 0;
        for (var i = 0; i < SmallDrillsRunning.Length; i++)
            if (SmallDrillsRunning[i])
                active++;
        return active;
    }

    static int ActiveFrontLights()
    {
        var active = 0;
        for (var i = 0; i < FrontLightsOn.Length; i++)
            if (FrontLightsOn[i])
                active++;
        return active;
    }

    public static bool IsTaskCompleted(int taskIndex1Based)
    {
        switch (taskIndex1Based)
        {
            case 1:
                return false;
            case 2:
                return HeadIsRotating;
            case 3:
                return false;
            case 4:
                return HeadIsExtended;
            case 5:
                return DrillPower >= 20;
            case 6:
                return DrillPower >= 30;
            case 7:
                return false;
            default:
                return false;
        }
    }

    public static string DescribeTaskState(int taskIndex1Based)
    {
        switch (taskIndex1Based)
        {
            case 1:
                return "Состояние этапа: запустите машину клавишей пробел.";
            case 2:
                return HeadIsRotating
                    ? $"Состояние этапа: вращение включено (ω={HeadRotationSpeedDegPerSec:F0}°/с)."
                    : "Состояние этапа: вращение выключено.";
            case 3:
                return "Состояние этапа: двигайтесь к породе клавишей F.";
            case 4:
                return HeadIsExtended
                    ? "Состояние этапа: буровая голова выдвинута."
                    : "Состояние этапа: буровая голова не выдвинута.";
            case 5:
                return $"Состояние этапа: текущая мощность {DrillPower}, требуется 20.";
            case 6:
                return $"Состояние этапа: текущая мощность {DrillPower}, требуется 30.";
            case 7:
                return "Состояние этапа: завершите прохождение тоннеля.";
            default:
                return "Состояние этапа: выберите задание из списка.";
        }
    }

    /// <summary>Строка для таблицы результатов (автодополнение к «Запись»).</summary>
    public static string FormatAutoNoteForTask(int taskIndex1Based)
    {
        var t = Time.time;
        switch (taskIndex1Based)
        {
            case 1:
                return $"t={t:F1}с запуск машины P={DrillPower} ({DescribeRockByPower(DrillPower)})";
            case 2:
                return
                    $"t={t:F1}с ω={HeadRotationSpeedDegPerSec:F0}°/с вращ={HeadIsRotating} Iбур={RelativeDrillingIntensity:F2} P={DrillPower} ({DescribeRockByPower(DrillPower)})";
            case 3:
                return $"t={t:F1}с движение к породе P={DrillPower} ({DescribeRockByPower(DrillPower)})";
            case 4:
                return $"t={t:F1}с headExtended={HeadIsExtended} P={DrillPower} ({DescribeRockByPower(DrillPower)})";
            case 5:
                return $"t={t:F1}с смена мощности до 20, P={DrillPower} ({DescribeRockByPower(DrillPower)})";
            case 6:
                return $"t={t:F1}с смена мощности до 30, P={DrillPower} ({DescribeRockByPower(DrillPower)})";
            case 7:
                return $"t={t:F1}с финиш тоннеля P={DrillPower} ({DescribeRockByPower(DrillPower)})";
            default:
                return $"t={t:F1}с P={DrillPower} ({DescribeRockByPower(DrillPower)})";
        }
    }
}

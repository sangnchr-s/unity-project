using System;
using UnityEngine;

[System.Serializable]
public class SpeedZone
{
    [Tooltip("Z координата, начиная с которой действует эта зона")]
    public float z;

    [Tooltip("Скорость в этой зоне")]
    public float speed = 1f;
}

public class Movement : MonoBehaviour
{
    [Header("Manual Speeds (1 / 2 / 3)")]
    public float speed1 = 1f;
    public float speed2 = 3f;
    public float speed3 = 6f;

    [Header("Z Speed Zones")]
    public SpeedZone[] zones;

    [Header("Turn")]
    public KeyCode turnLeftKey = KeyCode.K;
    public KeyCode turnRightKey = KeyCode.L;
    public float turnSpeed = 45f;

    [Header("Debug")]
    public float currentSpeed;
    public float zoneSpeed;
    public float manualSpeed;

    public bool moving { get; private set; }

    public int taskBorderZ = -28;
    public float taskFinishZ = -5.23f;
    public static event Action<DrillTaskAction> OnDrillHitRock;
    public static event Action<DrillTaskAction> OnPowerChanged;
    public static event Action<DrillTaskAction> OnFinish;
    public static event Action<int> OnPowerLevelChanged;
    public static event Action<string, int> OnRockContextChanged;

    int currentGear = 1;
    string _currentRockName = "";
    int _currentRockRequiredPower;
    bool _finishTriggered;

    void Update()
    {
        if (SimulatorPracticePanel.IsTypingInPracticeInput)
        {
            moving = false;
            return;
        }

        HandleGearInput();
        UpdateRockContext();

        manualSpeed = GetManualSpeed();
        zoneSpeed = GetZoneSpeed();


        currentSpeed = manualSpeed * zoneSpeed;

        Turn();
        Move();
    }

    void Turn()
    {
        float direction = 0f;

        if (Input.GetKey(turnLeftKey))
            direction -= 1f;

        if (Input.GetKey(turnRightKey))
            direction += 1f;

        if (Mathf.Approximately(direction, 0f))
            return;

        transform.Rotate(Vector3.up, direction * turnSpeed * Time.deltaTime, Space.Self);
    }

    void Move()
    {
        if (zones != null && zones.Length > 0 && Input.GetKeyDown(KeyCode.Alpha2) && transform.position.z >= zones[0].z) //начало гранита
        {
            OnPowerChanged?.Invoke(DrillTaskAction.OnPowerChanged1);
        }

        if (zones != null && zones.Length > 1 && Input.GetKeyDown(KeyCode.Alpha3) && transform.position.z >= zones[1].z) //начало диорита
        {
            OnPowerChanged?.Invoke(DrillTaskAction.OnPowerChanged2);
        }

        if (!_finishTriggered && transform.position.z >= taskFinishZ)
        {
            _finishTriggered = true;
            OnFinish?.Invoke(DrillTaskAction.AchieveFinish);
        }
        if (Input.GetKey(KeyCode.F))
        {
            transform.position += transform.forward * currentSpeed * Time.deltaTime;
            moving = true;
            if (transform.position.z >= taskBorderZ && transform.position.z <= taskBorderZ + 1)
                OnDrillHitRock?.Invoke(DrillTaskAction.MovementHitRock);
            return;
        }

        if (Input.GetKey(KeyCode.B))
        {
            transform.position -= transform.forward * currentSpeed * Time.deltaTime;
            moving = true;
            return;
        }

        moving = false;
    }

    void HandleGearInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentGear = 1;
            ApplyGearAsDrillPower();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentGear = 2;
            ApplyGearAsDrillPower();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentGear = 3;
            ApplyGearAsDrillPower();
        }
    }

    void ApplyGearAsDrillPower()
    {
        var power = currentGear switch
        {
            1 => 10,
            2 => 20,
            3 => 30,
            _ => 10
        };

        DrillPracticeTelemetry.SetDrillPower(power);
        OnPowerLevelChanged?.Invoke(power);
    }

    void UpdateRockContext()
    {
        var z = transform.position.z;
        var rockName = "мягкая порода";
        var requiredPower = 10;

        if (zones != null && zones.Length > 0 && z >= zones[0].z)
        {
            rockName = "гранит";
            requiredPower = 20;
        }

        if (zones != null && zones.Length > 1 && z >= zones[1].z)
        {
            rockName = "диорит";
            requiredPower = 30;
        }

        if (_currentRockName == rockName && _currentRockRequiredPower == requiredPower)
            return;

        _currentRockName = rockName;
        _currentRockRequiredPower = requiredPower;
        OnRockContextChanged?.Invoke(_currentRockName, _currentRockRequiredPower);
    }

    float GetManualSpeed()
    {
        return currentGear switch
        {
            1 => speed1,
            2 => speed2,
            3 => speed3,
            _ => speed1
        };
    }

    float GetZoneSpeed()
    {
        if (zones == null || zones.Length == 0)
            return 1f;

        float z = transform.position.z;

        float result = 1f;

        for (int i = 0; i < zones.Length; i++)
        {
            if (z >= zones[i].z)
                result = zones[i].speed;
        }

        return result;
    }
}

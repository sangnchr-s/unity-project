using UnityEngine;

public class SmallDrillsController : MonoBehaviour
{
    [System.Serializable]
    public class SmallDrill
    {
        public string label;
        public Animator animator;
        public KeyCode key;
        [HideInInspector] public bool isRunning;
    }

    [Header("Малые буры (L01, L02, R01, R02)")]
    public SmallDrill[] drills = new SmallDrill[]
    {
        new SmallDrill { label = "DrillSmalldrillL01", key = KeyCode.Alpha1 },
        new SmallDrill { label = "DrillSmalldrillL02", key = KeyCode.Alpha2 },
        new SmallDrill { label = "DrillSmalldrillR01", key = KeyCode.Alpha3 },
        new SmallDrill { label = "DrillSmalldrillR02", key = KeyCode.Alpha4 },
    };

    private void Awake()
    {
        for (var i = 0; i < drills.Length; i++)
        {
            var drill = drills[i];
            if (drill.animator == null)
            {
                // Ищем дочерний объект с таким именем
                Transform found = transform.Find(drill.label);
                if (found != null)
                    drill.animator = found.GetComponent<Animator>();

                // Если не нашли среди прямых детей — ищем рекурсивно
                if (drill.animator == null)
                {
                    foreach (Animator a in GetComponentsInChildren<Animator>(true))
                    {
                        if (a.gameObject.name == drill.label)
                        {
                            drill.animator = a;
                            break;
                        }
                    }
                }

                if (drill.animator == null)
                {
                    Debug.LogWarning($"[SmallDrills] Объект '{drill.label}' не найден. Назначь Animator вручную в инспекторе.");
                    continue;
                }

                Debug.Log($"[SmallDrills] Animator для '{drill.label}' найден автоматически.");
            }

            drill.isRunning = false;
            drill.animator.enabled = false;
            DrillPracticeTelemetry.UpdateSmallDrillState(i, false);
        }
    }

    private void Update()
    {
        if (SimulatorPracticePanel.IsTypingInPracticeInput)
            return;

        for (var i = 0; i < drills.Length; i++)
        {
            var drill = drills[i];
            if (drill.animator == null) continue;

            if (Input.GetKeyDown(drill.key))
                Toggle(drill, i);
        }
    }

    private void Toggle(SmallDrill drill, int index)
    {
        drill.isRunning = !drill.isRunning;
        drill.animator.enabled = drill.isRunning;
        DrillPracticeTelemetry.UpdateSmallDrillState(index, drill.isRunning);
        Debug.Log($"[SmallDrills] {drill.label} — {(drill.isRunning ? "запущен" : "остановлен")}");
    }
}

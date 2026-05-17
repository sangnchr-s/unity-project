using UnityEngine;

public class LightsController : MonoBehaviour
{
    [System.Serializable]
    public class DrillLight
    {
        public string label;
        public Animator animator;
        public KeyCode key;
        public Light[] lightSources;
        public float onIntensity = 3f;
        public float onRange = 25f;
        [HideInInspector] public bool isOn;
    }

    [Header("Прожекторы")]
    public DrillLight[] lights = new DrillLight[]
    {
        new DrillLight { label = "DrillFrontLight1_Dummy",  key = KeyCode.Z },
        new DrillLight { label = "DrillFrontLight2_Dummy", key = KeyCode.X },
    };

    private void Awake()
    {
        for (var i = 0; i < lights.Length; i++)
        {
            var light = lights[i];
            if (light.animator == null)
                light.animator = FindByName(light.label);

            if (light.lightSources == null || light.lightSources.Length == 0)
                light.lightSources = FindLights(light);

            if (light.animator == null && (light.lightSources == null || light.lightSources.Length == 0))
            {
                Debug.LogWarning($"[Lights] Объект '{light.label}' не найден. Назначь Animator или Light вручную.");
                continue;
            }

            light.isOn = false;
            if (light.animator != null)
                light.animator.enabled = false;
            ApplyLightState(light, false);
            DrillPracticeTelemetry.UpdateLightState(i, false);
        }
    }

    private void Update()
    {
        if (SimulatorPracticePanel.IsTypingInPracticeInput)
            return;

        for (var i = 0; i < lights.Length; i++)
        {
            var light = lights[i];
            if (light.animator == null && (light.lightSources == null || light.lightSources.Length == 0))
                continue;

            if (Input.GetKeyDown(light.key))
                Toggle(light, i);
        }
    }

    private void Toggle(DrillLight light, int index)
    {
        light.isOn = !light.isOn;

        if (light.isOn)
        {
            if (light.animator != null)
            {
                light.animator.enabled = true;
                light.animator.SetTrigger("hit");
            }
        }
        else
        {
            if (light.animator != null)
            {
                // Rebind сбрасывает все анимированные свойства в исходное состояние
                light.animator.Rebind();
                light.animator.Update(0f);
                light.animator.enabled = false;
            }
        }

        ApplyLightState(light, light.isOn);
        DrillPracticeTelemetry.UpdateLightState(index, light.isOn);
        Debug.Log($"[Lights] {light.label} — {(light.isOn ? "включён" : "выключен")}");
    }

    private void ApplyLightState(DrillLight light, bool isOn)
    {
        if (light.lightSources == null)
            return;

        float intensity = light.onIntensity > 0f ? light.onIntensity : 3f;
        float range = light.onRange > 0f ? light.onRange : 25f;

        foreach (var source in light.lightSources)
        {
            if (source == null)
                continue;

            source.enabled = true;
            source.intensity = isOn ? intensity : 0f;
            source.range = range;
        }
    }

    private Animator FindByName(string objName)
    {
        foreach (Animator a in GetComponentsInChildren<Animator>(true))
            if (a.gameObject.name == objName)
                return a;
        return null;
    }

    private Light[] FindLights(DrillLight drillLight)
    {
        if (drillLight.animator != null)
            return drillLight.animator.GetComponentsInChildren<Light>(true);

        foreach (var source in GetComponentsInChildren<Light>(true))
        {
            if (source.gameObject.name == drillLight.label)
                return new[] { source };
        }

        return new Light[0];
    }
}

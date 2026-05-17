using UnityEngine;

/// <summary>
/// Сонар: по клавише <see cref="pingKey"/> от бура выпускается визуальная сферическая
/// волна, и на несколько секунд впереди по тоннелю появляются «маяки» в точках интереса —
/// границах слоёв породы (мягкая → гранит → диорит). Помогает игроку видеть, когда нужно
/// заранее переключить передачу. Никаких UI‑оверлеев — всё происходит в мире сцены.
///
/// Системы определяет автоматически через <see cref="Movement"/> и <see cref="DrillStateManager"/>.
/// </summary>
public sealed class SonarSystem : MonoBehaviour
{
    [Tooltip("Клавиша, по которой выпускается импульс сонара.")]
    [SerializeField] KeyCode pingKey = KeyCode.Q;

    [Tooltip("Сколько секунд расширяется визуальная волна сонара.")]
    [SerializeField] float waveDurationSeconds = 1.4f;

    [Tooltip("Финальный радиус волны (метры).")]
    [SerializeField] float waveMaxRadius = 45f;

    [Tooltip("Сколько секунд горят маяки в точках интереса.")]
    [SerializeField] float beaconDurationSeconds = 4f;

    [Tooltip("Кулдаун между импульсами (секунды).")]
    [SerializeField] float cooldownSeconds = 3f;

    [Tooltip("Максимальный диапазон поиска точек интереса (метры по Z).")]
    [SerializeField] float scanRangeZ = 60f;

    [Tooltip("Размер маяка‑сферы (метры).")]
    [SerializeField] float beaconDiameter = 1.1f;

    [Tooltip("Высота маяка над уровнем бура (метры).")]
    [SerializeField] float beaconVerticalOffset = 1.0f;

    static readonly Color GraniteColor = new Color(0.95f, 0.55f, 0.18f, 1f);
    static readonly Color DioriteColor = new Color(0.35f, 0.6f, 1f, 1f);
    static readonly Color WaveColor = new Color(0.3f, 0.9f, 1f, 1f);

    Movement _movement;
    Transform _drill;
    float _nextAllowedTime;

    void Update()
    {
        if (_drill == null)
            ResolveDrill();
        if (_drill == null)
            return;

        if (!Input.GetKeyDown(pingKey))
            return;

        if (Time.unscaledTime < _nextAllowedTime)
            return;

        if (SimulatorPracticePanel.IsTypingInPracticeInput)
            return;

        _nextAllowedTime = Time.unscaledTime + cooldownSeconds;

        SpawnWave(_drill.position);
        SpawnPointsOfInterest(_drill.position);
    }

    void ResolveDrill()
    {
        if (_movement == null)
#if UNITY_2023_1_OR_NEWER
            _movement = Object.FindAnyObjectByType<Movement>();
#else
            _movement = Object.FindObjectOfType<Movement>();
#endif
        if (_movement != null)
            _drill = _movement.transform;
    }

    void SpawnWave(Vector3 origin)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "SonarWave";
        var col = go.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        go.transform.SetParent(transform, true);
        go.transform.position = origin;
        go.transform.localScale = Vector3.one * 0.3f;

        var rend = go.GetComponent<MeshRenderer>();
        if (rend != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(WaveColor.r, WaveColor.g, WaveColor.b, 0.18f);
            mat.SetFloat("_Mode", 3f); // transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", WaveColor * 1.3f);
            rend.sharedMaterial = mat;
        }

        var anim = go.AddComponent<SonarWaveAnimator>();
        anim.Initialize(waveMaxRadius, waveDurationSeconds);
    }

    void SpawnPointsOfInterest(Vector3 origin)
    {
        if (_movement == null || _movement.zones == null)
            return;

        for (int i = 0; i < _movement.zones.Length; i++)
        {
            var zone = _movement.zones[i];
            if (zone == null)
                continue;

            // Только зоны впереди по +Z в пределах радиуса
            float dz = zone.z - origin.z;
            if (dz < -2f || dz > scanRangeZ)
                continue;

            Color tint = i == 0 ? GraniteColor : DioriteColor;
            Vector3 beaconPos = new Vector3(origin.x, origin.y + beaconVerticalOffset, zone.z);
            SpawnBeacon(beaconPos, tint);
        }
    }

    void SpawnBeacon(Vector3 position, Color tint)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "SonarBeacon";
        var col = go.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        go.transform.SetParent(transform, true);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * beaconDiameter;

        var rend = go.GetComponent<MeshRenderer>();
        if (rend != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = tint;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", tint * 2.2f);
            rend.sharedMaterial = mat;
        }

        var anim = go.AddComponent<SonarBeaconAnimator>();
        anim.Initialize(beaconDurationSeconds);
    }
}

/// <summary>Расширяющаяся сфера сонара. Сама себя уничтожает по окончании.</summary>
public sealed class SonarWaveAnimator : MonoBehaviour
{
    float _maxRadius;
    float _duration;
    float _t;
    Material _mat;

    public void Initialize(float maxRadius, float duration)
    {
        _maxRadius = maxRadius;
        _duration = Mathf.Max(0.05f, duration);
        var rend = GetComponent<MeshRenderer>();
        if (rend != null)
            _mat = rend.sharedMaterial;
    }

    void Update()
    {
        _t += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(_t / _duration);
        float radius = Mathf.Lerp(0.3f, _maxRadius, k);
        transform.localScale = Vector3.one * radius * 2f;

        if (_mat != null)
        {
            float alpha = Mathf.Lerp(0.22f, 0f, k);
            var c = _mat.color;
            _mat.color = new Color(c.r, c.g, c.b, alpha);
            _mat.SetColor("_EmissionColor", new Color(0.3f, 0.9f, 1f, 1f) * (1f - k) * 1.3f);
        }

        if (k >= 1f)
            Destroy(gameObject);
    }
}

/// <summary>«Маяк» сонара: пульсирующая сфера, гаснущая со временем.</summary>
public sealed class SonarBeaconAnimator : MonoBehaviour
{
    float _duration;
    float _t;
    Color _baseColor;
    Material _mat;

    public void Initialize(float duration)
    {
        _duration = Mathf.Max(0.05f, duration);
        var rend = GetComponent<MeshRenderer>();
        if (rend != null)
        {
            _mat = rend.sharedMaterial;
            _baseColor = _mat.color;
        }
    }

    void Update()
    {
        _t += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(_t / _duration);
        float pulse = 0.6f + 0.4f * Mathf.Sin(_t * 6f);

        if (_mat != null)
        {
            float fade = 1f - k;
            _mat.SetColor("_EmissionColor", _baseColor * (2.2f * pulse * fade));
        }

        if (k >= 1f)
            Destroy(gameObject);
    }
}

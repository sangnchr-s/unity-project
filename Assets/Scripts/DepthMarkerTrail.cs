using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Маркеры глубины: каждые N метров продвижения бура по +Z в мире появляется маленькая
/// светящаяся сфера. Цвет соответствует текущей породе (мягкая/гранит/диорит), благодаря
/// чему игрок видит, на какой глубине он сменил слой — без HUD‑оверлеев.
///
/// Маркеры — обычные GameObject‑ы со сферой; коллайдер отключён, чтобы они не цеплялись
/// за бур или физику.
/// </summary>
public sealed class DepthMarkerTrail : MonoBehaviour
{
    [Tooltip("Шаг глубины (метры) между соседними маркерами.")]
    [SerializeField] float stepMeters = 5f;

    [Tooltip("Z‑оффсет позади бура, на котором ставится маркер (чтобы он остался в уже пробуренном тоннеле).")]
    [SerializeField] float backOffsetZ = -1.5f;

    [Tooltip("Сдвиг маркера по вертикали относительно бура (метры).")]
    [SerializeField] float verticalOffset = 1.2f;

    [Tooltip("Диаметр сферы‑маркера (метры).")]
    [SerializeField] float markerDiameter = 0.55f;

    [Tooltip("Максимум одновременно живущих маркеров. Самые старые отбрасываются.")]
    [SerializeField] int maxMarkers = 200;

    static readonly Color SoftRockColor = new Color(0.8f, 0.85f, 0.9f, 1f);
    static readonly Color GraniteColor = new Color(0.95f, 0.55f, 0.18f, 1f);
    static readonly Color DioriteColor = new Color(0.35f, 0.6f, 1f, 1f);

    readonly Queue<GameObject> _markers = new Queue<GameObject>();
    Transform _drill;
    Movement _movement;
    bool _hasFirstAnchor;
    float _lastSpawnZ;
    string _currentRock = "мягкая порода";

    void OnEnable()
    {
        Movement.OnRockContextChanged += HandleRockChanged;
    }

    void OnDisable()
    {
        Movement.OnRockContextChanged -= HandleRockChanged;
    }

    void HandleRockChanged(string rockName, int requiredPower)
    {
        _currentRock = string.IsNullOrWhiteSpace(rockName) ? "мягкая порода" : rockName;
    }

    void Update()
    {
        if (_drill == null)
            ResolveDrill();
        if (_drill == null)
            return;

        float z = _drill.position.z;
        if (!_hasFirstAnchor)
        {
            _lastSpawnZ = z;
            _hasFirstAnchor = true;
            return;
        }

        if (z - _lastSpawnZ < stepMeters)
            return;

        SpawnMarker(_drill.position);
        _lastSpawnZ += stepMeters;
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

    void SpawnMarker(Vector3 drillPos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = $"DepthMarker_{Mathf.RoundToInt(drillPos.z)}m";

        var col = go.GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        go.transform.SetParent(transform, true);
        go.transform.position = new Vector3(drillPos.x, drillPos.y + verticalOffset, drillPos.z + backOffsetZ);
        go.transform.localScale = Vector3.one * markerDiameter;

        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            Color tint = ColorForRock(_currentRock);
            mat.color = tint;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", tint * 1.6f);
            renderer.sharedMaterial = mat;
        }

        _markers.Enqueue(go);
        while (_markers.Count > maxMarkers)
        {
            var old = _markers.Dequeue();
            if (old != null)
                Destroy(old);
        }
    }

    static Color ColorForRock(string rockName)
    {
        if (string.IsNullOrEmpty(rockName))
            return SoftRockColor;
        if (rockName.IndexOf("гранит", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return GraniteColor;
        if (rockName.IndexOf("диорит", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return DioriteColor;
        return SoftRockColor;
    }
}

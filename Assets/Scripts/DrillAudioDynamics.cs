using UnityEngine;

/// <summary>
/// Динамика звука бурения: чем выше тепло бура, тем выше питч и громкость loop‑звука
/// бурения. Игрок слышит «напряжение» в звуке вместо HUD-индикатора температуры.
///
/// Источник звука берётся у <see cref="DrillStateManager"/> по дочернему имени
/// «DrillDrilling_Audio» — без правок DrillStateManager.
/// </summary>
public sealed class DrillAudioDynamics : MonoBehaviour
{
    const string DrillingAudioChildName = "DrillDrilling_Audio";

    [Tooltip("Питч звука бурения, когда бур холодный (норма).")]
    [SerializeField] float pitchCold = 1.0f;

    [Tooltip("Питч звука бурения в критическом перегреве.")]
    [SerializeField] float pitchHot = 1.28f;

    [Tooltip("Множитель громкости при холодном буре.")]
    [SerializeField] float volumeMultiplierCold = 1.0f;

    [Tooltip("Множитель громкости при пике перегрева.")]
    [SerializeField] float volumeMultiplierHot = 1.15f;

    [Tooltip("Скорость сглаживания питча/громкости.")]
    [SerializeField] float smoothingRate = 3.5f;

    AudioSource _source;
    float _baseVolume;
    bool _baseVolumeCaptured;
    float _smoothedHeat;

    void Update()
    {
        if (_source == null)
            ResolveSource();
        if (_source == null)
            return;

        if (!_baseVolumeCaptured)
        {
            _baseVolume = _source.volume;
            _baseVolumeCaptured = true;
        }

        float targetHeat = DrillOverheatSystem.Instance != null ? DrillOverheatSystem.Instance.Heat : 0f;
        _smoothedHeat = Mathf.MoveTowards(_smoothedHeat, targetHeat, smoothingRate * Time.unscaledDeltaTime);

        float pitch = Mathf.Lerp(pitchCold, pitchHot, _smoothedHeat);
        float volumeMul = Mathf.Lerp(volumeMultiplierCold, volumeMultiplierHot, _smoothedHeat);

        _source.pitch = pitch;
        if (_baseVolume > 0f)
            _source.volume = Mathf.Clamp01(_baseVolume * volumeMul);
    }

    void ResolveSource()
    {
        var dsm = DrillStateManager.instance;
        if (dsm == null)
            return;
        var child = dsm.transform.Find(DrillingAudioChildName);
        if (child == null)
            return;
        _source = child.GetComponent<AudioSource>();
    }
}

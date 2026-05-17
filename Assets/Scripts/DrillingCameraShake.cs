using UnityEngine;

/// <summary>
/// Лёгкая тряска основной камеры во время бурения. Тем сильнее, чем горячее бур —
/// это даёт игроку тактильную обратную связь о перегрузе без UI.
///
/// Работает поверх <see cref="OrbitCameraAroundTarget"/>/<see cref="FlyCamera"/>:
/// в LateUpdate камера уже спозиционирована, мы добавляем дрожание поверх. На следующем
/// кадре камера-контроллер снова перезапишет transform, так что эффект «не накапливается».
/// </summary>
[DefaultExecutionOrder(100)]
public sealed class DrillingCameraShake : MonoBehaviour
{
    [Tooltip("Базовая амплитуда тряски по позиции (метры), когда идёт бурение и бур не перегрет.")]
    [SerializeField] float basePositionAmplitude = 0.018f;

    [Tooltip("Дополнительная амплитуда тряски на пике перегрева.")]
    [SerializeField] float overheatPositionAmplitude = 0.06f;

    [Tooltip("Базовая амплитуда углового дрожания (градусы).")]
    [SerializeField] float baseRotationAmplitude = 0.12f;

    [Tooltip("Доп. угловое дрожание на пике перегрева (градусы).")]
    [SerializeField] float overheatRotationAmplitude = 0.5f;

    [Tooltip("Скорость шумовой кривой. Больше — мельче и быстрее.")]
    [SerializeField] float noiseFrequency = 18f;

    [Tooltip("Сглаживание входной интенсивности (1/секунд).")]
    [SerializeField] float intensitySmoothing = 8f;

    Camera _camera;
    float _intensity;
    Vector2 _noiseSeed;

    void Awake()
    {
        _noiseSeed = new Vector2(Random.value * 1000f, Random.value * 1000f);
    }

    void LateUpdate()
    {
        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null)
            return;

        float target = ComputeTargetIntensity();
        _intensity = Mathf.MoveTowards(_intensity, target, intensitySmoothing * Time.unscaledDeltaTime);
        if (_intensity <= 0.0001f)
            return;

        float heat = DrillOverheatSystem.Instance != null ? DrillOverheatSystem.Instance.Heat : 0f;
        float posAmp = Mathf.Lerp(basePositionAmplitude, basePositionAmplitude + overheatPositionAmplitude, heat) * _intensity;
        float rotAmp = Mathf.Lerp(baseRotationAmplitude, baseRotationAmplitude + overheatRotationAmplitude, heat) * _intensity;

        float t = Time.unscaledTime * noiseFrequency;
        float nx = Mathf.PerlinNoise(_noiseSeed.x, t) - 0.5f;
        float ny = Mathf.PerlinNoise(_noiseSeed.y, t) - 0.5f;
        float nz = Mathf.PerlinNoise(_noiseSeed.x + 11.7f, t * 0.83f) - 0.5f;

        var camTransform = _camera.transform;
        camTransform.position += camTransform.right * (nx * 2f * posAmp)
                              + camTransform.up * (ny * 2f * posAmp);
        camTransform.rotation *= Quaternion.Euler(ny * 2f * rotAmp, nx * 2f * rotAmp, nz * 2f * rotAmp);
    }

    float ComputeTargetIntensity()
    {
        var dsm = DrillStateManager.instance;
        if (dsm == null || !dsm.isDrilling)
            return 0f;
        return 1f;
    }
}

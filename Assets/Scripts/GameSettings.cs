using UnityEngine;

/// <summary>
/// Лёгкие настройки симулятора: громкость и чувствительность мыши.
/// Хранятся в PlayerPrefs, применяются автоматически на старте сцены.
/// Используется PauseMenu и FlyCamera.
/// </summary>
public static class GameSettings
{
    const string KeyMasterVolume = "settings.masterVolume";
    const string KeyMouseSensitivity = "settings.mouseSensitivity";

    public const float DefaultMasterVolume = 0.85f;
    public const float DefaultMouseSensitivity = 2f;

    public const float MinMasterVolume = 0f;
    public const float MaxMasterVolume = 1f;
    public const float MinMouseSensitivity = 0.2f;
    public const float MaxMouseSensitivity = 5f;

    public static float MasterVolume
    {
        get
        {
            float stored = PlayerPrefs.GetFloat(KeyMasterVolume, DefaultMasterVolume);
            return Mathf.Clamp(stored, MinMasterVolume, MaxMasterVolume);
        }
        set
        {
            float clamped = Mathf.Clamp(value, MinMasterVolume, MaxMasterVolume);
            PlayerPrefs.SetFloat(KeyMasterVolume, clamped);
            AudioListener.volume = clamped;
        }
    }

    public static float MouseSensitivity
    {
        get
        {
            float stored = PlayerPrefs.GetFloat(KeyMouseSensitivity, DefaultMouseSensitivity);
            return Mathf.Clamp(stored, MinMouseSensitivity, MaxMouseSensitivity);
        }
        set
        {
            float clamped = Mathf.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity);
            PlayerPrefs.SetFloat(KeyMouseSensitivity, clamped);
        }
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyOnLoad()
    {
        AudioListener.volume = MasterVolume;
    }
}

using UnityEngine;

public static class AudioPreferencesManager
{
    private const string KEY_MIC_GAIN = "Audio_MicGain";
    private const string KEY_MASTER_VOLUME = "Audio_MasterVolume";
    private const string KEY_RECORDER_QUALITY = "Audio_RecorderQuality";

    // Guardar todos los parámetros de configuración
    public static void SaveSettings(float micGain, float masterVolume, int qualityIndex)
    {
        PlayerPrefs.SetFloat(KEY_MIC_GAIN, micGain);
        PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, masterVolume);
        PlayerPrefs.SetInt(KEY_RECORDER_QUALITY, qualityIndex);
        PlayerPrefs.Save();

#if UNITY_EDITOR
        Debug.Log($"[AudioPreferencesManager] Parámetros guardados - Gain Micro: {micGain} | Volumen: {masterVolume} | Calidad: {(AudioInputManager.RecorderQuality)qualityIndex}");
#endif
    }

    // Cargar los parámetros guardados (con valores por defecto)
    public static void LoadSettings(out float micGain, out float masterVolume, out AudioInputManager.RecorderQuality quality)
    {
        micGain = PlayerPrefs.GetFloat(KEY_MIC_GAIN, 1.0f);
        masterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, 1.0f);
        quality = (AudioInputManager.RecorderQuality)PlayerPrefs.GetInt(KEY_RECORDER_QUALITY, (int)AudioInputManager.RecorderQuality.Low);

#if UNITY_EDITOR
        Debug.Log($"[AudioPreferencesManager] Parámetros cargados - Gain Micro: {micGain} | Volumen: {masterVolume} | Calidad: {quality}");
#endif
    }

    // Guardar un parámetro individual cuando se cambia en tiempo real
    public static void SaveParameter(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();

#if UNITY_EDITOR
        Debug.Log($"[AudioPreferencesManager] Parámetro actualizado ({key}): {value}");
#endif
    }

    public static void SaveParameter(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();

#if UNITY_EDITOR
        Debug.Log($"[AudioPreferencesManager] Parámetro actualizado ({key}): {value}");
#endif
    }
}
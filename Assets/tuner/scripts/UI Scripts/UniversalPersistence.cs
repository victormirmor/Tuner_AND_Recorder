using UnityEngine;

public static class UniversalPersistence
{
    // --- GUARDAR (SAVE) ---

    // Sobrecarga 1: Guarda valores numéricos usando 'isInt' para alternar entre Int y Float
    public static void Save(string key, float value, bool isInt)
    {
        if (isInt)
        {
            PlayerPrefs.SetInt(key, Mathf.RoundToInt(value));
        }
        else
        {
            PlayerPrefs.SetFloat(key, value);
        }
        PlayerPrefs.Save();
    }

    // Sobrecarga 2: Guarda cadenas de texto directamente
    public static void Save(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    // --- CARGAR (LOAD) ---

    // Sobrecarga 1: Carga valores numéricos convirtiendo a Int o Float según el bool
    public static float Load(string key, bool isInt, float defaultValue = 0f)
    {
        if (!PlayerPrefs.HasKey(key)) return defaultValue;

        if (isInt)
        {
            return PlayerPrefs.GetInt(key, Mathf.RoundToInt(defaultValue));
        }
        else
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }
    }

    // Sobrecarga 2: Carga cadenas de texto
    public static string Load(string key, string defaultValue = "")
    {
        return PlayerPrefs.GetString(key, defaultValue);
    }

    // --- MÉTODOS DE SOPORTE ---

    public static bool HasKey(string key) => PlayerPrefs.HasKey(key);
    public static void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
}
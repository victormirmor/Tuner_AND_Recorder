using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUIController : MonoBehaviour
{
    private const string MIC_GAIN_PREF_KEY = "MicGain";
    private const string UPDATE_RATE_PREF_KEY = "UpdateInterval";
    private const string TUNER_MODE_PREF_KEY = "TunerMode";

    [Header("Referencias a Scripts Principales")]
    public AudioInputManager audioInputManager;
    public PitchDetector pitchDetector;
    public NoteMapper noteMapper;
    public FreeTunerUIController tunerUIController;

    [Header("Elementos UI del Panel de Opciones")]
    public GameObject settingsPanel;
    public TMP_Dropdown instrumentDropdown;
    public TMP_Dropdown modeDropdown;

    [Header("Sliders de Controles")]
    public Slider micGainSlider;
    public Slider updateRateSlider;

    private List<AudioInputManager.TunerMode> availableModes = new List<AudioInputManager.TunerMode>();

    void Start()
    {
        InitializeUI();
    }

    void OnDestroy()
    {
        if (instrumentDropdown != null) instrumentDropdown.onValueChanged.RemoveListener(OnInstrumentChanged);
        if (modeDropdown != null) modeDropdown.onValueChanged.RemoveListener(OnTunerModeChanged);
        if (micGainSlider != null) micGainSlider.onValueChanged.RemoveListener(OnMicGainChanged);
        if (updateRateSlider != null) updateRateSlider.onValueChanged.RemoveListener(OnUpdateIntervalChanged);
    }

    private void InitializeUI()
    {
        // 1. Selector de Instrumento
        if (instrumentDropdown != null && tunerUIController != null)
        {
            instrumentDropdown.value = (int)tunerUIController.currentInstrument;
            instrumentDropdown.onValueChanged.AddListener(OnInstrumentChanged);
        }

        // 2. Selector de Modo
        PopulateModeDropdown();
        if (modeDropdown != null)
        {
            int savedModeIndex = Mathf.RoundToInt(UniversalPersistence.Load(TUNER_MODE_PREF_KEY, true, 0f));
            savedModeIndex = Mathf.Clamp(savedModeIndex, 0, Mathf.Max(0, availableModes.Count - 1));

            modeDropdown.value = savedModeIndex;
            OnTunerModeChanged(savedModeIndex);

            modeDropdown.onValueChanged.AddListener(OnTunerModeChanged);
        }

        // 3. Slider de Ganancia
        if (micGainSlider != null && audioInputManager != null)
        {
            float defaultGain = audioInputManager.micGain;
            float savedGain = UniversalPersistence.Load(MIC_GAIN_PREF_KEY, false, defaultGain);

            audioInputManager.micGain = savedGain;
            micGainSlider.value = savedGain;

            micGainSlider.onValueChanged.AddListener(OnMicGainChanged);
        }

        // 4. Slider de Velocidad de Actualización (Conversión Hz a Segundos)
        if (updateRateSlider != null && pitchDetector != null)
        {
            // Cargar valor guardado en Hz (por defecto 20 Hz)
            float savedHz = UniversalPersistence.Load(UPDATE_RATE_PREF_KEY, false, 20f);
            
            updateRateSlider.value = savedHz;
            // Convertir Hz a intervalo en segundos (1 / Hz)
            pitchDetector.updateInterval = 1f / Mathf.Max(savedHz, 1f);

            updateRateSlider.onValueChanged.AddListener(OnUpdateIntervalChanged);
        }
    }

    private void PopulateModeDropdown()
    {
        if (modeDropdown == null) return;

        modeDropdown.ClearOptions();
        availableModes.Clear();

        AudioInputManager.TunerMode[] allModes = (AudioInputManager.TunerMode[])Enum.GetValues(typeof(AudioInputManager.TunerMode));
        List<string> optionsList = new List<string>();

        foreach (var mode in allModes){
            availableModes.Add(mode);
            optionsList.Add(mode.ToString());
        }

        modeDropdown.AddOptions(optionsList);
    }

    public void OnInstrumentChanged(int index)
    {
        if (tunerUIController != null)
        {
            tunerUIController.SetInstrumentType(index);
        }
    }

    public void OnTunerModeChanged(int dropdownIndex)
    {
        if (audioInputManager == null || dropdownIndex < 0 || dropdownIndex >= availableModes.Count) return;

        AudioInputManager.TunerMode selectedMode = availableModes[dropdownIndex];
        audioInputManager.SetTunerMode(selectedMode);

        UniversalPersistence.Save(TUNER_MODE_PREF_KEY, dropdownIndex, true);
    }

    public void OnMicGainChanged(float value)
    {
        if (audioInputManager != null)
        {
            audioInputManager.micGain = value;
            UniversalPersistence.Save(MIC_GAIN_PREF_KEY, value, false);
        }
    }

    public void OnUpdateIntervalChanged(float valueInHz)
    {
        if (pitchDetector != null)
        {
            // Convertir Hz a segundos inmediatamente
            pitchDetector.updateInterval = 1f / Mathf.Max(valueInHz, 1f);
            UniversalPersistence.Save(UPDATE_RATE_PREF_KEY, valueInHz, false);
        }
    }

    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }
}
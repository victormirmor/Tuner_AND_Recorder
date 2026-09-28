using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioUIManager : MonoBehaviour
{
    [Header("Referencias Principales")]
    public AudioInputManager audioInputManager;

    [Header("Paneles de la Interfaz")]
    public GameObject mainRecorderPanel;
    public GameObject settingsPanel;

    [Header("Botones de Control de Grabación")]
    public Button recButton;
    public Button playButton;
    public Button stopButton;
    public Button pauseButton;
    public Button saveButton;
    public Button quitButton;
    public Button settingsGearButton;

    [Header("Botones de Configuración y Navegación")]
    public Button returnButton;
    public Button tunerButton;
    public Button metronomeButton;

    [Header("Dropdowns / Selectores")]
    public TMP_Dropdown modeDropdown;

    [Header("Sliders")]
    public Slider gainMicroSlider;
    public Slider volumenSlider;

    [Header("Textos de Display")]
    public TMP_Text statusText;
    public TMP_Text timerText;
    public TMP_Text fileNameText;
    public TMP_Text qualityText; // <-- NUEVO: Etiqueta para mostrar la calidad actual en pantalla

    void Start(){
        
        if (audioInputManager == null) audioInputManager = FindFirstObjectByType<AudioInputManager>();
        SetupUIListeners();
        SyncInitialValues();
        ShowMainPanel();
        if (fileNameText) fileNameText.text = "LISTO_PARA_GRABAR.WAV";
    }

    void Update()
    {
        UpdateDigitalDisplay();
    }

    private void SetupUIListeners()
    {
        if (recButton) recButton.onClick.AddListener(() => {
            audioInputManager.StartRecording();
            if (fileNameText) fileNameText.text = "GRABANDO...";
        });

        if (playButton) playButton.onClick.AddListener(() => {
            audioInputManager.PlayLastSavedRecording();
            if (fileNameText) fileNameText.text = "REPRODUCIENDO...";
        });
        
        if (stopButton) stopButton.onClick.AddListener(() => {
            audioInputManager.StopAction();
            if (fileNameText) fileNameText.text = "00:00:00.WAV";
        });

        if (pauseButton) pauseButton.onClick.AddListener(() => audioInputManager.TogglePause());

        if (saveButton) saveButton.onClick.AddListener(() => {
            string savedName = audioInputManager.SaveRecording();
            if (!string.IsNullOrEmpty(savedName) && fileNameText)
            {
                fileNameText.text = savedName;
            }
        });

        if (quitButton) quitButton.onClick.AddListener(() => Application.Quit());

        if (settingsGearButton) settingsGearButton.onClick.AddListener(ShowSettingsPanel);
        if (returnButton) returnButton.onClick.AddListener(ShowMainPanel);

        if (tunerButton) tunerButton.onClick.AddListener(() => {
            audioInputManager.SwitchMode(AudioInputManager.AppMode.Tuner);
        });

        if (gainMicroSlider) gainMicroSlider.onValueChanged.AddListener((val) => audioInputManager.SetMicGain(val));
        if (volumenSlider) volumenSlider.onValueChanged.AddListener((val) => audioInputManager.SetMasterVolume(val));

        

        if (modeDropdown) modeDropdown.onValueChanged.AddListener((idx) => {
            audioInputManager.SetRecorderQuality((AudioInputManager.RecorderQuality)idx);
        });
    }

    private void ShowMainPanel()
    {
        if (mainRecorderPanel) mainRecorderPanel.SetActive(true);
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    private void ShowSettingsPanel()
    {
        if (mainRecorderPanel) mainRecorderPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(true);
    }

    private void SyncInitialValues()
    {
        if (gainMicroSlider) gainMicroSlider.value = audioInputManager.micGain;
        if (volumenSlider) volumenSlider.value = audioInputManager.masterVolume;
        
        // Sincronizar el dropdown con el valor inicial del manager
        if (modeDropdown) modeDropdown.value = (int)audioInputManager.recorderQuality;
    }

    private void UpdateDigitalDisplay(){
    // Mostrar la calidad seleccionada arriba en el panel derecho
    if (qualityText)
    {
        qualityText.text = $"MODE:\n{audioInputManager.recorderQuality.ToString().ToUpper()}";
    }

    // Estados visuales exactos: STOP, PLAY, RECORD, PAUSED
    if (statusText)
    {
        string stateStr = "STOP";

        if (audioInputManager.isPaused)
        {
            stateStr = "PAUSED";
        }
        else if (audioInputManager.recorderState == AudioInputManager.RecorderState.Recording)
        {
            stateStr = "RECORD";
        }
        else if (audioInputManager.recorderState == AudioInputManager.RecorderState.Playing)
        {
            stateStr = "PLAY";
        }
        else if (audioInputManager.recorderState == AudioInputManager.RecorderState.Idle)
        {
            stateStr = "STOP";
        }

        statusText.text = stateStr;
    }

    // Actualizar el temporizador digital
    if (timerText){
        float time = audioInputManager.elapsedTime;
        int minutes = Mathf.FloorToInt(time / 60F);
        int seconds = Mathf.FloorToInt(time % 60F);
        int milliseconds = Mathf.FloorToInt((time * 100F) % 100F);
        timerText.text = $"{minutes:00}:{seconds:00}:{milliseconds:00}";
        }
    }
}
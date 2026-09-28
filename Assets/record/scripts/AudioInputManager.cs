using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

[RequireComponent(typeof(AudioSource))]
public class AudioInputManager : MonoBehaviour{

    public enum TunerMode { Fast = 2048, Accurate = 4096, Ultra = 8192 }
    public enum RecorderQuality { Low, Medium, High }
    public enum RecorderState { Idle, Recording, Paused, Playing }


    [Header("Configuración de Afinador (Tuner)")]
    public int baseSampleRate = 44100;
    public TunerMode currentTunerMode = TunerMode.Fast;

    [Header("Configuración de Grabadora (Recorder)")]
    public RecorderQuality recorderQuality = RecorderQuality.Low;
    public RecorderState recorderState = RecorderState.Idle;

    [Header("Ajustes de Audio (Sliders)")]
    [Range(0.5f, 5.0f)]
    public float micGain = 1.0f;
    [Range(0.0f, 1.0f)]
    public float masterVolume = 1.0f;

    [Header("Estado de Tiempo y Control")]
    public float elapsedTime = 0f;
    public bool isPaused = false;

    private AudioSource audioSource;
    private string selectedMicrophone;
    private float[] samplesBuffer;
    private bool isInitialized = false;
    private AudioClip recordedClip;

    public float[] SamplesBuffer => samplesBuffer;
    public int SampleWindow => (int)currentTunerMode;
    public bool IsInitialized => isInitialized;
    public int SampleRate => baseSampleRate;
    public float[] GetAudioSamples() => samplesBuffer;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Cargar preferencias guardadas mediante el script externo
        AudioPreferencesManager.LoadSettings(out micGain, out masterVolume, out recorderQuality);
        
        ReallocateBuffer();
    }

    void Start()
    {
        CheckPermissionsAndInit();
    }

    void Update()
    {
        if (!isInitialized) return;

        if (recorderState == RecorderState.Recording && !isPaused)
        {
            elapsedTime += Time.deltaTime;
        }

        if (recorderState == RecorderState.Playing && !isPaused)
        {
            elapsedTime += Time.deltaTime;

            if (elapsedTime > 0.2f && !audioSource.isPlaying)
            {
                StopAction();
            }
        }

        if (audioSource != null)
        {
            // Limitar el volumen de forma segura entre 0 y 1 para evitar desbordamientos en Unity
            audioSource.volume = Mathf.Clamp01(masterVolume);
        }
    }

    public void SetMicGain(float value)
    {
        micGain = value;
        AudioPreferencesManager.SaveParameter("Audio_MicGain", micGain);
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = value;
        AudioPreferencesManager.SaveParameter("Audio_MasterVolume", masterVolume);
    }

    public void SetRecorderQuality(RecorderQuality quality)
    {
        recorderQuality = quality;
        AudioPreferencesManager.SaveParameter("Audio_RecorderQuality", (int)recorderQuality);
    }

    private void ReallocateBuffer()
    {
        lock (this)
        {
            samplesBuffer = new float[(int)currentTunerMode];
        }
    }

    private void ReadAudioSamplesForTuner()
    {
#if !UNITY_WEBGL
        if (string.IsNullOrEmpty(selectedMicrophone)) return;

        int window = (int)currentTunerMode;
        int micPosition = Microphone.GetPosition(selectedMicrophone) - (window + 1);
        if (micPosition < 0) return;

        lock (this){
        }
#endif
    }

    private int GetSampleRateForRecorderQuality()
    {
        switch (recorderQuality)
        {
            case RecorderQuality.Low: return 11025;
            case RecorderQuality.Medium: return 22050;
            case RecorderQuality.High: return 44100;
            default: return 44100;
        }
    }

    public void StartRecording()
    {
#if !UNITY_WEBGL
        if (string.IsNullOrEmpty(selectedMicrophone)) return;

        if (audioSource.isPlaying || isPaused) StopAction();

        int targetSampleRate = GetSampleRateForRecorderQuality();
        audioSource.clip = Microphone.Start(selectedMicrophone, false, 300, targetSampleRate);
        
        recorderState = RecorderState.Recording;
        isPaused = false;
        elapsedTime = 0f;
#endif
    }

    public void StopAction()
    {
#if !UNITY_WEBGL
        if (recorderState == RecorderState.Recording)
        {
            int recordedLength = Microphone.GetPosition(selectedMicrophone);
            Microphone.End(selectedMicrophone);

            if (audioSource.clip != null && recordedLength > 0)
            {
                // Aquí aplicamos micGain a las muestras de la grabadora
                recordedClip = TrimAudioClip(audioSource.clip, recordedLength);
                audioSource.clip = recordedClip;
            }
        }
#endif

        if (audioSource.isPlaying || isPaused)
        {
            audioSource.Stop();
        }

        recorderState = RecorderState.Idle;
        isPaused = false;
        elapsedTime = 0f;
    }

    public void TogglePause()
    {
        if (recorderState == RecorderState.Idle) return;

        if (!isPaused)
        {
            isPaused = true;
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
            }
        }
        else
        {
            isPaused = false;
            if (recorderState == RecorderState.Playing || recorderState == RecorderState.Recording)
            {
                audioSource.UnPause();
            }
        }
    }

    public void PlayLastSavedRecording()
    {
        StartCoroutine(LoadAndPlayLatestRoutine());
    }

    private IEnumerator LoadAndPlayLatestRoutine()
    {
        string musicDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        string appFolder = Path.Combine(musicDirectory, "TuJuegoOApp");

        if (!Directory.Exists(appFolder))
        {
            Debug.LogWarning("[AudioInputManager] La carpeta de música no existe o aún no hay grabaciones.");
            yield break;
        }

        DirectoryInfo dirInfo = new DirectoryInfo(appFolder);
        FileInfo[] files = dirInfo.GetFiles("*.wav");

        if (files.Length == 0)
        {
            Debug.LogWarning("[AudioInputManager] No se encontraron archivos .wav guardados.");
            yield break;
        }

        Array.Sort(files, (x, y) => y.CreationTime.CompareTo(x.CreationTime));
        string latestFilePath = files[0].FullName;

        string fileUri = new Uri(latestFilePath).AbsoluteUri;

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fileUri, AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip loadedClip = DownloadHandlerAudioClip.GetContent(www);
                if (loadedClip != null)
                {
                    if (audioSource.isPlaying || isPaused) StopAction();

                    audioSource.clip = loadedClip;
                    audioSource.Play();
                    recorderState = RecorderState.Playing;
                    isPaused = false;
                    elapsedTime = 0f;

                    Debug.Log($"[AudioInputManager] Reproduciendo última grabación desde disco: {files[0].Name}");
                }
            }
            else
            {
                Debug.LogError($"[AudioInputManager] Error al cargar el archivo de audio: {www.error}");
            }
        }
    }

    public string SaveRecording()
    {
        if (recordedClip == null) return string.Empty;

        string extension = ".wav";
        string timestamp = DateTime.Now.ToString("dd_MM_yy_HHmmss");
        string fileName = $"GRABACION_{timestamp}";

        string musicDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        string appFolder = Path.Combine(musicDirectory, "TuJuegoOApp");

        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }

        string filePath = Path.Combine(appFolder, fileName + extension);
        WavUtility.Save(filePath, recordedClip);

        AudioPreferencesManager.SaveSettings(micGain, masterVolume, (int)recorderQuality);

        return fileName + extension.ToUpper();
    }

    private void ApplyGainToBuffer()
    {
        if (Mathf.Abs(micGain - 1.0f) > 0.01f)
        {
            for (int i = 0; i < samplesBuffer.Length; i++)
            {
                samplesBuffer[i] *= micGain;
            }
        }
    }

    private AudioClip TrimAudioClip(AudioClip original, int length)
    {
        float[] data = new float[length * original.channels];
        original.GetData(data, 0);

        // APLICAR GANANCIA DEL MICRÓFONO A LA GRABACIÓN
        if (Mathf.Abs(micGain - 1.0f) > 0.01f)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] *= micGain;
            }
        }

        AudioClip trimmed = AudioClip.Create(original.name, length, original.channels, original.frequency, false);
        trimmed.SetData(data, 0);
        return trimmed;
    }

    private void CheckPermissionsAndInit()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            StartCoroutine(WaitForAndroidPermission());
            return;
        }
#endif
        InitMicrophoneForCurrentMode();
    }

#if UNITY_ANDROID
    private IEnumerator WaitForAndroidPermission()
    {
        while (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            yield return new WaitForSeconds(0.2f);
        }
        InitMicrophoneForCurrentMode();
    }
#endif

    private void InitMicrophoneForCurrentMode()
    {
#if !UNITY_WEBGL
        if (Microphone.devices.Length == 0) return;

        selectedMicrophone = Microphone.devices[0];
        isInitialized = true;
#endif
    }
}
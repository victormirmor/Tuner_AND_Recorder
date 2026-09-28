using UnityEngine;

public class PitchDetector : MonoBehaviour
{
    [Header("Referencias Base")]
    public AudioInputManager audioInputManager;

    [Header("Configuración de Rendimiento")]
    [Tooltip("Tiempo en segundos entre lecturas. Gestionado automáticamente por SettingsUIController.")]
    public float updateInterval = 0.05f;

    [Header("Estabilidad y Puerta de Ruido (Noise Gate)")]
    [Tooltip("Volumen RMS mínimo para procesar audio. Se establece bajo (0.003) para no filtrar cuerdas suaves.")]
    public float minVolumeThreshold = 0.003f;

    [Tooltip("Velocidad de suavizado de la frecuencia.")]
    public float frequencySmoothSpeed = 18f;

    [Header("Frecuencia Detectada (Solo Lectura)")]
    [SerializeField] private float currentFrequency = 0f;
    public float CurrentFrequency
    {
        get => currentFrequency;
        private set => currentFrequency = value;
    }

    private float updateTimer = 0f;
    private float smoothedFrequency = 0f;

    void Update()
    {
        updateTimer += Time.deltaTime;

        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            ProcessPitchDetection();
        }
    }

    private void ProcessPitchDetection()
    {
        if (audioInputManager == null) return;

        float[] sampleBuffer = audioInputManager.GetAudioSamples();
        if (sampleBuffer == null || sampleBuffer.Length == 0)
        {
            ResetPitch();
            return;
        }

        // 1. PUERTA DE RUIDO (RMS)
        float rmsVolume = CalculateRMS(sampleBuffer);
        if (rmsVolume < minVolumeThreshold)
        {
            ResetPitch();
            return;
        }

        // 2. DETECCIÓN DE PITCH
        float rawFrequency = DetectPitchAutocorrelation(sampleBuffer, audioInputManager.SampleRate);

        // 3. SUAVIZADO DE FRECUENCIA
        if (rawFrequency > 0f)
        {
            if (smoothedFrequency <= 0f)
            {
                smoothedFrequency = rawFrequency;
            }
            else
            {
                smoothedFrequency = Mathf.Lerp(smoothedFrequency, rawFrequency, Time.deltaTime * frequencySmoothSpeed);
            }

            CurrentFrequency = smoothedFrequency;
        }
        else
        {
            ResetPitch();
        }
    }

    private float CalculateRMS(float[] buffer)
    {
        float sum = 0f;
        for (int i = 0; i < buffer.Length; i++)
        {
            sum += buffer[i] * buffer[i];
        }
        return Mathf.Sqrt(sum / buffer.Length);
    }

    private float DetectPitchAutocorrelation(float[] samples, int sampleRate)
    {
        int bufferSize = samples.Length;
        int maxLag = bufferSize / 2;
        float[] autoCorr = new float[maxLag];

        for (int lag = 0; lag < maxLag; lag++)
        {
            float sum = 0f;
            for (int i = 0; i < maxLag; i++)
            {
                sum += samples[i] * samples[i + lag];
            }
            autoCorr[lag] = sum;
        }

        int bestLag = -1;
        float maxCorr = 0f;

        for (int lag = 1; lag < maxLag - 1; lag++)
        {
            if (autoCorr[lag] > autoCorr[lag - 1] && autoCorr[lag] > autoCorr[lag + 1])
            {
                if (autoCorr[lag] > maxCorr)
                {
                    maxCorr = autoCorr[lag];
                    bestLag = lag;
                }
            }
        }

        if (bestLag > 0)
        {
            return (float)sampleRate / bestLag;
        }

        return 0f;
    }

    private void ResetPitch()
    {
        smoothedFrequency = 0f;
        CurrentFrequency = 0f;
    }
}
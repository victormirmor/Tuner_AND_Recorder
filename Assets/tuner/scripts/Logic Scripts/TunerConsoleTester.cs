using UnityEngine;

public class TunerConsoleTester : MonoBehaviour
{
    [Header("Referencias")]
    public AudioInputManager inputManager;
    public PitchDetector pitchDetector;
    public NoteMapper noteMapper;

    private string lastLoggedNote = "";

    void Update()
    {
#if UNITY_EDITOR
        if (pitchDetector == null || noteMapper == null) return;

        float freq = pitchDetector.CurrentFrequency;

        if (freq > 0f)
        {
            NoteMapper.ChromaticResult result = noteMapper.MapFrequencyToNote(freq);

            if (result.isValid && result.targetNoteName != lastLoggedNote)
            {
                lastLoggedNote = result.targetNoteName;
                Debug.Log($"<color=cyan>[TUNER RESULT]</color> Nota Capturada: <color=red>{result.capturedNoteName}</color> | Nota Objetivo: <color=green>{result.targetNoteName}</color> | Freq: {result.detectedFrequency:F2} Hz | Cents: {result.centsOffset:F1}");
            }
        }
        else
        {
            lastLoggedNote = "";
        }
#endif
    }
}
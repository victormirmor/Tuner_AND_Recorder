using UnityEngine;

public class NoteMapper : MonoBehaviour
{
    [Header("Afinación de Referencia")]
    public float A4Frequency = 440f;

    public static readonly string[] NoteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    public struct ChromaticResult
    {
        public string capturedNoteName; // Nota capturada real (texto rojo)
        public string targetNoteName;   // Nota objetivo cercana (texto verde)
        public float detectedFrequency; // Frecuencia en Hz
        public float targetFrequency;   // Frecuencia teórica de la nota objetivo
        public float centsOffset;       // Desviación (-50 a +50)
        public bool isValid;            // Flag de señal válida
    }

    public ChromaticResult MapFrequencyToNote(float frequency)
    {
        if (frequency <= 0f)
        {
            return new ChromaticResult { isValid = false };
        }

        // Número MIDI fraccionario (logaritmo base 2 con Mathf.Log)
        float midiNoteNum = 69f + 12f * Mathf.Log(frequency / A4Frequency, 2f);

        // Nota objetivo (entero MIDI)
        int targetMidi = Mathf.RoundToInt(midiNoteNum);

        // Frecuencia teórica exacta
        float targetFreq = A4Frequency * Mathf.Pow(2f, (targetMidi - 69) / 12f);

        // Desviación en cents (logaritmo base 2 con Mathf.Log)
        float cents = 1200f * Mathf.Log(frequency / targetFreq, 2f);

        // Nota capturada real
        int capturedMidi = Mathf.FloorToInt(midiNoteNum);

        return new ChromaticResult
        {
            capturedNoteName = GetNoteNameWithOctave(capturedMidi),
            targetNoteName = GetNoteNameWithOctave(targetMidi),
            detectedFrequency = frequency,
            targetFrequency = targetFreq,
            centsOffset = cents,
            isValid = true
        };
    }

    private string GetNoteNameWithOctave(int midiNumber)
    {
        int noteIndex = (midiNumber % 12 + 12) % 12;
        int octave = (midiNumber / 12) - 1;
        return $"{NoteNames[noteIndex]}{octave}";
    }
}
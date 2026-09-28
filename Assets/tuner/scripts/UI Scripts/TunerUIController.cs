using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TunerUIController : MonoBehaviour
{
    private const string A4_PREF_KEY = "A4Frequency";

    [Header("Referencias de Scripts")]
    public PitchDetector pitchDetector;
    public NoteMapper noteMapper;

    [Header("Elementos de Texto UI (TextMeshPro)")]
    public TMP_Text capturedNoteText;   // Corresponde a Text_textoDESafinada (Rojo)
    public TMP_Text targetNoteText;     // Corresponde a Text_texto_afinada (Verde)
    public TMP_Text centsText;          // Corresponde a Text_falta_afinar (Ej: 10 CENT)
    public TMP_Text referencePitchText; // Corresponde a Text_A-referencia (A4= 440HZ)

    [Header("Controles de Referencia A4")]
    public Button plusButton;           // Botón para aumentar la frecuencia de A4
    public Button minusButton;          // Botón para disminuir la frecuencia de A4
    public float minA4Frequency = 415f;  // Límite inferior (ej. Afinación barroca)
    public float maxA4Frequency = 466f;  // Límite superior
    public float stepA4Frequency = 1f;   // Paso de incremento/decremento en Hz

    [Header("Contenedor de Notas Superiores")]
    public Transform notesContainer;    // Arrastrar el GameObject 'notes'
    private TMP_Text[] stringNoteTexts;

    [Header("Indicador Visual")]
    public RectTransform needleTransform; // Corresponde a Image_puntero
    public float maxNeedleAngle = 40f;    // Ángulo máximo configurado en Inspector
    public float smoothSpeed = 10f;       // Velocidad de suavizado de la aguja

    [Header("Configuración de Umbrales y Colores")]
    public Color farColor = Color.red;       // Desafinado (> 20 cents)
    public Color nearColor = Color.yellow;   // Cerca (5 a 20 cents)
    public Color tunedColor = Color.green;    // Afinado (<= 5 cents)
    
    public float nearThreshold = 20f;
    public float tunedThreshold = 5f;

    private float targetAngle = 0f;

    void Start()
    {
        // 1. Asignar los listeners a los botones mediante código
        if (plusButton != null)
        {
            plusButton.onClick.AddListener(IncreaseA4);
        }

        if (minusButton != null)
        {
            minusButton.onClick.AddListener(DecreaseA4);
        }

        // 2. Cargar la frecuencia A4 persistida de PlayerPrefs (por defecto 440 Hz)
        float savedA4 = PlayerPrefs.GetFloat(A4_PREF_KEY, 440f);
        if (noteMapper != null)
        {
            noteMapper.A4Frequency = savedA4;
        }

        // 3. Inicializar la interfaz con el valor recuperado
        UpdateReferenceText();

#if UNITY_EDITOR
        Debug.Log($"[TunerUIController] Cargada frecuencia de referencia A4: {savedA4} Hz desde PlayerPrefs.");
        LogDebugFrequencies(savedA4);
#endif

        // 4. Llenar el arreglo buscando en los hijos del objeto contenedor 'notes'
        if (notesContainer != null)
        {
            stringNoteTexts = notesContainer.GetComponentsInChildren<TMP_Text>();
        }
    }

    void OnDestroy()
    {
        // Limpiar los listeners al destruir el objeto
        if (plusButton != null)
        {
            plusButton.onClick.RemoveListener(IncreaseA4);
        }

        if (minusButton != null)
        {
            minusButton.onClick.RemoveListener(DecreaseA4);
        }
    }

    void Update()
    {
        if (pitchDetector == null || noteMapper == null) return;

        float freq = pitchDetector.CurrentFrequency;

        if (freq > 0f)
        {
            NoteMapper.ChromaticResult result = noteMapper.MapFrequencyToNote(freq);

            if (result.isValid)
            {
                UpdateTexts(result.capturedNoteName, result.targetNoteName);
                UpdateCentsAndColors(result.centsOffset, result.targetNoteName);
                CalculateNeedlePosition(result.centsOffset);
            }
            else
            {
                ResetUI();
            }
        }
        else
        {
            ResetUI();
        }

        AnimateNeedle();
    }

    // --- LÓGICA DE CONTROL DE REFERENCIA A4 Y PERSISTENCIA ---

    public void IncreaseA4()
    {
        ModifyA4Frequency(stepA4Frequency);
    }

    public void DecreaseA4()
    {
        ModifyA4Frequency(-stepA4Frequency);
    }

    private void ModifyA4Frequency(float delta)
    {
        if (noteMapper == null) return;

        // Modificar la frecuencia respetando los límites
        float newA4 = Mathf.Clamp(noteMapper.A4Frequency + delta, minA4Frequency, maxA4Frequency);
        noteMapper.A4Frequency = newA4;

        // Guardar de forma persistente
        PlayerPrefs.SetFloat(A4_PREF_KEY, newA4);
        PlayerPrefs.Save();

        // Actualizar la interfaz de usuario
        UpdateReferenceText();

#if UNITY_EDITOR
        Debug.Log($"[TunerUIController] Referencia A4 actualizada y guardada: {newA4} Hz.");
        LogDebugFrequencies(newA4);
#endif
    }

    private void UpdateReferenceText()
    {
        if (referencePitchText != null && noteMapper != null)
        {
            referencePitchText.text = $"A4= {Mathf.RoundToInt(noteMapper.A4Frequency)}HZ";
        }
    }

    private void LogDebugFrequencies(float currentA4)
    {
        // Muestra en la consola del Editor el recalculo dinamico de notas clave:
        // A4 (Central, MIDI 69), E2 (6ª cuerda guitarra, MIDI 40), E1 (4ª cuerda bajo, MIDI 28)
        float freqE2 = currentA4 * Mathf.Pow(2f, (40 - 69) / 12f);
        float freqE1 = currentA4 * Mathf.Pow(2f, (28 - 69) / 12f);

        Debug.Log($"[DEBUG EDITOR] Recálculo de frecuencias objetivo para A4 = {currentA4} Hz:\n" +
                  $" - A4 (Nota de Referencia): {currentA4:F2} Hz\n" +
                  $" - E2 (Guitarra Cuerda 6): {freqE2:F2} Hz\n" +
                  $" - E1 (Bajo Cuerda 4): {freqE1:F2} Hz");
    }

    // --- LÓGICA DE UI Y RENDERIZADO ---

    private void UpdateTexts(string captured, string target)
    {
        if (capturedNoteText != null) capturedNoteText.text = captured;
        if (targetNoteText != null) targetNoteText.text = target;
    }

    private void UpdateCentsAndColors(float centsOffset, string targetNote)
    {
        float absCents = Mathf.Abs(centsOffset);
        Color statusColor;

        if (absCents <= tunedThreshold)
        {
            statusColor = tunedColor;
        }
        else if (absCents <= nearThreshold)
        {
            statusColor = nearColor;
        }
        else
        {
            statusColor = farColor;
        }

        if (centsText != null)
        {
            centsText.text = $"{Mathf.RoundToInt(absCents)} CENT";
            centsText.color = statusColor;
        }

        if (stringNoteTexts != null)
        {
            foreach (TMP_Text noteTxt in stringNoteTexts)
            {
                if (targetNote.StartsWith(noteTxt.text.Trim(), System.StringComparison.OrdinalIgnoreCase))
                {
                    noteTxt.color = statusColor;
                }
                else
                {
                    noteTxt.color = Color.white;
                }
            }
        }
    }

    private void CalculateNeedlePosition(float cents)
    {
        float clampedCents = Mathf.Clamp(cents, -50f, 50f);
        targetAngle = (clampedCents / 50f) * -maxNeedleAngle;
    }

    private void AnimateNeedle()
    {
        if (needleTransform == null) return;

        Quaternion currentRot = needleTransform.localRotation;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetAngle);
        needleTransform.localRotation = Quaternion.Lerp(currentRot, targetRot, Time.deltaTime * smoothSpeed);
    }

    private void ResetUI()
    {
        targetAngle = 0f;
        if (capturedNoteText != null) capturedNoteText.text = "--";
        if (targetNoteText != null) targetNoteText.text = "--";
        if (centsText != null)
        {
            centsText.text = "0 CENT";
            centsText.color = Color.white;
        }

        if (stringNoteTexts != null)
        {
            foreach (TMP_Text noteTxt in stringNoteTexts)
            {
                noteTxt.color = Color.white;
            }
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FreeTunerUIController : MonoBehaviour
{
    private const string A4_PREF_KEY = "A4Frequency";
    private const string NOTATION_PREF_KEY = "NotationType";

    public enum InstrumentType
    {
        Chromatic,
        Guitar6String,
        Bass4String
    }

    public enum NotationType
    {
        American,  // E1, A1, D2, G2...
        European   // Mi1, La1, Re2, Sol2...
    }

    [Header("Tipo de Instrumento y Notación")]
    public InstrumentType currentInstrument = InstrumentType.Chromatic;
    public NotationType currentNotation = NotationType.American;

    [Header("Controles de Referencia A4 y Notación")]
    public Button plusButton;           
    public Button minusButton;          
    public Button notationButton;       
    public float minA4Frequency = 415f;  
    public float maxA4Frequency = 466f;  
    public float stepA4Frequency = 1f;   

    [Header("Rangos de Frecuencia por Modo (Hz)")]
    public float minBassFreq = 30f;
    public float maxBassFreq = 180f;
    public float minGuitarFreq = 60f;
    public float maxGuitarFreq = 450f;
    public float minChromaticFreq = 30f;
    public float maxChromaticFreq = 1320f;

    [Header("Referencias a Scripts Base")]
    public PitchDetector pitchDetector;
    public NoteMapper noteMapper;

    [Header("Elementos de Texto UI (TextMeshPro)")]
    public TMP_Text capturedNoteText;   
    public TMP_Text targetNoteText;     
    public TMP_Text centsText;          
    public TMP_Text referencePitchText; 

    [Header("Contenedor de Notas Superiores")]
    public Transform notesContainer;    
    private TMP_Text[] stringNoteTexts;

    [Header("Indicador Visual Lineal")]
    public RectTransform needleTransform; 
    public float maxNeedleX = 150f;       
    public float smoothSpeed = 10f;       

    [Header("Configuración de Umbrales y Colores")]
    public Color farColor = Color.red;       
    public Color nearColor = Color.yellow;   
    public Color tunedColor = Color.green;    
    
    public float nearThreshold = 20f;
    public float tunedThreshold = 5f;

    private static readonly string[] Guitar6Notes = { "E2", "A2", "D3", "G3", "B3", "E4" };
    private static readonly string[] Bass4Notes = { "E1", "A1", "D2", "G2" };

    private float targetX = 0f;

    void Start()
    {
        // 1. Asignar los listeners a los botones mediante código
        if (plusButton != null) plusButton.onClick.AddListener(IncreaseA4);
        if (minusButton != null) minusButton.onClick.AddListener(DecreaseA4);
        if (notationButton != null) notationButton.onClick.AddListener(ToggleNotation);

        // 2. Cargar frecuencia A4 y Notación usando UniversalPersistence
        float savedA4 = UniversalPersistence.Load(A4_PREF_KEY, false, 440f);
        if (noteMapper != null)
        {
            noteMapper.A4Frequency = savedA4;
        }

        int savedNotation = Mathf.RoundToInt(UniversalPersistence.Load(NOTATION_PREF_KEY, true, (int)NotationType.American));
        currentNotation = (NotationType)savedNotation;

        // 3. Inicializar la interfaz con los valores recuperados
        UpdateReferenceText();

        if (notesContainer != null)
        {
            stringNoteTexts = notesContainer.GetComponentsInChildren<TMP_Text>();
        }

        SetInstrumentType((int)currentInstrument);
    }

    void OnDestroy()
    {
        if (plusButton != null) plusButton.onClick.RemoveListener(IncreaseA4);
        if (minusButton != null) minusButton.onClick.RemoveListener(DecreaseA4);
        if (notationButton != null) notationButton.onClick.RemoveListener(ToggleNotation);
    }

    void Update()
    {
        if (pitchDetector == null || noteMapper == null) return;

        float freq = pitchDetector.CurrentFrequency;

        if (IsFrequencyInValidRange(freq))
        {
            NoteMapper.ChromaticResult result = noteMapper.MapFrequencyToNote(freq);

            if (result.isValid)
            {
                UpdateTexts(result.capturedNoteName, result.targetNoteName, result.centsOffset);
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

    public void IncreaseA4() => ModifyA4Frequency(stepA4Frequency);
    public void DecreaseA4() => ModifyA4Frequency(-stepA4Frequency);

    private void ModifyA4Frequency(float delta)
    {
        if (noteMapper == null) return;

        float newA4 = Mathf.Clamp(noteMapper.A4Frequency + delta, minA4Frequency, maxA4Frequency);
        noteMapper.A4Frequency = newA4;

        // Persistencia mediante UniversalPersistence (float)
        UniversalPersistence.Save(A4_PREF_KEY, newA4, false);

        UpdateReferenceText();
    }

    private void UpdateReferenceText()
    {
        if (referencePitchText != null && noteMapper != null)
        {
            string formattedLabel = FormatNoteName("A4");
            referencePitchText.text = $"{formattedLabel}= {Mathf.RoundToInt(noteMapper.A4Frequency)}HZ";
        }
    }

    // --- CONTROL DE NOTACIÓN (AMERICANA / EUROPEA) ---

    public void SetNotationType(int notationIndex)
    {
        currentNotation = (NotationType)notationIndex;

        // Persistencia mediante UniversalPersistence (int)
        UniversalPersistence.Save(NOTATION_PREF_KEY, notationIndex, true);

        PopulateNotesContainer();
        UpdateReferenceText();
    }

    public void ToggleNotation()
    {
        int nextNotation = (int)currentNotation == 0 ? 1 : 0;
        SetNotationType(nextNotation);
    }

    private string FormatNoteName(string rawNote)
    {
        if (string.IsNullOrEmpty(rawNote) || rawNote == "--") return rawNote;
        if (currentNotation == NotationType.American) return rawNote;

        return rawNote
            .Replace("C#", "Do#").Replace("C", "Do")
            .Replace("D#", "Re#").Replace("D", "Re")
            .Replace("E", "Mi")
            .Replace("F#", "Fa#").Replace("F", "Fa")
            .Replace("G#", "Sol#").Replace("G", "Sol")
            .Replace("A#", "La#").Replace("A", "La")
            .Replace("B", "Si");
    }

    // --- FILTRADO POR INSTRUMENTO Y RANGOS ---

    private bool IsFrequencyInValidRange(float freq)
    {
        switch (currentInstrument)
        {
            case InstrumentType.Bass4String:
                return freq >= minBassFreq && freq <= maxBassFreq;

            case InstrumentType.Guitar6String:
                return freq >= minGuitarFreq && freq <= maxGuitarFreq;

            case InstrumentType.Chromatic:
            default:
                return freq >= minChromaticFreq && freq <= maxChromaticFreq;
        }
    }

    public void SetInstrumentType(int typeIndex)
    {
        currentInstrument = (InstrumentType)typeIndex;
        PopulateNotesContainer();
    }

    private void PopulateNotesContainer()
    {
        if (stringNoteTexts == null) return;

        for (int i = 0; i < stringNoteTexts.Length; i++)
        {
            stringNoteTexts[i].text = "";
            stringNoteTexts[i].color = Color.white;
        }

        switch (currentInstrument)
        {
            case InstrumentType.Guitar6String:
                for (int i = 0; i < stringNoteTexts.Length && i < Guitar6Notes.Length; i++)
                {
                    stringNoteTexts[i].text = FormatNoteName(Guitar6Notes[i]);
                }
                break;

            case InstrumentType.Bass4String:
                for (int i = 0; i < Bass4Notes.Length; i++)
                {
                    int targetIndex = i + 1;
                    if (targetIndex < stringNoteTexts.Length)
                    {
                        stringNoteTexts[targetIndex].text = FormatNoteName(Bass4Notes[i]);
                    }
                }
                break;

            case InstrumentType.Chromatic:
                break;
        }
    }

    // --- DIBUJADO Y LÓGICA DE UI ---

    private void UpdateTexts(string captured, string target, float centsOffset)
    {
        if (Mathf.Abs(centsOffset) <= tunedThreshold)
        {
            captured = target;
        }

        if (capturedNoteText != null) capturedNoteText.text = FormatNoteName(captured);
        if (targetNoteText != null) targetNoteText.text = FormatNoteName(target);
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

        if (currentInstrument != InstrumentType.Chromatic && stringNoteTexts != null)
        {
            string formattedTarget = FormatNoteName(targetNote);

            foreach (TMP_Text noteTxt in stringNoteTexts)
            {
                if (string.IsNullOrEmpty(noteTxt.text)) continue;

                if (formattedTarget.StartsWith(noteTxt.text.Trim(), System.StringComparison.OrdinalIgnoreCase))
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
        targetX = (clampedCents / 50f) * maxNeedleX;
    }

    private void AnimateNeedle()
    {
        if (needleTransform == null) return;

        needleTransform.localRotation = Quaternion.identity;

        Vector2 currentPos = needleTransform.anchoredPosition;
        Vector2 targetPos = new Vector2(targetX, currentPos.y);

        needleTransform.anchoredPosition = Vector2.Lerp(currentPos, targetPos, Time.deltaTime * smoothSpeed);
    }

    private void ResetUI()
    {
        targetX = 0f;
        if (capturedNoteText != null) capturedNoteText.text = "--";
        if (targetNoteText != null) targetNoteText.text = "--";
        if (centsText != null)
        {
            centsText.text = "0 CENT";
            centsText.color = Color.white;
        }

        if (currentInstrument != InstrumentType.Chromatic && stringNoteTexts != null)
        {
            foreach (TMP_Text noteTxt in stringNoteTexts)
            {
                if (!string.IsNullOrEmpty(noteTxt.text))
                {
                    noteTxt.color = Color.white;
                }
            }
        }
    }
}
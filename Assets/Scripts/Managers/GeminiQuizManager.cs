using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

// ==========================================
// 1. STRUKTUR CLASS REQUEST & RESPONSE API
// ==========================================
[Serializable]
public class GeminiPart { public string text; }

[Serializable]
public class GeminiContent { public List<GeminiPart> parts; }

[Serializable]
public class GeminiRequest { public List<GeminiContent> contents; }

[Serializable]
public class GeminiResponse { public List<GeminiCandidate> candidates; }

[Serializable]
public class GeminiCandidate { public GeminiResponseContent content; }

[Serializable]
public class GeminiResponseContent { public List<GeminiPart> parts; }

// ==========================================
// 2. SCRIPT UTAMA MANAGER
// ==========================================
public class GeminiQuizManager : MonoBehaviour
{
    [Header("API Configuration")]
    [SerializeField] private string apiKey = "";
    private string geminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite:generateContent?key=";

    [Header("UI Canvas Gameplay References")]
    [SerializeField] private GameObject canvasQuizMenu;
    [SerializeField] private GameObject canvasQuizGameplay;
    [SerializeField] private GameObject canvasQuizFeedback;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI numberQuizText;
    [SerializeField] private TextMeshProUGUI questionText;

    [Header("UI Solo Quiz Setup")]
    [SerializeField] private GameObject panelAwal;
    [SerializeField] private GameObject panelPilihMode;         // Panel Pemilihan Mode (Online / Offline)
    [SerializeField] private GameObject panelPilihJumlahSoal;   // Panel Pemilihan Jumlah Soal (5, 10, 15)
    
    [Header("UI Buttons")]
    [SerializeField] private Button[] optionButtons; 
    [SerializeField] private TextMeshProUGUI[] optionTexts; 

    [Header("UI Feedback References")]
    [SerializeField] private TextMeshProUGUI feedbackTitleText; 
    [SerializeField] private TextMeshProUGUI feedbackExplanationText;

    [Header("UI Separate Panels (Fase 2)")]
    [SerializeField] private GameObject panelPopupFeedback; 
    [SerializeField] private GameObject panelEndFeedback;   
    [SerializeField] private TextMeshProUGUI endFeedbackText;

    [Header("Multiplayer Room Setup")]
    [SerializeField] private GameObject panelGameplaySiswa;  
    [SerializeField] private GameObject popupKeluarGameplay;
    [SerializeField] private GameObject panelDashboardGuru;   
    [SerializeField] private FirebaseStudentManager firebaseStudentManager;

    // --- STATE KONTROL ONLINE / OFFLINE ---
    private bool isOfflineMode = false;
    private List<Question> quizDataList = new List<Question>();
    private int currentQuestionIndex = 0;
    private int totalQuestions = 5;
    private float timePerQuestion = 20f;
    private Coroutine timerCoroutine;
    private bool isAnswering = false;
    private int score = 0;

    void Awake()
    {
        if (firebaseStudentManager == null)
        {
            firebaseStudentManager = FindAnyObjectByType<FirebaseStudentManager>();
            if (firebaseStudentManager != null)
            {
                Debug.Log("[Auto-Assign] FirebaseStudentManager berhasil ditemukan otomatis!");
            }
        }

        if (panelGameplaySiswa == null)
        {
            GameObject goSiswa = GameObject.Find("Panel_gameplaySiswa");
            if (goSiswa != null) panelGameplaySiswa = goSiswa;
        }

        if (panelDashboardGuru == null)
        {
            GameObject goGuru = GameObject.Find("Panel_dashboardGuru");
            if (goGuru != null) panelDashboardGuru = goGuru;
        }

        LoadApiKey();
    }

    void LoadApiKey()
    {
        TextAsset keyAsset = Resources.Load<TextAsset>("config");

        if (keyAsset != null)
        {
            apiKey = keyAsset.text.Trim();
            Debug.Log($"[Resources] API Key sukses dimuat!");
        }
        else
        {
            Debug.LogError("File config.txt tidak ditemukan di folder Assets/Resources/!");
        }
    }

    // ==========================================
    // ALUR PEMILIHAN MODE & JUMLAH SOAL
    // ==========================================
    // Memilih Latihan Soal
    public void pilihLatihanSoal()
    {
        panelAwal.SetActive(false);
        panelPilihMode.SetActive(true);
    }

    public void batalLatihanSoal()
    {
        panelPilihMode.SetActive(false);
        panelAwal.SetActive(true);
    }

    // Hubungkan ke Tombol "Solo Quiz (Online AI)"
    public void PilihModeSoloOnline()
    {
        isOfflineMode = false;
        if (panelPilihMode != null) panelPilihMode.SetActive(false);
        if (panelPilihJumlahSoal != null) panelPilihJumlahSoal.SetActive(true);
        Debug.Log("[Solo Mode] Online Mode Dipilih.");
    }

    // Hubungkan ke Tombol "Solo Quiz (Offline / Tanpa Internet)"
    public void PilihModeSoloOffline()
    {
        isOfflineMode = true;
        if (panelPilihMode != null) panelPilihMode.SetActive(false);
        if (panelPilihJumlahSoal != null) panelPilihJumlahSoal.SetActive(true);
        Debug.Log("[Solo Mode] Offline Mode Dipilih.");
    }

    // Hubungkan ke Tombol Batal di Panel Pilih Jumlah Soal
    public void BatalPilihJumlahSoal()
    {
        if (panelPilihJumlahSoal != null) panelPilihJumlahSoal.SetActive(false);
        if (panelPilihMode != null) panelPilihMode.SetActive(true);
    }

    // Hubungkan ke Tombol Angka (5, 10, 15) di Panel Pilih Jumlah Soal
    public void PilihJumlahSoalDanMulai(int jumlah)
    {
        totalQuestions = jumlah;

        if (panelPilihJumlahSoal != null) panelPilihJumlahSoal.SetActive(false);
        if (canvasQuizMenu != null) canvasQuizMenu.SetActive(false);
        if (canvasQuizGameplay != null) canvasQuizGameplay.SetActive(true);
        if (panelGameplaySiswa != null) panelGameplaySiswa.SetActive(true);

        if (isOfflineMode)
        {
            StartSoloQuizOffline();
        }
        else
        {
            StartSoloQuizOnline();
        }
    }

    // ==========================================
    // LOGIKA SOAL OFFLINE (LOCAL JSON BANK)
    // ==========================================
    private void StartSoloQuizOffline()
    {
        currentQuestionIndex = 0;
        score = 0;
        quizDataList.Clear();

        TextAsset jsonBank = Resources.Load<TextAsset>("offline_quiz_bank");

        if (jsonBank != null)
        {
            try
            {
                QuizContainer container = JsonUtility.FromJson<QuizContainer>(jsonBank.text);

                if (container != null && container.questions != null && container.questions.Count > 0)
                {
                    List<Question> masterList = new List<Question>(container.questions);

                    // --- ALGORITMA FISHER-YATES SHUFFLE (PENGACAKAN SOAL) ---
                    System.Random rng = new System.Random();
                    int n = masterList.Count;
                    while (n > 1)
                    {
                        n--;
                        int k = rng.Next(n + 1);
                        Question value = masterList[k];
                        masterList[k] = masterList[n];
                        masterList[n] = value;
                    }

                    // Ambil sejumlah totalQuestions dari hasil pengacakan
                    int limit = Mathf.Min(totalQuestions, masterList.Count);
                    quizDataList = masterList.GetRange(0, limit);

                    Debug.Log($"[Offline Quiz] Sukses memuat dan mengacak {quizDataList.Count} soal dari bank lokal!");
                    DisplayQuestion();
                }
                else
                {
                    questionText.text = "Format file JSON bank soal lokal tidak sesuai.";
                }
            }
            catch (Exception e)
            {
                questionText.text = "Gagal memproses bank soal lokal.";
                Debug.LogError("[Offline Quiz] Error parsing JSON: " + e.Message);
            }
        }
        else
        {
            questionText.text = "File offline_quiz_bank.json tidak ditemukan di Assets/Resources/";
            Debug.LogError("[Offline Quiz] File offline_quiz_bank.json hilang!");
        }
    }

    // ==========================================
    // LOGIKA SOAL ONLINE (GEMINI API)
    // ==========================================
    private void StartSoloQuizOnline()
    {
        currentQuestionIndex = 0;
        score = 0;
        quizDataList.Clear();

        questionText.text = "Sedang meracik soal dari AI Gemini...";
        ToggleButtonsInteractable(false);

        StartCoroutine(FetchQuestionsFromGemini());
    }

    IEnumerator FetchQuestionsFromGemini()
    {
        string fullUrl = geminiUrl + apiKey;

        string prompt = $"Buat {totalQuestions} soal pilihan ganda interaktif tentang nutrisi, zat gizi, dan makanan sehat untuk anak Sekolah Dasar berbentuk cerita pendek. " +
                        "Format output WAJIB dalam bentuk JSON mentah dengan struktur tepat seperti ini: " +
                        "{\"questions\": [{\"questionText\":\"...\", \"optionA\":\"...\", \"optionB\":\"...\", \"optionC\":\"...\", \"optionD\":\"...\", \"correctAnswer\":\"A/B/C/D\", \"explanation\":\"...\"}]}. " +
                        "Jangan berikan teks tambahan atau penjelasan di luar format JSON. Jangan pakai format markdown ```json.";

        GeminiRequest requestBody = new GeminiRequest();
        requestBody.contents = new List<GeminiContent>
        {
            new GeminiContent
            {
                parts = new List<GeminiPart> { new GeminiPart { text = prompt } }
            }
        };

        string jsonBody = JsonUtility.ToJson(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(fullUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string rawResponse = request.downloadHandler.text;
                ParseAndStartQuiz(rawResponse);
            }
            else
            {
                questionText.text = "Gagal terhubung ke AI. Silakan periksa jaringan internet atau gunakan Mode Offline.";
                Debug.LogError("Error Gemini API: " + request.error + " | Response: " + request.downloadHandler.text);
            }
        }
    }

    void ParseAndStartQuiz(string rawJson)
    {
        try
        {
            GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(rawJson);

            if (response != null && response.candidates != null && response.candidates.Count > 0)
            {
                string cleanJson = response.candidates[0].content.parts[0].text;

                if (cleanJson.StartsWith("```json")) cleanJson = cleanJson.Replace("```json", "");
                if (cleanJson.EndsWith("```")) cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                cleanJson = cleanJson.Trim();

                QuizContainer container = JsonUtility.FromJson<QuizContainer>(cleanJson);

                if (container != null && container.questions != null && container.questions.Count > 0)
                {
                    quizDataList = container.questions;
                    DisplayQuestion();
                }
                else
                {
                    questionText.text = "Format kuis dari AI tidak sesuai. Coba lagi.";
                }
            }
            else
            {
                questionText.text = "AI tidak memberikan respon. Coba klik mulai ulang.";
            }
        }
        catch (Exception e)
        {
            questionText.text = "Gagal memproses soal AI. Coba klik mulai ulang.";
            Debug.LogError("Parsing Error: " + e.Message);
        }
    }

    // ==========================================
    // GAMEPLAY CONTROLLER (DUNIA APLIKASI)
    // ==========================================
    void DisplayQuestion()
    {
        if (currentQuestionIndex >= quizDataList.Count)
        {
            EndQuiz();
            return;
        }

        isAnswering = true;
        ToggleButtonsInteractable(true);
        canvasQuizFeedback.SetActive(false);

        Question currentQuestion = quizDataList[currentQuestionIndex];

        numberQuizText.text = (currentQuestionIndex + 1).ToString();
        questionText.text = currentQuestion.questionText;
        optionTexts[0].text = currentQuestion.optionA;
        optionTexts[1].text = currentQuestion.optionB;
        optionTexts[2].text = currentQuestion.optionC;
        optionTexts[3].text = currentQuestion.optionD;

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(StartTimerCountdown());
    }

    IEnumerator StartTimerCountdown()
    {
        float timeLeft = timePerQuestion;
        while (timeLeft > 0)
        {
            timerText.text = Mathf.CeilToInt(timeLeft).ToString();
            yield return new WaitForSeconds(1f);
            timeLeft -= 1f;
        }

        timerText.text = "0";
        HandleAnswerSelected(""); 
    }

    public void OnAnswerButtonClick(string selectedOption)
    {
        if (!isAnswering) return;
        HandleAnswerSelected(selectedOption);
    }

    void HandleAnswerSelected(string selectedOption)
    {
        isAnswering = false;
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        ToggleButtonsInteractable(false);

        Question currentQuestion = quizDataList[currentQuestionIndex];
        canvasQuizFeedback.SetActive(true);
        panelPopupFeedback.SetActive(true);
        panelEndFeedback.SetActive(false);

        if (selectedOption == "")
        {
            feedbackTitleText.text = "Waktu Habis!";
            feedbackTitleText.color = new Color32(230, 126, 34, 255); 
        }
        else if (selectedOption == currentQuestion.correctAnswer)
        {
            feedbackTitleText.text = "Jawabanmu Benar!";
            feedbackTitleText.color = new Color32(46, 204, 113, 255); 
            score += 10; 
        }
        else
        {
            feedbackTitleText.text = $"Jawabanmu Salah!\n(Kunci: {currentQuestion.correctAnswer})";
            feedbackTitleText.color = new Color32(231, 76, 60, 255); 
        }

        feedbackExplanationText.text = currentQuestion.explanation; 
    }

    public void NextQuestion()
    {
        canvasQuizFeedback.SetActive(false);
        currentQuestionIndex++;
        DisplayQuestion();
    }

    void ToggleButtonsInteractable(bool state)
    {
        foreach (Button btn in optionButtons)
        {
            btn.interactable = state;
        }
    }

    public void StartMultiplayerQuiz(string cleanJsonDariFirebase)
    {
        currentQuestionIndex = 0;
        score = 0;
        quizDataList.Clear();

        try
        {
            QuizContainer container = JsonUtility.FromJson<QuizContainer>(cleanJsonDariFirebase);

            if (container != null && container.questions != null && container.questions.Count > 0)
            {
                quizDataList = container.questions;

                if (canvasQuizGameplay != null) canvasQuizGameplay.SetActive(true);

                if (panelGameplaySiswa != null) 
                {
                    panelGameplaySiswa.SetActive(true);
                }

                if (panelDashboardGuru != null) panelDashboardGuru.SetActive(false); 

                DisplayQuestion();
            }
            else
            {
                questionText.text = "Format kuis multiplayer kosong atau tidak cocok.";
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Multiplayer Parsing Error: " + e.Message);
        }
    }

    public void KlikTombolBackGameplay()
    {
        if (popupKeluarGameplay != null)
        {
            popupKeluarGameplay.SetActive(true);
        }
    }

    public void KonfirmasiKeluarGameplayYA()
    {
        if (popupKeluarGameplay != null) popupKeluarGameplay.SetActive(false);

        StopAllCoroutines();

        if (canvasQuizGameplay != null) canvasQuizGameplay.SetActive(false);
        if (canvasQuizMenu != null) canvasQuizMenu.SetActive(true); 

        if (firebaseStudentManager != null && !isOfflineMode)
        {
            firebaseStudentManager.SiswaKeluarTengahGameplay();
        }
    }

    public void KonfirmasiKeluarGameplayTIDAK()
    {
        if (popupKeluarGameplay != null)
        {
            popupKeluarGameplay.SetActive(false);
        }
    }

    void EndQuiz()
    {
        canvasQuizGameplay.SetActive(false);
        canvasQuizMenu.SetActive(false);
        canvasQuizFeedback.SetActive(true);
        panelPopupFeedback.SetActive(false); 
        popupKeluarGameplay.SetActive(false);
        panelEndFeedback.SetActive(true);

        endFeedbackText.text = $"KUIS SELESAI!\n\nTotal Skor Kamu:\n<color=green>{score}</color>";

        // Tembak skor ke Firebase hanya jika tidak dalam mode Offline
        if (!isOfflineMode && firebaseStudentManager != null)
        {
            firebaseStudentManager.UpdateSkorAkhirSiswa(score);
        }
    }
}
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
// 1. STRUKTUR CLASS UNTUK REQUEST & RESPONSE API
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
    [SerializeField] private GameObject panelGameplaySiswa;  // Drag Panel_GameplaySiswa ke sini
    [SerializeField] private GameObject popupKeluarGameplay;
    [SerializeField] private GameObject panelDashboardGuru;   // Drag Panel_dashboardGuru ke sini
    [SerializeField] private FirebaseStudentManager firebaseStudentManager; // Drag object script murid ke sini

    private List<Question> quizDataList = new List<Question>();
    private int currentQuestionIndex = 0;
    private int totalQuestions = 5;
    private float timePerQuestion = 20f;
    private Coroutine timerCoroutine;
    private bool isAnswering = false;
    private int score = 0;

    void Awake()
    {
        // 1. AUTO-ASSIGN: Cari FirebaseStudentManager secara otomatis di dalam scene
        if (firebaseStudentManager == null)
        {
            firebaseStudentManager = FindAnyObjectByType<FirebaseStudentManager>();
            if (firebaseStudentManager != null)
            {
                Debug.Log("[Auto-Assign] FirebaseStudentManager berhasil ditemukan otomatis oleh GeminiQuizManager bro!");
            }
        }

        // 2. AUTO-ASSIGN PANEL: Jika slot panel kosong, coba cari berdasarkan nama objek di hierarchy
        if (panelGameplaySiswa == null)
        {
            GameObject goSiswa = GameObject.Find("Panel_gameplaySiswa"); // Sesuaikan dengan nama persis di hierarchy lu
            if (goSiswa != null) panelGameplaySiswa = goSiswa;
        }

        if (panelDashboardGuru == null)
        {
            GameObject goGuru = GameObject.Find("Panel_dashboardGuru"); // Sesuaikan dengan nama persis di hierarchy lu
            if (goGuru != null) panelDashboardGuru = goGuru;
        }
        LoadApiKey();
    }

    void LoadApiKey()
    {
        // Menggunakan Resources.Load, trik paling sakti dan aman lintas platform!
        TextAsset keyAsset = Resources.Load<TextAsset>("config");

        if (keyAsset != null)
        {
            apiKey = keyAsset.text.Trim();
            Debug.Log($"[Resources] API Key sukses dimuat! Karakter depan: {apiKey.Substring(0, 5)}...");
        }
        else
        {
            Debug.LogError("File config.txt tidak ditemukan di folder Assets/Resources/ bro!");
        }
    }

    public void StartSoloQuiz()
    {
        currentQuestionIndex = 0;
        score = 0;
        quizDataList.Clear();
        
        questionText.text = "Sedang membuat soal kuis gizi dengan AI...";
        ToggleButtonsInteractable(false);

        StartCoroutine(Fetch15QuestionsFromGemini());
    }

    IEnumerator Fetch15QuestionsFromGemini()
    {
        string fullUrl = geminiUrl + apiKey;

        string prompt = $"Buat {totalQuestions} soal pilihan ganda interaktif tentang nutrisi, zat gizi, dan makanan sehat untuk anak Sekolah Dasar berbentuk cerita pendek. " +
                        "Format output WAJIB dalam bentuk JSON mentah dengan struktur tepat seperti ini: " +
                        "{\"questions\": [{\"questionText\":\"...\", \"optionA\":\"...\", \"optionB\":\"...\", \"optionC\":\"...\", \"optionD\":\"...\", \"correctAnswer\":\"A/B/C/D\", \"explanation\":\"...\"}]}. " +
                        "Jangan berikan teks tambahan atau penjelasan di luar format JSON. Jangan pakai format markdown ```json.";

        // FIX 400: Bungkus ke Class Request, biarkan Unity melakukan auto-escape tanda kutip
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
                questionText.text = "Gagal terhubung ke AI. Cek koneksi internet lu bro.";
                Debug.LogError("Error Gemini API: " + request.error + " | Response: " + request.downloadHandler.text);
            }
        }
    }

    void ParseAndStartQuiz(string rawJson)
    {
        try
        {
            // FIX PARSING: Bongkar json respon Google API dulu bro
            GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(rawJson);
            
            if (response != null && response.candidates != null && response.candidates.Count > 0)
            {
                // Ambil string text kuis murni yang ada di dalam response Gemini
                string cleanJson = response.candidates[0].content.parts[0].text;
                
                // Antisipasi kalau Gemini nakal tetep ngasih tag markdown ```json
                if (cleanJson.StartsWith("```json")) cleanJson = cleanJson.Replace("```json", "");
                if (cleanJson.EndsWith("```")) cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                cleanJson = cleanJson.Trim();

                // Baru masukkan ke container kuis kita
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
        catch (System.Exception e)
        {
            questionText.text = "Gagal memproses soal AI. Coba klik mulai ulang.";
            Debug.LogError("Parsing Error: " + e.Message);
        }
    }

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

// Fungsi baru untuk menerima limpahan soal dari Firebase (Multiplayer)
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
            
            // --- BAGIAN INI WAJIB ADA JANGAN SAMPAI KELIPAT ATAU TERHAPUS ---
            if (canvasQuizGameplay != null) canvasQuizGameplay.SetActive(true);
            
            if (panelGameplaySiswa != null) 
            {
                panelGameplaySiswa.SetActive(true);  // Ini yang bikin murid otomatis masuk kuis!
                Debug.Log("Panel Gameplay Siswa diaktifkan otomatis.");
            }
            else {
                Debug.LogError("panelGameplaySiswa masih NULL di GeminiQuizManager!");
            }

            if (panelDashboardGuru != null) panelDashboardGuru.SetActive(false); 
            
            DisplayQuestion();
        }
        else
        {
            questionText.text = "Format kuis multiplayer kosong atau tidak cocok.";
        }
    }
    catch (System.Exception e)
    {
        Debug.LogError("Multiplayer Parsing Error: " + e.Message);
    }
}

// 1. HUBUNGKAN FUNGSI INI KE ON CLICK TOMBOL BACK DI PANEL GAMEPLAY SISWA LU
    public void KlikTombolBackGameplay()
    {
        if (popupKeluarGameplay != null)
        {
            popupKeluarGameplay.SetActive(true); // Cuma memunculkan popup konfirmasi, GAK langsung keluar!
            Debug.Log("Popup konfirmasi keluar gameplay diaktifkan.");
        }
        else
        {
            Debug.LogError("popupKeluarGameplay masih NULL di Inspector, drag dulu bro!");
        }
    }

    // 2. HUBUNGKAN FUNGSI INI KE TOMBOL "YA" (Siswa fix mau keluar)
    public void KonfirmasiKeluarGameplayYA()
    {
        // 1. Tutup popup konfirmasinya terlebih dahulu biar gak numpuk
        if (popupKeluarGameplay != null) popupKeluarGameplay.SetActive(false);

        // 2. STOP COUNTDOWN TIMER (Sapu jagat agar timer latar belakang mati total!)
        StopAllCoroutines();
        Debug.Log("Semua coroutine dan timer gameplay berhasil dihentikan.");

        // 3. MATIKAN CANVAS GAMEPLAY & AKTIFKAN KEMBALI CANVAS MENU UTAMA
        // Kode ini ditaruh di luar IF agar mode SOLO maupun MULTIPLAYER sama-sama bisa balik ke menu!
        if (canvasQuizGameplay != null) canvasQuizGameplay.SetActive(false);
        if (canvasQuizMenu != null) canvasQuizMenu.SetActive(true); 

        // 4. JALANKAN LOGIKA TAMBAHAN KHUSUS MULTIPLAYER (JIKA FIREBASE AKTIF)
        if (firebaseStudentManager != null)
        {
            // Tembak data ke Firebase bahwa murid ini statusnya "canceled"
            firebaseStudentManager.SiswaKeluarTengahGameplay();
            Debug.Log("Siswa mengonfirmasi keluar kuis MULTIPLAYER tengah jalan.");
        }
        else
        {
            // Jika masuk ke sini, artinya siswa sedang bermain kuis mode SOLO
            Debug.Log("Siswa mengonfirmasi keluar kuis SOLO, kembali ke menu utama.");
        }
    }

    // 3. HUBUNGKAN FUNGSI INI KE TOMBOL "TIDAK" (Siswa gak sengaja pencet / batal keluar)
    public void KonfirmasiKeluarGameplayTIDAK()
    {
        if (popupKeluarGameplay != null)
        {
            popupKeluarGameplay.SetActive(false); // Sembunyikan lagi popup-nya, kuis otomatis lanjut!
            Debug.Log("Siswa membatalkan keluar kuis, permainan dilanjutkan.");
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
        
        // Pengaturan teks skor bawaan lu (sesuaikan dengan kode asli lu bro)
        endFeedbackText.text = $"KUIS SELESAI!\n\nTotal Skor Kamu:\n<color=green>{score}</color>";

        // --- TEMBAK NILAI & STATUS KE FIREBASE SAAT MASUK MENU INFO NILAI ---
        if (firebaseStudentManager != null)
        {
            firebaseStudentManager.UpdateSkorAkhirSiswa(score);
        }
        else
        {
            Debug.LogWarning("FirebaseStudentManager belum di-drag ke Inspector GeminiQuizManager, skor gagal dikirim otomatis.");
        }
    }
}
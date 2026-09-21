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
// 2. STRUKTUR DATA FASE 2: KANTIN SEHAT
// ==========================================
[Serializable]
public class FoodItem
{
    public string foodName;            // Contoh: "Susu UHT Plain", "Es Lilin Sirup"
    public Sprite foodSprite;          // Gambar Makanan/Minuman
    public bool isHealthy;             // true = Sehat (+10), false = Buruk/Junk Food (+0)
    [TextArea(2, 4)]
    public string eduText;             // Teks Edukasi / Penjelasan Zibo
}

public class GeneratedKantinQuestion
{
    public FoodItem[] options = new FoodItem[4]; // 4 Slot pilihan (A, B, C, D)
    public int correctIndex;                     // Indeks slot gambar sehat (0, 1, 2, atau 3)
}

public enum QuizPhase
{
    TeoriText,
    TransitionPopup,
    KantinSehat
}

// ==========================================
// 3. SCRIPT UTAMA MANAGER
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
    
    [Header("UI Buttons Teori (Fase 1)")]
    [SerializeField] private Button[] optionButtons; 
    [SerializeField] private TextMeshProUGUI[] optionTexts; 

    [Header("UI Feedback References")]
    [SerializeField] private TextMeshProUGUI feedbackTitleText; 
    [SerializeField] private TextMeshProUGUI feedbackExplanationText;

    [Header("UI Separate Panels (Feedback & Result)")]
    [SerializeField] private GameObject panelPopupFeedback; 
    [SerializeField] private GameObject panelEndFeedback;   
    [SerializeField] private TextMeshProUGUI endFeedbackText;

    [Header("Multiplayer Room Setup")]
    [SerializeField] private GameObject panelGameplaySiswa;  
    [SerializeField] private GameObject popupKeluarGameplay;
    [SerializeField] private GameObject panelDashboardGuru;   
    [SerializeField] private FirebaseStudentManager firebaseStudentManager;

    // ==========================================
    // FASE 2: SIMULASI KANTIN SEHAT UI & DATABASE
    // ==========================================
    [Header("Fase 2: Sub-Panels Gameplay")]
    [SerializeField] private GameObject panelGameplayTeori;       // Sub-panel Soal Teks
    [SerializeField] private GameObject panelKantinSehat;         // Sub-panel Soal Bergambar Kantin
    [SerializeField] private GameObject panelTransisiKantin;      // Child Popup Transisi Bonus Stage (Di dalam Panel Kantin)

    [Header("Fase 2: UI Elements Kantin Sehat")]
    [SerializeField] private TextMeshProUGUI kantinQuestionText;
    [SerializeField] private Button[] kantinOptionButtons;        // 4 Button kantin
    [SerializeField] private Image[] kantinOptionImages;          // 4 Component Image di button kantin
    [SerializeField] private TextMeshProUGUI[] kantinOptionNameTexts; // 4 Text nama makanan di button kantin

    [Header("Fase 2: Database Makanan Lokal")]
    [SerializeField] private List<FoodItem> healthyFoodList = new List<FoodItem>();   // Min 5 item sehat
    [SerializeField] private List<FoodItem> unhealthyFoodList = new List<FoodItem>(); // Min 15 item buruk

    // --- STATE KONTROL & TRACKING ---
    private QuizPhase currentPhase = QuizPhase.TeoriText;
    private bool isOfflineMode = false;
    private List<Question> quizDataList = new List<Question>();
    private List<GeneratedKantinQuestion> generatedKantinQuestions = new List<GeneratedKantinQuestion>();

    private int currentQuestionIndex = 0; // Indeks Soal Teori
    private int kantinCurrentIndex = 0;   // Indeks Soal Kantin (0 s/d 4)
    private int totalQuestions = 5;       // Pilihan awal jumlah soal teori (5, 10, atau 15)
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
    public void pilihLatihanSoal()
    {
        if (panelAwal != null) panelAwal.SetActive(false);
        if (panelPilihMode != null) panelPilihMode.SetActive(true);
    }

    public void batalLatihanSoal()
    {
        if (panelPilihMode != null) panelPilihMode.SetActive(false);
        if (panelAwal != null) panelAwal.SetActive(true);
    }

    public void PilihModeSoloOnline()
    {
        isOfflineMode = false;
        if (panelPilihMode != null) panelPilihMode.SetActive(false);
        if (panelPilihJumlahSoal != null) panelPilihJumlahSoal.SetActive(true);
        Debug.Log("[Solo Mode] Online Mode Dipilih.");
    }

    public void PilihModeSoloOffline()
    {
        isOfflineMode = true;
        if (panelPilihMode != null) panelPilihMode.SetActive(false);
        if (panelPilihJumlahSoal != null) panelPilihJumlahSoal.SetActive(true);
        Debug.Log("[Solo Mode] Offline Mode Dipilih.");
    }

    public void BatalPilihJumlahSoal()
    {
        if (panelPilihJumlahSoal != null) panelPilihJumlahSoal.SetActive(false);
        if (panelPilihMode != null) panelPilihMode.SetActive(true);
    }

    public void PilihJumlahSoalDanMulai(int jumlah)
    {
        totalQuestions = jumlah;
        currentPhase = QuizPhase.TeoriText;

        if (panelPilihJumlahSoal != null) panelPilihJumlahSoal.SetActive(false);
        if (canvasQuizMenu != null) canvasQuizMenu.SetActive(false);
        if (canvasQuizGameplay != null) canvasQuizGameplay.SetActive(true);
        if (panelGameplaySiswa != null) panelGameplaySiswa.SetActive(true);

        // Reset Tampilan Sub-Panel Gameplay
        if (panelGameplayTeori != null) panelGameplayTeori.SetActive(true);
        if (panelKantinSehat != null) panelKantinSehat.SetActive(false);
        if (panelTransisiKantin != null) panelTransisiKantin.SetActive(false);

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
    // LOGIKA SOAL TEORI OFFLINE & ONLINE
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
                    List<Question> masterList = ShuffleList(container.questions);
                    int limit = Mathf.Min(totalQuestions, masterList.Count);
                    quizDataList = masterList.GetRange(0, limit);

                    Debug.Log($"[Offline Quiz] Sukses memuat {quizDataList.Count} soal teori!");
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
    // GAMEPLAY CONTROLLER (FASE 1 & FASE 2)
    // ==========================================
    void DisplayQuestion()
    {
        // Pengecekan Transisi: Jika Fase Teori Selesai -> Buka Popup Transisi Kantin
        if (currentPhase == QuizPhase.TeoriText && currentQuestionIndex >= quizDataList.Count)
        {
            TampilkanPopupTransisiKantin();
            return;
        }

        // Pengecekan Selesai: Jika 5 Soal Kantin Selesai -> Panggil EndQuiz()
        if (currentPhase == QuizPhase.KantinSehat && kantinCurrentIndex >= 5)
        {
            EndQuiz();
            return;
        }

        isAnswering = true;
        canvasQuizFeedback.SetActive(false);

        if (currentPhase == QuizPhase.TeoriText)
        {
            // --- TAMPILKAN UI SOAL TEORI AI ---
            if (panelGameplayTeori != null) panelGameplayTeori.SetActive(true);
            if (panelKantinSehat != null) panelKantinSehat.SetActive(false);
            ToggleButtonsInteractable(true);

            Question currentQuestion = quizDataList[currentQuestionIndex];

            numberQuizText.text = $"Soal {currentQuestionIndex + 1}/{quizDataList.Count}";
            questionText.text = currentQuestion.questionText;
            optionTexts[0].text = currentQuestion.optionA;
            optionTexts[1].text = currentQuestion.optionB;
            optionTexts[2].text = currentQuestion.optionC;
            optionTexts[3].text = currentQuestion.optionD;
        }
        else if (currentPhase == QuizPhase.KantinSehat)
        {
            // --- TAMPILKAN UI SOAL KANTIN SEHAT ---
            if (panelGameplayTeori != null) panelGameplayTeori.SetActive(false);
            if (panelKantinSehat != null) panelKantinSehat.SetActive(true);
            if (panelTransisiKantin != null) panelTransisiKantin.SetActive(false); // Sembunyikan popup transisi
            ToggleKantinButtonsInteractable(true);

            GeneratedKantinQuestion kq = generatedKantinQuestions[kantinCurrentIndex];

            numberQuizText.text = $"Tantangan Kantin: {kantinCurrentIndex + 1}/5";
            if (kantinQuestionText != null) 
                kantinQuestionText.text = "Pilih 1 makanan atau minuman yang paling SEHAT & BERGIZI!";

            for (int i = 0; i < 4; i++)
            {
                FoodItem item = kq.options[i];
                if (kantinOptionImages != null && i < kantinOptionImages.Length && kantinOptionImages[i] != null) 
                    kantinOptionImages[i].sprite = item.foodSprite;
                if (kantinOptionNameTexts != null && i < kantinOptionNameTexts.Length && kantinOptionNameTexts[i] != null) 
                    kantinOptionNameTexts[i].text = item.foodName;
            }
        }

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(StartTimerCountdown());
    }

    // ==========================================
    // LOGIKA GENERATE & TRANSISI KANTIN SEHAT
    // ==========================================
    private void TampilkanPopupTransisiKantin()
    {
        currentPhase = QuizPhase.TransitionPopup;
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        if (panelGameplayTeori != null) panelGameplayTeori.SetActive(false);
        if (panelKantinSehat != null) panelKantinSehat.SetActive(true);
        if (panelTransisiKantin != null) panelTransisiKantin.SetActive(true); // Tampilkan popup transisi di atas panel kantin
    }

    // Hubungkan ke Tombol "Masuk Kantin Sehat" pada Popup Transisi
    public void KlikMulaiTantanganKantin()
    {
        GenerateKantinQuestions(); // Racik 5 Soal Kantin Sehat secara acak (1 Sehat + 3 Buruk)
        kantinCurrentIndex = 0;
        currentPhase = QuizPhase.KantinSehat;

        DisplayQuestion();
    }

    private void GenerateKantinQuestions()
    {
        generatedKantinQuestions.Clear();

        List<FoodItem> shuffledHealthy = ShuffleList(healthyFoodList);

        for (int i = 0; i < 5; i++) // Selalu 5 Soal Kantin Sehat
        {
            GeneratedKantinQuestion kq = new GeneratedKantinQuestion();
            kq.options = new FoodItem[4];

            // 1. Ambil 1 makanan sehat untuk soal ini
            FoodItem healthyItem = (i < shuffledHealthy.Count) ? shuffledHealthy[i] : healthyFoodList[UnityEngine.Random.Range(0, healthyFoodList.Count)];

            // 2. Ambil 3 makanan buruk secara acak tanpa duplikasi dalam 1 soal
            List<FoodItem> shuffledUnhealthy = ShuffleList(unhealthyFoodList);
            List<FoodItem> selectedUnhealthy = new List<FoodItem>();
            for (int j = 0; j < 3 && j < shuffledUnhealthy.Count; j++)
            {
                selectedUnhealthy.Add(shuffledUnhealthy[j]);
            }

            // 3. Acak posisi slot kunci jawaban sehat (0 = A, 1 = B, 2 = C, 3 = D)
            kq.correctIndex = UnityEngine.Random.Range(0, 4);
            kq.options[kq.correctIndex] = healthyItem;

            // 4. Isi 3 slot sisanya dengan makanan buruk
            int unhealthyIdx = 0;
            for (int slot = 0; slot < 4; slot++)
            {
                if (slot == kq.correctIndex) continue;
                kq.options[slot] = selectedUnhealthy[unhealthyIdx];
                unhealthyIdx++;
            }

            generatedKantinQuestions.Add(kq);
        }
    }

    // ==========================================
    // HANDLING JAWABAN & FEEDBACK
    // ==========================================
    public void OnAnswerButtonClick(string selectedOption)
    {
        if (!isAnswering || currentPhase != QuizPhase.TeoriText) return;
        HandleAnswerSelected(selectedOption);
    }

    // Hubungkan ke 4 Button Image Kantin Sehat (Passing int index 0, 1, 2, 3)
    public void OnKantinAnswerButtonClick(int chosenIndex)
    {
        if (!isAnswering || currentPhase != QuizPhase.KantinSehat) return;

        isAnswering = false;
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        ToggleKantinButtonsInteractable(false);

        GeneratedKantinQuestion kq = generatedKantinQuestions[kantinCurrentIndex];
        FoodItem chosenFood = kq.options[chosenIndex];

        canvasQuizFeedback.SetActive(true);
        panelPopupFeedback.SetActive(true);
        panelEndFeedback.SetActive(false);

        if (chosenFood.isHealthy)
        {
            feedbackTitleText.text = "Mantap! Pilihan Sehat!";
            feedbackTitleText.color = new Color32(46, 204, 113, 255);
            score += 10;
        }
        else
        {
            feedbackTitleText.text = "Aduh, Kurang Sehat!";
            feedbackTitleText.color = new Color32(231, 76, 60, 255);
        }

        feedbackExplanationText.text = $"<b>{chosenFood.foodName}</b>\n{chosenFood.eduText}";
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

        if (currentPhase == QuizPhase.TeoriText)
        {
            currentQuestionIndex++;
        }
        else if (currentPhase == QuizPhase.KantinSehat)
        {
            kantinCurrentIndex++;
        }

        DisplayQuestion();
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
        if (currentPhase == QuizPhase.TeoriText)
        {
            HandleAnswerSelected("");
        }
        else if (currentPhase == QuizPhase.KantinSehat)
        {
            OnKantinAnswerButtonClick(0); // Default opsi 0 jika waktu habis
        }
    }

    void ToggleButtonsInteractable(bool state)
    {
        foreach (Button btn in optionButtons)
        {
            btn.interactable = state;
        }
    }

    void ToggleKantinButtonsInteractable(bool state)
    {
        if (kantinOptionButtons != null)
        {
            foreach (Button btn in kantinOptionButtons)
            {
                if (btn != null) btn.interactable = state;
            }
        }
    }

    // ==========================================
    // MULTIPLAYER & EXIT HANDLING
    // ==========================================
    public void StartMultiplayerQuiz(string cleanJsonDariFirebase)
    {
        currentQuestionIndex = 0;
        kantinCurrentIndex = 0;
        score = 0;
        currentPhase = QuizPhase.TeoriText;
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

                if (panelGameplayTeori != null) panelGameplayTeori.SetActive(true);
                if (panelKantinSehat != null) panelKantinSehat.SetActive(false);

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
        if (panelAwal != null) panelAwal.SetActive(true);

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

        // Tembak Akumulasi Skor Total (Teori + Kantin) ke Firebase
        if (!isOfflineMode && firebaseStudentManager != null)
        {
            firebaseStudentManager.UpdateSkorAkhirSiswa(score);
        }
    }

    // ==========================================
    // HELPER METHOD UNIVERSAL SHUFFLE
    // ==========================================
    private List<T> ShuffleList<T>(List<T> originalList)
    {
        List<T> list = new List<T>(originalList);
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
        return list;
    }
}
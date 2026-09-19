using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions; 
using UnityEngine.Networking;
using UnityEngine.UI;

public class FirebaseRoomManager : MonoBehaviour
{
    private DatabaseReference dbReference;
    private string geminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite:generateContent?key=";
    private string apiKey = ""; 
    private string roomId;

    [Header("UI Guru References")]
    [SerializeField] private TextMeshProUGUI textKodeRoom;
    [SerializeField] private TextMeshProUGUI textStatusLoading;
    
    [Header("Waiting Room UI Elements")]
    [SerializeField] private GameObject panelCreateRoom;   
    [SerializeField] private GameObject panelWaitingRoom;  
    [SerializeField] private Transform studentListContainer; 
    [SerializeField] private GameObject studentNamePrefab;   
    [SerializeField] private Button buttonLanjut;          

    [Header("Popup Konfirmasi Baru")]
    [SerializeField] private GameObject panelPopupKeluar; 
    [SerializeField] private GameObject panelPopupMulai;  

    [Header("Panel Review Soal Guru (Fitur Baru)")]
    [SerializeField] private GameObject panelReviewSoalGuru;
    [SerializeField] private TextMeshProUGUI textReviewNavigasi;
    [SerializeField] private TMP_InputField inputReviewQuestion;
    [SerializeField] private TMP_InputField inputReviewOptionA;
    [SerializeField] private TMP_InputField inputReviewOptionB;
    [SerializeField] private TMP_InputField inputReviewOptionC;
    [SerializeField] private TMP_InputField inputReviewOptionD;
    // [SerializeField] private TMP_InputField inputReviewCorrectAns;
    [SerializeField] private TMP_Dropdown dropdownReviewCorrectAns;
    [SerializeField] private TMP_InputField inputReviewExplanation;
    [SerializeField] private Button btnPrevReview;
    [SerializeField] private Button btnNextReview;

    [Header("Arsitektur Canvas Gameplay Baru")]
    [SerializeField] private GameObject canvasQuizGameplay;      
    [SerializeField] private GameObject panelGameplaySiswa;     
    [SerializeField] private GameObject panelDashboardGuru;     
    
    [Header("Dashboard Excel Elements")]
    [SerializeField] private TextMeshProUGUI textRataRataNilai;   
    [SerializeField] private TextMeshProUGUI textStatusSelesai;   
    [SerializeField] private Transform tableContentContainer;    
    [SerializeField] private GameObject tableRowPrefab;          
    [SerializeField] private GameObject panelKeluarDashboard;

    [Header("Script References")]
    [SerializeField] private QuizFlowManager quizFlowManager; 

    // --- VARIABEL LOKAL REVIEW SOAL ---
    private List<Question> reviewQuestionsList = new List<Question>();
    private int reviewCurrentIndex = 0;
    private int selectedJumlahSoal = 5; // Default 5 soal

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            DependencyStatus dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync().ContinueWithOnMainThread(authTask => {
                    if (authTask.IsCompletedSuccessfully)
                    {
                        FirebaseDatabase dbInstance = FirebaseDatabase.GetInstance("https://zibo-ar-lidm-default-rtdb.asia-southeast1.firebasedatabase.app/");
                        dbReference = dbInstance.RootReference;
                        Debug.Log("[Guru] Login Anonim & Firebase RTDB Berhasil!");
                    }
                    else
                    {
                        Debug.LogError("[Guru] Gagal Login Anonim: " + authTask.Exception);
                    }
                });
            }
            else
            {
                Debug.LogError("Gagal inisialisasi Firebase Guru: " + dependencyStatus);
            }
        });

        LoadLocalKey();

        if (panelReviewSoalGuru != null) panelReviewSoalGuru.SetActive(false);
    }

    // --- FITUR PILIH JUMLAH SOAL (MULTIPLAYER GURU) ---
    public void SetJumlahSoalMultiplayer(int jumlah)
    {
        selectedJumlahSoal = jumlah;
        Debug.Log($"[Guru] Jumlah soal multiplayer diset ke: {selectedJumlahSoal}");
    }

    // --- LOGIKA PEMBUATAN ROOM ---
    public void KlikTombolGenerateCode()
    {
        roomId = GenerateRandomRoomCode(5);
        textKodeRoom.text = "KODE ROOM: " + roomId;
        
        panelCreateRoom.SetActive(false);
        panelWaitingRoom.SetActive(true);
        if (buttonLanjut != null)
        {
            buttonLanjut.gameObject.SetActive(true);
            buttonLanjut.interactable = true;
        } 
        
        dbReference.Child("rooms").Child(roomId).Child("roomStatus").SetValueAsync("waiting");
        textStatusLoading.text = "Room siap! Menunggu siswa bergabung...";

        dbReference.Child("rooms").Child(roomId).OnDisconnect().RemoveValue();
        dbReference.Child("rooms").Child(roomId).Child("students").ValueChanged += HandleSiswaBergabung;
    }

    private void HandleSiswaBergabung(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;

        foreach (Transform child in studentListContainer) {
            child.SetParent(null);
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        if (args.Snapshot.Exists)
        {
            foreach (DataSnapshot studentSnapshot in args.Snapshot.Children)
            {
                string namaSiswa = studentSnapshot.Key; 
                GameObject go = Instantiate(studentNamePrefab, studentListContainer);
                go.GetComponent<TextMeshProUGUI>().text = " " + namaSiswa;
            }
        }
    }

    // ========================================================
    // POPUP KONFIRMASI MULAI & FETCH GEMINI
    // ========================================================
    public void KlikLanjutKeKuis()
    {
        if (panelPopupMulai != null) panelPopupMulai.SetActive(true);
    }

    public void KonfirmasiMulaiYA()
    {
        if (panelPopupMulai != null) panelPopupMulai.SetActive(false);
        textStatusLoading.text = "Sedang meracik soal dari AI Gemini, mohon tunggu...";
        if (buttonLanjut != null) buttonLanjut.interactable = false;
        
        StartCoroutine(FetchGeminiQuestionsCoroutine());
    }

    public void KonfirmasiMulaiTIDAK()
    {
        if (panelPopupMulai != null) panelPopupMulai.SetActive(false);
    }

    // --- GENERATE SOAL GEMINI & DIVERSIKAN KE REVIEW PANEL ---
    private IEnumerator FetchGeminiQuestionsCoroutine()
    {
        textStatusLoading.text = "Menghubungi AI Gemini...";

        GeminiRequest geminiRequest = new GeminiRequest();
        geminiRequest.contents = new List<GeminiContent>();
        GeminiContent contentObj = new GeminiContent();
        contentObj.parts = new List<GeminiPart>();
        GeminiPart partObj = new GeminiPart();
        
        partObj.text = $"Buatlah {selectedJumlahSoal} soal pilihan ganda interaktif tentang materi gizi seimbang dan zat gizi pada makanan untuk anak Sekolah Dasar berbentuk cerita pendek. " +
                        "Format output WAJIB dalam bentuk JSON mentah dengan struktur tepat seperti ini: " +
                        "{\"questions\": [{\"questionText\":\"...\", \"optionA\":\"...\", \"optionB\":\"...\", \"optionC\":\"...\", \"optionD\":\"...\", \"correctAnswer\":\"A/B/C/D\", \"explanation\":\"...\"}]}. " +
                        "Jangan berikan teks tambahan atau penjelasan di luar format JSON. Jangan pakai format markdown ```json.";

        contentObj.parts.Add(partObj);
        geminiRequest.contents.Add(contentObj);

        string jsonPayload = JsonUtility.ToJson(geminiRequest);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(geminiUrl + apiKey, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string rawJsonFromGemini = request.downloadHandler.text;

                try
                {
                    GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(rawJsonFromGemini);
                    if (response != null && response.candidates != null && response.candidates.Count > 0)
                    {
                        string cleanJson = response.candidates[0].content.parts[0].text;
                        
                        if (cleanJson.StartsWith("```json")) cleanJson = cleanJson.Replace("```json", "");
                        if (cleanJson.EndsWith("```")) cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                        cleanJson = cleanJson.Trim();

                        QuizContainer container = JsonUtility.FromJson<QuizContainer>(cleanJson);
                        if (container != null && container.questions != null && container.questions.Count > 0)
                        {
                            reviewQuestionsList = container.questions;
                            reviewCurrentIndex = 0;

                            // ALIH KAN UI KE PANEL REVIEW GURU
                            panelWaitingRoom.SetActive(false);
                            if (panelReviewSoalGuru != null) panelReviewSoalGuru.SetActive(true);

                            TampilkanSoalKeFormReview(reviewCurrentIndex);
                            Debug.Log($"[Guru Review] {reviewQuestionsList.Count} soal berhasil dimuat untuk di-review!");
                        }
                        else
                        {
                            textStatusLoading.text = "Format soal dari AI tidak sesuai.";
                            if (buttonLanjut != null) buttonLanjut.interactable = true;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    textStatusLoading.text = "Gagal memproses struktur AI Gemini.";
                    Debug.LogError("Guru Parsing Error: " + e.Message);
                    if (buttonLanjut != null) buttonLanjut.interactable = true;
                }
            }
            else
            {
                textStatusLoading.text = "Gagal mengambil soal Gemini.";
                Debug.LogError($"Gemini API Error: {request.error}");
                if (buttonLanjut != null) buttonLanjut.interactable = true;
            }
        }
    }

    // ========================================================
    // MODUL MODERASI & EDIT SOAL GURU (REVIEW SYSTEM)
    // ========================================================
    private void TampilkanSoalKeFormReview(int index)
    {
        if (reviewQuestionsList == null || reviewQuestionsList.Count == 0) return;

        Question q = reviewQuestionsList[index];

        if (textReviewNavigasi != null) textReviewNavigasi.text = $"Soal {index + 1} dari {reviewQuestionsList.Count}";
        if (inputReviewQuestion != null) inputReviewQuestion.text = q.questionText;
        if (inputReviewOptionA != null) inputReviewOptionA.text = q.optionA;
        if (inputReviewOptionB != null) inputReviewOptionB.text = q.optionB;
        if (inputReviewOptionC != null) inputReviewOptionC.text = q.optionC;
        if (inputReviewOptionD != null) inputReviewOptionD.text = q.optionD;
        if (inputReviewExplanation != null) inputReviewExplanation.text = q.explanation;

        // --- MAPPING STRING ("A"/"B"/"C"/"D") KE INDEX DROPDOWN (0, 1, 2, 3) ---
        if (dropdownReviewCorrectAns != null)
        {
            string key = string.IsNullOrEmpty(q.correctAnswer) ? "A" : q.correctAnswer.Trim().ToUpper();
            switch (key)
            {
                case "A": dropdownReviewCorrectAns.value = 0; break;
                case "B": dropdownReviewCorrectAns.value = 1; break;
                case "C": dropdownReviewCorrectAns.value = 2; break;
                case "D": dropdownReviewCorrectAns.value = 3; break;
                default: dropdownReviewCorrectAns.value = 0; break;
            }
            dropdownReviewCorrectAns.RefreshShownValue(); // Memastikan teks UI dropdown ter-refresh detik itu juga
        }

        if (btnPrevReview != null) btnPrevReview.interactable = (index > 0);
        if (btnNextReview != null) btnNextReview.interactable = (index < reviewQuestionsList.Count - 1);
    }

    private void SimpanEditanFormSaatIni()
    {
        if (reviewQuestionsList == null || reviewQuestionsList.Count == 0) return;
        if (reviewCurrentIndex < 0 || reviewCurrentIndex >= reviewQuestionsList.Count) return;

        Question q = reviewQuestionsList[reviewCurrentIndex];
        if (inputReviewQuestion != null) q.questionText = inputReviewQuestion.text;
        if (inputReviewOptionA != null) q.optionA = inputReviewOptionA.text;
        if (inputReviewOptionB != null) q.optionB = inputReviewOptionB.text;
        if (inputReviewOptionC != null) q.optionC = inputReviewOptionC.text;
        if (inputReviewOptionD != null) q.optionD = inputReviewOptionD.text;
        if (inputReviewExplanation != null) q.explanation = inputReviewExplanation.text;

        // --- CONVERT INDEX DROPDOWN (0, 1, 2, 3) KEMBALI KE STRING ("A"/"B"/"C"/"D") ---
        if (dropdownReviewCorrectAns != null)
        {
            switch (dropdownReviewCorrectAns.value)
            {
                case 0: q.correctAnswer = "A"; break;
                case 1: q.correctAnswer = "B"; break;
                case 2: q.correctAnswer = "C"; break;
                case 3: q.correctAnswer = "D"; break;
                default: q.correctAnswer = "A"; break;
            }
        }
    }

    public void KlikNextReviewSoal()
    {
        SimpanEditanFormSaatIni();
        if (reviewCurrentIndex < reviewQuestionsList.Count - 1)
        {
            reviewCurrentIndex++;
            TampilkanSoalKeFormReview(reviewCurrentIndex);
        }
    }

    public void KlikPrevReviewSoal()
    {
        SimpanEditanFormSaatIni();
        if (reviewCurrentIndex > 0)
        {
            reviewCurrentIndex--;
            TampilkanSoalKeFormReview(reviewCurrentIndex);
        }
    }

    public void KlikBatalReview()
    {
        if (panelReviewSoalGuru != null) panelReviewSoalGuru.SetActive(false);
        panelWaitingRoom.SetActive(true);
        if (buttonLanjut != null) buttonLanjut.interactable = true;
        textStatusLoading.text = "Review dibatalkan. Siap untuk generate ulang.";
    }

    // --- EKSEKUSI RILIS KUIS TERVERIFIKASI KE FIREBASE ---
    public void KlikRilisKuisKeSiswa()
    {
        SimpanEditanFormSaatIni(); // Amankan editan soal terakhir

        QuizContainer finalContainer = new QuizContainer();
        finalContainer.questions = reviewQuestionsList;

        string finalCleanJson = JsonUtility.ToJson(finalContainer);

        dbReference.Child("rooms").Child(roomId).Child("questions").SetValueAsync(finalCleanJson).ContinueWithOnMainThread(uploadTask => {
            if (uploadTask.IsCompletedSuccessfully)
            {
                dbReference.Child("rooms").Child(roomId).Child("students").ValueChanged -= HandleSiswaBergabung;
                dbReference.Child("rooms").Child(roomId).Child("roomStatus").SetValueAsync("started");
                
                if (panelReviewSoalGuru != null) panelReviewSoalGuru.SetActive(false);
                if (canvasQuizGameplay != null) canvasQuizGameplay.SetActive(true);
                if (panelGameplaySiswa != null) panelGameplaySiswa.SetActive(false);
                if (panelDashboardGuru != null) panelDashboardGuru.SetActive(true);

                dbReference.Child("rooms").Child(roomId).Child("students").ValueChanged += HandleRealtimeDashboardGuru;
                
                Debug.Log("[Guru] Soal terverifikasi berhasil dirilis ke siswa! Membuka Live Dashboard.");
            }
            else
            {
                Debug.LogError("Gagal rilis soal ke Firebase: " + uploadTask.Exception);
            }
        });
    }

    // ========================================================
    // LOGIKA KELUAR & CLEANUP
    // ========================================================
    public void BukaPopupKeluar()
    {
        if (panelPopupKeluar != null) panelPopupKeluar.SetActive(true);
    }

    public void KonfirmasiKeluarYA()
    {
        if (panelPopupKeluar != null) panelPopupKeluar.SetActive(false);

        if (!string.IsNullOrEmpty(roomId) && dbReference != null)
        {
            dbReference.Child("rooms").Child(roomId).Child("students").ValueChanged -= HandleSiswaBergabung;
            StartCoroutine(RoutineHapusRoomPermanen(roomId, "cancelled"));
            roomId = null;
        }

        panelWaitingRoom.SetActive(false);
        if (panelReviewSoalGuru != null) panelReviewSoalGuru.SetActive(false);
        if (panelCreateRoom != null) panelCreateRoom.SetActive(true);
        if (quizFlowManager != null) quizFlowManager.KembaliKePilihMode();
    }

    public void KonfirmasiKeluarTIDAK()
    {
        if (panelPopupKeluar != null) panelPopupKeluar.SetActive(false);
    }

    public void KlikTombolKeluarDashboard()
    {
        if (panelKeluarDashboard != null) panelKeluarDashboard.SetActive(true);
    }

    public void BatalKeluarDashboard()
    {
        if (panelKeluarDashboard != null) panelKeluarDashboard.SetActive(false);
    }

    public void KonfirmasiKeluarDashboard()
    {
        if (panelKeluarDashboard != null) panelKeluarDashboard.SetActive(false);

        if (!string.IsNullOrEmpty(roomId) && dbReference != null)
        {
            dbReference.Child("rooms").Child(roomId).Child("students").ValueChanged -= HandleRealtimeDashboardGuru;
            StartCoroutine(RoutineHapusRoomPermanen(roomId, "finished"));
            roomId = null;
        }

        if (canvasQuizGameplay != null) canvasQuizGameplay.SetActive(false);
        if (panelDashboardGuru != null) panelDashboardGuru.SetActive(false);
        if (panelWaitingRoom != null) panelWaitingRoom.SetActive(false);
        if (panelReviewSoalGuru != null) panelReviewSoalGuru.SetActive(false);
        if (panelCreateRoom != null) panelCreateRoom.SetActive(true);

        if (quizFlowManager != null) quizFlowManager.KembaliKePilihMode();
    }

    private IEnumerator RoutineHapusRoomPermanen(string targetRoomId, string finalStatus)
    {
        if (dbReference == null || string.IsNullOrEmpty(targetRoomId)) yield break;

        dbReference.Child("rooms").Child(targetRoomId).OnDisconnect().Cancel();
        dbReference.Child("rooms").Child(targetRoomId).Child("roomStatus").SetValueAsync(finalStatus);

        yield return new WaitForSeconds(3f);

        dbReference.Child("rooms").Child(targetRoomId).RemoveValueAsync();
        Debug.Log($"[Auto-Cleanup] Room {targetRoomId} berhasil dibersihkan dari Firebase!");
    }

    private void HandleRealtimeDashboardGuru(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;

        foreach (Transform child in tableContentContainer) {
            child.SetParent(null);
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        int totalSiswa = 0;
        int siswaSelesai = 0;
        int siswaDihitungNilai = 0; 
        float totalNilaiSemuaSiswa = 0;

        if (args.Snapshot.Exists)
        {
            foreach (DataSnapshot studentSnapshot in args.Snapshot.Children)
            {
                totalSiswa++;
                string namaSiswa = studentSnapshot.Key;
                
                int skorSiswa = 0;
                if (studentSnapshot.Child("score").Exists)
                {
                    skorSiswa = int.Parse(studentSnapshot.Child("score").Value.ToString());
                }

                string statusSiswa = "joined";
                if (studentSnapshot.Child("status").Exists)
                {
                    statusSiswa = studentSnapshot.Child("status").Value.ToString();
                }

                if (statusSiswa == "finished")
                {
                    siswaSelesai++;
                    totalNilaiSemuaSiswa += skorSiswa;
                    siswaDihitungNilai++;
                }
                else if (statusSiswa == "canceled")
                {
                }
                else
                {
                    totalNilaiSemuaSiswa += skorSiswa;
                    siswaDihitungNilai++;
                }

                GameObject newRow = Instantiate(tableRowPrefab, tableContentContainer);
                TableRowItem rowItem = newRow.GetComponent<TableRowItem>();
                
                if (rowItem != null)
                {
                    rowItem.textNama.text = namaSiswa;
                    
                    if (statusSiswa == "finished")
                    {
                        rowItem.textSkor.text = skorSiswa.ToString();
                        rowItem.textStatus.text = "<color=#2ecc71>SELESAI</color>";
                    }
                    else if (statusSiswa == "canceled")
                    {
                        rowItem.textSkor.text = "-"; 
                        rowItem.textStatus.text = "<color=#e74c3c>KELUAR QUIZ</color>";
                    }
                    else
                    {
                        rowItem.textSkor.text = skorSiswa.ToString();
                        rowItem.textStatus.text = "<color=#f39c12>MENGERJAKAN</color>";
                    }
                }
            }
        }

        float rataRataKelas = 0;
        if (siswaDihitungNilai > 0)
        {
            rataRataKelas = totalNilaiSemuaSiswa / siswaDihitungNilai;
        }

        textRataRataNilai.text = rataRataKelas.ToString("F1"); 
        textStatusSelesai.text = $"{siswaSelesai}/{totalSiswa} Siswa Sudah Selesai Mengerjakan";
    }

    private void OnDestroy()
    {
        if (dbReference != null && !string.IsNullOrEmpty(roomId))
        {
            dbReference.Child("rooms").Child(roomId).Child("students").ValueChanged -= HandleRealtimeDashboardGuru;
            dbReference.Child("rooms").Child(roomId).Child("students").ValueChanged -= HandleSiswaBergabung;
        }
    }

    private string GenerateRandomRoomCode(int length)
    {
        const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZ123456789";
        System.Random random = new System.Random();
        char[] stringChars = new char[length];
        for (int i = 0; i < stringChars.Length; i++) {
            stringChars[i] = chars[random.Next(chars.Length)];
        }
        return new string(stringChars);
    }

    private void LoadLocalKey()
    {
        TextAsset keyAsset = Resources.Load<TextAsset>("config");

        if (keyAsset != null)
        {
            apiKey = keyAsset.text.Trim();
            Debug.Log($"[Resources] API Key sukses dimuat!");
        }
        else
        {
            Debug.LogError("File config.txt tidak ditemukan di folder Assets/Resources/ bro!");
        }
    }
}
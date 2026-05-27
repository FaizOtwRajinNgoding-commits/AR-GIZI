using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions; 
using UnityEngine.Networking;
using UnityEngine.UI;

public class FirebaseRoomManager : MonoBehaviour
{
    private DatabaseReference dbReference;
    private string geminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key=";
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
    [SerializeField] private GameObject panelPopupKeluar; // Popup konfirmasi keluar (Pojok kiri atas)
    [SerializeField] private GameObject panelPopupMulai;  // Popup "Yakin Memulai Quiz?"

    [Header("Script References")]
    [SerializeField] private QuizFlowManager quizFlowManager; // Referensi untuk pindah panel halaman utama

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            DependencyStatus dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                FirebaseDatabase dbInstance = FirebaseDatabase.GetInstance("https://zibo-ar-lidm-default-rtdb.asia-southeast1.firebasedatabase.app/");
                dbInstance.SetPersistenceEnabled(false);
                dbReference = dbInstance.RootReference;
                Debug.Log("Firebase Realtime Database Berhasil Terhubung di Sisi Guru!");
            }
        });
        LoadLocalKey();
    }

    // --- LOGIKAL PEMBUATAN ROOM ---
    public void KlikTombolGenerateCode()
    {
        roomId = GenerateRandomRoomCode(5);
        textKodeRoom.text = "KODE ROOM: " + roomId;
        
        panelCreateRoom.SetActive(false);
        panelWaitingRoom.SetActive(true);
        buttonLanjut.gameObject.SetActive(true); 
        
        dbReference.Child("rooms").Child(roomId).Child("roomStatus").SetValueAsync("waiting");
        textStatusLoading.text = "Room siap! Menunggu siswa bergabung...";

        dbReference.Child("rooms").Child(roomId).Child("students").ValueChanged += HandleSiswaBergabung;
    }

    private void HandleSiswaBergabung(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;

        foreach (Transform child in studentListContainer) {
            Destroy(child.gameObject);
        }

        if (args.Snapshot.Exists)
        {
            foreach (DataSnapshot studentSnapshot in args.Snapshot.Children)
            {
                string namaSiswa = studentSnapshot.Key; 
                GameObject go = Instantiate(studentNamePrefab, studentListContainer);
                go.GetComponent<TextMeshProUGUI>().text = "👤 " + namaSiswa;
            }
        }
    }

    // ========================================================
    // POPUP 1: KONFIRMASI KELUAR (Tombol Pojok Kiri Atas)
    // ========================================================
    public void BukaPopupKeluar()
    {
        panelPopupKeluar.SetActive(true); // Munculkan popup keluar
    }

    public void KonfirmasiKeluarYA()
    {
        panelPopupKeluar.SetActive(false);

        if (!string.IsNullOrEmpty(roomId))
        {
            // 1. Matikan fungsi telinga/listener biar ga memory leak
            dbReference.Child("rooms").Child(roomId).Child("students").ValueChanged -= HandleSiswaBergabung;

            // 2. Beritahu Firebase kalau room dibatalkan (Biar HP Murid otomatis keluar juga nanti)
            dbReference.Child("rooms").Child(roomId).Child("roomStatus").SetValueAsync("cancelled");
        }

        // 3. Kembalikan Panel UI Guru ke menu pilih mode utama
        panelWaitingRoom.SetActive(false);
        if (quizFlowManager != null)
        {
            quizFlowManager.KembaliKePilihMode();
        }
    }

    public void KonfirmasiKeluarTIDAK()
    {
        panelPopupKeluar.SetActive(false); // Tutup popup saja, tetap di waiting room
    }


    // ========================================================
    // POPUP 2: YAKIN MEMULAI QUIZ? (Tombol Lanjut Kanan Bawah)
    // ========================================================
    public void KlikLanjutKeKuis()
    {
        panelPopupMulai.SetActive(true); // Jangan langsung generate, munculkan popup dulu
    }

    public void KonfirmasiMulaiYA()
    {
        panelPopupMulai.SetActive(false); // Tutup popupnya
        textStatusLoading.text = "Sedang meracik soal dari AI Gemini, mohon tunggu...";
        buttonLanjut.interactable = false; // Kunci tombol biar gak di-spam ganda
        
        StartCoroutine(FetchGeminiQuestionsCoroutine());
    }

    public void KonfirmasiMulaiTIDAK()
    {
        panelPopupMulai.SetActive(false); // Tutup popup, kembali nunggu di waiting room
    }


    // --- GENERATE SOAL GEMINI ---
    private IEnumerator FetchGeminiQuestionsCoroutine()
    {
    textStatusLoading.text = "Menghubungi AI Gemini...";

    // 1. BUAT OBJECT REQUEST SECARA RESMI (Biar Unity yang urus masalah tanda kutip)
    GeminiRequest geminiRequest = new GeminiRequest();
    geminiRequest.contents = new List<GeminiContent>();

    GeminiContent contentObj = new GeminiContent();
    contentObj.parts = new List<GeminiPart>();

    GeminiPart partObj = new GeminiPart();
    partObj.text = "Buatlah 5 soal pilihan ganda interaktif tentang materi gizi seimbang dan zat gizi pada makanan untuk anak Sekolah Dasar berbentuk cerita pendek. " +
                    "Format output WAJIB dalam bentuk JSON mentah dengan struktur tepat seperti ini: " +
                    "{\"questions\": [{\"questionText\":\"...\", \"optionA\":\"...\", \"optionB\":\"...\", \"optionC\":\"...\", \"optionD\":\"...\", \"correctAnswer\":\"A/B/C/D\", \"explanation\":\"...\"}]}. " +
                    "Jangan berikan teks tambahan atau penjelasan di luar format JSON. Jangan pakai format markdown ```json.";

    contentObj.parts.Add(partObj);
    geminiRequest.contents.Add(contentObj);

    // Ubah object menjadi string JSON yang dijamin 100% valid secara otomatis
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

                    // Simpan ke Firebase secara asinkron
                    dbReference.Child("rooms").Child(roomId).Child("questions").SetValueAsync(cleanJson).ContinueWithOnMainThread(uploadTask => {
                        if (uploadTask.IsCompleted)
                        {
                            dbReference.Child("rooms").Child(roomId).Child("students").ValueChanged -= HandleSiswaBergabung;
                            dbReference.Child("rooms").Child(roomId).Child("roomStatus").SetValueAsync("started");
                            panelWaitingRoom.SetActive(false);
                            Debug.Log("Soal sukses ter-upload sempurna ke Firebase. Room dimulai!");
                        }
                        else
                        {
                            textStatusLoading.text = "Gagal mengunggah soal ke Firebase.";
                            buttonLanjut.interactable = true;
                        }
                    });
                }
                else
                {
                    textStatusLoading.text = "Format respon Gemini kosong.";
                    buttonLanjut.interactable = true;
                }
            }
            catch (System.Exception e)
            {
                textStatusLoading.text = "Gagal memproses struktur AI Gemini.";
                Debug.LogError("Guru Parsing Error: " + e.Message);
                buttonLanjut.interactable = true;
            }
        }
        else
        {
            textStatusLoading.text = "Gagal mengambil soal Gemini.";
            // Cetak log eror asli di console biar lu gampang ngecek kendala API-nya bro
            Debug.LogError($"Gemini API Error: {request.error} | Response: {request.downloadHandler.text}");
            buttonLanjut.interactable = true;
        }
    }
}

    private string GenerateRandomRoomCode(int length)
    {
        const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();
        char[] stringChars = new char[length];
        for (int i = 0; i < stringChars.Length; i++) {
            stringChars[i] = chars[random.Next(chars.Length)];
        }
        return new string(stringChars);
    }

    private void LoadLocalKey()
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
}
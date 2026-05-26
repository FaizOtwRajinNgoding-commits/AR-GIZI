using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions; // SAKTI: Wajib ada untuk ContinueWithOnMainThread
using UnityEngine.Networking;

public class FirebaseRoomManager : MonoBehaviour
{
    private DatabaseReference dbReference;
    private string geminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key=";
    private string apiKey = ""; 

    [Header("UI Guru References")]
    [SerializeField] private TextMeshProUGUI textKodeRoom;
    [SerializeField] private TextMeshProUGUI textStatusLoading;

    void Start()
    {
        // FIX DEADLOCK: Menggunakan ContinueWithOnMainThread agar aman di Unity Editor
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            DependencyStatus dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
            // 1. Ambil instance database-nya dulu
            FirebaseDatabase dbInstance = FirebaseDatabase.GetInstance("https://zibo-ar-lidm-default-rtdb.asia-southeast1.firebasedatabase.app/");
            
            // 2. Matikan cache lokal agar tidak berebutan file dengan clone ParrelSync!
            dbInstance.SetPersistenceEnabled(false);
            
            // 3. Baru ambil RootReference-nya
            dbReference = dbInstance.RootReference;
            Debug.Log("Firebase Realtime Database Berhasil Terhubung di Sisi Guru!");
            }
            else
            {
                Debug.LogError("Gagal inisialisasi Firebase Guru: " + dependencyStatus);
            }
        });

        LoadLocalKey();
    }

    public void KlikBuatRoomGuru()
    {
        string randomRoomId = GenerateRandomRoomCode(5);
        textKodeRoom.text = $"KODE ROOM: {randomRoomId}";
        textStatusLoading.text = "Zibo sedang menyiapkan soal di Firebase...";

        StartCoroutine(FetchGeminiAndCreateRoom(randomRoomId));
    }

    IEnumerator FetchGeminiAndCreateRoom(string roomId)
{
    string fullUrl = geminiUrl + apiKey;
    string prompt = "Buat 5 soal pilihan ganda interaktif tentang nutrisi anak SD. " +
                    "Format output WAJIB JSON mentah: " +
                    "{\"questions\": [{\"questionText\":\"...\", \"optionA\":\"...\", \"optionB\":\"...\", \"optionC\":\"...\", \"optionD\":\"...\", \"correctAnswer\":\"A/B/C/D\", \"explanation\":\"...\"}]}. " +
                    "Jangan pakai format markdown ```json.";

    // SOLUSI SAKTI: Memanfaatkan class data global agar JsonUtility nge-escape karakter secara otomatis
    GeminiRequest requestBody = new GeminiRequest();
    requestBody.contents = new List<GeminiContent>
    {
        new GeminiContent
        {
            parts = new List<GeminiPart>
            {
                new GeminiPart { text = prompt }
            }
        }
    };

    // Mengubah objek menjadi string JSON yang valid & aman secara otomatis
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
            string rawJsonFromGemini = request.downloadHandler.text;

            // Push ke Firebase
            // KODE BARU (MENYIMPAN SEBAGAI TEKS STRING UTUH)
            dbReference.Child("rooms").Child(roomId).Child("questions").SetValueAsync(rawJsonFromGemini);   
            dbReference.Child("rooms").Child(roomId).Child("roomStatus").SetValueAsync("waiting");

            textStatusLoading.text = "Room siap! Menunggu siswa bergabung...";
            Debug.Log($"Room {roomId} berhasil dibuat beserta 5 soal dari Gemini!");
        }
        else
        {
            textStatusLoading.text = "Gagal membuat soal. Cek internet/API Key!";
            Debug.LogError("Gemini Error: " + request.error);
        }
    }
}

    private string GenerateRandomRoomCode(int length)
    {
        const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();
        char[] stringChars = new char[length];
        for (int i = 0; i < stringChars.Length; i++)
        {
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
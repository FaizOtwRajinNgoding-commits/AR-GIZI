using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class FirebaseStudentManager : MonoBehaviour
{
    private DatabaseReference dbReference;
    private string savedCodeInput;
    private string savedNameInput;

    [Header("UI Siswa Inputs")]
    [SerializeField] private TMP_InputField inputKodeRoom;
    [SerializeField] private TMP_InputField inputNamaSiswa;
    [SerializeField] private TextMeshProUGUI textStatusSiswa; // Teks info di menu join awal

    [Header("Waiting Room UI Student References")]
    [SerializeField] private GameObject panelJoinRoom;         // Panel menu ngetik kode awal siswa
    [SerializeField] private GameObject panelWaitingRoom;        // Panel waiting room siswa
    [SerializeField] private TextMeshProUGUI textWaitingKodeSiswa; // Menampilkan Kode Room di atas
    [SerializeField] private TextMeshProUGUI textStatusLoadingSiswa; // Menampilkan status ("Menunggu guru...")
    [SerializeField] private Transform studentListContainer;     // Content dari Scroll View sisi Siswa
    [SerializeField] private GameObject studentNamePrefab;       // Prefab teks nama siswa

    [Header("Script References")]
    [SerializeField] private GeminiQuizManager geminiQuizManager;
    [SerializeField] private QuizFlowManager quizFlowManager;   // Referensi untuk balik ke menu utama kuis

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            DependencyStatus dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                FirebaseDatabase dbInstance = FirebaseDatabase.GetInstance("https://zibo-ar-lidm-default-rtdb.asia-southeast1.firebasedatabase.app/");
                dbInstance.SetPersistenceEnabled(false);
                dbReference = dbInstance.RootReference;
                textStatusSiswa.text = "Firebase Siswa Siap.";
            }
        });
    }

    // --- FUNGSI MASUK ROOM ---
    public void KlikJoinRoomSiswa()
    {
    // PENGAMAN: Cek apakah Firebase sudah benar-benar selesai loading di Start()
    if (dbReference == null)
    {
        textStatusSiswa.text = "Firebase belum siap atau internet terputus. Tunggu sebentar!";
        Debug.LogWarning("Mencoba Join Room, tapi dbReference masih null!");
        return; // Gagalkan proses di bawahnya agar tidak crash!
    }

    savedCodeInput = inputKodeRoom.text.Trim().ToUpper();
    savedNameInput = inputNamaSiswa.text.Trim();

    if (string.IsNullOrEmpty(savedCodeInput) || string.IsNullOrEmpty(savedNameInput)) {
        textStatusSiswa.text = "Nama dan Kode tidak boleh kosong!";
        return;
    }

    // Sekarang baris ini dijamin 100% aman dari NullReferenceException, bro!
    dbReference.Child("rooms").Child(savedCodeInput).GetValueAsync().ContinueWithOnMainThread(task => {
        if (task.IsFaulted || task.IsCanceled) {
            textStatusSiswa.text = "Koneksi bermasalah.";
            return;
        }

        DataSnapshot snapshot = task.Result;
        if (snapshot.Exists)
        {
            string roomStatus = snapshot.Child("roomStatus").Value.ToString();

            if (roomStatus == "waiting")
            {
                dbReference.Child("rooms").Child(savedCodeInput).Child("students").Child(savedNameInput).Child("score").SetValueAsync(0);
                dbReference.Child("rooms").Child(savedCodeInput).Child("students").Child(savedNameInput).Child("status").SetValueAsync("joined");

                panelJoinRoom.SetActive(false);
                panelWaitingRoom.SetActive(true);
                
                textWaitingKodeSiswa.text = "KODE ROOM: " + savedCodeInput;
                textStatusLoadingSiswa.text = "Room siap! Menunggu guru memulai...";

                dbReference.Child("rooms").Child(savedCodeInput).Child("roomStatus").ValueChanged += HandleStatusRoomBerubah;
                dbReference.Child("rooms").Child(savedCodeInput).Child("students").ValueChanged += HandleSiswaBergabungSiswa;
            }
            else {
                textStatusSiswa.text = "Room sudah mulai atau ditutup!";
            }
        }
        else {
            textStatusSiswa.text = "Kode Room tidak ditemukan!";
        }
    });
}

    // --- REALTIME MENGUPDATE DAFTAR NAMA SISI SISWA ---
    private void HandleSiswaBergabungSiswa(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;

        // Bersihkan daftar nama lama di Scroll View siswa
        foreach (Transform child in studentListContainer) {
            Destroy(child.gameObject);
        }

        // Cetak ulang semua siswa yang terdata di Firebase secara live
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

    // --- REALTIME PANTAU PERINTAH GURU (MULAI / BATAL) ---
    private void HandleStatusRoomBerubah(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;

        if (args.Snapshot.Exists)
        {
            string statusTerbaru = args.Snapshot.Value.ToString();

            // KONDISI A: Guru Memulai Game
            if (statusTerbaru == "started")
            {
                textStatusLoadingSiswa.text = "Guru mulai memproses soal Gemini...";
                LepasSemuaListener();

                dbReference.Child("rooms").Child(savedCodeInput).Child("questions").GetValueAsync().ContinueWithOnMainThread(task => {
                    if (task.IsCompleted && task.Result.Exists)
                    {
                        string rawJsonQuestions = task.Result.Value.ToString();
                        panelWaitingRoom.SetActive(false);
                        geminiQuizManager.StartMultiplayerQuiz(rawJsonQuestions);
                    }
                });
            }
            // KONDISI B: Guru Keluar / Membatalkan Room (Auto-Kick Murid)
            else if (statusTerbaru == "cancelled")
            {
                LepasSemuaListener();
                panelWaitingRoom.SetActive(false);
                panelJoinRoom.SetActive(true);
                textStatusSiswa.text = "Room telah dibubarkan oleh Guru!";
            }
        }
    }

    // ========================================================
    // AKSI KETIKA MURID KLIK POPUP KELUAR (YA)
    // ========================================================
    public void KlikKeluarWaitingRoomSiswa()
    {
        if (!string.IsNullOrEmpty(savedCodeInput) && !string.IsNullOrEmpty(savedNameInput))
        {
            // 1. Matikan pendengaran data biar gak bentrok
            LepasSemuaListener();

            // 2. HAPUS folder nama murid ini dari database Firebase secara permanen!
            dbReference.Child("rooms").Child(savedCodeInput).Child("students").Child(savedNameInput).RemoveValueAsync().ContinueWithOnMainThread(task => {
                Debug.Log($"Siswa {savedNameInput} telah menghapus diri dari Room {savedCodeInput}");
            });
        }

        // 3. Kembalikan UI Siswa ke menu ketik kode room awal
        panelWaitingRoom.SetActive(false);
        panelJoinRoom.SetActive(true);
        textStatusSiswa.text = "Kamu keluar dari ruang tunggu kuis.";
    }

    private void LepasSemuaListener()
    {
        if (dbReference != null && !string.IsNullOrEmpty(savedCodeInput))
        {
            dbReference.Child("rooms").Child(savedCodeInput).Child("roomStatus").ValueChanged -= HandleStatusRoomBerubah;
            dbReference.Child("rooms").Child(savedCodeInput).Child("students").ValueChanged -= HandleSiswaBergabungSiswa;
        }
    }

    private void OnDestroy()
    {
        LepasSemuaListener(); // Jaga-jaga kalau aplikasi ditutup paksa
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Auth;
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
    [SerializeField] private GameObject panelPopupKeluar;
    // [SerializeField] private GameObject popupKeluarGameplay;
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
                // 2. JALANKAN LOGIN ANONIM SISWA
                FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync().ContinueWithOnMainThread(authTask => {
                    if (authTask.IsCompletedSuccessfully)
                    {
                        dbReference = FirebaseDatabase.GetInstance("https://zibo-ar-lidm-default-rtdb.asia-southeast1.firebasedatabase.app/").RootReference;
                        textStatusSiswa.text = "Firebase Siswa Siap (Authenticated).";
                        Debug.Log("[Siswa] Login Anonim & Firebase RTDB Berhasil!");
                    }
                    else
                    {
                        textStatusSiswa.text = "Gagal Auth Firebase.";
                        Debug.LogError("[Siswa] Gagal Login Anonim: " + authTask.Exception);
                    }
                });
            }
            else
            {
                textStatusSiswa.text = "Gagal inisialisasi Firebase.";
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
                panelPopupKeluar.SetActive(false);
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
                go.GetComponent<TextMeshProUGUI>().text = "" + namaSiswa;
            }
        }
    }

    // --- REALTIME PANTAU PERINTAH GURU (MULAI / BATAL) ---
    private void HandleStatusRoomBerubah(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;

        if (args.Snapshot.Exists && args.Snapshot.Value != null)
        {
            string statusTerbaru = args.Snapshot.Value.ToString();

            if (statusTerbaru == "started")
            {
                textStatusLoadingSiswa.text = "Guru mulai memproses soal Gemini...";
                LepasSemuaListener();

                dbReference.Child("rooms").Child(savedCodeInput).Child("questions").GetValueAsync().ContinueWithOnMainThread(task => {
                    // FIX FATAL C# TASK BUG: Cek !IsFaulted dan !IsCanceled agar aman dari AggregateException
                    if (!task.IsFaulted && !task.IsCanceled && task.Result != null && task.Result.Exists)
                    {
                        string rawJsonQuestions = task.Result.Value.ToString();
                        panelWaitingRoom.SetActive(false);
                        
                        if (geminiQuizManager != null)
                        {
                            geminiQuizManager.StartMultiplayerQuiz(rawJsonQuestions);
                        }
                        else
                        {
                            Debug.LogError("[Siswa] GeminiQuizManager NULL di Inspector!");
                        }
                    }
                    else
                    {
                        textStatusLoadingSiswa.text = "Gagal mengambil soal dari server. Periksa koneksi!";
                        Debug.LogError("[Siswa] Task Fetch Soal Error: " + task.Exception);
                    }
                });
            }
            else if (statusTerbaru == "cancelled" || statusTerbaru == "finished")
            {
                LepasSemuaListener();
                panelWaitingRoom.SetActive(false);
                panelJoinRoom.SetActive(true);
                textStatusSiswa.text = "Room telah dibubarkan oleh Guru!";
            }
        }
    }

    // ========================================================
    // FUNGSI BARU: UPDATE SKOR AKHIR DAN STATUS SISWA KE FIREBASE
    // ========================================================
    public void UpdateSkorAkhirSiswa(int skorAkhir)
    {
        if (dbReference != null && !string.IsNullOrEmpty(savedCodeInput) && !string.IsNullOrEmpty(savedNameInput))
        {
            // Tembak skor akhir hasil pengerjaan siswa
            dbReference.Child("rooms").Child(savedCodeInput).Child("students").Child(savedNameInput).Child("score").SetValueAsync(skorAkhir);
            
            // Ubah status menjadi finished agar di dashboard guru langsung berubah jadi hijau "SELESAI"
            dbReference.Child("rooms").Child(savedCodeInput).Child("students").Child(savedNameInput).Child("status").SetValueAsync("finished");
            
            Debug.Log($"[Firebase] Berhasil submit skor untuk {savedNameInput}: {skorAkhir} dengan status SELESAI!");
        }
        else
        {
            Debug.LogError("[Firebase] Gagal update skor akhir karena data Room ID atau Nama Siswa kosong!");
        }
    }

    // ========================================================
    // AKSI KETIKA MURID KLIK POPUP KELUAR (YA)
    // ========================================================

    public void BukaPopupKeluar()
    {
        panelPopupKeluar.SetActive(true);
    }

    public void KonfirmasiKeluarTIDAK()
    {
        panelPopupKeluar.SetActive(false);
    }
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

    public void SiswaKeluarTengahGameplay()
    {
        if (!string.IsNullOrEmpty(savedCodeInput) && !string.IsNullOrEmpty(savedNameInput))
        {
            // 1. Matikan pendengaran data biar gak bentrok atau memory leak
            LepasSemuaListener();

            // 2. Set status siswa di Firebase menjadi "canceled" (TIDAK DINGANUR/DIHAPUS, tapi diubah statusnya)
            dbReference.Child("rooms").Child(savedCodeInput).Child("students").Child(savedNameInput).Child("status").SetValueAsync("canceled").ContinueWithOnMainThread(task => {
                if (task.IsCompleted)
                {
                    Debug.Log($"[Gameplay] Siswa {savedNameInput} berhasil set status 'canceled' di Firebase.");
                }
            });
        }

        // 3. Kembalikan UI ke menu join room awal
        panelWaitingRoom.SetActive(false);
        panelJoinRoom.SetActive(true);
        textStatusSiswa.text = "Kamu keluar dari permainan kuis tengah jalan.";
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
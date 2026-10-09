using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] private TextMeshProUGUI textStatusSiswa;

    [Header("Waiting Room UI Student References")]
    [SerializeField] private GameObject panelJoinRoom;
    [SerializeField] private GameObject panelWaitingRoom;
    [SerializeField] private GameObject panelPopupKeluar;
    [SerializeField] private TextMeshProUGUI textWaitingKodeSiswa;
    [SerializeField] private TextMeshProUGUI textStatusLoadingSiswa;
    [SerializeField] private Transform studentListContainer;
    [SerializeField] private GameObject studentNamePrefab;
    
    [Header("Fitur Coba Lagi / Reload Soal (Baru)")]
    [SerializeField] private Button buttonReloadSoal; // Tombol Coba Lagi jika gagal load soal

    [Header("Script References")]
    [SerializeField] private GeminiQuizManager geminiQuizManager;
    [SerializeField] private QuizFlowManager quizFlowManager;

    void Start()
    {
        if (buttonReloadSoal != null) buttonReloadSoal.gameObject.SetActive(false);

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            DependencyStatus dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
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

    public void KlikJoinRoomSiswa()
    {
        if (dbReference == null)
        {
            textStatusSiswa.text = "Firebase belum siap atau internet terputus. Tunggu sebentar!";
            return;
        }

        savedCodeInput = inputKodeRoom.text.Trim().ToUpper();
        savedNameInput = inputNamaSiswa.text.Trim();

        if (string.IsNullOrEmpty(savedCodeInput) || string.IsNullOrEmpty(savedNameInput)) {
            textStatusSiswa.text = "Nama dan Kode tidak boleh kosong!";
            return;
        }

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
                    if (buttonReloadSoal != null) buttonReloadSoal.gameObject.SetActive(false);
                    
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

    private void HandleSiswaBergabungSiswa(object sender, ValueChangedEventArgs args)
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
                go.GetComponent<TextMeshProUGUI>().text = "" + namaSiswa;
            }
        }
    }

    private void HandleStatusRoomBerubah(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;

        if (args.Snapshot.Exists && args.Snapshot.Value != null)
        {
            string statusTerbaru = args.Snapshot.Value.ToString();

            if (statusTerbaru == "started")
            {
                FetchSoalDariServer();
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

    // --- FUNGSI AMBIL SOAL DENGAN SISTEM RETRY (BISA DIPANGGIL ULANG) ---
    public void FetchSoalDariServer()
    {
        if (buttonReloadSoal != null) buttonReloadSoal.gameObject.SetActive(false);
        textStatusLoadingSiswa.text = "Memuat soal dari server, mohon tunggu...";

        dbReference.Child("rooms").Child(savedCodeInput).Child("questions").GetValueAsync().ContinueWithOnMainThread(task => {
            if (!task.IsFaulted && !task.IsCanceled && task.Result != null && task.Result.Exists)
            {
                LepasSemuaListener();

                string rawJsonQuestions = task.Result.GetRawJsonValue();
                
                if (string.IsNullOrEmpty(rawJsonQuestions) && task.Result.Value != null)
                {
                    rawJsonQuestions = task.Result.Value.ToString();
                }

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
                textStatusLoadingSiswa.text = "<color=red>Gagal mengambil soal dari server!</color>\nCek koneksi internetmu lalu tekan tombol Coba Lagi di bawah.";
                if (buttonReloadSoal != null) buttonReloadSoal.gameObject.SetActive(true);
                
                Debug.LogError("[Siswa] Task Fetch Soal Error: " + (task.Exception != null ? task.Exception.ToString() : "Task Faulted"));
            }
        });
    }

    public void UpdateSkorAkhirSiswa(int skorAkhir)
    {
        if (dbReference != null && !string.IsNullOrEmpty(savedCodeInput) && !string.IsNullOrEmpty(savedNameInput))
        {
            dbReference.Child("rooms").Child(savedCodeInput).Child("students").Child(savedNameInput).Child("score").SetValueAsync(skorAkhir);
            dbReference.Child("rooms").Child(savedCodeInput).Child("students").Child(savedNameInput).Child("status").SetValueAsync("finished");
            Debug.Log($"[Firebase] Berhasil submit skor untuk {savedNameInput}: {skorAkhir} dengan status SELESAI!");
        }
    }

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
            LepasSemuaListener();
            dbReference.Child("rooms").Child(savedCodeInput).Child("students").Child(savedNameInput).RemoveValueAsync();
        }

        panelWaitingRoom.SetActive(false);
        panelJoinRoom.SetActive(true);
        textStatusSiswa.text = "Kamu keluar dari ruang tunggu kuis.";
    }

    public void SiswaKeluarTengahGameplay()
    {
        if (!string.IsNullOrEmpty(savedCodeInput) && !string.IsNullOrEmpty(savedNameInput))
        {
            LepasSemuaListener();
            dbReference.Child("rooms").Child(savedCodeInput).Child("students").Child(savedNameInput).Child("status").SetValueAsync("canceled");
        }

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
        LepasSemuaListener();
    }
}
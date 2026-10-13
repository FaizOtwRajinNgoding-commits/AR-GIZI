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
    private bool isInitializing = false;

    [Header("UI Siswa Inputs")]
    [SerializeField] private TMP_InputField inputKodeRoom;
    [SerializeField] private TMP_InputField inputNamaSiswa;
    [SerializeField] private TextMeshProUGUI textStatusSiswa;
    [SerializeField] private Button buttonJoinRoom; // Tombol Join Room di menu awal siswa

    [Header("Waiting Room UI Student References")]
    [SerializeField] private GameObject panelJoinRoom;
    [SerializeField] private GameObject panelWaitingRoom;
    [SerializeField] private GameObject panelPopupKeluar;
    [SerializeField] private TextMeshProUGUI textWaitingKodeSiswa;
    [SerializeField] private TextMeshProUGUI textStatusLoadingSiswa;
    [SerializeField] private Transform studentListContainer;
    [SerializeField] private GameObject studentNamePrefab;
    
    [Header("Fitur Coba Lagi / Reload Soal (Baru)")]
    [SerializeField] private Button buttonReloadSoal;

    [Header("Script References")]
    [SerializeField] private GeminiQuizManager geminiQuizManager;
    [SerializeField] private QuizFlowManager quizFlowManager;

    void Start()
    {
        if (buttonReloadSoal != null) buttonReloadSoal.gameObject.SetActive(false);
        InisialisasiFirebase();
    }

    void OnEnable()
    {
        // Jika belum terhubung saat panel aktif, coba hubungkan otomatis
        if (dbReference == null && !isInitializing)
        {
            InisialisasiFirebase();
        }
    }

    // --- FUNGSI INISIALISASI TERPROTEKSI (PREVENT STUCK) ---
    public void InisialisasiFirebase()
    {
        if (dbReference != null)
        {
            SetStatusFirebaseSiap();
            return;
        }

        isInitializing = true;

        // Lock tombol join & beri info indikator loading
        if (buttonJoinRoom != null) buttonJoinRoom.interactable = false;
        if (textStatusSiswa != null) textStatusSiswa.text = "<color=#f39c12>Menghubungkan ke server...</color>";

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            DependencyStatus dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync().ContinueWithOnMainThread(authTask => {
                    isInitializing = false;
                    if (authTask.IsCompletedSuccessfully)
                    {
                        dbReference = FirebaseDatabase.GetInstance("https://zibo-ar-lidm-default-rtdb.asia-southeast1.firebasedatabase.app/").RootReference;
                        SetStatusFirebaseSiap();
                        Debug.Log("[Siswa] Login Anonim & Firebase RTDB Berhasil!");
                    }
                    else
                    {
                        if (textStatusSiswa != null) 
                            textStatusSiswa.text = "<color=red>Gagal Auth Firebase. Ketuk Join untuk coba lagi.</color>";
                        if (buttonJoinRoom != null) buttonJoinRoom.interactable = true;
                        Debug.LogError("[Siswa] Gagal Login Anonim: " + authTask.Exception);
                    }
                });
            }
            else
            {
                isInitializing = false;
                if (textStatusSiswa != null) 
                    textStatusSiswa.text = "<color=red>Koneksi internet bermasalah.</color>";
                if (buttonJoinRoom != null) buttonJoinRoom.interactable = true;
                Debug.LogError("Gagal inisialisasi Firebase: " + dependencyStatus);
            }
        });
    }

    private void SetStatusFirebaseSiap()
    {
        if (textStatusSiswa != null) 
            textStatusSiswa.text = "<color=#2ecc71>Siap! Silakan masukkan nama dan kode room.</color>";
        if (buttonJoinRoom != null) 
            buttonJoinRoom.interactable = true;
    }

    public void KlikJoinRoomSiswa()
    {
        // Proteksi: Jika belum siap saat tombol diklik, coba hubungkan ulang secara otomatis
        if (dbReference == null)
        {
            if (textStatusSiswa != null) 
                textStatusSiswa.text = "<color=#f39c12>Sedang mencoba menghubungkan ulang ke server...</color>";
            InisialisasiFirebase();
            return;
        }

        savedCodeInput = inputKodeRoom.text.Trim().ToUpper();
        savedNameInput = inputNamaSiswa.text.Trim();

        if (string.IsNullOrEmpty(savedCodeInput) || string.IsNullOrEmpty(savedNameInput)) {
            if (textStatusSiswa != null) textStatusSiswa.text = "<color=red>Nama dan Kode tidak boleh kosong!</color>";
            return;
        }

        if (buttonJoinRoom != null) buttonJoinRoom.interactable = false;

        dbReference.Child("rooms").Child(savedCodeInput).GetValueAsync().ContinueWithOnMainThread(task => {
            if (buttonJoinRoom != null) buttonJoinRoom.interactable = true;

            if (task.IsFaulted || task.IsCanceled) {
                if (textStatusSiswa != null) textStatusSiswa.text = "<color=red>Koneksi bermasalah. Coba lagi!</color>";
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
                    if (textStatusSiswa != null) textStatusSiswa.text = "<color=red>Room sudah mulai atau ditutup!</color>";
                }
            }
            else {
                if (textStatusSiswa != null) textStatusSiswa.text = "<color=red>Kode Room tidak ditemukan!</color>";
            }
        });
    }

    private void HandleSiswaBergabungSiswa(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;

        // Menghapus elemen UI dari belakang agar tidak duplikat
        for (int i = studentListContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(studentListContainer.GetChild(i).gameObject);
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
                if (textStatusSiswa != null) textStatusSiswa.text = "Room telah dibubarkan oleh Guru!";
            }
        }
    }

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
                textStatusLoadingSiswa.text = "<color=red>Gagal mengambil soal dari server!</color>\nCek koneksi internetmu lalu coba lagi!";
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
        if (textStatusSiswa != null) textStatusSiswa.text = "Kamu keluar dari ruang tunggu kuis.";
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
        if (textStatusSiswa != null) textStatusSiswa.text = "Kamu keluar dari permainan kuis tengah jalan.";
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
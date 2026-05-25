using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions; // SAKTI: Ditambahkan agar bisa pakai ContinueWithOnMainThread

public class FirebaseStudentManager : MonoBehaviour
{
    private DatabaseReference dbReference;

    [Header("UI Siswa Inputs")]
    [SerializeField] private TMP_InputField inputKodeRoom;
    [SerializeField] private TMP_InputField inputNamaSiswa;
    [SerializeField] private TextMeshProUGUI textStatusSiswa;

    [Header("Script References")]
    [SerializeField] private GeminiQuizManager geminiQuizManager;

    void Start()
    {
        // FIX UI THREAD: Menggunakan ContinueWithOnMainThread agar bisa langsung update teks UI tanpa crash
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            DependencyStatus dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                dbReference = FirebaseDatabase.GetInstance("https://zibo-ar-lidm-default-rtdb.asia-southeast1.firebasedatabase.app/").RootReference;
                textStatusSiswa.text = "Firebase Siswa Siap.";
            }
            else
            {
                textStatusSiswa.text = "Gagal inisialisasi Firebase.";
            }
        });
    }

    public void KlikJoinRoomSiswa()
    {
        string codeInput = inputKodeRoom.text.Trim().ToUpper(); 
        string nameInput = inputNamaSiswa.text.Trim();

        if (string.IsNullOrEmpty(codeInput) || string.IsNullOrEmpty(nameInput))
        {
            textStatusSiswa.text = "Kode Room atau Nama lu kosong bro!";
            return;
        }

        textStatusSiswa.text = "Mencari ruangan di cloud...";

        // FIX THREAD & METHOD: Menggunakan ContinueWithOnMainThread bawaan Firebase SDK modern
        dbReference.Child("rooms").Child(codeInput).GetValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.IsCanceled)
            {
                textStatusSiswa.text = "Koneksi Firebase bermasalah.";
                return;
            }

            DataSnapshot snapshot = task.Result;

            if (snapshot.Exists) // FIX: Hapus tanda kurung () karena Exists adalah property
            {
                string roomStatus = snapshot.Child("roomStatus").Value.ToString();

                if (roomStatus == "waiting")
                {
                    dbReference.Child("rooms").Child(codeInput).Child("students").Child(nameInput).Child("score").SetValueAsync(0);
                    dbReference.Child("rooms").Child(codeInput).Child("students").Child(nameInput).Child("status").SetValueAsync("joined");

                    string rawJsonQuestions = snapshot.Child("questions").Value.ToString();

                    // Otomatis berjalan di thread utama berkat ContinueWithOnMainThread
                    EksekusiMulaiKuisSiswa(rawJsonQuestions);
                }
                else
                {
                    textStatusSiswa.text = "Room sudah mulai atau ditutup gurunya!";
                }
            }
            else
            {
                textStatusSiswa.text = "Kode Room salah atau tidak ada!";
            }
        });
    }

    private void EksekusiMulaiKuisSiswa(string rawJson)
    {
        textStatusSiswa.text = "Berhasil Masuk! Membuka soal...";
        geminiQuizManager.StartMultiplayerQuiz(rawJson); 
    }
}
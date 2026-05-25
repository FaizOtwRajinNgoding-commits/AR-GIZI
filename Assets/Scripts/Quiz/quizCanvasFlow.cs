using UnityEngine;
using UnityEngine.SceneManagement;

public class QuizFlowManager : MonoBehaviour
{
    [Header("Canvases")]
    [SerializeField] private GameObject canvasQuizMenu;
    [SerializeField] private GameObject canvasQuizGameplay;
    [SerializeField] private GameObject canvasQuizFeedback;

    [Header("Menu Panels")]
    [SerializeField] private GameObject panelPilihMode;
    [SerializeField] private GameObject panelCreateRoom;
    [SerializeField] private GameObject panelJoinRoom;

    void Start()
    {
        // Kondisi awal saat masuk QuizScene
        canvasQuizMenu.SetActive(true);
        panelPilihMode.SetActive(true);
        
        canvasQuizGameplay.SetActive(false);
        canvasQuizFeedback.SetActive(false);
        panelCreateRoom.SetActive(false);
        panelJoinRoom.SetActive(false);
    }

    // --- FUNGSI UNTUK TOMBOL-TOMBOL ---

    public void KlikSoloQuiz()
    {
        // Matikan menu utama kuis, langsung nyalakan gameplay kuis offline
        canvasQuizMenu.SetActive(false);
        canvasQuizGameplay.SetActive(true);
        
        // PANGGIL FUNGSI AMBIL SOAL GEMINI DI SINI (Fase 1 kemarin)
        Debug.Log("Memulai Solo Quiz Mode...");
    }

    public void KlikCreateRoom()
    {
        // Sembunyikan pilihan mode, munculkan panel guru buat room
        panelPilihMode.SetActive(false);
        panelCreateRoom.SetActive(true);
    }

    public void KlikJoinRoom()
    {
        // Sembunyikan pilihan mode, munculkan panel murid join room
        panelPilihMode.SetActive(false);
        panelJoinRoom.SetActive(true);
    }

    public void KembaliKePilihMode()
    {
        // Fungsi tombol back dari panel create/join room
        panelCreateRoom.SetActive(false);
        panelJoinRoom.SetActive(false);
        panelPilihMode.SetActive(true);
    }

    public void KembaliKeMainMenu()
    {
        // Fungsi tombol back ke menu utama game awal
        SceneManager.LoadScene("MainMenu");
    }

    public void KeluarDariQuizKePilihMode()
    {
        // Matikan gameplay dan semua panel feedback kuis
        canvasQuizGameplay.SetActive(false);
        canvasQuizFeedback.SetActive(false);
        
        // Nyalakan kembali menu utama kuis dan panel pilih mode
        canvasQuizMenu.SetActive(true);
        panelPilihMode.SetActive(true);
        
        // Pastikan panel room guru/murid dalam kondisi tertutup awal
        panelCreateRoom.SetActive(false);
        panelJoinRoom.SetActive(false);
    }
}
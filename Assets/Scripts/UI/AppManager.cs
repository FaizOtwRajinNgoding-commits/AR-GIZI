using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public string GameScene;
    public string ARScene;
    public string QuizScene;
    
    [SerializeField] private GameObject popupGameMenu; 

    // 1. KUNCI UTAMA: Variabel statis untuk menyimpan mode game yang dipilih
    public static string ModeGameTerpilih = "ZatGizi";

    void Start()
    {
        popupGameMenu.SetActive(false);
    }

    public void BukaPopupGame()
    {
        popupGameMenu.SetActive(true);
    }

    public void BukaAR()
    {
        SceneManager.LoadScene("ARScene");
    }

    public void TutupPopupGame()
    {
        popupGameMenu.SetActive(false);
    }

    public void LoadQuizScene()
    {
        SceneManager.LoadScene("QuizScene");
    }

    // 2. FUNGSI BARU: Jika siswa memilih game Drag & Drop Zat Gizi
    public void PilihGameZatGizi()
    {
        ModeGameTerpilih = "ZatGizi"; // Set tandanya
        SceneManager.LoadScene("GameMenu"); // Load scene game
    }

    // 3. FUNGSI BARU: Jika siswa memilih game Piring-Ku
    public void PilihGamePiringku()
    {
        ModeGameTerpilih = "Piringku"; // Set tandanya
        SceneManager.LoadScene("GameMenu"); // Load scene game
    }

    // Fungsi lama ini bisa lu hapus atau diemin aja
    public void LoadGameScene()
    {
        SceneManager.LoadScene("GameMenu");
    }
}
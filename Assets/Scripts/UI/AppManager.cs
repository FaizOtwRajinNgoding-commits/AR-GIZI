using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public string ARScene;
    public string GameScene;
    public string QuizScene;
    // Seret game object "Popup_GameMenu" dari hierarchy ke slot ini di Inspector
    [SerializeField] private GameObject popupGameMenu; 

    void Start()
    {
        // Pas game pertama kali jalan, pastiin popup-nya ngumpet dulu
        popupGameMenu.SetActive(false);
    }

    // Fungsi ini dipasang di OnClick() tombol STIK GAME lu
    public void BukaPopupGame()
    {
        popupGameMenu.SetActive(true);
    }

    // Fungsi ini dipasang di OnClick() tombol CLOSE (X) di dalam popup
    public void TutupPopupGame()
    {
        popupGameMenu.SetActive(false);
    }

    public void MulaiAR()
    {
        SceneManager.LoadScene(ARScene);
    }
    public void LoadQuizScene()
    {
        // Pastikan "QuizScene" udah lu daftarkan di File -> Build Settings
        SceneManager.LoadScene("QuizScene");
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene("GameMenu");
    }
}
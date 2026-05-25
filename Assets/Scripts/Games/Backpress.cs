using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Backpress : MonoBehaviour {

	// berfungsi untuk keluar aplikasi menggunakan tombol back
	public string SceneName;
	public GameObject popupPanel;

	// Dipanggil saat tombol Back ditekan
	public void ShowPopup()
    {
        popupPanel.SetActive(true);
    }

    // Tombol "Tidak"
    public void CancelExit()
    {
        popupPanel.SetActive(false);
    }

    // Tombol "Iya"
    public void ConfirmExit()
    {
        SceneManager.LoadScene(SceneName);
    }
}
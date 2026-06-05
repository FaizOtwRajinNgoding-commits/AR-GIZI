using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Backpress : MonoBehaviour {

	// berfungsi untuk keluar aplikasi menggunakan tombol back
	public string SceneName;
	public GameObject popupPanel;
    public GameObject popupPanelPiring;

	// Dipanggil saat tombol Back ditekan
	public void ShowPopupZat()
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

    // Untuk Piring
    public void ShowPopupPiring()
    {
        popupPanelPiring.SetActive(true);
    }

    // Tombol "Tidak"
    public void CancelExitPiring()
    {
        popupPanelPiring.SetActive(false);
    }

    // Tombol "Iya"
    public void ConfirmExitPiring()
    {
        SceneManager.LoadScene(SceneName);
    }
}
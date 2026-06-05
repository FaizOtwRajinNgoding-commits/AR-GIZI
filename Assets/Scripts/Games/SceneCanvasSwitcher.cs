using UnityEngine;

public class SceneCanvasSwitcher : MonoBehaviour
{
    [Header("Canvas References")]
    [SerializeField] private GameObject canvasZatGizi;
    [SerializeField] private GameObject canvasPiringku;

    [Header("Game Manager Reference")]
    [SerializeField] private GameObject gameManagerZatGizi; // Seret GameObject "GameManager" lama lu ke sini

    void Start()
    {
        // Otomatis ngecek pilihan dari Main Menu pas scene baru kebuka
        if (MainMenuController.ModeGameTerpilih == "Piringku")
        {
            BukaSistemPiringku();
        }
        else
        {
            BukaSistemZatGizi();
        }
    }

    private void BukaSistemPiringku()
    {
        canvasZatGizi.SetActive(false);
        canvasPiringku.SetActive(true);

        // Matikan GameManager Zat Gizi biar timernya gak jalan di background
        if (gameManagerZatGizi != null) gameManagerZatGizi.SetActive(false);

        // Jalankan game piringku
        if (PiringGameManager.Instance != null)
        {
            PiringGameManager.Instance.MulaiGamePiringku();
        }
    }

    private void BukaSistemZatGizi()
    {
        canvasZatGizi.SetActive(true);
        canvasPiringku.SetActive(false);

        // Pastikan GameManager Zat Gizi aktif dan mulai levelnya
        if (gameManagerZatGizi != null)
        {
            gameManagerZatGizi.SetActive(true);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartLevel();
            }
        }
    }

    // Fungsi navigasi balik (jika lu bikin tombol switcher di dalam game)
    // public void PindahKeGamePiringku() { BukaSistemPiringku(); }
    // public void PindahKeGameZatGizi() { BukaSistemZatGizi(); }
}
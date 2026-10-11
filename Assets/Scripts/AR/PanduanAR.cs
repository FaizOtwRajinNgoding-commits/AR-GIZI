using UnityEngine;

public class PanduanAR : MonoBehaviour
{
    [Header("Daftar Slide Halaman")]
    public GameObject[] slides;

    [Header("Tombol Navigasi Utama (Dari Canvas Utama)")]
    public GameObject nextButton;
    public GameObject prevButton;
    public GameObject confirmButton;
    public GameObject panelPanduan;

    private int currentIndex = 0;

    void Awake()
    {
        InfoPanduan();
    }
    public void InfoPanduan()
    {
        currentIndex = 0;
        panelPanduan.SetActive(true);
        UpdateCarousel();
    }

    // Otomatis berjalan saat Panel di-SetActive(false) atau Target Lost
    public void TutupPanduan()
    {
        currentIndex = 0;
        panelPanduan.SetActive(false);
    }

    public void NextSlide()
    {
        if (slides == null || slides.Length == 0) return;

        if (currentIndex < slides.Length - 1)
        {
            currentIndex++;
            UpdateCarousel();
        }
    }

    public void PrevSlide()
    {
        if (slides == null || slides.Length == 0) return;

        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateCarousel();
        }
    }

    public void UpdateCarousel()
    {
        if (slides == null || slides.Length == 0) return;

        // Aktifkan slide sesuai index, sisanya matikan
        for (int i = 0; i < slides.Length; i++)
        {
            if (slides[i] != null)
            {
                slides[i].SetActive(i == currentIndex);
            }
        }

        // Atur visibilitas tombol Next & Prev berdasarkan halaman
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(currentIndex < slides.Length - 1);
        }

        if (prevButton != null)
        {
            prevButton.gameObject.SetActive(currentIndex > 0);
        }

        if (confirmButton != null) {
            confirmButton.gameObject.SetActive(currentIndex == slides.Length - 1);
        }

    }
}

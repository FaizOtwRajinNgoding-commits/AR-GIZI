using UnityEngine;
using UnityEngine.UI;

public class CarouselSet : MonoBehaviour
{
    // Referensi carousel yang sedang aktif di layar
    public static CarouselSet activeCarousel;

    [Header("Daftar Slide Halaman")]
    public GameObject[] slides;

    [Header("Tombol Navigasi Utama (Dari Canvas Utama)")]
    public Button nextButton;
    public Button prevButton;
    public Button TTSButton;

    private int currentIndex = 0;

    public void ShowCarousel()
    {
        activeCarousel = this;
        currentIndex = 0;
        UpdateCarousel();
    }

    // Otomatis berjalan saat Panel di-SetActive(false) atau Target Lost
    public void HideCarousel()
    {
        currentIndex = 0;
        if (activeCarousel == this || activeCarousel == null)
        {
            activeCarousel = null;
            HideNavigationButtons();
        }
    }

    // Dipanggil oleh OnClick () pada ButtonNext di Canvas
    public static void GlobalNext()
    {
        if (activeCarousel != null)
        {
            activeCarousel.NextSlide();
        }
    }

    // Dipanggil oleh OnClick () pada ButtonPrev di Canvas
    public static void GlobalPrev()
    {
        if (activeCarousel != null)
        {
            activeCarousel.PrevSlide();
        }
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

        if (TTSButton != null)
        {
            TTSButton.gameObject.SetActive(true);
        }
    }

    private void HideNavigationButtons()
    {
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (prevButton != null) prevButton.gameObject.SetActive(false);
        if (TTSButton != null) TTSButton.gameObject.SetActive(false);
    }
}
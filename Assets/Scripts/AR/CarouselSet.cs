using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [Header("Audio Source")]
    public AudioSource audioSource;

    private int currentIndex = 0;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }
    public void ShowCarousel()
    {
        activeCarousel = this;
        currentIndex = 0;
        StopAudio();
        UpdateCarousel();
    }

    // Otomatis berjalan saat Panel di-SetActive(false) atau Target Lost
    public void HideCarousel()
    {
        StopAudio();
        currentIndex = 0;
        if (activeCarousel == this || activeCarousel == null)
        {
            activeCarousel = null;
            HideNavigationButtons();
        }
    }

    public static void GlobalPlayTTS()
    {
        if (activeCarousel != null) activeCarousel.PlayCurrentSlideTTS();
    }

    // Dipanggil oleh OnClick () pada ButtonNext di Canvas
    public static void GlobalNext()
    {
        if (activeCarousel != null) activeCarousel.NextSlide();
    }

    // Dipanggil oleh OnClick () pada ButtonPrev di Canvas
    public static void GlobalPrev()
    {
        if (activeCarousel != null) activeCarousel.PrevSlide();
    }

    public void NextSlide()
    {
        if (slides == null || slides.Length == 0) return;

        if (currentIndex < slides.Length - 1)
        {
            StopAudio();
            currentIndex++;
            UpdateCarousel();
        }
    }

    public void PrevSlide()
    {
        if (slides == null || slides.Length == 0) return;

        if (currentIndex > 0)
        {
            StopAudio();
            currentIndex--;
            UpdateCarousel();
        }
    }

    public void UpdateCarousel()
    {
        if (slides == null || slides.Length == 0) return;

        for (int i = 0; i < slides.Length; i++)
        {
            if (slides[i] != null)
            {
                slides[i].SetActive(i == currentIndex);
            }
        }

        if (nextButton != null) nextButton.gameObject.SetActive(currentIndex < slides.Length - 1);
        if (prevButton != null) prevButton.gameObject.SetActive(currentIndex > 0);
        if (TTSButton != null) TTSButton.gameObject.SetActive(true);
    }

    public void PlayCurrentSlideTTS()
    {
        if (slides == null || slides.Length == 0) return;

        GameObject slideAktif = slides[currentIndex];
        if (slideAktif == null) return;

        // Ambil teks UI dari slide yang sedang aktif
        TextMeshProUGUI teksUI = slideAktif.GetComponentInChildren<TextMeshProUGUI>(true);

        if (teksUI != null && !string.IsNullOrEmpty(teksUI.text))
        {
            string isiText = teksUI.text;
            
            // Nama file audio dibuat unik berdasarkan nama marker dan slide index
            string namaFile = $"Audio_{transform.root.name}_{gameObject.name}_Slide_{currentIndex}";

            if (EdgeTTS.instance != null)
            {
                EdgeTTS.instance.GetTTSAudio(isiText, namaFile, (clip) =>
                {
                    // Pastikan audio hanya diputar jika panel marker ini MASIH aktif di layar
                    if (clip != null && activeCarousel == this)
                    {
                        if (audioSource == null) audioSource = GetComponent<AudioSource>();
                        if (audioSource != null)
                        {
                            audioSource.Stop();
                            audioSource.clip = clip;
                            audioSource.Play();
                        }
                    }
                });
            }
        }
        else
        {
            Debug.LogWarning($"[CarouselSet] Teks TextMeshProUGUI tidak ditemukan pada slide index {currentIndex}!");
        }
    }

    public void StopAudio()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }
    }

    private void HideNavigationButtons()
    {
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (prevButton != null) prevButton.gameObject.SetActive(false);
        if (TTSButton != null) TTSButton.gameObject.SetActive(false);
    }
}
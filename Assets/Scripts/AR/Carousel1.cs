using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class SlideData
{
    public GameObject slideObject;
    public TextMeshProUGUI teksUI;
    public string idAudioUnik;
}

public class Carousel1 : MonoBehaviour
{
    [Header ("Daftar Slide Halaman")]
    public SlideData[] daftarSlide;
    [Header ("AudioOutput")]
    public AudioSource audioSource;
    private int indexSekarang = 0;

    [Header ("Daftar Halaman")]
    public GameObject[] daftarHalaman;
    [Header ("Navigate Button")]
    public Button prev;
    public Button next;
    private int indeksHalamanSekarang = 0;

    void Start()
    {
        if (prev != null) prev.onClick.AddListener(PrevHalaman);
        if (next != null) next.onClick.AddListener(NeksHalaman);
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        HideButton();
    }

    public void ResetCarousel()
    {
        indeksHalamanSekarang = 0;
        indexSekarang = 0;
        UpdateCarousel();
    }

    public void NeksHalaman()
    {
        if (indeksHalamanSekarang < daftarHalaman.Length - 1)
        {
            indeksHalamanSekarang++;
            UpdateCarousel();
        }
        if (indexSekarang < daftarSlide.Length - 1)
        {
            indexSekarang++;
            UpdateCarousel();
        }
    }

    public void PrevHalaman()
    {
        if (indeksHalamanSekarang > 0)
        {
            indeksHalamanSekarang--;
            UpdateCarousel();
        }
        if (indexSekarang > 0)
        {
            indexSekarang--;
            UpdateCarousel();
        }
    }

    public void UpdateCarousel()
    {
        if(daftarHalaman == null || daftarHalaman.Length == 0) return;

        for (int i = 0; i < daftarHalaman.Length; i++)
        {
            if(daftarHalaman[i] != null) daftarHalaman[i].SetActive(i == indeksHalamanSekarang);
        }

        if (prev != null) prev.gameObject.SetActive(indeksHalamanSekarang > 0);
        if (next != null) next.gameObject.SetActive(indeksHalamanSekarang < daftarHalaman.Length - 1);

        for(int i = 0; i < daftarSlide.Length; i++)
        {
            if (daftarSlide[i].slideObject != null)
            {
                daftarSlide[i].slideObject.SetActive(i == indexSekarang);
            }
        }
    }

    public void PlayCurrentSlide()
    {
        SlideData slideAktif = daftarSlide[indexSekarang];
        if (slideAktif.teksUI != null)
        {
            string isiText = slideAktif.teksUI.text;
            string namaFile = string.IsNullOrEmpty(slideAktif.idAudioUnik)
                ? $"Slide_Audio_{indexSekarang}" : slideAktif.idAudioUnik;
            if (!string.IsNullOrEmpty(isiText))
            {
                EdgeTTS.instance.GetTTSAudio(isiText, namaFile, (clip) =>
                {
                    if (clip != null && audioSource != null)
                    {
                        audioSource.Stop();
                        audioSource.clip = clip;
                        audioSource.Play();
                    }
                });
            }
        }
    }

    public void HideButton()
    {
        indeksHalamanSekarang = 0;

        if (daftarHalaman != null && daftarHalaman.Length > 0)
        {
            for(int i = 0; i < daftarHalaman.Length; i++)
            {
                if (daftarHalaman[i] != null) daftarHalaman[i].SetActive(i == 0);
            }
        }

        if(next != null) next.gameObject.SetActive(false);
        if(prev != null) prev.gameObject.SetActive(false);
    }

}

using UnityEngine;
using UnityEngine.UI;

public class Carousel1 : MonoBehaviour
{
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

        HideButton();
    }

    public void ResetCarousel()
    {
        indeksHalamanSekarang = 0;
        UpdateCarousel();
    }

    public void NeksHalaman()
    {
        if (indeksHalamanSekarang < daftarHalaman.Length - 1)
        {
            indeksHalamanSekarang++;
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

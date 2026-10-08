using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Struct untuk menggabungkan teks dan gambar dalam 1 halaman panduan
[System.Serializable]
public class HalamanPanduanData
{
    [TextArea(2, 5)] public string teksPanduan;
    public Sprite gambarPanduan; // Boleh diisi Sprite atau dikosongkan
}

public class PanduanPopupManager : MonoBehaviour
{
    [Header("UI Component References")]
    [SerializeField] private GameObject panelPanduan;
    [SerializeField] private TextMeshProUGUI textJudulPanduan;
    [SerializeField] private TextMeshProUGUI textIsiPanduan;
    [SerializeField] private Image imagePanduan; // Slot Komponen UI Image
    [SerializeField] private TextMeshProUGUI textIndikatorHalaman;
    [SerializeField] private Button buttonNext;
    [SerializeField] private Button buttonPrev;
    [SerializeField] private Button buttonSelesai;

    [Header("Pengaturan Panduan")]
    [SerializeField] private string judulPanduan = "Panduan Fitur";
    [SerializeField] private List<HalamanPanduanData> daftarHalamanPanduan;

    private int indexHalaman = 0;

    public void BukaPanduan()
    {
        if (panelPanduan != null) panelPanduan.SetActive(true);
        indexHalaman = 0;
        TampilkanHalaman();
    }

    private void TampilkanHalaman()
    {
        if (daftarHalamanPanduan == null || daftarHalamanPanduan.Count == 0) return;

        HalamanPanduanData dataSekarang = daftarHalamanPanduan[indexHalaman];

        // 1. Set Judul & Teks Panduan
        if (textJudulPanduan != null) textJudulPanduan.text = judulPanduan;
        if (textIsiPanduan != null) textIsiPanduan.text = dataSekarang.teksPanduan;

        // 2. Set Gambar Panduan (Otomatis Sembunyi Jika Sprite Kosong)
        if (imagePanduan != null)
        {
            if (dataSekarang.gambarPanduan != null)
            {
                imagePanduan.gameObject.SetActive(true);
                imagePanduan.sprite = dataSekarang.gambarPanduan;
            }
            else
            {
                imagePanduan.gameObject.SetActive(false);
            }
        }

        // 3. Set Indikator Halaman (Contoh: "1 / 4")
        if (textIndikatorHalaman != null)
        {
            textIndikatorHalaman.text = $"{indexHalaman + 1} / {daftarHalamanPanduan.Count}";
        }

        // 4. Logika Tombol Prev, Next, dan Selesai
        if (buttonPrev != null) buttonPrev.gameObject.SetActive(indexHalaman > 0);

        bool isHalamanTerakhir = (indexHalaman == daftarHalamanPanduan.Count - 1);
        if (buttonNext != null) buttonNext.gameObject.SetActive(!isHalamanTerakhir);
        if (buttonSelesai != null) buttonSelesai.gameObject.SetActive(isHalamanTerakhir);
    }

    public void KlikNext()
    {
        if (indexHalaman < daftarHalamanPanduan.Count - 1)
        {
            indexHalaman++;
            TampilkanHalaman();
        }
    }

    public void KlikPrev()
    {
        if (indexHalaman > 0)
        {
            indexHalaman--;
            TampilkanHalaman();
        }
    }

    public void KlikTutupPanduan()
    {
        if (panelPanduan != null) panelPanduan.SetActive(false);
    }
}
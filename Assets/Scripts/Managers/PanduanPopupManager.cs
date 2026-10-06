using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PanduanPopupManager : MonoBehaviour
{
    [Header("UI Component References")]
    [SerializeField] private GameObject panelPanduan;
    [SerializeField] private TextMeshProUGUI textJudulPanduan;
    [SerializeField] private TextMeshProUGUI textIsiPanduan;
    [SerializeField] private TextMeshProUGUI textIndikatorHalaman;
    [SerializeField] private Button buttonNext;
    [SerializeField] private Button buttonPrev;
    [SerializeField] private Button buttonSelesai; // Tombol Ceklis / OK

    [Header("Pengaturan Panduan")]
    [SerializeField] private string judulPanduan = "Panduan Game Isi Piringku";
    [SerializeField] [TextArea(3, 8)] private List<string> daftarHalamanPanduan;

    private int indexHalaman = 0;

    // void OnEnable()
    // {
    //     // Otomatis buka panduan saat Canvas/Panel pertama kali diaktifkan
    //     BukaPanduan();
    // }

    public void BukaPanduan()
    {
        if (panelPanduan != null) panelPanduan.SetActive(true);
        indexHalaman = 0;
        TampilkanHalaman();
    }

    private void TampilkanHalaman()
    {
        if (daftarHalamanPanduan == null || daftarHalamanPanduan.Count == 0) return;

        // Set Teks Judul & Isi Panduan
        if (textJudulPanduan != null) textJudulPanduan.text = judulPanduan;
        if (textIsiPanduan != null) textIsiPanduan.text = daftarHalamanPanduan[indexHalaman];

        // Set Indikator Halaman (Contoh: "1 / 3")
        if (textIndikatorHalaman != null)
        {
            textIndikatorHalaman.text = $"{indexHalaman + 1} / {daftarHalamanPanduan.Count}";
        }

        // Logika Tombol Prev
        if (buttonPrev != null)
        {
            buttonPrev.gameObject.SetActive(indexHalaman > 0);
        }

        // Cek Apakah Halaman Terakhir
        bool isHalamanTerakhir = (indexHalaman == daftarHalamanPanduan.Count - 1);

        // Jika halaman terakhir: Sembunyikan 'Next', Tampilkan 'Selesai/OK'
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
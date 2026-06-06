using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PiringGameManager : MonoBehaviour
{
    public static PiringGameManager Instance;

    // Mapping Object untuk mendaftarkan makanan berdasarkan rumpun Isi Piringku
    [System.Serializable]
    public struct PiringFoodMapping
    {
        public FoodData.TipeGizi tipeGizi; // Karbohidrat/Protein/Serat/Mineral
        public List<FoodData> daftarMakanan; // List ScriptableObject makanan yang cocok
    }

    [Header("Food Mapping Data")]
    [SerializeField] private List<PiringFoodMapping> semuaKategoriMakanan;

    [Header("UI Canvas & Panel References")]
    [SerializeField] private Transform panelTempatMunculMakanan; // Panel horizontal di bawah keranjang
    [SerializeField] private GameObject prefabTombolMakanan; // Prefab UI untuk menampilkan pilihan makanan
    [SerializeField] private Transform piringGridParent; // Objek Piring yang dipasangi Grid Layout Group

    [Header("UI Text Persentase Angka")]
    [SerializeField] private TextMeshProUGUI textPersenPokok;
    [SerializeField] private TextMeshProUGUI textPersenSayur;
    [SerializeField] private TextMeshProUGUI textPersenLauk;
    [SerializeField] private TextMeshProUGUI textPersenBuah;
    [SerializeField] private TextMeshProUGUI textLevelIndicator;
    [SerializeField] private TextMeshProUGUI textTimer;

    [Header("UI Bar Fill Elements")]
    [SerializeField] private Image barMakananPokok; 
    [SerializeField] private Image barSayuran;
    [SerializeField] private Image barLaukPauk;
    [SerializeField] private Image barBuahBuahan;

    [Header("Feedback & Popups")]
    [SerializeField] private TextMeshProUGUI textPeringatanZibo; 
    [SerializeField] private GameObject panelPopupWarning;
    [SerializeField] private GameObject buttonNextLevel;

    private float currentPokok, currentSayur, currentLauk, currentBuah;
    private float timeLeft = 70f;
    private bool isGameActive = false;
    private int currentLevel = 1;

    void Awake() 
    { 
        Instance = this; 
    }

    void Start()
    {
        // Supaya pas lu pencet Play langsung di scene GameMenu (buat testing),
        // game Piringku bisa langsung otomatis berjalan dan isGameActive berubah jadi true!
        MulaiGamePiringku();
    }

    void Update()
    {
        if (isGameActive)
        {
            timeLeft -= Time.deltaTime;
            textTimer.text = Mathf.CeilToInt(timeLeft).ToString() + "s";
            if (timeLeft <= 0) TriggerGameOver();
        }
    }

    public void MulaiGamePiringku()
    {
        currentLevel = 1;
        GenerateLevelAcak();
    }

    // MEKANIK POIN 1: Mengacak kondisi awal piring secara matematis aman
    public void GenerateLevelAcak()
    {
        textLevelIndicator.text = "LEVEL " + currentLevel;
        panelPopupWarning.SetActive(false);
        buttonNextLevel.SetActive(false);
        ClearPiring();

        // Acak isi awal berdasarkan kelipatan step gizi masing-masing
        float[] opsiPokokSayur = { 0f, 11f, 22f };
        float[] opsiLaukBuah = { 0f, 8.5f };

        currentPokok = opsiPokokSayur[Random.Range(0, opsiPokokSayur.Length)];
        currentSayur = opsiPokokSayur[Random.Range(0, opsiPokokSayur.Length)];
        currentLauk = opsiLaukBuah[Random.Range(0, opsiLaukBuah.Length)];
        currentBuah = opsiLaukBuah[Random.Range(0, opsiLaukBuah.Length)];

        // Antisipasi kalau hoki dapet acakan langsung menang (penuh semua), kita paksa kosongin satu
        if (currentPokok == 22f && currentSayur == 22f && currentLauk == 8.5f && currentBuah == 8.5f)
        {
            currentPokok = 0f;
        }

        UpdateVisualDanAngkaUI();
        timeLeft = 80f;
        isGameActive = true;
    }

    public void TutupPanelWarning()
    {
        panelPopupWarning.SetActive(false); // Menyembunyikan panel warning gizi
    }

    // MEKANIK POIN 3: Munculin daftar makanan pas keranjang gizi di-klik
    public void AmbilMakananDariKeranjang(string namaKategori)
    {
        if (!isGameActive) return;

        // Bersihkan dulu sisa makanan dari keranjang sebelumnya
        foreach (Transform child in panelTempatMunculMakanan)
        {
            Destroy(child.gameObject);
        }

        FoodData.TipeGizi giziDicari = FoodData.TipeGizi.Karbohidrat;
        if (namaKategori == "protein") giziDicari = FoodData.TipeGizi.Protein;
        else if (namaKategori == "serat") giziDicari = FoodData.TipeGizi.Serat;
        else if (namaKategori == "mineral") giziDicari = FoodData.TipeGizi.Mineral;

        // Cari daftarnya di Mapping Object Inspector
        foreach (var mapping in semuaKategoriMakanan)
        {
            if (mapping.tipeGizi == giziDicari)
            {
                foreach (FoodData dataFood in mapping.daftarMakanan)
                {
                    // Spawn tombol item makanannya ke panel pemilihan bawah keranjang
                    GameObject tombolBaru = Instantiate(prefabTombolMakanan, panelTempatMunculMakanan);
                    
                    // Set gambar makanannya menggunakan script FoodDisplay bawaan lu
                    FoodDisplay fd = tombolBaru.GetComponent<FoodDisplay>();
                    if (fd != null)
                    {
                        fd.data = dataFood;
                        fd.InisialisasiGambar();
                    } 
                }
                break;
            }
        }
    }

    public void TambahBahanKePiring(FoodData.TipeGizi tipe, GameObject itemObyek)
    {
        if (!isGameActive) return;

        if (tipe == FoodData.TipeGizi.Karbohidrat)
        {
            if (currentPokok + 11f > 33f) { TampilkanPeringatan("Waduh, Makanan Pokokmu sudah penuh!"); Destroy(itemObyek); return; }
            currentPokok += 11f;
        }
        else if (tipe == FoodData.TipeGizi.Serat)
        {
            if (currentSayur + 11f > 33f) { TampilkanPeringatan("Eits, Sayurannya kebanyakan!"); Destroy(itemObyek); return; }
            currentSayur += 11f;
        }
        else if (tipe == FoodData.TipeGizi.Protein)
        {
            if (currentLauk + 8.5f > 17f) { TampilkanPeringatan("Waduh, Laukmu udah cukup tuh!"); Destroy(itemObyek); return; }
            currentLauk += 8.5f;
        }
        else if (tipe == FoodData.TipeGizi.Mineral)
        {
            if (currentBuah + 8.5f > 17f) { TampilkanPeringatan("Buah-buahannya sudah cukup manis!"); Destroy(itemObyek); return; }
            currentBuah += 8.5f;
        }

        // Poin Utama: Pindahkan objek makanan agar masuk ke susunan grid rapi di piring
        itemObyek.transform.SetParent(piringGridParent);
        
        // Matikan script DraggableItem bawaan lu biar gak bisa ditarik-tarik lagi pas udah di piring
        if(itemObyek.GetComponent<DraggableItem>() != null)
        {
            Destroy(itemObyek.GetComponent<DraggableItem>());
        }

        UpdateVisualDanAngkaUI();
        CekKondisiMenang();
    }

    private void UpdateVisualDanAngkaUI()
    {
        // Update Bar Fill
        barMakananPokok.fillAmount = currentPokok / 33f;
        barSayuran.fillAmount = currentSayur / 33f;
        barLaukPauk.fillAmount = currentLauk / 17f;
        barBuahBuahan.fillAmount = currentBuah / 17f;

        // Update Angka Teks Persen (Permintaan Poin 3)
        textPersenPokok.text = currentPokok.ToString("F1") + "% / 33%";
        textPersenSayur.text = currentSayur.ToString("F1") + "% / 33%";
        textPersenLauk.text = currentLauk.ToString("F1") + "% / 17%";
        textPersenBuah.text = currentBuah.ToString("F1") + "% / 17%";
    }

    private void CekKondisiMenang()
    {
        if (Mathf.Approximately(currentPokok, 33f) && 
            Mathf.Approximately(currentSayur, 33f) && 
            Mathf.Approximately(currentLauk, 17f) && 
            Mathf.Approximately(currentBuah, 17f))
        {
            isGameActive = false; 
            buttonNextLevel.SetActive(true); 
            TampilkanPeringatan("<color=green>Hebat! Piring Gizi Seimbangmu Sempurna!</color>");
        }
    }

    public void KlikNextLevel()
    {
        currentLevel++;
        GenerateLevelAcak();
    }

    private void TampilkanPeringatan(string pesan)
    {
        textPeringatanZibo.text = pesan;
        panelPopupWarning.SetActive(true);
    }

    private void ClearPiring()
    {
        foreach (Transform child in piringGridParent)
        {
            Destroy(child.gameObject);
        }
    }

    private void TriggerGameOver()
    {
        isGameActive = false;
        TampilkanPeringatan("<color=red>WAKTU HABIS! Yuk Coba Lagi.</color>");
        Invoke("MulaiGamePiringku", 3f);
    }
}
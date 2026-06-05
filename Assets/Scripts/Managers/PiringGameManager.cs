using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PiringGameManager : MonoBehaviour
{
    public static PiringGameManager Instance;

    [System.Serializable]
    public struct LevelConfig
    {
        public float awalPokok; // Isi awal dalam persen (misal: 11f atau 33f)
        public float awalSayur;
        public float awalLauk;
        public float awalBuah;
    }

    [System.Serializable]
    public struct UndoAction
    {
        public string tipeGizi; 
        public float jumlahPersen;
    }

    [Header("Level System Configuration")]
    [SerializeField] private LevelConfig[] daftarLevel = new LevelConfig[10];
    private int currentLevelIndex = 0; 

    [Header("Timer Elements")]
    [SerializeField] private TextMeshProUGUI textTimer;
    [SerializeField] private TextMeshProUGUI textLevelIndicator;
    private float timeLeft = 70f;
    private bool isGameActive = false;

    [Header("UI Bar Fill Elements")]
    [SerializeField] private Image barMakananPokok; 
    [SerializeField] private Image barSayuran;
    [SerializeField] private Image barLaukPauk;
    [SerializeField] private Image barBuahBuahan;

    [Header("Feedback & Navigation UI")]
    [SerializeField] private TextMeshProUGUI textPeringatanZibo; 
    [SerializeField] private GameObject panelPopupWarning;
    [SerializeField] private Button buttonNextLevel;
    [SerializeField] private Button buttonUndo;

    private float currentPokok, currentSayur, currentLauk, currentBuah;
    private Stack<UndoAction> riwayatAksi = new Stack<UndoAction>();

    void Awake() 
    { 
        Instance = this; 
    }

    // Fungsi ini dipanggil saat Canvas Piringku dinyalakan
    public void MulaiGamePiringku()
    {
        SetupLevelMulai(0); // Start dari level 1
    }

    void Update()
    {
        if (isGameActive)
        {
            HitungMundurTimer();
        }
    }

    public void SetupLevelMulai(int levelIndex)
    {
        currentLevelIndex = levelIndex;
        textLevelIndicator.text = "LEVEL " + (currentLevelIndex + 1);
        
        LevelConfig config = daftarLevel[currentLevelIndex];
        currentPokok = config.awalPokok;
        currentSayur = config.awalSayur;
        currentLauk = config.awalLauk;
        currentBuah = config.awalBuah;

        riwayatAksi.Clear(); 
        UpdateVisualBar();

        timeLeft = 70f;
        isGameActive = true;
        buttonNextLevel.gameObject.SetActive(false);
        panelPopupWarning.SetActive(false);
    }

    private void HitungMundurTimer()
    {
        timeLeft -= Time.deltaTime;
        textTimer.text = Mathf.CeilToInt(timeLeft).ToString() + "s";

        if (timeLeft <= 0)
        {
            TriggerGameOver();
        }
    }

    public void TambahBahanMakanan(string kategori, string namaMakanan)
    {
        if (!isGameActive) return;

        UndoAction aksiBaru = new UndoAction();
        aksiBaru.tipeGizi = kategori;

        if (kategori == "pokok")
        {
            if (currentPokok + 11f > 33f) { TampilkanPeringatan("Waduh, Makanan Pokokmu sudah penuh!"); return; }
            currentPokok += 11f;
            aksiBaru.jumlahPersen = 11f;
        }
        else if (kategori == "sayur")
        {
            if (currentSayur + 11f > 33f) { TampilkanPeringatan("Eits, Sayurannya kebanyakan! Perutmu bisa kembung."); return; }
            currentSayur += 11f;
            aksiBaru.jumlahPersen = 11f;
        }
        else if (kategori == "lauk")
        {
            if (currentLauk + 8.5f > 17f) { TampilkanPeringatan("Waduh, Laukmu udah cukup tuh!"); return; }
            currentLauk += 8.5f;
            aksiBaru.jumlahPersen = 8.5f;
        }
        else if (kategori == "buah")
        {
            if (currentBuah + 8.5f > 17f) { TampilkanPeringatan("Buah-buahannya sudah cukup manis kok!"); return; }
            currentBuah += 8.5f;
            aksiBaru.jumlahPersen = 8.5f;
        }

        riwayatAksi.Push(aksiBaru); 
        UpdateVisualBar();
        CekKondisiMenang();
    }

    public void KlikTombolUndo()
    {
        if (riwayatAksi.Count == 0 || !isGameActive) return;

        UndoAction aksiTerakhir = riwayatAksi.Pop(); 

        if (aksiTerakhir.tipeGizi == "pokok") currentPokok -= aksiTerakhir.jumlahPersen;
        else if (aksiTerakhir.tipeGizi == "sayur") currentSayur -= aksiTerakhir.jumlahPersen;
        else if (aksiTerakhir.tipeGizi == "lauk") currentLauk -= aksiTerakhir.jumlahPersen;
        else if (aksiTerakhir.tipeGizi == "lauk") currentLauk -= aksiTerakhir.jumlahPersen;
        else if (aksiTerakhir.tipeGizi == "buah") currentBuah -= aksiTerakhir.jumlahPersen;

        UpdateVisualBar();
        panelPopupWarning.SetActive(false); 
    }

    private void UpdateVisualBar()
    {
        // Diubah ke pembagian proporsional max target masing-masing gizi
        barMakananPokok.fillAmount = currentPokok / 33f;
        barSayuran.fillAmount = currentSayur / 33f;
        barLaukPauk.fillAmount = currentLauk / 17f;
        barBuahBuahan.fillAmount = currentBuah / 17f;
    }

    private void CekKondisiMenang()
    {
        if (Mathf.Approximately(currentPokok, 33f) && 
            Mathf.Approximately(currentSayur, 33f) && 
            Mathf.Approximately(currentLauk, 17f) && 
            Mathf.Approximately(currentBuah, 17f))
        {
            isGameActive = false; 
            buttonNextLevel.gameObject.SetActive(true); 
            TampilkanPeringatan("<color=green>Hebat! Piring Gizi Seimbangmu Sempurna!</color>");
        }
    }

    private void TampilkanPeringatan(string pesan)
    {
        textPeringatanZibo.text = pesan;
        panelPopupWarning.SetActive(true);
    }

    public void KlikNextLevel()
    {
        if (currentLevelIndex + 1 < daftarLevel.Length)
        {
            SetupLevelMulai(currentLevelIndex + 1);
        }
        else
        {
            TampilkanPeringatan("Selamat! Kamu Tamat Menjadi Master Gizi Piring-Ku!");
        }
    }

    private void TriggerGameOver()
    {
        isGameActive = false;
        TampilkanPeringatan("<color=red>WAKTU HABIS! Game Over.</color>");
        Invoke("ResetKeLevelSatu", 3f);
    }

    private void ResetKeLevelSatu()
    {
        SetupLevelMulai(0); 
    }
}
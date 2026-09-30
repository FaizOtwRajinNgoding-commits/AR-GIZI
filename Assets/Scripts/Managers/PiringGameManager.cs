using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PiringGameManager : MonoBehaviour
{
    public static PiringGameManager Instance;

    [Header("Chance Selection Pool References")]
    public FoodGiziPool giziPool;
    [SerializeField] private ChancePoolVisualizer visualizerUI;

    [Header("Studi Kasus & Narasi UI")]
    [SerializeField] [TextArea(2, 5)] private List<string> daftarCeritaStudiKasus;
    [SerializeField] private TextMeshProUGUI textStudiKasusUI;

    [Header("Etalase Rak Kiri References")]
    [SerializeField] private Transform rakKarbohidrat;
    [SerializeField] private Transform rakProtein;
    [SerializeField] private Transform rakSerat;
    [SerializeField] private Transform rakVitamin;

    [Header("Piring Drop Zone References")]
    [SerializeField] private Transform wadahPiringPokok;
    [SerializeField] private Transform wadahPiringLauk;
    [SerializeField] private Transform wadahPiringSayur;
    [SerializeField] private Transform wadahPiringBuah;
    [SerializeField] private GameObject prefabTombolMakanan; 

    [Header("Panel Submit & Feedback UI")]
    [SerializeField] private GameObject panelPopupFeedback;
    [SerializeField] private TextMeshProUGUI textFeedbackMessege;

    // Cache List Data Makanan dari Rak
    private List<FoodData> listKarbo = new List<FoodData>();
    private List<FoodData> listProtein = new List<FoodData>();
    private List<FoodData> listSerat = new List<FoodData>();
    private List<FoodData> listMineral = new List<FoodData>();

    // Counter Makanan di Piring
    private int countKarbo = 0;
    private int countProtein = 0;
    private int countSerat = 0;
    private int countVitamin = 0;

    void Awake()
    {
        Instance = this;
    }

    // 🛠️ DIPANGGIL SETIAP KALI CANVAS PIRINGKU DIAKTIFKAN (Bisa berulang kali dari Main Menu)
    void OnEnable()
    {
        MulaiGamePiringku();
    }

    public void MulaiGamePiringku()
    {
        if (panelPopupFeedback != null) panelPopupFeedback.SetActive(false);
        
        KumpulkanDataDariEtalase();
        InisialisasiChancePool();
        KlikRefreshStudiKasus();
    }

    private void InisialisasiChancePool()
    {
        if (giziPool == null) giziPool = new FoodGiziPool();
        
        // Paksa nama pool agar tidak menggunakan teks "Weapon Pool / Pistol"
        giziPool.poolName = "Isi Piringku";
        
        if (visualizerUI != null) giziPool.visualizer = visualizerUI;

        // Reset & Re-add item pool
        giziPool.AddItem(FoodData.TipeGizi.Karbohidrat, 0);
        giziPool.AddItem(FoodData.TipeGizi.Protein, 0);
        giziPool.AddItem(FoodData.TipeGizi.Serat, 0);
        giziPool.AddItem(FoodData.TipeGizi.Mineral, 0);

        giziPool.RedrawVisualizer();
    }

    private void KumpulkanDataDariEtalase()
    {
        listKarbo.Clear();
        listProtein.Clear();
        listSerat.Clear();
        listMineral.Clear();

        ScanRak(rakKarbohidrat, listKarbo);
        ScanRak(rakProtein, listProtein);
        ScanRak(rakSerat, listSerat);
        ScanRak(rakVitamin, listMineral);
    }

    private void ScanRak(Transform rak, List<FoodData> targetList)
    {
        if (rak == null) return;
        foreach (Transform child in rak)
        {
            FoodDisplay display = child.GetComponent<FoodDisplay>();
            if (display != null && display.data != null && !targetList.Contains(display.data))
            {
                targetList.Add(display.data);
            }
        }
    }

    public void TambahBahanKePiring(FoodData data, GameObject itemKloning, Transform targetWadahDrop)
    {
        if (data == null || itemKloning == null) return;

        itemKloning.transform.SetParent(targetWadahDrop, false);
        itemKloning.transform.localRotation = Quaternion.identity;
        itemKloning.transform.localScale = Vector3.one; 

        CanvasGroup cg = itemKloning.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
        }

        DraggableItemPiring dragScript = itemKloning.GetComponent<DraggableItemPiring>();
        if (dragScript != null) dragScript.enabled = false;

        Button btn = itemKloning.GetComponent<Button>();
        if (btn == null) btn = itemKloning.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => HapusBahanDariPiring(data, itemKloning));

        if (data.jenisGizi != null && data.jenisGizi.Count > 0)
        {
            UpdateHitunganGizi(data.jenisGizi[0], 1);
        }
    }

    public void HapusBahanDariPiring(FoodData data, GameObject itemPiring)
    {
        if (data != null && data.jenisGizi != null && data.jenisGizi.Count > 0)
        {
            UpdateHitunganGizi(data.jenisGizi[0], -1);
        }
        Destroy(itemPiring);
    }

    private void UpdateHitunganGizi(FoodData.TipeGizi tipe, int delta)
    {
        if (tipe == FoodData.TipeGizi.Karbohidrat) countKarbo = Mathf.Max(0, countKarbo + delta);
        else if (tipe == FoodData.TipeGizi.Protein) countProtein = Mathf.Max(0, countProtein + delta);
        else if (tipe == FoodData.TipeGizi.Serat) countSerat = Mathf.Max(0, countSerat + delta);
        else if (tipe == FoodData.TipeGizi.Mineral) countVitamin = Mathf.Max(0, countVitamin + delta);

        giziPool.SetChance(FoodData.TipeGizi.Karbohidrat, countKarbo);
        giziPool.SetChance(FoodData.TipeGizi.Protein, countProtein);
        giziPool.SetChance(FoodData.TipeGizi.Serat, countSerat);
        giziPool.SetChance(FoodData.TipeGizi.Mineral, countVitamin);

        giziPool.RedrawVisualizer();
    }

    public void KlikRefreshStudiKasus()
    {
        // 1. Bersihkan isi piring lama
        if (wadahPiringPokok != null) foreach (Transform child in wadahPiringPokok) Destroy(child.gameObject);
        if (wadahPiringLauk != null) foreach (Transform child in wadahPiringLauk) Destroy(child.gameObject);
        if (wadahPiringSayur != null) foreach (Transform child in wadahPiringSayur) Destroy(child.gameObject);
        if (wadahPiringBuah != null) foreach (Transform child in wadahPiringBuah) Destroy(child.gameObject);

        countKarbo = 0; countProtein = 0; countSerat = 0; countVitamin = 0;

        if (daftarCeritaStudiKasus != null && daftarCeritaStudiKasus.Count > 0)
        {
            int indeksAcak = Random.Range(0, daftarCeritaStudiKasus.Count);
            textStudiKasusUI.text = daftarCeritaStudiKasus[indeksAcak];
        }

        // 2. Spawn Makanan Acak Bawaan di Atas Piring
        SpawnItemAcakAwal(listKarbo, wadahPiringPokok, Random.Range(1, 3)); // 1-2 Karbo
        SpawnItemAcakAwal(listProtein, wadahPiringLauk, Random.Range(1, 2)); // 1 Protein
        SpawnItemAcakAwal(listSerat, wadahPiringSayur, Random.Range(1, 3)); // 1-2 Sayur
        SpawnItemAcakAwal(listMineral, wadahPiringBuah, Random.Range(1, 2)); // 1 Buah

        if (panelPopupFeedback != null) panelPopupFeedback.SetActive(false);
    }

    private void SpawnItemAcakAwal(List<FoodData> sourceList, Transform targetWadah, int jumlah)
    {
        if (sourceList == null || sourceList.Count == 0 || targetWadah == null) return;

        for (int i = 0; i < jumlah; i++)
        {
            FoodData randomData = sourceList[Random.Range(0, sourceList.Count)];
            GameObject itemObj = Instantiate(prefabTombolMakanan, targetWadah);
            
            FoodDisplay display = itemObj.GetComponent<FoodDisplay>();
            if (display != null)
            {
                display.data = randomData;
                display.InisialisasiGambar();
            }

            DraggableItemPiring dragScript = itemObj.GetComponent<DraggableItemPiring>();
            if (dragScript != null) dragScript.enabled = false;

            Button btn = itemObj.GetComponent<Button>();
            if (btn == null) btn = itemObj.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            FoodData dataLokal = randomData;
            btn.onClick.AddListener(() => HapusBahanDariPiring(dataLokal, itemObj));

            if (randomData.jenisGizi != null && randomData.jenisGizi.Count > 0)
            {
                UpdateHitunganGizi(randomData.jenisGizi[0], 1);
            }
        }
    }

    public void KlikCekIsiPiringku()
    {
        if (panelPopupFeedback != null) panelPopupFeedback.SetActive(true);

        if (countKarbo >= 2 && countProtein >= 1 && countSerat >= 2 && countVitamin >= 1)
        {
            textFeedbackMessege.text = "HEBAT!\nKomposisi Piringmu Sudah Memenuhi Gizi Seimbang!";
        }
        else
        {
            textFeedbackMessege.text = "BELUM SEIMBANG!\nCoba perhatikan lagi grafik persentase gizi piringmu ya!";
        }
    }

    public void TutupPopupFeedback()
    {
        if (panelPopupFeedback != null) panelPopupFeedback.SetActive(false);
    }
}
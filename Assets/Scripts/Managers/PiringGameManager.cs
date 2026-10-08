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

    int totalItem = countKarbo + countProtein + countSerat + countVitamin;

    // 1. Cek piring kosong
    if (totalItem == 0)
    {
        textFeedbackMessege.text = "<color=red>PIRING MASIH KOSONG!</color>\nSeret makanan dari rak etalase ke piring terlebih dahulu ya!";
        return;
    }

    // 2. Cek apakah ada kelompok gizi yang sama sekali belum dimasukkan
    if (countKarbo == 0 || countProtein == 0 || countSerat == 0 || countVitamin == 0)
    {
        textFeedbackMessege.text = "<color=red>BELUM SEIMBANG!</color>\nSemua jenis gizi (Karbohidrat, Lauk, Sayur, dan Buah) harus ada di atas piringmu!";
        return;
    }

    // 3. Hitung persentase proporsi gizi real-time (%)
    float pctKarbo = ((float)countKarbo / totalItem) * 100f;
    float pctProtein = ((float)countProtein / totalItem) * 100f;
    float pctSerat = ((float)countSerat / totalItem) * 100f;
    float pctVitamin = ((float)countVitamin / totalItem) * 100f;

    /*
     * KAIDAH ISI PIRINGKU (Rasio 2 : 1 : 2 : 1 dari Total 6 Bagian):
     * - Makanan Pokok (Karbo) = 33.3% (Toleransi: 28% - 38%)
     * - Lauk Pauk (Protein)    = 16.7% (Toleransi: 12% - 22%)
     * - Sayuran (Serat)        = 33.3% (Toleransi: 28% - 38%)
     * - Buah (Vitamin)        = 16.7% (Toleransi: 12% - 22%)
     */

    bool karboPass = pctKarbo >= 28f && pctKarbo <= 38f;
    bool proteinPass = pctProtein >= 12f && pctProtein <= 22f;
    bool seratPass = pctSerat >= 28f && pctSerat <= 38f;
    bool vitaminPass = pctVitamin >= 12f && pctVitamin <= 22f;

    // 4. Evaluasi hasil
    if (karboPass && proteinPass && seratPass && vitaminPass)
    {
        textFeedbackMessege.text = "<color=green>HEBAT! SANGAT SEIMBANG! </color>\nKomposisi piringmu sudah sesuai dengan kaidah Isi Piringku:\n" +
                                   "• Makanan Pokok: 2/3 Setengah Piring (~33%)\n" +
                                   "• Lauk Pauk: 1/3 Setengah Piring (~17%)\n" +
                                   "• Sayuran: 2/3 Setengah Piring (~33%)\n" +
                                   "• Buah-buahan: 1/3 Setengah Piring (~17%)";
    }
    else
    {
        // Berikan petunjuk edukatif bagian mana yang kurang/kelebihan
        string saran = "";
        if (pctKarbo > 38f) saran += "• Makanan Pokok (Karbohidrat) terlalu banyak.\n";
        else if (pctKarbo < 28f) saran += "• Makanan Pokok (Karbohidrat) masih kurang.\n";

        if (pctSerat < 28f) saran += "• Sayuran (Serat) masih kurang.\n";
        else if (pctSerat > 38f) saran += "• Sayuran (Serat) terlalu banyak.\n";

        if (pctProtein < 12f) saran += "• Lauk Pauk (Protein) masih kurang.\n";
        else if (pctProtein > 22f) saran += "• Lauk Pauk (Protein) terlalu banyak.\n";

        if (pctVitamin < 12f) saran += "• Buah-buahan (Vitamin) masih kurang.\n";
        else if (pctVitamin > 22f) saran += "• Buah-buahan (Vitamin) terlalu banyak.\n";

        textFeedbackMessege.text = "<color=red>BELUM SEIMBANG! </color>\n" + saran +
                                   "\nIngat rasio Isi Piringku: Makanan Pokok & Sayur masing-masing 2/3 dari setengah piring!";
    }
}

    public void TutupPopupFeedback()
    {
        if (panelPopupFeedback != null) panelPopupFeedback.SetActive(false);
    }
}
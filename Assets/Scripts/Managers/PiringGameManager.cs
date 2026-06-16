using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PiringGameManager : MonoBehaviour
{
    public static PiringGameManager Instance;

    [Header("Easy Chart Lite Link (Bypass Error Version)")]
    [SerializeField] private GameObject pieChartUtama; 

    [Header("Studi Kasus (Cuma Isi Teks Cerita Saja di Inspector!)")]
    [SerializeField] [TextArea(2, 5)] private List<string> daftarCeritaStudiKasus;
    [SerializeField] private TextMeshProUGUI textStudiKasusUI;

    [Header("Etalase Wadah References (Seret Objek Wadah dari Hierarchy)")]
    [SerializeField] private Transform wadahPokok;
    [SerializeField] private Transform wadahSayuran;
    [SerializeField] private Transform wadahLaukPauk;
    [SerializeField] private Transform wadahBuah;

    [Header("Piring & Prefab References")]
    [SerializeField] private Transform piringGridParent; 
    [SerializeField] private GameObject prefabTombolMakanan; 

    [Header("Popup & Feedback UI")]
    [SerializeField] private GameObject panelPopupFeedback;
    [SerializeField] private TextMeshProUGUI textFeedbackMessege;

    // List internal hasil auto-scan dari UI etalase lu
    private List<FoodData> listDataPokok = new List<FoodData>();
    private List<FoodData> listDataSayur = new List<FoodData>();
    private List<FoodData> listDataLauk = new List<FoodData>();
    private List<FoodData> listDataBuah = new List<FoodData>();

    private int currentPokok = 0;
    private int currentSayur = 0;
    private int currentLauk = 0;
    private int currentBuah = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        panelPopupFeedback.SetActive(false);
        
        // JALANKAN AUTO-SCAN: Ambil data gizi langsung dari tombol-tombol yang lu susun di UI!
        KumpulkanDataDariEtalase();

        KlikRefreshStudiKasus(); 
    }

    // Fungsi pembantu untuk scan otomatis isi objek wadah di UI Hierarchy
    private void KumpulkanDataDariEtalase()
    {
        ScanWadahGizi(wadahPokok, listDataPokok);
        ScanWadahGizi(wadahSayuran, listDataSayur);
        ScanWadahGizi(wadahLaukPauk, listDataLauk);
        ScanWadahGizi(wadahBuah, listDataBuah);
    }

    private void ScanWadahGizi(Transform wadah, List<FoodData> targetList)
    {
        if (wadah == null) return;
        foreach (Transform child in wadah)
        {
            FoodDisplay display = child.GetComponent<FoodDisplay>();
            if (display != null && display.data != null)
            {
                if (!targetList.Contains(display.data))
                {
                    targetList.Add(display.data);
                }
            }
        }
    }

    // Fungsi jembatan untuk dipanggil dari SceneCanvasSwitcher
    public void MulaiGamePiringku()
    {
        KlikRefreshStudiKasus();
    }

    public void TambahBahanKePiring(FoodData data, GameObject itemKloning)
    {
        itemKloning.transform.SetParent(piringGridParent);
        
        // Perbaikan Komponen ke DraggableItemPiring
        DraggableItemPiring dragScript = itemKloning.GetComponent<DraggableItemPiring>();
        if (dragScript != null) dragScript.enabled = false;

        Button btn = itemKloning.GetComponent<Button>();
        if (btn == null) btn = itemKloning.AddComponent<Button>();
        
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => HapusBahanDariPiring(data, itemKloning));

        HitungGizi(data.jenisGizi[0], 1);
        UpdateVisualPieChart();
    }

    public void HapusBahanDariPiring(FoodData data, GameObject itemPiring)
    {
        HitungGizi(data.jenisGizi[0], -1);
        Destroy(itemPiring);
        Invoke("UpdateVisualPieChart", 0.05f);
    }

    private void HitungGizi(FoodData.TipeGizi tipe, int nilai)
    {
        if (tipe == FoodData.TipeGizi.Karbohidrat) currentPokok += nilai;
        else if (tipe == FoodData.TipeGizi.Serat) currentSayur += nilai;
        else if (tipe == FoodData.TipeGizi.Protein) currentLauk += nilai;
        else if (tipe == FoodData.TipeGizi.Mineral) currentBuah += nilai;

        currentPokok = Mathf.Max(0, currentPokok);
        currentSayur = Mathf.Max(0, currentSayur);
        currentLauk = Mathf.Max(0, currentLauk);
        currentBuah = Mathf.Max(0, currentBuah);
    }

    private void UpdateVisualPieChart()
    {
        if (pieChartUtama == null) return;

        List<float> dataGiziTerbaru = new List<float>()
        {
            (float)currentPokok,
            (float)currentSayur,
            (float)currentLauk,
            (float)currentBuah
        };

        pieChartUtama.SendMessage("Plot", dataGiziTerbaru, SendMessageOptions.DontRequireReceiver);
    }

    public void KlikRefreshStudiKasus()
    {
        if (daftarCeritaStudiKasus.Count == 0) return;

        foreach (Transform child in piringGridParent)
        {
            Destroy(child.gameObject);
        }

        currentPokok = 0; currentSayur = 0; currentLauk = 0; currentBuah = 0;

        int indeksAcak = Random.Range(0, daftarCeritaStudiKasus.Count);
        textStudiKasusUI.text = daftarCeritaStudiKasus[indeksAcak];

        // Tentukan jumlah makanan acak awal yang nangkring di piring
        int randomPokok = Random.Range(0, 3); 
        int randomSayur = Random.Range(0, 2); 
        int randomLauk = Random.Range(0, 2); 
        int randomBuah = Random.Range(0, 2); 

        SpawnItemBawaan(FoodData.TipeGizi.Karbohidrat, randomPokok);
        SpawnItemBawaan(FoodData.TipeGizi.Serat, randomSayur);
        SpawnItemBawaan(FoodData.TipeGizi.Protein, randomLauk);
        SpawnItemBawaan(FoodData.TipeGizi.Mineral, randomBuah);

        Invoke("UpdateVisualPieChart", 0.05f);
    }

    private void SpawnItemBawaan(FoodData.TipeGizi tipe, int jumlah)
    {
        for (int i = 0; i < jumlah; i++)
        {
            FoodData dataAcak = AmbilMakananAcakBerdasarkanGizi(tipe);
            if (dataAcak != null)
            {
                GameObject itemBawaan = Instantiate(prefabTombolMakanan, piringGridParent);
                itemBawaan.GetComponent<FoodDisplay>().data = dataAcak;
                itemBawaan.GetComponent<FoodDisplay>().InisialisasiGambar();
                
                // Perbaikan Komponen ke DraggableItemPiring
                DraggableItemPiring dragScript = itemBawaan.GetComponent<DraggableItemPiring>();
                if (dragScript != null) dragScript.enabled = false;
                
                HitungGizi(tipe, 1);
            }
        }
    }

    private FoodData AmbilMakananAcakBerdasarkanGizi(FoodData.TipeGizi tipe)
    {
        if (tipe == FoodData.TipeGizi.Karbohidrat && listDataPokok.Count > 0) return listDataPokok[Random.Range(0, listDataPokok.Count)];
        if (tipe == FoodData.TipeGizi.Serat && listDataSayur.Count > 0) return listDataSayur[Random.Range(0, listDataSayur.Count)];
        if (tipe == FoodData.TipeGizi.Protein && listDataLauk.Count > 0) return listDataLauk[Random.Range(0, listDataLauk.Count)];
        if (tipe == FoodData.TipeGizi.Mineral && listDataBuah.Count > 0) return listDataBuah[Random.Range(0, listDataBuah.Count)];
        return null;
    }

    public void KlikCekIsiPiringku()
    {
        panelPopupFeedback.SetActive(true);

        if (currentPokok == 3 && currentSayur == 3 && currentLauk == 2 && currentBuah == 2)
        {
            textFeedbackMessege.text = "<color=green>HEBAT! 🎉\nKomposisi Piringmu Sempurna Gizi Seimbang!</color>";
        }
        else
        {
            textFeedbackMessege.text = "<color=red>WADUH, BELUM SEIMBANG! ❌\nYuk, perhatikan lagi grafik lingkaran gizi dan sesuaikan jumlahnya ya!</color>";
        }
    }

    public void TutupPopupFeedback()
    {
        panelPopupFeedback.SetActive(false);
    }
}
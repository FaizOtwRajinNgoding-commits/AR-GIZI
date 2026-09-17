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
    // 4 Wadah Spesifik Baru untuk pembagian kuadran gizi
    [SerializeField] private Transform wadahPiringPokok;
    [SerializeField] private Transform wadahPiringSayur;
    [SerializeField] private Transform wadahPiringLauk;
    [SerializeField] private Transform wadahPiringBuah;
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

        // 🛠️ SCRIPT SCANNER JALUR NINJA BY MIN:
        if (pieChartUtama != null)
        {
            Debug.Log("<color=cyan>================= START SCANNING EASYCHART =================</color>");
            Component[] components = pieChartUtama.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                string typeName = comp.GetType().FullName;
                
                // Cek apakah komponen ini bagian dari library EasyChart
                if (typeName.Contains("EasyChart"))
                {
                    Debug.Log($"<color=yellow>[KOMPONEN DITEMUKAN]: {typeName}</color>");
                    
                    // Bongkar semua fungsi/method publiknya
                    var methods = comp.GetType().GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
                    foreach (var m in methods)
                    {
                        Debug.Log($"   -> Nama Fungsi: <color=green>{m.Name}()</color>");
                    }
                    
                    // Bongkar semua variabel/property publiknya
                    var properties = comp.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
                    foreach (var p in properties)
                    {
                        Debug.Log($"   -> Nama Properti: <color=orange>{p.Name}</color>");
                    }
                }
            }
            Debug.Log("<color=cyan>================== END SCANNING EASYCHART ==================</color>");
        }
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
        if (data == null || itemKloning == null) return;

        // 1. Tentukan target wadah secara dinamis berdasarkan tipe gizi makanan (data.jenisGizi[0])
        Transform targetWadah = piringGridParent; // Fallback ke parent utama jika wadah spesifik di inspector kosong

        if (data.jenisGizi != null && data.jenisGizi.Count > 0)
        {
            FoodData.TipeGizi tipe = data.jenisGizi[0];
            
            if (tipe == FoodData.TipeGizi.Karbohidrat && wadahPiringPokok != null)
            {
                targetWadah = wadahPiringPokok;
            }
            else if (tipe == FoodData.TipeGizi.Serat && wadahPiringSayur != null)
            {
                targetWadah = wadahPiringSayur;
            }
            else if (tipe == FoodData.TipeGizi.Protein && wadahPiringLauk != null)
            {
                targetWadah = wadahPiringLauk;
            }
            else if (tipe == FoodData.TipeGizi.Mineral && wadahPiringBuah != null)
            {
                targetWadah = wadahPiringBuah;
            }
        }

        // 2. Pindahkan objek makanan kloningan ke wadah kuadran piring yang tepat
        // Menggunakan 'false' agar ukuran UI Toolkit / UGUI Layout Group mengontrol skalanya dengan benar
        itemKloning.transform.SetParent(targetWadah, false);
        
        // 🛠️ RE-SCALE & RESET ROTASI (Tetap Dipertahankan dari Script Asli Lu!)
        itemKloning.transform.localRotation = Quaternion.identity;
        itemKloning.transform.localScale = Vector3.one; 

        // 🛠️ PERBAIKAN UTAMA (Tetap Dipertahankan dari Script Asli Lu!): Paksa reset CanvasGroup agar opacity kembali terang (1f) dan BISA DIKLIK!
        CanvasGroup cg = itemKloning.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
        }

        // Matikan script drag-nya agar makanan tidak melayang-layang lagi saat sudah masuk piring
        DraggableItemPiring dragScript = itemKloning.GetComponent<DraggableItemPiring>();
        if (dragScript != null) dragScript.enabled = false;

        // Atur ulang listener tombolnya agar bisa dihapus dari piring saat diklik
        Button btn = itemKloning.GetComponent<Button>();
        if (btn == null) btn = itemKloning.AddComponent<Button>();
        
        btn.onClick.RemoveAllListeners(); // Bersihkan fungsi klik lama dari etalase
        btn.onClick.AddListener(() => HapusBahanDariPiring(data, itemKloning));

        // 3. Jalankan kalkulasi penambahan gizi ke variabel internal (currentPokok, dll)
        if (data.jenisGizi != null && data.jenisGizi.Count > 0)
        {
            HitungGizi(data.jenisGizi[0], 1);
        }

        // 4. Update visual grafik Pie Chart secara real-time
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

        var bridge = pieChartUtama.GetComponent<EasyChart.UGUI.UGUIChartBridge>();
        if (bridge != null)
        {
            var targetChart = bridge.ChartElement;
            if (targetChart != null && targetChart.Data != null)
            {
                try
                {
                    // 1. Ambil field 'Series' dari ChartData via Reflection biar AMAN dari error private/public access
                    var seriesField = targetChart.Data.GetType().GetField("Series", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (seriesField != null)
                    {
                        var seriesList = seriesField.GetValue(targetChart.Data) as System.Collections.IList;
                        
                        if (seriesList != null && seriesList.Count > 0)
                        {
                            // Kita ambil Serie pertama (karena Pie Chart biasanya cuma pakai 1 deret Serie data)
                            var firstSerie = seriesList[0];
                            
                            // 📝 SEKALIAN LOG: Kita cetak isi Serie ke file teks buat jaga-jaga kalau strukturnya unik
                            System.Text.StringBuilder sb = new System.Text.StringBuilder();
                            sb.AppendLine("============= STRUKTUR INTERNAL SERIE =============");
                            sb.AppendLine($"Tipe Serie Asli: {firstSerie.GetType().FullName}");
                            
                            var fields = firstSerie.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            foreach (var f in fields) sb.AppendLine($"-> Field: {f.FieldType.FullName} {f.Name}");
                            
                            var props = firstSerie.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            foreach (var p in props) sb.AppendLine($"-> Properti: {p.PropertyType.FullName} {p.Name}");
                            
                            System.IO.File.WriteAllText(Application.dataPath + "/Struktur_Serie_EasyChart.txt", sb.ToString());

                            // 🛠️ AKSI PENYUNTIKAN OTOMATIS (Smart Proxy Search)
                            foreach (var f in fields)
                            {
                                // Cari field di dalam Serie yang bertipe koleksi/list data gizi (Array atau Generic List)
                                if (f.FieldType.IsGenericType || f.FieldType.IsArray)
                                {
                                    object listObj = f.GetValue(firstSerie);
                                    if (listObj != null)
                                    {
                                        var enumerable = listObj as System.Collections.IEnumerable;
                                        if (enumerable != null)
                                        {
                                            int index = 0;
                                            foreach (var item in enumerable)
                                            {
                                                if (item == null) continue;
                                                
                                                // Tentukan target nilai gizi baru berdasarkan urutan indeks potongan data pie chart
                                                float nilaiGiziBaru = 0;
                                                if (index == 0) nilaiGiziBaru = currentPokok;
                                                else if (index == 1) nilaiGiziBaru = currentSayur;
                                                else if (index == 2) nilaiGiziBaru = currentLauk;
                                                else if (index == 3) nilaiGiziBaru = currentBuah;

                                                // Cari & tembak field angka di dalam item gizi (contoh: value, x, y)
                                                var itemFields = item.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                                foreach (var itemF in itemFields)
                                                {
                                                    if (itemF.Name.ToLower() == "value" || itemF.Name.ToLower() == "y")
                                                    {
                                                        // Konversi otomatis ke tipe aslinya (float/double/int) biar gak crash
                                                        object valConverted = System.Convert.ChangeType(nilaiGiziBaru, itemF.FieldType);
                                                        itemF.SetValue(item, valConverted);
                                                    }
                                                }

                                                // Cari & tembak properti angka di dalam item gizi
                                                var itemProps = item.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                                foreach (var itemP in itemProps)
                                                {
                                                    if (itemP.CanWrite && (itemP.Name.ToLower() == "value" || itemP.Name.ToLower() == "y"))
                                                    {
                                                        object valConverted = System.Convert.ChangeType(nilaiGiziBaru, itemP.PropertyType);
                                                        itemP.SetValue(item, valConverted, null);
                                                    }
                                                }
                                                index++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Perbarui data internal chart
                    targetChart.SetData(targetChart.Data);
                    targetChart.RefreshData();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("[Min Error] Gagal eksekusi refleksi: " + ex.Message);
                }

                // Paksa komponen jembatan UI Toolkit merender ulang visualnya di Canvas
                bridge.Refresh();
            }
        }
    }

    public void KlikRefreshStudiKasus()
    {
        // 1. Bersihkan makanan lama HANYA dari dalam 4 wadah gizi (Jangan hancurkan wadahnya!)
        if (wadahPiringPokok != null) foreach (Transform child in wadahPiringPokok) Destroy(child.gameObject);
        if (wadahPiringSayur != null) foreach (Transform child in wadahPiringSayur) Destroy(child.gameObject);
        if (wadahPiringLauk != null) foreach (Transform child in wadahPiringLauk) Destroy(child.gameObject);
        if (wadahPiringBuah != null) foreach (Transform child in wadahPiringBuah) Destroy(child.gameObject);

        // 📝 CATATAN MIN: Baris pembunuh yang kemarin menghapus 4 wadah lu sudah gw lenyapkan dari sini!

        if (daftarCeritaStudiKasus.Count == 0) return;

        currentPokok = 0; currentSayur = 0; currentLauk = 0; currentBuah = 0;

        int indeksAcak = Random.Range(0, daftarCeritaStudiKasus.Count);
        textStudiKasusUI.text = daftarCeritaStudiKasus[indeksAcak];

        // Tentukan jumlah makanan acak awal yang nangkring di piring
        int randomPokok = Random.Range(0, 3); 
        int randomSayur = Random.Range(0, 2); 
        int randomLauk = Random.Range(0, 2); 
        int randomBuah = Random.Range(0, 2); 

        // 2. Spawn item bawaan langsung ke wadah porsinya masing-masing
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
                // Tentukan wadah spesifik berdasarkan tipe gizi sejak awal lahir
                Transform targetWadah = piringGridParent; // Fallback
                if (tipe == FoodData.TipeGizi.Karbohidrat && wadahPiringPokok != null) targetWadah = wadahPiringPokok;
                else if (tipe == FoodData.TipeGizi.Serat && wadahPiringSayur != null) targetWadah = wadahPiringSayur;
                else if (tipe == FoodData.TipeGizi.Protein && wadahPiringLauk != null) targetWadah = wadahPiringLauk;
                else if (tipe == FoodData.TipeGizi.Mineral && wadahPiringBuah != null) targetWadah = wadahPiringBuah;

                // Spawn langsung menjadi anak dari wadah spesifik tersebut
                GameObject itemBawaan = Instantiate(prefabTombolMakanan, targetWadah);
                itemBawaan.GetComponent<FoodDisplay>().data = dataAcak;
                itemBawaan.GetComponent<FoodDisplay>().InisialisasiGambar();
                
                // Matikan fungsi drag-nya agar makanan bawaan tidak bisa di-drag keluar piring
                DraggableItemPiring dragScript = itemBawaan.GetComponent<DraggableItemPiring>();
                if (dragScript != null) dragScript.enabled = false;
                
                // Suntikkan fungsi klik agar makanan bawaan bisa dihapus!
                Button btn = itemBawaan.GetComponent<Button>();
                if (btn == null) btn = itemBawaan.AddComponent<Button>();
                
                btn.onClick.RemoveAllListeners();
                FoodData dataLokal = dataAcak; // Mengunci referensi data gizi di dalam loop
                btn.onClick.AddListener(() => HapusBahanDariPiring(dataLokal, itemBawaan));
                
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
            textFeedbackMessege.text = "<color=green>HEBAT!\nKomposisi Piringmu Sempurna Gizi Seimbang!</color>";
        }
        else
        {
            textFeedbackMessege.text = "<color=red>WADUH, BELUM SEIMBANG!\nYuk, perhatikan lagi grafik lingkaran gizi dan sesuaikan jumlahnya ya!</color>";
        }
    }

    public void TutupPopupFeedback()
    {
        panelPopupFeedback.SetActive(false);
    }
}
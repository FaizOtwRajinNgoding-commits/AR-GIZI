using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class BookDownloadManager : MonoBehaviour
{
    public static BookDownloadManager Instance;

    [Header("Konfigurasi File PDF")]
    [Tooltip("Masukkan Direct Download URL HTTPS (misal dari Firebase Storage / GitHub)")]
    [SerializeField] private string pdfDownloadUrl = "https://github.com/Goneawynn/ZIBO_giZI-BOok-pdf/releases/download/v1.0-ZIBO/ZIBO.giZI.BOok.Kenali.Gizi.itu.Seru.pdf";
    [SerializeField] private string namaFileLokal = "Buku_Interaktif_ZIBO.pdf";

    [Header("UI Panel Instalasi Buku")]
    [SerializeField] private GameObject panelDownloadBuku;
    [SerializeField] private Slider sliderProgress;
    [SerializeField] private TextMeshProUGUI textProgressMB;
    [SerializeField] private TextMeshProUGUI textPersentase;
    [SerializeField] private TextMeshProUGUI textStatusDownload;

    [Header("Tombol Aksi")]
    [SerializeField] private Button btnBukaPanelMenuUtama; // Tombol ikon unduh buku di Menu Utama
    [SerializeField] private Button btnTutupPanel;         // Tombol X / Tutup di dalam popup
    [SerializeField] private Button btnMulaiUnduh;
    [SerializeField] private Button btnBukaBuku;

    [Header("Indikator Latar Belakang (Opsional di Menu Utama)")]
    [SerializeField] private GameObject badgeDownloadingMini;

    private string filePathTujuan;
    private string tempFilePath;
    private bool isDownloading = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            filePathTujuan = Path.Combine(Application.persistentDataPath, namaFileLokal);
            tempFilePath = filePathTujuan + ".tmp";

            HubungkanEventTombol();
        }
        else if (Instance != this)
        {
            // Oper seluruh referensi UI & tombol dari scene baru ke Instance yang bertahan hidup
            Instance.PerbaruiReferensiSceneBaru(
                panelDownloadBuku,
                sliderProgress,
                textProgressMB,
                textPersentase,
                textStatusDownload,
                btnBukaPanelMenuUtama,
                btnTutupPanel,
                btnMulaiUnduh,
                btnBukaBuku,
                badgeDownloadingMini
            );

            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (Instance != this) return;

        if (panelDownloadBuku != null) panelDownloadBuku.SetActive(false);
        if (badgeDownloadingMini != null) badgeDownloadingMini.SetActive(isDownloading);

        PerbaruiStatusUI();
    }

    // --- MENGOPER REFERENSI UI SAAT KEMBALI KE MENU UTAMA ---
    public void PerbaruiReferensiSceneBaru(
        GameObject newPanel,
        Slider newSlider,
        TextMeshProUGUI newTextMB,
        TextMeshProUGUI newTextPersen,
        TextMeshProUGUI newTextStatus,
        Button newBtnBukaPanel,
        Button newBtnTutupPanel,
        Button newBtnUnduh,
        Button newBtnBukaBuku,
        GameObject newBadgeMini)
    {
        panelDownloadBuku = newPanel;
        sliderProgress = newSlider;
        textProgressMB = newTextMB;
        textPersentase = newTextPersen;
        textStatusDownload = newTextStatus;
        btnBukaPanelMenuUtama = newBtnBukaPanel;
        btnTutupPanel = newBtnTutupPanel;
        btnMulaiUnduh = newBtnUnduh;
        btnBukaBuku = newBtnBukaBuku;
        badgeDownloadingMini = newBadgeMini;

        if (panelDownloadBuku != null) panelDownloadBuku.SetActive(false);
        if (badgeDownloadingMini != null) badgeDownloadingMini.SetActive(isDownloading);

        HubungkanEventTombol();
        PerbaruiStatusUI();
    }

    // --- MENGHUBUNGKAN TOMBOL SECARA OTOMATIS AGAR TIDAK PERNAH MISSING ---
    private void HubungkanEventTombol()
    {
        if (btnBukaPanelMenuUtama != null)
        {
            btnBukaPanelMenuUtama.onClick.RemoveListener(BukaPanelDownload);
            btnBukaPanelMenuUtama.onClick.AddListener(BukaPanelDownload);
        }

        if (btnTutupPanel != null)
        {
            btnTutupPanel.onClick.RemoveListener(TutupPanelKeBackground);
            btnTutupPanel.onClick.AddListener(TutupPanelKeBackground);
        }

        if (btnMulaiUnduh != null)
        {
            btnMulaiUnduh.onClick.RemoveListener(KlikMulaiDownload);
            btnMulaiUnduh.onClick.AddListener(KlikMulaiDownload);
        }

        if (btnBukaBuku != null)
        {
            btnBukaBuku.onClick.RemoveListener(KlikBukaAtauBagikanBuku);
            btnBukaBuku.onClick.AddListener(KlikBukaAtauBagikanBuku);
        }
    }

    private void SetSliderValueBersih(float nilai)
    {
        if (sliderProgress == null) return;

        sliderProgress.value = nilai;
        if (sliderProgress.fillRect != null)
        {
            sliderProgress.fillRect.gameObject.SetActive(nilai > 0.001f);
        }
    }

    public void BukaPanelDownload()
    {
        if (panelDownloadBuku != null) panelDownloadBuku.SetActive(true);
        PerbaruiStatusUI();
    }

    public void TutupPanelKeBackground()
    {
        if (panelDownloadBuku != null) panelDownloadBuku.SetActive(false);

        if (badgeDownloadingMini != null)
        {
            badgeDownloadingMini.SetActive(isDownloading);
        }
    }

    private void PerbaruiStatusUI()
    {
        if (isDownloading)
        {
            if (btnMulaiUnduh != null) btnMulaiUnduh.interactable = false;
            if (btnBukaBuku != null) btnBukaBuku.gameObject.SetActive(false);
            if (textStatusDownload != null) textStatusDownload.text = "Lagi mengunduh buku... Tutup jendela ini dan mainkan game lain dulu yuk!";
            return;
        }

        if (File.Exists(filePathTujuan))
        {
            SetSliderValueBersih(1f);
            if (textPersentase != null) textPersentase.text = "100%";
            if (textStatusDownload != null) textStatusDownload.text = "<color=#2ecc71>Hore! Buku ZIBO sudah siap. Buka sekarang dan arahkan kamera AR-mu!</color>";

            FileInfo info = new FileInfo(filePathTujuan);
            float ukuranMB = info.Length / (1024f * 1024f);
            if (textProgressMB != null) textProgressMB.text = $"{ukuranMB:F2} MB / {ukuranMB:F2} MB";

            if (btnMulaiUnduh != null)
            {
                btnMulaiUnduh.interactable = true;
                TextMeshProUGUI labelBtn = btnMulaiUnduh.GetComponentInChildren<TextMeshProUGUI>();
                if (labelBtn != null) labelBtn.text = "Unduh Ulang";
            }

            if (btnBukaBuku != null) btnBukaBuku.gameObject.SetActive(true);
        }
        else
        {
            SetSliderValueBersih(0f);
            if (textPersentase != null) textPersentase.text = "0%";
            if (textProgressMB != null) textProgressMB.text = "0.00 MB / -- MB";
            if (textStatusDownload != null) textStatusDownload.text = "Yuk unduh bukunya dulu supaya petualangan AR bisa dimulai!";

            if (btnMulaiUnduh != null) btnMulaiUnduh.interactable = true;
            if (btnBukaBuku != null) btnBukaBuku.gameObject.SetActive(false);
        }
    }

    public void KlikMulaiDownload()
    {
        if (isDownloading) return;
        StartCoroutine(ProsesDownloadBackground());
    }

    private IEnumerator ProsesDownloadBackground()
    {
        isDownloading = true;
        SetSliderValueBersih(0f);
        if (btnMulaiUnduh != null) btnMulaiUnduh.interactable = false;
        if (btnBukaBuku != null) btnBukaBuku.gameObject.SetActive(false);
        if (textStatusDownload != null) textStatusDownload.text = "Menghubungkan ke server...";

        if (File.Exists(tempFilePath)) File.Delete(tempFilePath);

        using (UnityWebRequest uwr = new UnityWebRequest(pdfDownloadUrl, UnityWebRequest.kHttpVerbGET))
        {
            uwr.downloadHandler = new DownloadHandlerFile(tempFilePath);
            uwr.SendWebRequest();

            while (!uwr.isDone)
            {
                string contentLengthStr = uwr.GetResponseHeader("Content-Length");
                long totalBytes = 0;
                long.TryParse(contentLengthStr, out totalBytes);

                ulong downloadedBytes = uwr.downloadedBytes;
                float downloadedMB = downloadedBytes / (1024f * 1024f);

                if (totalBytes > 0)
                {
                    float totalMB = totalBytes / (1024f * 1024f);
                    float progress = Mathf.Clamp01((float)downloadedBytes / totalBytes);

                    SetSliderValueBersih(progress);
                    if (textPersentase != null) textPersentase.text = $"{Mathf.RoundToInt(progress * 100)}%";
                    if (textProgressMB != null) textProgressMB.text = $"{downloadedMB:F2} MB / {totalMB:F2} MB";
                    if (textStatusDownload != null) textStatusDownload.text = "Lagi mengunduh buku... Tutup jendela ini dan mainkan game lain dulu yuk!";
                }
                else
                {
                    SetSliderValueBersih(uwr.downloadProgress);
                    if (textProgressMB != null) textProgressMB.text = $"{downloadedMB:F2} MB terunduh...";
                }

                yield return null;
            }

            isDownloading = false;
            if (badgeDownloadingMini != null) badgeDownloadingMini.SetActive(false);

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                if (File.Exists(filePathTujuan)) File.Delete(filePathTujuan);
                File.Move(tempFilePath, filePathTujuan);

                PerbaruiStatusUI();
                Debug.Log($"[ZIBO Download] Sukses tersimpan di: {filePathTujuan}");
            }
            else
            {
                SetSliderValueBersih(0f);
                if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
                if (textStatusDownload != null) textStatusDownload.text = "<color=#e74c3c>Yah, koneksi terputus! Cek internetmu lalu coba lagi ya.</color>";
                if (btnMulaiUnduh != null) btnMulaiUnduh.interactable = true;
                Debug.LogError($"[ZIBO Download] Error: {uwr.error}");
            }
        }
    }

    public void KlikBukaAtauBagikanBuku()
    {
        if (!File.Exists(filePathTujuan))
        {
            if (textStatusDownload != null) textStatusDownload.text = "File buku belum ditemukan, silakan unduh ulang ya!";
            return;
        }

        #if UNITY_EDITOR || UNITY_STANDALONE
            Application.OpenURL("file://" + filePathTujuan);
        #elif UNITY_ANDROID || UNITY_IOS
            Share.Items(new List<string> { filePathTujuan }, success =>
            {
                if (success)
                {
                    Debug.Log("[ZIBO Share] Jendela buka/bagikan PDF berhasil dibuka.");
                }
                else
                {
                    Debug.LogWarning("[ZIBO Share] Gagal membuka jendela bagikan.");
                    if (textStatusDownload != null) 
                        textStatusDownload.text = "Gagal membuka menu bagikan file.";
                }
            });
        #endif
    }
}
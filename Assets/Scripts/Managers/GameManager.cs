using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement; // Tambahkan namespace ini untuk LoadScene/Reload

[System.Serializable]
public struct BasketMapping
{
    public FoodData.TipeGizi tipeGizi;
    public Sprite gambarKeranjang; 
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI levelText;
    public GameObject heartsContainer;
    
    [Header("End Game Panel UI Elements")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverTitleText;  // Teks Judul ("GAME OVER" / "KAMU MENANG!")
    public TextMeshProUGUI gameOverDescText;   // Teks Deskripsi Singkat/Edukasi
    public TextMeshProUGUI gameOverScoreText;  // Teks Total Skor Akhir

    [Header("Prefabs & Data")]
    public GameObject foodPrefab;
    public GameObject basketPrefab;
    public List<FoodData> allFoodData;
    public Transform foodSpawnParent;
    public Transform basketSpawnParent;

    [Header("Basket Sprite Mapping")]
    public List<BasketMapping> semuaGambarKeranjang;

    [Header("Game State")]
    public int currentLevel = 1;
    public int score = 0;
    public int lives = 5;
    public float timer = 120f; 
    private bool isGameOver = false;

    private List<FoodData.TipeGizi> activeGiziTypes = new List<FoodData.TipeGizi>();
    private int remainingFoodsInLevel; 

    void Awake() { Instance = this; }

    void Start()
    {
        Time.timeScale = 1f; // Pastikan waktu berjalan normal saat start
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        UpdateUI();
        StartLevel();
    }

    void Update()
    {
        if (currentLevel >= 7 && !isGameOver)
        {
            timer -= Time.deltaTime;
            timerText.text = Mathf.Ceil(timer).ToString() + "s";
            if (timer <= 0)
            {
                TampilkanEndGame(false, "Waktu kamu telah habis! Yuk tingkatkan kecepatan dan ketelitianmu lagi!");
            }
        }
    }

    public void StartLevel()
    {
        ToggleLayoutGroups(true);

        foreach (Transform child in foodSpawnParent) Destroy(child.gameObject);
        foreach (Transform child in basketSpawnParent) Destroy(child.gameObject);

        int basketCount = 2;
        int foodCount = 3;

        if (currentLevel >= 4 && currentLevel <= 6) {
            basketCount = 3; 
            foodCount = 5;
        } else if (currentLevel >= 7) {
            basketCount = 4; 
            foodCount = 6;
            
            if (currentLevel == 7 && timer > 100f) {
                timer = 100f;
            }
        }

        if (currentLevel == 5 && lives < 5) {
            lives++;
            UpdateUI();
        }

        remainingFoodsInLevel = foodCount;

        activeGiziTypes.Clear();
        var allGizi = System.Enum.GetValues(typeof(FoodData.TipeGizi)).Cast<FoodData.TipeGizi>().ToList();
        for (int i = 0; i < basketCount; i++) {
            int randIndex = Random.Range(0, allGizi.Count);
            activeGiziTypes.Add(allGizi[randIndex]);
            allGizi.RemoveAt(randIndex);
        }

        foreach (var gizi in activeGiziTypes) {
            GameObject b = Instantiate(basketPrefab, basketSpawnParent);
            
            DropSlot slot = b.GetComponent<DropSlot>();
            if (slot != null) {
                slot.giziKeranjang = gizi;
            }

            Image basketImage = b.GetComponent<Image>();
            if (basketImage != null) {
                BasketMapping cocok = semuaGambarKeranjang.Find(x => x.tipeGizi == gizi);
                if (cocok.gambarKeranjang != null) {
                    basketImage.sprite = cocok.gambarKeranjang; 
                } else {
                    Debug.LogError("Gambar keranjang buat gizi " + gizi + " belum dimasukkan di Inspector!");
                }
            }
        }

        List<FoodData> validFoods = allFoodData.Where(f => f.jenisGizi.Any(g => activeGiziTypes.Contains(g))).ToList();

        for (int i = 0; i < foodCount; i++) {
            FoodData randomFood = validFoods[Random.Range(0, validFoods.Count)];
            GameObject f = Instantiate(foodPrefab, foodSpawnParent);
            f.GetComponent<FoodDisplay>().data = randomFood;
        }

        levelText.text = "LEVEL " + currentLevel;

        StartCoroutine(DisableLayoutsDelayed());
    }

    public void AddScore(int points)
    {
        score += points;
        UpdateUI();
        
        remainingFoodsInLevel--; 
        CheckWinCondition();
    }

    public void TakeDamage()
    {
        lives--;
        UpdateUI();
        
        remainingFoodsInLevel--; 

        if (lives <= 0) {
            TampilkanEndGame(false, "Nyawa kamu telah habis! Jangan menyerah, yuk pelajari lagi jenis zat gizinya!");
        } else {
            CheckWinCondition(); 
        }
    }

    void CheckWinCondition()
    {
        if (remainingFoodsInLevel <= 0 && lives > 0 && !isGameOver)
        {
            if (currentLevel < 10) {
                currentLevel++;
                Invoke("StartLevel", 1f); 
            } else {
                TampilkanEndGame(true, "LUAR BIASA! Kamu berhasil menyelesaikan semua level dan menguasai Klasifikasi Zat Gizi!");
            }
        }
    }

    void UpdateUI()
    {
        scoreText.text = " " + score;
        for (int i = 0; i < heartsContainer.transform.childCount; i++) {
            heartsContainer.transform.GetChild(i).gameObject.SetActive(i < lives);
        }
    }

    // ========================================================
    // PENANGANAN PANEL END GAME (GAME OVER / COMPLETED)
    // ========================================================
    void TampilkanEndGame(bool isWin, string deskripsi)
    {
        isGameOver = true;
        Time.timeScale = 0f; // Hentikan timer dan pergerakan di latar belakang

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (gameOverTitleText != null)
                gameOverTitleText.text = isWin ? "<color=#2ECC71>KAMU MENANG!</color>" : "<color=#E74C3C>GAME OVER</color>";

            if (gameOverDescText != null)
                gameOverDescText.text = deskripsi;

            if (gameOverScoreText != null)
                gameOverScoreText.text = "Total Skor: " + score;
        }
    }

    // ========================================================
    // SISTEM LOOP: TOMBOL RELOAD & KELUAR
    // ========================================================
    
    // Hubungkan ke Tombol Ikon Reload (↻) di Panel GameOver
    public void KlikMainLagi()
    {
        Time.timeScale = 1f; // Kembalikan skala waktu
        isGameOver = false;

        // Reset variabel game state
        currentLevel = 1;
        score = 0;
        lives = 5;
        timer = 120f;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        UpdateUI();
        StartLevel(); // Reset arena dan mulai dari level 1
    }

    // Hubungkan ke Tombol Ikon Silang (✕) di Panel GameOver
    public void KlikKembaliKeMenuUtama()
    {
        Time.timeScale = 1f; // Pastikan timeScale selalu di-reset sebelum pindah scene!

        // Opsi 1: Jika menggunakan SceneManager (Nama scene sesuaikan dengan scene menu utama lu)
        SceneManager.LoadScene("MainMenu"); 

        // Opsi 2: Jika menggunakan SceneCanvasSwitcher bawaan project lu, tinggal panggil method switcher-nya di Unity Inspector
    }

    System.Collections.IEnumerator DisableLayoutsDelayed()
    {
        yield return new WaitForEndOfFrame();
        ToggleLayoutGroups(false);
    }

    void ToggleLayoutGroups(bool state)
    {
        HorizontalLayoutGroup foodLayout = foodSpawnParent.GetComponent<HorizontalLayoutGroup>();
        HorizontalLayoutGroup basketLayout = basketSpawnParent.GetComponent<HorizontalLayoutGroup>();

        if (foodLayout != null) foodLayout.enabled = state;
        if (basketLayout != null) basketLayout.enabled = state;
    }
}
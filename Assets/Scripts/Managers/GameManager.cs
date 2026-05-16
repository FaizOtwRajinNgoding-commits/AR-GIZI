using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

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
    public GameObject gameOverPanel;

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
        UpdateUI();
        StartLevel();
    }

    void Update()
    {
        if (currentLevel >= 7 && !isGameOver)
        {
            timer -= Time.deltaTime;
            timerText.text = "Waktu: " + Mathf.Ceil(timer).ToString() + "s";
            if (timer <= 0) GameOver();
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
            
            // FIX TIMER: Hanya set ke 120 detik saat baru PERTAMA KALI menginjak level 7
            // Di level 8, 9, dan 10 dia gak akan meriset timernya lagi
            if (currentLevel == 7 && timer > 120f) {
                timer = 120f;
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
                    Debug.LogError("Waduh pan, gambar keranjang buat gizi " + gizi + " belum lu masukin di Inspector!");
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
            GameOver();
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
                Debug.Log("GAME TAMAT! Poin Akhir: " + score);
            }
        }
    }

    void UpdateUI()
    {
        scoreText.text = "Skor: " + score;
        for (int i = 0; i < heartsContainer.transform.childCount; i++) {
            heartsContainer.transform.GetChild(i).gameObject.SetActive(i < lives);
        }
    }

    void GameOver()
    {
        isGameOver = true;
        gameOverPanel.SetActive(true);
        Time.timeScale = 0;
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
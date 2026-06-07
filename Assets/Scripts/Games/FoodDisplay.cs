using UnityEngine;
using UnityEngine.UI; 

public class FoodDisplay : MonoBehaviour
{
    public FoodData data; 

    void Start()
    {
        InisialisasiGambar();
    }

    // Buat fungsi baru agar bisa ditembak langsung dari GameManager
    public void InisialisasiGambar()
    {
        if (data != null)
        {
            GetComponent<Image>().sprite = data.gambarMakanan;
        }
    }
}
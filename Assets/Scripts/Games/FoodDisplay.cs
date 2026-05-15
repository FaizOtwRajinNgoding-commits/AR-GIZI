using UnityEngine;
using UnityEngine.UI; // Wajib ada karena kita pakai UI Image

public class FoodDisplay : MonoBehaviour
{
    public FoodData data; // Tempat naruh "KTP" makanan (Scriptable Object)

    void Start()
    {
        // Baris ini yang otomatis ganti gambar pas game mulai
        if (data != null)
        {
            GetComponent<Image>().sprite = data.gambarMakanan;
        }
    }
}

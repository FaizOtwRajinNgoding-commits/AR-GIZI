using UnityEngine;
using System.Collections.Generic;

//Untuk create file melalui click kanan pada Unity Hub 
[CreateAssetMenu(fileName = "NewFoodData", menuName = "Zibo/Food Data")]
public class FoodData : ScriptableObject
{
    public string namaMakanan;
    public Sprite gambarMakanan;

    // Ini buat nentuin zat gizinya
    public enum TipeGizi { Karbohidrat, Protein, Lemak, Serat, Mineral }
    public List<TipeGizi> jenisGizi;
}

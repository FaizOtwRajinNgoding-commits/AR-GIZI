using UnityEngine;
using UnityEngine.EventSystems; // Wajib untuk deteksi drop

public class DropSlot : MonoBehaviour, IDropHandler
{
    // Tentukan jenis gizi keranjang ini di Inspector (Karbohidrat/Protein/dll)
    public FoodData.TipeGizi giziKeranjang;

    public void OnDrop(PointerEventData eventData)
    {
        // 1. Ambil objek yang sedang di-drag
        GameObject droppedObject = eventData.pointerDrag;

        if (droppedObject != null)
        {
            // 2. Ambil komponen FoodDisplay untuk cek datanya
            FoodDisplay display = droppedObject.GetComponent<FoodDisplay>();

            if (display != null)
            {
                // 3. Cek apakah gizi keranjang ini ada di dalam list gizi makanan tersebut
                if (display.data.jenisGizi.Contains(giziKeranjang))
                {
                    // LOGIKA BENAR
                    Debug.Log("<color=green>BENAR!</color> Poin +10. Makanan: " + display.data.namaMakanan);
                }
                else
                {
                    // LOGIKA SALAH
                    Debug.Log("<color=red>SALAH!</color> Hati Berkurang 1. Makanan: " + display.data.namaMakanan);
                }

                // 4. SESUAI REVISI: Apa pun hasilnya, makanan dihancurkan
                Destroy(droppedObject);
            }
        }
    }
}
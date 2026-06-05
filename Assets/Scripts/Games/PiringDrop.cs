using UnityEngine;
using UnityEngine.EventSystems;

public class PiringDropZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // Ambil objek makanan yang sedang ditarik siswa
        GameObject droppedObject = eventData.pointerDrag;
        
        if (droppedObject != null)
        {
            FoodDisplay display = droppedObject.GetComponent<FoodDisplay>();
            
            if (display != null && display.data != null)
            {
                // Cek zat gizi pertama dari ScriptableObject bawaan lu
                FoodData.TipeGizi giziMakanan = display.data.jenisGizi[0];

                // Kirim data ke PiringGameManager untuk dihitung persennya
                // Min sesuaikan dengan ENUM bawaan di FoodData.cs lu ya!
                string kategoriKirim = "";

                switch (giziMakanan)
                {
                    case FoodData.TipeGizi.Karbohidrat:
                        kategoriKirim = "pokok";
                        break;
                    case FoodData.TipeGizi.Protein:
                        kategoriKirim = "lauk";
                        break;
                    case FoodData.TipeGizi.Serat:
                        kategoriKirim = "sayur";
                        break;
                    case FoodData.TipeGizi.Mineral:
                        kategoriKirim = "buah";
                        break;
                    default:
                        kategoriKirim = "unknown";
                        break;
                }

                // Panggil fungsi tambah bahan di manager Piring-ku
                if (kategoriKirim != "unknown")
                {
                    PiringGameManager.Instance.TambahBahanMakanan(kategoriKirim, display.data.namaMakanan);
                    
                    // Hancurkan objek kloningan makanan setelah berhasil masuk piring
                    Destroy(droppedObject); 
                }
            }
        }
    }
}
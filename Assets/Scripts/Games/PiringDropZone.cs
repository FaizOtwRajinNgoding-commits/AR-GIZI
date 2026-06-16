using UnityEngine;
using UnityEngine.EventSystems;

public class PiringDropZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        
        if (droppedObject != null)
        {
            FoodDisplay display = droppedObject.GetComponent<FoodDisplay>();
            
            // 🛠️ PERBAIKAN DI SINI: Deteksi DraggableItemPiring, bukan DraggableItem lama!
            DraggableItemPiring dragPiring = droppedObject.GetComponent<DraggableItemPiring>();
            
            // Pastikan yang masuk piring adalah item hasil kloningan etalase
            if (display != null && display.data != null && dragPiring != null && dragPiring.isClone)
            {
                // Daftarkan makanan ke PiringGameManager
                PiringGameManager.Instance.TambahBahanKePiring(display.data, droppedObject);
            }
        }
    }
}
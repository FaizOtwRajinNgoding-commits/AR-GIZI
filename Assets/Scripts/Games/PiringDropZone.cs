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
            
            if (display != null && display.data != null)
            {
                // Eksekusi pemindahan parent dan kalkulasi gizi ke manager
                FoodData.TipeGizi giziMakanan = display.data.jenisGizi[0];
                PiringGameManager.Instance.TambahBahanKePiring(giziMakanan, droppedObject);
            }
        }
    }
}
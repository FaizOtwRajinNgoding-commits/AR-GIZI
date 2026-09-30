using UnityEngine;
using UnityEngine.EventSystems;

public class PiringDropZone : MonoBehaviour, IDropHandler
{
    [Header("Kategori Wadah Ini")]
    public FoodData.TipeGizi kategoriZona;

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        
        if (droppedObject != null)
        {
            FoodDisplay display = droppedObject.GetComponent<FoodDisplay>();
            DraggableItemPiring dragPiring = droppedObject.GetComponent<DraggableItemPiring>();
            
            if (display != null && display.data != null && dragPiring != null && dragPiring.isClone)
            {
                // Teruskan ke GameManager untuk proses validasi dan penambahan porsi
                PiringGameManager.Instance.TambahBahanKePiring(display.data, droppedObject, transform);
            }
        }
    }
}
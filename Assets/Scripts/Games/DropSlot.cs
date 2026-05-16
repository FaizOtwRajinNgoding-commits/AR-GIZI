using UnityEngine;
using UnityEngine.EventSystems; // Wajib untuk deteksi drop

public class DropSlot : MonoBehaviour, IDropHandler
{
    // Tentukan jenis gizi keranjang ini di Inspector (Karbohidrat/Protein/dll)
    public FoodData.TipeGizi giziKeranjang;

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject != null)
        {
        FoodDisplay display = droppedObject.GetComponent<FoodDisplay>();
        if (display != null)
        {
            if (display.data.jenisGizi.Contains(giziKeranjang))
            {
                GameManager.Instance.AddScore(10);
            }
            else
            {
                GameManager.Instance.TakeDamage();
            }
            Destroy(droppedObject);
        }
    }
}
}
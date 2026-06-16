using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggableItemPiring : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 posisiAwal;
    private Canvas canvas;

    [HideInInspector] public bool isClone = false; // Penanda apakah ini item hasil kloning atau master etalase

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // JIKA INI ITEM MASTER DI ETALASE, MAKA KLONING DIRINYA
        if (!isClone)
        {
            // Buat duplikat di Canvas utama biar gak keteledor di dalam scroll/panel etalase
            GameObject cloneObj = Instantiate(gameObject, canvas.transform);
            
            // 🛠️ DI SINI PERBAIKANNYA: Panggil DraggableItemPiring, bukan DraggableItem!
            DraggableItemPiring cloneScript = cloneObj.GetComponent<DraggableItemPiring>();
            
            if (cloneScript != null)
            {
                cloneScript.isClone = true; // Set bahwa objek baru ini adalah kloningan yang boleh dimanipulasi
                
                // Oper kendali drag dari kursor/sentuhan ke objek kloningan secara instan!
                eventData.pointerDrag = cloneObj;
                cloneScript.OnBeginDrag(eventData);
            }
            return;
        }

        // Logika drag normal untuk objek kloningan
        posisiAwal = rectTransform.anchoredPosition;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 posisiDunia;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvas.transform as RectTransform, 
            eventData.position, 
            canvas.worldCamera, 
            out posisiDunia))
        {
            transform.position = posisiDunia;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isClone) return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Jika dilepas tidak di atas piring (DropZone), langsung hancurkan biar gak nyampah di layar
        if (transform.parent == canvas.transform)
        {
            Destroy(gameObject);
        }
    }
}
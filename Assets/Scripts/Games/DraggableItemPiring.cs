using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggableItemPiring : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 posisiAwal;
    private Canvas canvas;

    [HideInInspector] public bool isClone = false; 

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isClone)
        {
            GameObject cloneObj = Instantiate(gameObject, canvas.transform);
            
            // 🛠️ PERBAIKAN BUG: Ambil komponen DraggableItemPiring, bukan DraggableItem lama!
            DraggableItemPiring cloneScript = cloneObj.GetComponent<DraggableItemPiring>();
            
            if (cloneScript != null)
            {
                cloneScript.isClone = true;
            }
            
            // 🛠️ PERBAIKAN ROTASI: Paksa objek kloningan tegak lurus sejak mulai di-drag
            cloneObj.transform.localRotation = Quaternion.identity;
            
            eventData.pointerDrag = cloneObj;
            if (cloneScript != null) cloneScript.OnBeginDrag(eventData);
            return;
        }

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

        if (transform.parent == canvas.transform)
        {
            Destroy(gameObject);
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggableItemPiring : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    private Canvas canvas;

    [HideInInspector] public bool isClone = false; 

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void EnsureCanvas()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        EnsureCanvas();

        if (!isClone)
        {
            if (canvas == null) return;

            // 1. Kloning objek ke Canvas Utama (Bebas dari kuncian GridLayoutGroup rak)
            GameObject cloneObj = Instantiate(gameObject, canvas.transform);
            DraggableItemPiring cloneScript = cloneObj.GetComponent<DraggableItemPiring>();
            
            if (cloneScript != null)
            {
                cloneScript.isClone = true;
                cloneScript.canvas = canvas;
                
                // Reset rotasi & skala agar pas dengan Canvas
                cloneObj.transform.localRotation = Quaternion.identity;
                cloneObj.transform.localScale = Vector3.one;

                // Update posisi kloningan sesuai kursor layar -> koordinat dunia Canvas
                cloneScript.UpdatePosition(eventData);

                // Alihkan pointer drag dari item rak ke item kloningan baru
                eventData.pointerDrag = cloneObj;
                ExecuteEvents.Execute(cloneObj, eventData, ExecuteEvents.beginDragHandler);
            }
            return;
        }

        // Jika ini ADALAH item kloningan:
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false; // Agar tidak menghalangi deteksi Raycast pada DropZone piring
        transform.SetAsLastSibling(); // Tampil paling depan di Canvas
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isClone)
        {
            UpdatePosition(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isClone) return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Jika dilepas di luar drop zone (masih menjadi child langsung dari Canvas utama), hancurkan kloningan
        if (canvas != null && transform.parent == canvas.transform)
        {
            Destroy(gameObject);
        }
    }

    // FUNGSI KUNCI: Konversi posisi kursor layar ke koordinat dunia Canvas secara akurat
    private void UpdatePosition(PointerEventData eventData)
    {
        EnsureCanvas();
        if (canvas == null) return;

        Vector3 posisiDunia;
        // Jika Canvas berjenis ScreenSpaceOverlay gunakan null, jika ScreenSpaceCamera gunakan canvas.worldCamera
        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvas.transform as RectTransform, 
            eventData.position, 
            cam, 
            out posisiDunia))
        {
            transform.position = posisiDunia;
        }
    }
}
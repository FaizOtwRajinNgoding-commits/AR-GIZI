using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Wajib ada buat deteksi sentuhan/klik

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 posisiAwal;
    private Canvas canvas;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        // Cari canvas di parent
        canvas = GetComponentInParent<Canvas>();
    }

    // 1. Pas mulai ditarik
    public void OnBeginDrag(PointerEventData eventData)
    {
        posisiAwal = rectTransform.anchoredPosition; // Catat posisi lokal biar bisa balik kalau salah
        canvasGroup.alpha = 0.6f; // Bikin agak transparan biar estetik
        canvasGroup.blocksRaycasts = false; // Biar "tembus" pas dicek keranjang nanti
        
        // supaya tampil di paling depan (di atas semua UI lain)
        transform.SetAsLastSibling();
    }

    // 2. Pas lagi digeser (SUDAH DIREVISI BIAR PAS DI TENGAH)
    public void OnDrag(PointerEventData eventData)
    {
        // Pake ScreenPointToWorldPointInRectangle biar posisi kursor langsung nempel 
        // tepat di titik tengah (Pivot) objek makanan lu, gak peduli setelan Anchor-nya lagi ngaco
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

    // 3. Pas dilepas
    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f; // Balikin warna normal
        canvasGroup.blocksRaycasts = true; // Bisa diklik lagi

        // Kalau nggak masuk keranjang, balik ke posisi awal di barisan
        rectTransform.anchoredPosition = posisiAwal;
    }
}
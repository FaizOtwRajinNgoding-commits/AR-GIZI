using UnityEngine;

public class ObjectSwitch : MonoBehaviour
{
    [Header("Hubungkan GameObject dari Hierarchy")]
    public GameObject makananObject;
    public GameObject characterObject;
    public GameObject panelInfo;
    public GameObject UI;

    private FoodPopUp makananAnim;
    private FoodPopUp panel;
    private FoodPopUp characterAnim;
    private Carousel1 carousel;
    private ARMenu manageAR;
    private bool isPanelActive = false;
    private bool isCharacterActive = false;

    void Start()
    {
        // Otomatis mengambil komponen FoodPopUp dari GameObject yang lu masukkan
        if (makananObject != null) makananAnim = makananObject.GetComponent<FoodPopUp>();
        if (characterObject != null) characterAnim = characterObject.GetComponent<FoodPopUp>();
        if (UI != null) manageAR = UI.GetComponent<ARMenu>(); 
        if (panelInfo != null) 
        {
            panel = panelInfo.GetComponent<FoodPopUp>();
            carousel = panelInfo.GetComponentInChildren<Carousel1>();
        }
        
        ResetStatus();
    }

    public void ResetStatus()
    {
        isPanelActive = false;
        isCharacterActive = false;
        if (manageAR != null) manageAR.HideButton();
        if (panel != null) panel.Sembunyikan();
        if (carousel != null) carousel.HideButton();
        if (makananAnim != null) makananAnim.Sembunyikan(); 
        if (characterAnim != null) characterAnim.MainkanAnimasi();
    }

    public void InfoPanel()
    {
        bool isMarkerActive = (characterObject != null && characterObject.activeInHierarchy) || (makananObject != null && makananObject.activeInHierarchy);
        if (!isMarkerActive) return;

        isPanelActive = !isPanelActive;
        if (isPanelActive)
            {
                if (panel != null) panel.MainkanAnimasi();
                if (carousel != null) carousel.ResetCarousel();
            } else
            {
                if (panel != null) panel.Sembunyikan();
                if (carousel != null) carousel.HideButton();
            }
    }

    public void SwitchObject()
    {
        isCharacterActive = !isCharacterActive;

        if (!isCharacterActive)
        {
            if (makananAnim != null) makananAnim.Sembunyikan();
            if (characterAnim != null) characterAnim.MainkanAnimasi(); 
        }
        else
        {
            if (characterAnim != null) characterAnim.Sembunyikan();
            if (makananAnim != null) makananAnim.MainkanAnimasi(); 
        }
    }
}
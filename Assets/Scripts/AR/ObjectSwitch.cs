using UnityEngine;

public class ObjectSwitch : MonoBehaviour
{
    public static ObjectSwitch activeSwitch;
    [Header("Hubungkan GameObject dari Hierarchy")]
    public GameObject makananObject;
    public GameObject characterObject;
    public GameObject panelInfo;
    public GameObject UI;

    private FoodPopUp makananAnim;
    private FoodPopUp panel;
    private FoodPopUp characterAnim;
    private CarouselSet Carousel;
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
            panel = panelInfo.GetComponentInChildren<FoodPopUp>(true);
            Carousel = panelInfo.GetComponentInChildren<CarouselSet>(true);
        }
        
        ResetStatus();
    }

    public void OnTargetFound()
    {
        activeSwitch = this;
        ResetStatus();
        if (manageAR != null) manageAR.ShowButton();
    }

    public static void GlobalInfoPanel()
    {
        if (activeSwitch != null) activeSwitch.InfoPanel();
    }

    public void ResetStatus()
    {
        isPanelActive = false;
        isCharacterActive = false;

        if (manageAR != null) manageAR.HideButton();
        if (panel != null) panel.Sembunyikan();
        if (Carousel != null) Carousel.HideCarousel();
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
                if (Carousel != null) Carousel.ShowCarousel();
            } else
            {
                if (panel != null) panel.Sembunyikan();
                if (Carousel != null) Carousel.HideCarousel();
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
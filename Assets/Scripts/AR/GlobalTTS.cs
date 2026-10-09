using UnityEngine;

public class GlobalTTS : MonoBehaviour
{
    public static GlobalTTS instance;

    public void Awake()
    {
        instance = this;
    }
    public void ToggleTTSButton()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
    public void HideButton()
    {
        gameObject.SetActive(false);
    }
    public void OnClickTTS()
    {
        //Carousel1.TriggerTTS();
    }
}
using UnityEngine;

public class ButtonSFX : MonoBehaviour
{
    [Header("UIToggle")]
    public GameObject buttonOn;
    public GameObject buttonOff;

    void Start()
    {
        Invoke("SyncUI", 0.05f);
        if (buttonOff != null && buttonOn != null)
        {
            if (SoundManager.instance != null && SoundManager.instance.audioSource != null)
            {
                UpdateVisual(SoundManager.instance.audioSource.mute);
            }
        }
    }

    void SyncUI()
    {
        if (buttonOff != null && buttonOn != null)
        {
            if (SoundManager.instance != null && SoundManager.instance.audioSource != null)
            {
                UpdateVisual(SoundManager.instance.audioSource.mute);
            }
        }
    }

    public void ToggleBGM()
    {
        if(SoundManager.instance != null)
        {
            bool newMutedStatus = !SoundManager.instance.audioSource.mute;

            SoundManager.instance.ControllBGM(!newMutedStatus);

            if (buttonOff != null && buttonOn != null)
            {
                UpdateVisual(newMutedStatus);
            }
        }
        ClickSound();
    }

    private void UpdateVisual(bool muted)
    {
        if (muted)
        {
            buttonOn.SetActive(false);
            buttonOff.SetActive(true);
        }
        else
        {
            buttonOn.SetActive(true);
            buttonOff.SetActive(false);
        }
    }
    public void ClickSound()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.PlaySfx();
        }
        else
        {
            Debug.LogWarning("SoundManager belom ada uyy");
        }
    }
}
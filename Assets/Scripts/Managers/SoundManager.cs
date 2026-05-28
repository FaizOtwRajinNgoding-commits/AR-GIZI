using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    public AudioSource audioSource;
    public AudioSource SfxSource;
    [Header("SFXSoundClip")]
    public AudioClip button;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        } 
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void Sfx()
    {
        if (SfxSource != null && button != null)
        {
            SfxSource.PlayOneShot(button);
        }
    }

    public static void PlaySfx()
    {
        if (instance != null)
        {
            instance.Sfx();
        } else
        {
            Debug.LogWarning("Instance nya gagal cuy");
        }
    }

    public void ControllBGM(bool tunrOn)
    {
        if (audioSource != null)
        {
            audioSource.mute = !tunrOn;
        }
    }
}

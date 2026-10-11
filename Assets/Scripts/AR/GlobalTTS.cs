using UnityEngine;

public class GlobalTTS : MonoBehaviour
{
    public static GlobalTTS instance;

    public void Awake()
    {
        instance = this;
    }
}
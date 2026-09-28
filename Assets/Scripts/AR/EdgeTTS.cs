using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class EdgeTTS : MonoBehaviour
{
    public static EdgeTTS instance;
    
    public enum VoiceType
    {
        Gadis_Wanita,
        Ardi_Pria
    }

    [Header("EdgeTTS Configuration")]
    [SerializeField] private VoiceType pilihanSuara = VoiceType.Gadis_Wanita;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void GetTTSAudio(string textToSpeech, string fileName, Action<AudioClip> onCompleted)
    {
        StartCoroutine(ProcesTTS(textToSpeech, fileName, onCompleted));
    }

    private IEnumerator ProcesTTS(string text, string fileName, Action<AudioClip> onCompleted)
    {
        string filePath = Path.Combine(Application.persistentDataPath, fileName + ".mp3");

        // 1. Cek Caching Lokal
        if (File.Exists(filePath))
        {
            Debug.Log($"[EdgeTTS] Memuat dari cache lokal : {filePath}");
            yield return StartCoroutine(LoadAudioFromLocal("file://" + filePath, onCompleted));
            yield break;
        }

        Debug.Log($"[EdgeTTS] Downloading Audio TTS : {filePath}");

        // 2. Gunakan Endpoint Google Translate TTS (Bahasa Indonesia)
        string encodedText = UnityWebRequest.EscapeURL(text);
        string url = $"https://translate.google.com/translate_tts?ie=UTF-8&q={encodedText}&tl=id&client=tw-ob";

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            // Tambahkan User-Agent agar request dianggap dari browser biasa (mencegah error 401/403)
            www.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // Simpan audio ke memori HP
                byte[] audioBytes = www.downloadHandler.data;
                File.WriteAllBytes(filePath, audioBytes);
                Debug.Log($"[EdgeTTS] Berhasil menyimpan Audio : {filePath}");

                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                onCompleted?.Invoke(clip);
            }
            else
            {
                Debug.LogError($"[EdgeTTS] Gagal terhubung dengan server : {www.error}");
                onCompleted?.Invoke(null);
            }
        }
    }

    private IEnumerator LoadAudioFromLocal(string pathWithProtocol, Action<AudioClip> onCompleted)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(pathWithProtocol, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                onCompleted?.Invoke(clip);
            } 
            else
            {
                Debug.LogError($"[EdgeTTS] Gagal load kedalam file : {www.error}");
                onCompleted?.Invoke(null);
            }
        }
    }
}
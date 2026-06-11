using System.Collections;
using UnityEngine;

public class FoodPopUp : MonoBehaviour
{
    [Header("3D PopUp Animation")]
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float totalDuration = 0.3f;

    private Vector3 initialScale;
    private Coroutine animationCoroutine;
    private bool hasCacheScale = false;

    void Awake()
    {
        CacheInitialScale();
    }

    void CacheInitialScale()
    {
        if (!hasCacheScale)
        {
            initialScale = transform.localScale;
            hasCacheScale = true;
        }
    }

    // Fungsi untuk memunculkan objek dengan animasi
    public void MainkanAnimasi()
    {
        CacheInitialScale();

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(PlayPopUpAR());
    }

    // Fungsi untuk menyembunyikan objek secara instan (Scale jadi 0)
    public void Sembunyikan()
    {
        CacheInitialScale();
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        transform.localScale = Vector3.zero;
    }

    IEnumerator PlayPopUpAR()
    {
        float timer = 0f;
        transform.localScale = Vector3.zero;

        while (timer < totalDuration)
        {
            timer += Time.deltaTime;

            // Menggunakan waktu ternormalisasi (0 hingga 1) agar pas dengan Curve Unity
            float normalizedTime = timer / totalDuration;
            float curveValue = scaleCurve.Evaluate(normalizedTime);

            transform.localScale = initialScale * curveValue;
            yield return null;
        }
        transform.localScale = initialScale;
    }
}
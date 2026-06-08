using System.Collections;
using UnityEngine;

public class FoodPopUp : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("3D PopUp Animation")]
    public AnimationCurve scaleCurve;
    public float totalDuration = 0.3f;

    private Vector3 initialScale;
    private Coroutine animationCoroutine;
    private bool hasCacheScale = false;


    void Awake()
    {
        ChaceIntitialScale();
    }
    void ChaceIntitialScale()
    {
        if (!hasCacheScale)
        {
            initialScale = transform.localScale;
            hasCacheScale = true;
        }
    }

    public void MainkanAnimasi()
    {
        ChaceIntitialScale();

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(PlayPopUpAR());
    }

    IEnumerator PlayPopUpAR()
    {
        float timer = 0f;
        transform.localScale = Vector3.zero;

        while (timer < totalDuration)
        {
            timer += Time.deltaTime;

            float curveValue = scaleCurve.Evaluate(timer);

            transform.localScale = initialScale * curveValue;
            yield return null;
        }
        transform.localScale = initialScale;
    }
}

using System.Collections;
using UnityEngine;

public class PopUp : MonoBehaviour
{
    [Header("PopUpAnimation")]
    public AnimationCurve scaleCurve;

    private Coroutine animationCoroutine;
    private float totalDuration = 0.6f;

    void OnEnable()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(PlayAnimation());
    }

    IEnumerator PlayAnimation()
    {
        float timer = 0f;
        transform.localScale = Vector3.zero;

        while (timer < totalDuration)
        {
            timer += Time.deltaTime;

            float currentScale = scaleCurve.Evaluate(timer);

            transform.localScale = new Vector3(currentScale, currentScale, currentScale);
            yield return null;
        }

        float finalScale = scaleCurve[scaleCurve.length - 1].value;
        transform.localScale = new Vector3(finalScale, finalScale, finalScale);
    }
}

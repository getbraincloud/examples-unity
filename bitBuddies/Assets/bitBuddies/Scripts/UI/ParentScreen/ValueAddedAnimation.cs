using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class ValueAddedAnimation : MonoBehaviour
{

    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private float moveDistance = 20f;
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private float fadeDuration = 0.3f; //How long the text fades out at the end
    [SerializeField] private bool fadeOutOnFinish = true; //Whether to fade out after the bounce animation
    [SerializeField] private TextMeshProUGUI textElement;
    
    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private CanvasGroup canvasGroup;
    
    public RectTransform TextRectTransform
    {
        get => rectTransform;
        set => rectTransform = value;
    }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        textElement = GetComponent<TextMeshProUGUI>();
        rectTransform = textElement.rectTransform;
    }

    public void SetUpPositiveNumberText(int in_amount)
    {
        originalPosition = rectTransform.localPosition;
        canvasGroup.alpha = 0;
        textElement.text = $"+{in_amount}";
        textElement.color = Color.green;
    }
    
    public void SetUpNegativeNumberText(int in_amount)
    {
        originalPosition = rectTransform.localPosition;
        canvasGroup.alpha = 0;
        textElement.text = $"-{in_amount}";
        textElement.color = Color.red;
    }

    public void PlayBounce()
    {
        StopAllCoroutines();
        canvasGroup.alpha = 1f; // Ensure visible
        StartCoroutine(BounceAnimation());
    }

    private IEnumerator BounceAnimation()
    {
        Vector3 downPos = originalPosition - new Vector3(0f, moveDistance, 0f);
        float timer = 0f;

        // Moving the text down
        while (timer < moveDuration)
        {
            float t = timer / moveDuration;
            rectTransform.localPosition = Vector3.Lerp(originalPosition, downPos, EaseOutTimeAlpha(t));
            timer += Time.deltaTime;
            yield return null;
        }

        rectTransform.localPosition = downPos;
        timer = 0f;

        // Moving the text up
        while (timer < moveDuration)
        {
            float t = timer / moveDuration;
            rectTransform.localPosition = Vector3.Lerp(downPos, originalPosition, EaseOutTimeAlpha(t));
            timer += Time.deltaTime;
            yield return null;
        }

        rectTransform.localPosition = originalPosition;
        yield return new WaitForSeconds(0.25f);

        // start fading
        if (fadeOutOnFinish)
        {
            timer = 0f;
            float startAlpha = canvasGroup.alpha;

            while (timer < fadeDuration)
            {
                float t = timer / fadeDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, EaseOutTimeAlpha(t));
                timer += Time.deltaTime;
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }
        
        Destroy(gameObject);
    }

    //meant to make the animation look smoother where it will start fast but end slowly
    private float EaseOutTimeAlpha(float t) => t * (2 - t);
}

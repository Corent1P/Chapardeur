using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class GoofyButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    public float scaleAmount = 1.1f;
    public float wiggleSpeed = 8f;
    public float wiggleStrength = 0.03f;

    private Coroutine wiggleRoutine;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnSelect(BaseEventData eventData)
    {
        StartWiggle();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        StopWiggle();
    }

    public void OnPointerEnter(PointerEventData eventData) => StartWiggle();
    public void OnPointerExit(PointerEventData eventData) => StopWiggle();

    void StartWiggle()
    {
        if (wiggleRoutine != null) StopCoroutine(wiggleRoutine);
        wiggleRoutine = StartCoroutine(WiggleAnimation());
    }

    void StopWiggle()
    {
        if (wiggleRoutine != null) StopCoroutine(wiggleRoutine);
        transform.localScale = originalScale; 
    }

    IEnumerator WiggleAnimation()
    {
        float timer = 0f;
        while (true)
        {
            timer += Time.unscaledDeltaTime * wiggleSpeed;
            float x = originalScale.x * scaleAmount + Mathf.Sin(timer) * wiggleStrength;
            float y = originalScale.y * scaleAmount + Mathf.Cos(timer) * wiggleStrength;
            
            transform.localScale = new Vector3(x, y, originalScale.z);
            yield return null;
        }
    }
}
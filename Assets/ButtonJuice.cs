using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    // При наведении мышки плавно увеличиваем
    public void OnPointerEnter(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(originalScale * 1.08f));
    }

    // Когда мышка уходит - возвращаем размер
    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(originalScale));
    }

    // При нажатии - кнопка сжимается
    public void OnPointerDown(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(originalScale * 0.94f));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StartCoroutine(ScaleTo(originalScale * 1.08f));
    }

    private System.Collections.IEnumerator ScaleTo(Vector3 targetScale)
    {
        float time = 0;
        Vector3 startScale = transform.localScale;
        while (time < 1f)
        {
            time += Time.deltaTime * 15f; // Скорость анимации
            transform.localScale = Vector3.Lerp(startScale, targetScale, time);
            yield return null;
        }
        transform.localScale = targetScale;
    }
}
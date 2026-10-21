using UnityEngine;

public class MenuCarMovement : MonoBehaviour
{
    public float speed = 150f;
    public float leftBorder = -900f;
    public float rightSpawn = 900f;

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        ImageRaycastOff();
    }

    void Update()
    {
        if (rectTransform == null) return;
        if (rectTransform.parent is RectTransform parent)
        {
            float half = parent.rect.width * .5f + rectTransform.rect.width;
            leftBorder = -half;
            rightSpawn = half;
            Vector2 position = rectTransform.anchoredPosition;
            position.y = -parent.rect.height * .5f + rectTransform.rect.height;
            rectTransform.anchoredPosition = position;
        }
        rectTransform.anchoredPosition += Vector2.left * speed * Time.deltaTime;

        if (rectTransform.anchoredPosition.x < leftBorder)
        {
            Vector2 pos = rectTransform.anchoredPosition;
            pos.x = rightSpawn;
            rectTransform.anchoredPosition = pos;
        }
    }

    private void ImageRaycastOff()
    {
        if (TryGetComponent(out UnityEngine.UI.Image image)) image.raycastTarget = false;
    }
}

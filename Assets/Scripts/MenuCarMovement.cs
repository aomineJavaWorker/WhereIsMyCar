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
    }

    void Update()
    {
        rectTransform.anchoredPosition += Vector2.left * speed * Time.deltaTime;

        if (rectTransform.anchoredPosition.x < leftBorder)
        {
            Vector2 pos = rectTransform.anchoredPosition;
            pos.x = rightSpawn;
            rectTransform.anchoredPosition = pos;
        }
    }
}
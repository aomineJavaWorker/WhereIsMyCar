using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class AnimalMovementScript : MonoBehaviour
{
    // Centre bounds in the parent Canvas, measured from CityScene's Karte image.
    public Vector2 minPosition;
    public Vector2 maxPosition;
    public float speed = 8f;

    private RectTransform rectTransform;
    private GameManager gameManager;
    private Vector2 startPosition;
    private Vector2 targetPosition;
    private float travelTime;
    private float elapsed;
    private float pause;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        gameManager = FindFirstObjectByType<GameManager>();
        GetComponent<Image>().raycastTarget = false;
        ChooseTarget();
    }

    private void ChooseTarget()
    {
        startPosition = rectTransform.anchoredPosition;
        targetPosition = new Vector2(
            Random.Range(minPosition.x, maxPosition.x),
            Random.Range(minPosition.y, maxPosition.y));
        // SmoothStep's peak speed is 1.5 times its average speed.
        travelTime = Mathf.Max(0.1f, 1.5f * Vector2.Distance(startPosition, targetPosition) / Mathf.Max(0.1f, speed));
        elapsed = 0f;
        float direction = targetPosition.x - startPosition.x;
        if (Mathf.Abs(direction) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = direction > 0f ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void Update()
    {
        if (gameManager != null && gameManager.GameEnded)
            return;

        if (pause > 0f)
        {
            pause -= Time.deltaTime;
            if (pause <= 0f)
                ChooseTarget();
            return;
        }

        elapsed += Time.deltaTime;
        float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / travelTime));
        rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, progress);
        if (elapsed >= travelTime)
            pause = Random.Range(0.8f, 2f);
    }
}

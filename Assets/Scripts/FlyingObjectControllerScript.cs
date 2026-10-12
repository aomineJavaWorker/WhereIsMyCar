using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FlyingObjectControllerScript : MonoBehaviour
{
    [HideInInspector] public float speed = 1f;
    public float fadeDuration = 1.5f;
    public float waveAmplitude = 25f;
    public float waveFrequency = 1f;

    private GameObjectsScript gameObjectsScript;
    private ScreenBounderiesScript screenBounderiesScript;
    private GameManager gameManager;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Image image;
    private Color originalColor;
    private bool isFadingOut;
    private bool isExploading;

    private bool GameEnded => gameManager != null && gameManager.GameEnded;

    private void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        if (image != null) originalColor = image.color;

        gameObjectsScript = FindFirstObjectByType<GameObjectsScript>();
        screenBounderiesScript = FindFirstObjectByType<ScreenBounderiesScript>();
        gameManager = FindFirstObjectByType<GameManager>();
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float time = 0f;
        while (time < fadeDuration)
        {
            if (GameEnded) yield break;
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f,
                Mathf.Clamp01(time / Mathf.Max(0.01f, fadeDuration)));
            yield return null;
        }
        if (!GameEnded) canvasGroup.alpha = 1f;
    }

    private void Update()
    {
        if (GameEnded)
        {
            StopAllCoroutines();
            return;
        }
        if (rectTransform == null) return;

        float waveOffset = Mathf.Sin(Time.time * waveFrequency) * waveAmplitude;
        rectTransform.anchoredPosition += new Vector2(
            -speed * Time.deltaTime, waveOffset * Time.deltaTime);

        if (screenBounderiesScript != null && !isFadingOut)
        {
            if (speed > 0f && transform.position.x < screenBounderiesScript.minX + 80f ||
                speed < 0f && transform.position.x > screenBounderiesScript.maxX - 80f)
            {
                isFadingOut = true;
                StartCoroutine(FadeOutAndDestroy());
                return;
            }
        }

        Vector2 pointer;
        if (!TryGetInputPosition(out pointer)) return;

        // Protect RectangleContainsScreenPoint against Infinity, NaN and
        // out-of-screen values (the Android 'screen pos inf, -inf' warning).
        if (!IsValidScreenPosition(pointer)) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera uiCamera = Camera.main;
        if (canvas != null)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                uiCamera = null;
            else if (canvas.worldCamera != null)
                uiCamera = canvas.worldCamera;
        }

        if (!RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform, pointer, uiCamera)) return;

        if (CompareTag("Bomb") && !isExploading)
        {
            Debug.Log("The cursor collided with a bomb!");
            TriggerExplosion();
        }

        if (!GameEnded && GameObjectsScript.isDragging && !isFadingOut)
        {
            Debug.Log("The cursor collided with a flying object!");
            GameObject dragged = GameObjectsScript.lastDragged;
            if (dragged != null)
            {
                // Cancel dragging immediately, but count the lost vehicle only
                // when its shrink animation finishes.
                GameObjectsScript.lastDragged = null;
                GameObjectsScript.isDragging = false;
                StartCoroutine(ShrinkAndDestroy(dragged, 0.5f));
            }
            StartAndDestroy(CompareTag("Bomb") ? Color.red : Color.cyan);
        }
    }

    private static bool TryGetInputPosition(out Vector2 inputPosition)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Input.touchCount > 0)
        {
            inputPosition = Input.GetTouch(0).position;
            return true;
        }
        inputPosition = Vector2.zero;
        return false;
#else
        inputPosition = Input.mousePosition;
        return true;
#endif
    }

    private static bool IsValidScreenPosition(Vector2 position)
    {
        return !float.IsNaN(position.x) && !float.IsNaN(position.y) &&
            !float.IsInfinity(position.x) && !float.IsInfinity(position.y) &&
            position.x >= 0f && position.x <= Screen.width &&
            position.y >= 0f && position.y <= Screen.height;
    }

    private IEnumerator ShrinkAndDestroy(GameObject obj, float duration)
    {
        if (GameEnded || obj == null) yield break;
        Vector3 originalScale = obj.transform.localScale;
        Quaternion originalRotation = obj.transform.rotation;
        float time = 0f;

        while (time < duration)
        {
            if (GameEnded || obj == null) yield break;
            time += Time.deltaTime;
            float progress = Mathf.Clamp01(time / duration);
            obj.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, progress);
            obj.transform.rotation = originalRotation * Quaternion.Euler(0f, 0f, 360f * progress);
            yield return null;
        }

        if (GameEnded || obj == null) yield break;
        Destroy(obj);
        if (gameManager != null) gameManager.VehicleDestroyed();
    }

    public void TriggerExplosion()
    {
        if (GameEnded || isExploading) return;
        isExploading = true;
        PlaySound(14, 5f);

        if (TryGetComponent<Animator>(out Animator animator))
            animator.SetBool("explode", true);
        if (image != null)
        {
            image.color = Color.red;
            StartCoroutine(RecoverColor(0.4f));
        }
        StartCoroutine(Vibrate());
        StartCoroutine(WaitBeforeExplode());
    }

    private void PlaySound(int index, float volume = 1f)
    {
        if (gameObjectsScript == null || gameObjectsScript.carSoundSource == null ||
            gameObjectsScript.sounds == null || index < 0 ||
            index >= gameObjectsScript.sounds.Length ||
            gameObjectsScript.sounds[index] == null) return;
        gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[index], volume);
    }

    private IEnumerator RecoverColor(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (!GameEnded && image != null) image.color = originalColor;
    }

    private IEnumerator Vibrate()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
        if (rectTransform == null) yield break;
        Vector2 originalPos = rectTransform.anchoredPosition;
        float duration = 0.4f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (GameEnded) yield break;
            rectTransform.anchoredPosition = originalPos + Random.insideUnitCircle * 5f;
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (!GameEnded) rectTransform.anchoredPosition = originalPos;
    }

    private IEnumerator WaitBeforeExplode()
    {
        if (GameEnded) yield break;
        float radius = 0f;
        if (TryGetComponent<CircleCollider2D>(out CircleCollider2D circle))
            radius = circle.radius * Mathf.Abs(transform.lossyScale.x);

        ExplodeAndDestroyNearbyObjects(radius);
        yield return new WaitForSeconds(1f);
        if (GameEnded) yield break;
        ExplodeAndDestroyNearbyObjects(radius);
        Destroy(gameObject);
    }

    private void ExplodeAndDestroyNearbyObjects(float radius)
    {
        if (GameEnded || radius <= 0f) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D item in hits)
        {
            if (GameEnded) return;
            if (item == null || item.gameObject == gameObject) continue;
            FlyingObjectControllerScript flyingObject =
                item.GetComponent<FlyingObjectControllerScript>();
            if (flyingObject != null && !flyingObject.isExploading)
                flyingObject.StartAndDestroy(Color.cyan);
        }
    }

    // Keep the optional argument from your Android version for compatibility.
    public void StartAndDestroy(Color color, bool triggerRetry = false)
    {
        if (GameEnded || isFadingOut) return;
        isFadingOut = true;
        if (image != null)
        {
            image.color = color;
            StartCoroutine(RecoverColor(0.5f));
        }
        StartCoroutine(Vibrate());
        StartCoroutine(FadeOutAndDestroy());
        PlaySound(14);
    }

    private IEnumerator FadeOutAndDestroy()
    {
        if (GameEnded) yield break;
        float time = 0f;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        while (time < fadeDuration)
        {
            if (GameEnded) yield break;
            time += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f,
                    Mathf.Clamp01(time / Mathf.Max(0.01f, fadeDuration)));
            yield return null;
        }
        if (GameEnded) yield break;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        Destroy(gameObject);
    }
}

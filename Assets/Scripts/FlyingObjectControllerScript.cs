using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

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
    private Coroutine fadeInRoutine;
    // Reuse screen-corner arrays to avoid per-frame garbage collection on phones.
    private readonly Vector3[] worldCorners = new Vector3[4];
    private readonly Vector2[] screenCorners = new Vector2[4];
    private static readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    private static PointerEventData pointerData;
    private static EventSystem pointerEventSystem;
    private Canvas parentCanvas;

    private bool GameEnded => gameManager != null && gameManager.GameEnded;

    private void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        parentCanvas = GetComponentInParent<Canvas>();
        if (image != null) originalColor = image.color;
        gameObjectsScript = FindFirstObjectByType<GameObjectsScript>();
        screenBounderiesScript = FindFirstObjectByType<ScreenBounderiesScript>();
        gameManager = FindFirstObjectByType<GameManager>();
        fadeInRoutine = StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float duration = Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;
        while (elapsed < duration && !GameEnded)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        if (!GameEnded && canvasGroup != null) canvasGroup.alpha = 1f;
        fadeInRoutine = null;
    }

    private void Update()
    {
        if (GameEnded) return;
        if (rectTransform == null) return;

        float waveOffset = Mathf.Sin(Time.time * waveFrequency) * waveAmplitude;
        rectTransform.anchoredPosition += new Vector2(
            -speed * Time.deltaTime, waveOffset * Time.deltaTime);

        if (screenBounderiesScript != null && !isFadingOut)
        {
            if ((speed > 0f && transform.position.x < screenBounderiesScript.minX + 80f) ||
                (speed < 0f && transform.position.x > screenBounderiesScript.maxX - 80f))
            {
                StartAndFadeOut();
                return;
            }
        }

        if (!GameObjectsScript.isDragging && (!CompareTag("Bomb") || isExploading)) return;

        Vector2 pointer;
        GameObject selected = GameObjectsScript.lastDragged;
        if (GameObjectsScript.isDragging && selected != null && selected.TryGetComponent(out DragAndDropScript drag))
        {
            if (!drag.TryGetDragPointer(out pointer)) return;
        }
        else if (!TryGetInputPosition(out pointer)) return;
        if (!IsValidScreenPosition(pointer)) return;
        if (IsTouchOnButton(pointer)) return;

        // Important Android fix: NEVER use RectangleContainsScreenPoint here.
        // It can project invalid world-space UI corners to (Infinity, -Infinity)
        // even when the touch position is valid. Project all four corners ourselves,
        // reject non-finite projections and test the convex quadrilateral.
        if (!IsPointInsideProjectedRect(pointer)) return;

        if (CompareTag("Bomb") && !isExploading)
            TriggerExplosion();

        if (!GameEnded && GameObjectsScript.isDragging && !isFadingOut)
        {
            GameObject dragged = GameObjectsScript.lastDragged;
            if (dragged != null)
            {
                if (dragged.TryGetComponent(out DragAndDropScript draggedCar)) draggedCar.BeginDestruction();
                GameObjectsScript.lastDragged = null;
                GameObjectsScript.isDragging = false;
                StartCoroutine(ShrinkAndDestroy(dragged, 0.5f));
            }
            StartAndDestroy(CompareTag("Bomb") ? Color.red : Color.cyan);
        }
    }

    private bool IsPointInsideProjectedRect(Vector2 pointer)
    {
        if (rectTransform == null) return false;
        Canvas canvas = parentCanvas;
        Camera uiCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && uiCamera == null)
            return false;

        rectTransform.GetWorldCorners(worldCorners);
        for (int i = 0; i < 4; i++)
        {
            Vector3 point = uiCamera != null ? uiCamera.WorldToScreenPoint(worldCorners[i]) : worldCorners[i];
            if (!IsFinite(point) || (uiCamera != null &&
                uiCamera.orthographic == false && point.z <= 0f)) return false;
            screenCorners[i] = new Vector2(point.x, point.y);
        }

        bool positive = false, negative = false;
        float area = 0f;
        for (int i = 0; i < 4; i++)
        {
            Vector2 a = screenCorners[i], b = screenCorners[(i + 1) % 4];
            area += a.x * b.y - b.x * a.y;
        }
        if (Mathf.Abs(area) < .01f) return false;
        for (int i = 0; i < 4; i++)
        {
            Vector2 a = screenCorners[i];
            Vector2 b = screenCorners[(i + 1) % 4];
            float cross = (b.x - a.x) * (pointer.y - a.y) -
                          (b.y - a.y) * (pointer.x - a.x);
            if (cross > 0.01f) positive = true;
            if (cross < -0.01f) negative = true;
            if (positive && negative) return false;
        }
        return true;
    }

    private static bool IsFinite(Vector3 p)
    {
        return !float.IsNaN(p.x) && !float.IsInfinity(p.x) &&
               !float.IsNaN(p.y) && !float.IsInfinity(p.y) &&
               !float.IsNaN(p.z) && !float.IsInfinity(p.z);
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
        if (Input.touchCount > 0)
        {
            inputPosition = Input.GetTouch(0).position;
            return true;
        }
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

    private static bool IsTouchOnButton(Vector2 position)
    {
        if (EventSystem.current == null) return false;
        if (pointerData == null || pointerEventSystem != EventSystem.current)
        {
            pointerEventSystem = EventSystem.current;
            pointerData = new PointerEventData(pointerEventSystem);
        }
        pointerData.Reset();
        pointerData.position = position;
        uiHits.Clear();
        EventSystem.current.RaycastAll(pointerData, uiHits);
        foreach (RaycastResult hit in uiHits)
        {
            if (hit.gameObject.GetComponentInParent<Button>() != null ||
                hit.gameObject.GetComponentInParent<MobileTransformButtonScript>() != null)
                return true;
        }
        return false;
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
        float elapsed = 0f;
        while (elapsed < 0.4f)
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
            FlyingObjectControllerScript flyingObject = item.GetComponent<FlyingObjectControllerScript>();
            if (flyingObject != null && !flyingObject.isExploading)
                flyingObject.StartAndDestroy(Color.cyan);
        }
    }

    // Optional argument is retained for compatibility with your scene/scripts.
    public void StartAndDestroy(Color color, bool triggerRetry = false)
    {
        if (GameEnded || isFadingOut) return;
        if (image != null)
        {
            image.color = color;
            StartCoroutine(RecoverColor(0.5f));
        }
        StartCoroutine(Vibrate());
        StartAndFadeOut();
        PlaySound(14);
    }

    private void StartAndFadeOut()
    {
        if (isFadingOut) return;
        isFadingOut = true;
        if (fadeInRoutine != null)
        {
            StopCoroutine(fadeInRoutine);
            fadeInRoutine = null;
        }
        StartCoroutine(FadeOutAndDestroy());
    }

    private IEnumerator FadeOutAndDestroy()
    {
        if (GameEnded) yield break;
        float time = 0f;
        float duration = Mathf.Max(0.01f, fadeDuration);
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        while (time < duration)
        {
            if (GameEnded) yield break;
            time += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(time / duration));
            yield return null;
        }
        if (GameEnded) yield break;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        Destroy(gameObject);
    }
}

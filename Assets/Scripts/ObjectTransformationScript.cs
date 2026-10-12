using UnityEngine;

public class ObjectTransformationScript : MonoBehaviour
{
    // Keep both the original reference and Inspector settings.
    public GameObjectsScript gameObjectScript;
    private GameManager gameManager;
    public float rotationSpeed = 90f;
    public float scaleSpeed = 0.5f;

    private bool rotateCW, rotateCCW;
    private bool scaleUpY, scaleDownY, scaleUpX, scaleDownX;
    public static bool isTransforming = false;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        if (gameObjectScript == null)
            gameObjectScript = FindFirstObjectByType<GameObjectsScript>();
    }

    private void Update()
    {
        if (gameManager != null && gameManager.GameEnded)
        {
            ResetControls();
            return;
        }

        GameObject selected = GameObjectsScript.lastDragged;
        if (selected == null)
        {
            isTransforming = false;
            return;
        }

        RectTransform car = selected.GetComponent<RectTransform>();
        if (car == null) return;

        bool clockwise = rotateCW || Input.GetKey(KeyCode.X);
        bool counterclockwise = rotateCCW || Input.GetKey(KeyCode.Z);
        bool growY = scaleUpY || Input.GetKey(KeyCode.UpArrow);
        bool shrinkY = scaleDownY || Input.GetKey(KeyCode.DownArrow);
        bool growX = scaleUpX || Input.GetKey(KeyCode.RightArrow);
        bool shrinkX = scaleDownX || Input.GetKey(KeyCode.LeftArrow);

        isTransforming = clockwise || counterclockwise || growY || shrinkY || growX || shrinkX;
        float dt = Time.deltaTime;

        if (clockwise) car.Rotate(0f, 0f, -rotationSpeed * dt);
        if (counterclockwise) car.Rotate(0f, 0f, rotationSpeed * dt);

        Vector3 scale = car.localScale;
        if (growY) scale.y = ResizeSignedScale(scale.y, scaleSpeed * dt);
        if (shrinkY) scale.y = ResizeSignedScale(scale.y, -scaleSpeed * dt);
        if (growX) scale.x = ResizeSignedScale(scale.x, scaleSpeed * dt);
        if (shrinkX) scale.x = ResizeSignedScale(scale.x, -scaleSpeed * dt);
        if (Input.GetKeyDown(KeyCode.Space)) scale.x = -scale.x;
        car.localScale = scale;
    }

    // Can be called from mobile UI EventTrigger PointerDown / PointerUp.
    public void StartRotateCW() => rotateCW = true;
    public void StopRotateCW() => rotateCW = false;
    public void StartRotateCCW() => rotateCCW = true;
    public void StopRotateCCW() => rotateCCW = false;
    public void StartScaleUpX() => scaleUpX = true;
    public void StopScaleUpX() => scaleUpX = false;
    public void StartScaleDownX() => scaleDownX = true;
    public void StopScaleDownX() => scaleDownX = false;
    public void StartScaleUpY() => scaleUpY = true;
    public void StopScaleUpY() => scaleUpY = false;
    public void StartScaleDownY() => scaleDownY = true;
    public void StopScaleDownY() => scaleDownY = false;

    public void FlipSelected()
    {
        if (gameManager != null && gameManager.GameEnded) return;
        GameObject selected = GameObjectsScript.lastDragged;
        if (selected == null) return;
        RectTransform car = selected.GetComponent<RectTransform>();
        if (car == null) return;
        Vector3 scale = car.localScale;
        scale.x = -scale.x;
        car.localScale = scale;
    }

    private static float ResizeSignedScale(float value, float delta)
    {
        float sign = value < 0f ? -1f : 1f;
        return sign * Mathf.Clamp(Mathf.Abs(value) + delta, 0.3f, 0.9f);
    }

    private void ResetControls()
    {
        rotateCW = rotateCCW = false;
        scaleUpY = scaleDownY = scaleUpX = scaleDownX = false;
        isTransforming = false;
    }

    private void OnDisable() => ResetControls();
}

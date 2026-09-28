using UnityEngine;

public class ObjectTransformationScript : MonoBehaviour
{
    public GameObjectsScript gameObjectScript;
    private GameManager gameManager;

    void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        gameObjectScript = FindFirstObjectByType<GameObjectsScript>();
    }

    void Update()
    {
        if (gameManager != null && gameManager.GameEnded)
            return;

        if (GameObjectsScript.lastDragged == null)
            return;

        RectTransform car = GameObjectsScript.lastDragged.GetComponent<RectTransform>();
        if (Input.GetKey(KeyCode.Z))
            car.Rotate(0, 0, Time.deltaTime * 24f);
        if (Input.GetKey(KeyCode.X))
            car.Rotate(0, 0, -Time.deltaTime * 24f);

        Vector3 scale = car.localScale;
        if (Input.GetKey(KeyCode.UpArrow))
            scale.y = ResizeSignedScale(scale.y, 0.0025f);
        if (Input.GetKey(KeyCode.DownArrow))
            scale.y = ResizeSignedScale(scale.y, -0.0025f);
        if (Input.GetKey(KeyCode.LeftArrow))
            scale.x = ResizeSignedScale(scale.x, -0.0025f);
        if (Input.GetKey(KeyCode.RightArrow))
            scale.x = ResizeSignedScale(scale.x, 0.0025f);
        if (Input.GetKeyDown(KeyCode.Space))
            scale.x = -scale.x;

        car.localScale = scale;
    }

    private static float ResizeSignedScale(float value, float delta)
    {
        return Mathf.Sign(value) * Mathf.Clamp(Mathf.Abs(value) + delta, 0.3f, 0.9f);
    }
}

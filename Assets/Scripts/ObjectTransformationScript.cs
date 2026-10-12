using UnityEngine;

public class ObjectTransformationScript : MonoBehaviour
{
    public GameObjectsScript gameObjectScript;
    private GameManager gameManager;
    public float rotationSpeed = 90f;
    public float scaleSpeed = 0.5f;
    private bool rotateCW, rotateCCW; scaleUpY, scaleDownY, scaleUpX, scaleDownX;
    public static bool isTransforming = false;


    void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        if (GameObjectsScript.lastDragged == null)
        {
            return;
        }
        RectTransform rc = GameObjectsScript.lastDragged.GetComponent<RectTransform>();

        if (rotateCW)
        {
            rc.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
        }
        if (rotateCCW)
        {
            rc.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
        if (scaleUpY && rc.localScale.y < 0.9f)
        {
            rc.localScale += new Vector3(0, scaleSpeed * Time.deltaTime, 0);
        }
        if (scaleDownY && rc.localScale.y > 0.3f)
        {
            rc.localScale -= new Vector3(0, scaleSpeed * Time.deltaTime, 0);
        }
        if (scaleUpX && rc.localScale.x < 0.9f)
        {
            rc.localScale += new Vector3(scaleSpeed * Time.deltaTime, 0, 0);
        }
        if (scaleDownX && rc.localScale.x > 0.3f)
        {
            rc.localScale -= new Vector3(scaleSpeed * Time.deltaTime, 0, 0);
        }
        isTransforming = rotateCCW || rotateCCW || scaleUpY || scaleDownY || scaleUpX || scaleDownX;

    }





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

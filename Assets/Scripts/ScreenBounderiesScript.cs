using UnityEngine;

public class ScreenBounderiesScript : MonoBehaviour
{
    [HideInInspector]
    public Vector3 screenPoint, offset;
    [HideInInspector]
    public float minX, maxX, minY, maxY;
    public float reductionFactor = 0.02f; //2% no ekrāna izmēra
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Vector3 lowerLeft = Camera.main.ScreenToWorldPoint(Vector3.zero);
        Vector3 upperRight = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));

        float widthReduction = (upperRight.x - lowerLeft.x) * reductionFactor;
        float heightReduction = (upperRight.y - lowerLeft.y) * reductionFactor;

        minX = lowerLeft.x + widthReduction;
        maxX = upperRight.x - widthReduction;
        minY = lowerLeft.y + heightReduction;
        maxY = upperRight.y - heightReduction;

    }

    public Vector2 GetClampedPosition(Vector3 curPosition)
    {
        return new Vector2(
            Mathf.Clamp(curPosition.x, minX,maxX),
            Mathf.Clamp(curPosition.y, minY,maxY)
            );
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

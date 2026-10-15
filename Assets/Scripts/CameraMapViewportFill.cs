using UnityEngine;

/// <summary>
/// Prevents the Camera's blue clear color from appearing around the map.
/// The map is never stretched: on tall screens it is deliberately cropped,
/// and the existing CameraZoomScript lets players pan to reach hidden areas.
/// </summary>
[DefaultExecutionOrder(900)]
[RequireComponent(typeof(Camera))]
public class CameraMapViewportFill : MonoBehaviour
{
    public RectTransform mapImage;
    public ScreenBounderiesScript boundaries;
    [Range(0.9f, 1f)] public float edgeSafety = .994f;

    private Camera cam;
    private readonly Vector3[] mapCorners = new Vector3[4];

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (mapImage == null)
        {
            GameObject map = GameObject.Find("backgroundImage");
            if (map != null) mapImage = map.GetComponent<RectTransform>();
        }
        if (boundaries == null)
            boundaries = FindFirstObjectByType<ScreenBounderiesScript>();
    }

    private void LateUpdate()
    {
        if (cam == null || !cam.orthographic || mapImage == null) return;
        // Use the actual map corners, not an assumed 1920x1080 size.
        mapImage.GetWorldCorners(mapCorners);
        float left = Mathf.Min(mapCorners[0].x, mapCorners[2].x);
        float right = Mathf.Max(mapCorners[0].x, mapCorners[2].x);
        float bottom = Mathf.Min(mapCorners[0].y, mapCorners[2].y);
        float top = Mathf.Max(mapCorners[0].y, mapCorners[2].y);
        float width = right - left;
        float height = top - bottom;
        if (width < .1f || height < .1f || cam.aspect <= 0f) return;

        float allowed = Mathf.Min(height * .5f, width * .5f / cam.aspect) * edgeSafety;
        if (cam.orthographicSize > allowed)
            cam.orthographicSize = Mathf.Max(.01f, allowed);

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 pos = cam.transform.position;
        pos.x = Mathf.Clamp(pos.x, left + halfW, right - halfW);
        pos.y = Mathf.Clamp(pos.y, bottom + halfH, top - halfH);
        cam.transform.position = pos;
    }
}

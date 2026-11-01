using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameObjectsScript : MonoBehaviour
{
    
    public RectTransform[] spawnPoints;

 
    public RectTransform[] dropPlaces;
    [Header("Road-only spawning (CityScene / Karte.jpg)")]
    [SerializeField] private RectTransform roadMap;
    // Clear asphalt rectangles in normalized source-image coordinates (top-left origin).
    // Legacy spawnPoints remain as editor markers; they do not define drivable ground.
    [SerializeField] private Rect[] roadSpawnRegions;
    [SerializeField, Min(0f)] private float roadEdgeClearance = 2f;
    [SerializeField, Min(0f)] private float spawnSeparation = 6f;



    public GameObject garbageTruck;
    public GameObject Ambulance;
    public GameObject schoolBus;
    public GameObject Cement;
    public GameObject Ugunsdzeseji;
    public GameObject e61;
    public GameObject e46;
    public GameObject Viegla;
    public GameObject Eskavators;
    public GameObject Traktors_dzeltens;
    public GameObject Traktors_Zals;
    public GameObject Policija;


    [HideInInspector]
    public Vector2 garbageTruckCoord;

    [HideInInspector]
    public Vector2 AmbulanceCoord;

    [HideInInspector]
    public Vector2 schoolBusCoord;

    [HideInInspector]
    public Vector2 CementCoord;

    [HideInInspector]
    public Vector2 UgunsdzesejiCoord;

    [HideInInspector]
    public Vector2 VieglaCoord;

    [HideInInspector]
    public Vector2 e61Coord;

    [HideInInspector]
    public Vector2 e46Coord;

    [HideInInspector]
    public Vector2 EskavatorsCoord;

    [HideInInspector]
    public Vector2 Traktors_dzeltensCoord;

    [HideInInspector]
    public Vector2 Traktors_ZalsCoord;

    [HideInInspector]
    public Vector2 PolicijaCoord;


    public Canvas canvas;
    public AudioSource carSoundSource;
    public AudioClip[] sounds;


    [HideInInspector]
    public bool inRightPlace = false;

    public static GameObject lastDragged = null;
    public static bool isDragging = false;


    void Awake()
    {
        ConfigureDropRaycaster();
        SpawnDropPlacesRandomly();
       
        garbageTruckCoord =
            GetVehiclePosition(garbageTruck);

        AmbulanceCoord =
            GetVehiclePosition(Ambulance);

        schoolBusCoord =
            GetVehiclePosition(schoolBus);

        CementCoord =
            GetVehiclePosition(Cement);

        UgunsdzesejiCoord =
            GetVehiclePosition(Ugunsdzeseji);

        e61Coord =
            GetVehiclePosition(e61);

        e46Coord =
            GetVehiclePosition(e46);

        VieglaCoord =
            GetVehiclePosition(Viegla);

        EskavatorsCoord =
            GetVehiclePosition(Eskavators);

        Traktors_dzeltensCoord =
            GetVehiclePosition(Traktors_dzeltens);

        Traktors_ZalsCoord =
            GetVehiclePosition(Traktors_Zals);

        PolicijaCoord =
            GetVehiclePosition(Policija);
    }


    private static Vector2 GetVehiclePosition(GameObject vehicle)
    {
        return vehicle != null && vehicle.TryGetComponent(out RectTransform rect)
            ? (Vector2)rect.localPosition : Vector2.zero;
    }

    private void ConfigureDropRaycaster()
    {
        // Y = 180 silhouettes must receive drops despite facing away from the camera.
        if (canvas != null && canvas.TryGetComponent(out GraphicRaycaster raycaster))
            raycaster.ignoreReversedGraphics = false;
    }

    private void SpawnDropPlacesRandomly()
    {
        if (roadMap == null || roadSpawnRegions == null || roadSpawnRegions.Length == 0 ||
            dropPlaces == null || dropPlaces.Length == 0)
        {
            Debug.LogError("CityScene road spawning is not configured.", this);
            return;
        }

        Canvas.ForceUpdateCanvases();
        var items = new List<RectTransform>();
        var positions = new Vector3[dropPlaces.Length];
        var rotations = new Quaternion[dropPlaces.Length];
        var scales = new Vector3[dropPlaces.Length];
        for (int i = 0; i < dropPlaces.Length; i++)
        {
            RectTransform item = dropPlaces[i];
            if (item == null || items.Contains(item))
            {
                Debug.LogError("Road spawning requires unique, non-null cars and places.", this);
                return;
            }
            items.Add(item);
            positions[i] = item.position;
            rotations[i] = item.localRotation;
            scales[i] = item.localScale;
        }

        foreach (RectTransform item in items)
        {
            item.localRotation = Quaternion.identity;
            SetFlip(item, Random.value > 0.5f);
        }
        EnsureFlipChallenge();
        // Place the widest vehicles first: they fit fewer stretches of asphalt.
        items.Sort((a, b) =>
        {
            Rect first = GetMapBounds(a, true);
            Rect second = GetMapBounds(b, true);
            return Mathf.Min(second.width, second.height).CompareTo(Mathf.Min(first.width, first.height));
        });

        var occupied = new List<Rect>();
        var regions = new List<int>();
        for (int i = 0; i < roadSpawnRegions.Length; i++) regions.Add(i);
        // Both the complete-layout retries and the candidate searches are bounded.
        for (int attempt = 0; attempt < 8; attempt++)
        {
            occupied.Clear();
            bool complete = true;
            foreach (RectTransform item in items)
            {
                for (int i = regions.Count - 1; i > 0; i--)
                {
                    int other = Random.Range(0, i + 1);
                    int saved = regions[i];
                    regions[i] = regions[other];
                    regions[other] = saved;
                }
                if (!TryPlaceOnRoad(item, regions, occupied))
                {
                    complete = false;
                    break;
                }
            }
            if (!complete) continue;

            // The array contains both target silhouettes and draggable cars.
            foreach (RectTransform item in items)
                if (item.TryGetComponent(out DragAndDropScript car)) car.SaveStartPosition();
            return;
        }

        for (int i = 0; i < dropPlaces.Length; i++)
        {
            dropPlaces[i].position = positions[i];
            dropPlaces[i].localRotation = rotations[i];
            dropPlaces[i].localScale = scales[i];
        }
        Debug.LogError("Cannot fit all cars and places in the configured road regions. Check their sizes and road capacity.", this);
    }

    private bool TryPlaceOnRoad(RectTransform item, List<int> regions, List<Rect> occupied)
    {
        int firstRotation = Random.Range(0, 4);
        for (int turn = 0; turn < 4; turn++)
        {
            item.localRotation = Quaternion.Euler(0f, 0f, ((firstRotation + turn) % 4) * 90f);
            Rect visible = GetMapBounds(item, true);
            Rect hitArea = GetMapBounds(item, false);
            Vector3 origin = roadMap.InverseTransformPoint(item.position);
            Rect map = roadMap.rect;
            foreach (int index in regions)
            {
                // Regions follow the source image: normalized X right, Y down.
                Rect road = roadSpawnRegions[index];
                float minX = map.xMin + road.xMin * map.width + roadEdgeClearance - visible.xMin + origin.x;
                float maxX = map.xMin + road.xMax * map.width - roadEdgeClearance - visible.xMax + origin.x;
                float minY = map.yMax - road.yMax * map.height + roadEdgeClearance - visible.yMin + origin.y;
                float maxY = map.yMax - road.yMin * map.height - roadEdgeClearance - visible.yMax + origin.y;
                if (minX > maxX || minY > maxY) continue;

                for (int sample = 0; sample < 8; sample++)
                    if (TryRoadPosition(item, new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY)),
                        origin, hitArea, occupied)) return true;

                // Finite grid fallback finds free ends of a busy road segment.
                float step = Mathf.Max(8f, Mathf.Min(hitArea.width, hitArea.height) * 0.5f);
                int columns = Mathf.Max(1, Mathf.CeilToInt((maxX - minX) / step));
                int rows = Mathf.Max(1, Mathf.CeilToInt((maxY - minY) / step));
                for (int x = 0; x <= columns; x++)
                    for (int y = 0; y <= rows; y++)
                        if (TryRoadPosition(item, new Vector2(Mathf.Lerp(minX, maxX, (float)x / columns),
                            Mathf.Lerp(minY, maxY, (float)y / rows)), origin, hitArea, occupied)) return true;
            }
        }
        return false;
    }

    private bool TryRoadPosition(RectTransform item, Vector2 candidate, Vector3 origin,
        Rect hitArea, List<Rect> occupied)
    {
        hitArea.position += candidate - (Vector2)origin;
        Rect reserved = Rect.MinMaxRect(hitArea.xMin - spawnSeparation, hitArea.yMin - spawnSeparation,
            hitArea.xMax + spawnSeparation, hitArea.yMax + spawnSeparation);
        foreach (Rect other in occupied)
            if (reserved.Overlaps(other)) return false;

        item.position = roadMap.TransformPoint(new Vector3(candidate.x, candidate.y, origin.z));
        occupied.Add(hitArea);
        return true;
    }

    private Rect GetMapBounds(RectTransform item, bool renderedImage)
    {
        Rect rect = item.rect;
        if (renderedImage && item.TryGetComponent(out Image image))
        {
            rect = image.GetPixelAdjustedRect();
            Sprite sprite = image.overrideSprite != null ? image.overrideSprite : image.sprite;
            if (image.preserveAspect && sprite != null)
            {
                // Same aspect fitting as UI.Image, including non-central pivots.
                // Keep the sprite's transparent padding for conservative road clearance.
                float aspect = sprite.rect.width / sprite.rect.height;
                if (aspect > rect.width / rect.height)
                {
                    float height = rect.width / aspect;
                    rect.y += (rect.height - height) * item.pivot.y;
                    rect.height = height;
                }
                else
                {
                    float width = rect.height * aspect;
                    rect.x += (rect.width - width) * item.pivot.x;
                    rect.width = width;
                }
            }
        }

        Matrix4x4 matrix = roadMap.worldToLocalMatrix * item.localToWorldMatrix;
        Vector2 a = matrix.MultiplyPoint3x4(new Vector3(rect.xMin, rect.yMin));
        Vector2 b = matrix.MultiplyPoint3x4(new Vector3(rect.xMin, rect.yMax));
        Vector2 c = matrix.MultiplyPoint3x4(new Vector3(rect.xMax, rect.yMin));
        Vector2 d = matrix.MultiplyPoint3x4(new Vector3(rect.xMax, rect.yMax));
        Vector2 min = Vector2.Min(Vector2.Min(a, b), Vector2.Min(c, d));
        Vector2 max = Vector2.Max(Vector2.Max(a, b), Vector2.Max(c, d));
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }


    private static void SetFlip(RectTransform target, bool flipped)
    {
        Vector3 scale = target.localScale;
        scale.x = flipped ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        target.localScale = scale;
    }

    private void EnsureFlipChallenge()
    {
        // At least one random car must require Space, even if all random flips match.
        List<RectTransform> cars = new List<RectTransform>();
        foreach (RectTransform item in dropPlaces)
        {
            if (item != null && item.TryGetComponent<DragAndDropScript>(out _))
                cars.Add(item);
        }

        if (cars.Count == 0)
            return;

        RectTransform car = cars[Random.Range(0, cars.Count)];
        foreach (RectTransform place in dropPlaces)
        {
            if (place != null && place.TryGetComponent<DropPlaceScript>(out _) &&
                place.CompareTag(car.tag))
            {
                SetFlip(car, place.localScale.x > 0f);
                return;
            }
        }
    }
}

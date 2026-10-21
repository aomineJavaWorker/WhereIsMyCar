using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameObjectsScript : MonoBehaviour
{
    
    public RectTransform[] spawnPoints;

 
    public RectTransform[] dropPlaces;


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
        if (spawnPoints == null || dropPlaces == null)
        {
            Debug.LogError("SpawnPoints vai DropPlaces nav pievienoti!");
            return;
        }


        if (spawnPoints.Length < dropPlaces.Length)
        {
            Debug.LogError("SpawnPoint nepietiek visām DropPlace vietām!");
            return;
        }


        // Izveidojam kopiju prieks SpawnPoint, lai viens punkts neatkartojas vairak rezes
        List<RectTransform> availablePoints =
            new List<RectTransform>();
        foreach (RectTransform point in spawnPoints)
            if (point != null && !availablePoints.Contains(point)) availablePoints.Add(point);
        if (availablePoints.Count < dropPlaces.Length)
        {
            Debug.LogError("Not enough unique, non-null spawn points.");
            return;
        }


        foreach (RectTransform dropPlace in dropPlaces)
        {
            if (dropPlace == null)
            {
                Debug.LogWarning("Viens DropPlace nav pievienots!");
                continue;
            }


            // Izvelamies random vietu
            int randomIndex =
                Random.Range(0, availablePoints.Count);


            RectTransform randomPoint =
                availablePoints[randomIndex];


            // Parvietojam place
            dropPlace.position = randomPoint.position;

            // Nejaušs pagrieziens: 0, 90, 180 vai 270 grādi
            float randomAngle = Random.Range(0, 4) * 90f;

            // Use one mirror representation (signed X), including legacy Y=180 places.
            dropPlace.localRotation = Quaternion.Euler(0f, 0f, randomAngle);
            SetFlip(dropPlace, Random.value > 0.5f);

            // The array intentionally contains both places and draggable cars.
            if (dropPlace.TryGetComponent(out DragAndDropScript car))
                car.SaveStartPosition();


       
            availablePoints.RemoveAt(randomIndex);
        }

        EnsureFlipChallenge();
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

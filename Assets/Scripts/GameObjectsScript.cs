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
            garbageTruck.GetComponent<RectTransform>().localPosition;

        AmbulanceCoord =
            Ambulance.GetComponent<RectTransform>().localPosition;

        schoolBusCoord =
            schoolBus.GetComponent<RectTransform>().localPosition;

        CementCoord =
            Cement.GetComponent<RectTransform>().localPosition;

        UgunsdzesejiCoord =
            Ugunsdzeseji.GetComponent<RectTransform>().localPosition;

        e61Coord =
            e61.GetComponent<RectTransform>().localPosition;

        e46Coord =
            e46.GetComponent<RectTransform>().localPosition;

        VieglaCoord =
            Viegla.GetComponent<RectTransform>().localPosition;

        EskavatorsCoord =
            Eskavators.GetComponent<RectTransform>().localPosition;

        Traktors_dzeltensCoord =
            Traktors_dzeltens.GetComponent<RectTransform>().localPosition;

        Traktors_ZalsCoord =
            Traktors_Zals.GetComponent<RectTransform>().localPosition;

        PolicijaCoord =
            Policija.GetComponent<RectTransform>().localPosition;
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
            new List<RectTransform>(spawnPoints);


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

using UnityEngine;
using System.Collections.Generic;

public class GameObjectsScript : MonoBehaviour
{
    // Точки, на которых могут появляться места для машин
    public RectTransform[] spawnPoints;

    // Места, куда игрок должен поставить машины
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
        // Рандомно размещаем МЕСТА для машин
        SpawnDropPlacesRandomly();


        // Запоминаем обычные стартовые позиции самих машин
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


        // Создаём копию SpawnPoint,
        // чтобы одна точка не использовалась несколько раз
        List<RectTransform> availablePoints =
            new List<RectTransform>(spawnPoints);


        foreach (RectTransform dropPlace in dropPlaces)
        {
            if (dropPlace == null)
            {
                Debug.LogWarning("Viens DropPlace nav pievienots!");
                continue;
            }


            // Выбираем случайную свободную точку
            int randomIndex =
                Random.Range(0, availablePoints.Count);


            RectTransform randomPoint =
                availablePoints[randomIndex];


            // Перемещаем место для машины
            dropPlace.position = randomPoint.position;


            // Эта SpawnPoint больше недоступна
            availablePoints.RemoveAt(randomIndex);
        }
    }
}
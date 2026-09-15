using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class GameObjectsScript : MonoBehaviour
{
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
    // Vēlāk jāpievieno pārējās mašīnas un to sākuma koordinātas...

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
        garbageTruckCoord = garbageTruck.GetComponent<RectTransform>().localPosition;
        AmbulanceCoord = Ambulance.GetComponent<RectTransform>().localPosition;
        schoolBusCoord = schoolBus.GetComponent<RectTransform>().localPosition;
        CementCoord = Cement.GetComponent<RectTransform>().localPosition;
        UgunsdzesejiCoord = Ugunsdzeseji.GetComponent<RectTransform>().localPosition;
        e61Coord = e61.GetComponent<RectTransform>().localPosition;
        e46Coord = e46.GetComponent<RectTransform>().localPosition;
        VieglaCoord = Viegla.GetComponent<RectTransform>().localPosition;
        EskavatorsCoord = Eskavators.GetComponent<RectTransform>().localPosition;
        Traktors_dzeltensCoord = Traktors_dzeltens.GetComponent<RectTransform>().localPosition;
        Traktors_ZalsCoord = Traktors_Zals.GetComponent<RectTransform>().localPosition;
        PolicijaCoord = Policija.GetComponent<RectTransform>().localPosition;
    }
}
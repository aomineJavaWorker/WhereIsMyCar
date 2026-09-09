using UnityEngine;

public class GameObjectsScript : MonoBehaviour
{

    public GameObject garbageTruck;
    public GameObject medicine;
    public GameObject schoolBuss;

    [HideInInspector]
    public Vector2 garbageTruckCoord;
    [HideInInspector]
    public Vector2 medicineCoord;
    [HideInInspector]
    public Vector2 schoolBussCoord;

    public Canvas canvas;
    public AudioSource carSoundSource;
    public AudioClip[] sounds;


    [HideInInspector]
    public bool inRightPlace = false;
    public static GameObjectsScript lastDraged;
    public static bool isDragging = false;


    void Awake()
    {
        garbageTruckCoord = garbageTruck.GetComponent<RectTransform>().localPosition;
        medicineCoord = medicine.GetComponent<RectTransform>().localPosition;
        schoolBussCoord = schoolBuss.GetComponent<RectTransform>().localPosition;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

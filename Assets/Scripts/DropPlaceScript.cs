using UnityEngine;
using UnityEngine.EventSystems;

public class DropPlaceScript : MonoBehaviour, IDropHandler
{
    private float placeZRot, carZRot, diffZRot;
    private Vector3 placeSize, carSize;
    private float xSizeDiff, ySizeDiff;
    public GameObjectsScript gameObjectsScript;


    void Start()
    {
        gameObjectsScript = Object.FindFirstObjectByType<GameObjectsScript>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if ((eventData.pointerDrag != null) && Input.GetMouseButtonUp(0) &&
            (!Input.GetMouseButton(2)))
        {
            if (eventData.pointerDrag.tag.Equals(tag))
            {
                placeZRot =
                    eventData.pointerDrag.GetComponent<RectTransform>().transform.eulerAngles.z;
                carZRot = GetComponent<RectTransform>().transform.eulerAngles.z;
                diffZRot = Mathf.Abs(placeZRot - carZRot);
                Debug.Log("Diff Z Rot: " + diffZRot);

                placeSize = eventData.pointerDrag.GetComponent<RectTransform>().localScale;
                carSize = GetComponent<RectTransform>().localScale;
                xSizeDiff = Mathf.Abs(placeSize.x - carSize.x);
                ySizeDiff = Mathf.Abs(placeSize.y - carSize.y);
                Debug.Log("Diff X Size: " + xSizeDiff);
                Debug.Log("Diff Y Size: " + ySizeDiff);

                if ((diffZRot <= 7 || (diffZRot >= 353 && diffZRot <= 360)) &&
                    (xSizeDiff <= 0.08f && ySizeDiff <= 0.08f))
                {
                    Debug.Log("Car placed correctly!");
                    gameObjectsScript.inRightPlace = true;
                    eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition =
                        GetComponent<RectTransform>().anchoredPosition;

                    eventData.pointerDrag.GetComponent<RectTransform>().localScale =
                        GetComponent<RectTransform>().localScale;

                    eventData.pointerDrag.GetComponent<RectTransform>().localRotation =
                        GetComponent<RectTransform>().localRotation;

                    switch (eventData.pointerDrag.tag)
                    {
                        case "Garbage":
                            gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[1]);
                            break;

                        case "Ambulance":
                            gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[2]);
                            break;

                        case "School":
                            gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[3]);
                            break;
                        case "Cement":
                            gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[4]);
                            break;
                        case "Ugunsdzeseji":
                            gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[5]);
                            break;
                        case "e61":
                            gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[6]);
                            break;
                        case "e46":
                            gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[7]);
                            break;
                        case "viegla":
                            gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[8]);
                            break;
                        case "Eskavators":
                            gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[9]);
                            break;
                        case "Traktors_dzeltens":
                            gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[9]);
                            break;
                        case "Traktors_Zals":
                            gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[10]);
                            break;
                        case "Policija":
                            gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[11]);
                            break;
                        default:
                            Debug.Log("No matching tag found for the dropped object.");
                            break;
                    }
                }

            }
            else
            {
                gameObjectsScript.inRightPlace = false;
                gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[4]);

                switch (eventData.pointerDrag.tag)
                {
                    case "Garbage":
                        gameObjectsScript.garbageTruck.GetComponent<RectTransform>().localPosition =
                            gameObjectsScript.garbageTruckCoord;
                        break;

                    case "Ambulance":
                        gameObjectsScript.Ambulance.GetComponent<RectTransform>().localPosition =
                             gameObjectsScript.AmbulanceCoord;
                        break;

                    case "School":
                        gameObjectsScript.schoolBus.GetComponent<RectTransform>().localPosition =
                             gameObjectsScript.schoolBusCoord;
                        break;

                    case "Cement":
                        gameObjectsScript.Cement.GetComponent<RectTransform>().localPosition = gameObjectsScript.CementCoord;
                        break;
                    case "Ugunsdzeseji":
                        gameObjectsScript.Ugunsdzeseji.GetComponent<RectTransform>().localPosition = gameObjectsScript.UgunsdzesejiCoord;
                        break;
                    case "e61":
                        gameObjectsScript.e61.GetComponent<RectTransform>().localPosition = gameObjectsScript.e61Coord;
                        break;
                    case "e46":
                        gameObjectsScript.e46.GetComponent<RectTransform>().localPosition = gameObjectsScript.e46Coord;
                        break;
                    case "viegla":
                        gameObjectsScript.Viegla.GetComponent<RectTransform>().localPosition = gameObjectsScript.VieglaCoord;
                        break;
                    case "Eskavators":
                        gameObjectsScript.Eskavators.GetComponent<RectTransform>().localPosition = gameObjectsScript.EskavatorsCoord;
                        break;
                    case "Traktors_Dzeltens":
                        gameObjectsScript.Traktors_dzeltens.GetComponent<RectTransform>().localPosition = gameObjectsScript.Traktors_dzeltensCoord;
                        break;
                    case "Traktors_Zals":
                        gameObjectsScript.Traktors_Zals.GetComponent<RectTransform>().localPosition = gameObjectsScript.Traktors_ZalsCoord;
                        break;
                    case "Policija":
                        gameObjectsScript.Policija.GetComponent<RectTransform>().localPosition = gameObjectsScript.PolicijaCoord;
                        break;

                    default:
                        Debug.Log("No matching tag found for the dropped object.");
                        break;
                }
            }
        }
    }
}
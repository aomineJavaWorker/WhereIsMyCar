using UnityEngine;
using UnityEngine.EventSystems;

public class DropPllaceScript : MonoBehaviour, IDropHandler
{
    private float placeZRot, carZRot, diffZRot;
    private Vector3 placeSize, carSize;
    private float xSizeDiff, ySizeDiff;
    public GameObjectsScript gameObjectsScript;

    void Awake()
    {
        gameObjectsScript = Object.FindFirstObjectByType<GameObjectsScript>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        if (eventData.pointerDrag.CompareTag(tag))
        {
            placeZRot = eventData.pointerDrag
                .GetComponent<RectTransform>().eulerAngles.z;

            carZRot = GetComponent<RectTransform>().eulerAngles.z;

            diffZRot = Mathf.Abs(placeZRot - carZRot);

            placeSize = eventData.pointerDrag
                .GetComponent<RectTransform>().localScale;

            carSize = GetComponent<RectTransform>().localScale;

            xSizeDiff = Mathf.Abs(placeSize.x - carSize.x);
            ySizeDiff = Mathf.Abs(placeSize.y - carSize.y);

            Debug.Log("Diff Z Rot: " + diffZRot);
            Debug.Log("Diff X Size: " + xSizeDiff);
            Debug.Log("Diff Y Size: " + ySizeDiff);

            if ((diffZRot <= 8 || diffZRot >= 352) &&
                xSizeDiff <= 0.08f &&
                ySizeDiff <= 0.08f)
            {
                Debug.Log("Car placed correctly!");

                gameObjectsScript.inRightPlace = true;

                RectTransform draggedRect =
                    eventData.pointerDrag.GetComponent<RectTransform>();

                RectTransform placeRect =
                    GetComponent<RectTransform>();

                draggedRect.anchoredPosition =
                    placeRect.anchoredPosition;

                draggedRect.localScale =
                    placeRect.localScale;

                draggedRect.localRotation =
                    placeRect.localRotation;

                switch (eventData.pointerDrag.tag)
                {
                    case "Garbage":
                        gameObjectsScript.carSoundSource
                            .PlayOneShot(gameObjectsScript.sounds[1]);
                        break;

                    case "Ambulance":
                        gameObjectsScript.carSoundSource
                            .PlayOneShot(gameObjectsScript.sounds[2]);
                        break;

                    case "School":
                        gameObjectsScript.carSoundSource
                            .PlayOneShot(gameObjectsScript.sounds[3]);
                        break;
                }
            }
            else
            {
                Debug.Log("Wrong rotation or size!");

                gameObjectsScript.inRightPlace = false;

                gameObjectsScript.carSoundSource
                    .PlayOneShot(gameObjectsScript.sounds[4]);

                ReturnToSpawn(eventData.pointerDrag);
            }
        }
        else
        {
            Debug.Log("Wrong place!");

            gameObjectsScript.inRightPlace = false;

            gameObjectsScript.carSoundSource
                .PlayOneShot(gameObjectsScript.sounds[4]);

            ReturnToSpawn(eventData.pointerDrag);
        }
    }

    private void ReturnToSpawn(GameObject car)
    {
        switch (car.tag)
        {
            case "Garbage":
                gameObjectsScript.garbageTruck
                    .GetComponent<RectTransform>().localPosition =
                    gameObjectsScript.garbageTruckCoord;
                break;

            case "Ambulance":
                gameObjectsScript.medicine
                    .GetComponent<RectTransform>().localPosition =
                    gameObjectsScript.medicineCoord;
                break;

            case "School":
                gameObjectsScript.schoolBuss
                    .GetComponent<RectTransform>().localPosition =
                    gameObjectsScript.schoolBussCoord;
                break;
        }
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

using UnityEngine;
using UnityEngine.EventSystems;

public class DropPlaceScript : MonoBehaviour, IDropHandler
{
    private const float SizeTolerance = 0.08f;
    private const float AngleTolerance = 7f;
    private GameManager gameManager;

    public GameObjectsScript gameObjectsScript;

    void Start()
    {
        if (gameObjectsScript == null)
        {
            gameObjectsScript =
                FindFirstObjectByType<GameObjectsScript>();
        }

        gameManager =
            FindFirstObjectByType<GameManager>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (gameManager != null && gameManager.GameEnded)
            return;

        if (eventData.pointerDrag == null ||
            !eventData.pointerDrag.TryGetComponent(out DragAndDropScript draggedCar))
            return;

        if (draggedCar.isPlaced || draggedCar.IsBeingDestroyed) return;
        if (gameObjectsScript != null) gameObjectsScript.inRightPlace = false;
        if (!draggedCar.CompareTag(tag))
        {
            // Return the actual dragged instance before playing feedback.
            draggedCar.ReturnToStart();
            PlaySound(4);
            return;
        }

        RectTransform car = draggedCar.GetComponent<RectTransform>();
        RectTransform place = GetComponent<RectTransform>();
        if (car == null || place == null) return;
        bool sizeCorrect =
            Mathf.Abs(Mathf.Abs(car.localScale.x) - Mathf.Abs(place.localScale.x)) <= SizeTolerance &&
            Mathf.Abs(Mathf.Abs(car.localScale.y) - Mathf.Abs(place.localScale.y)) <= SizeTolerance;

        // The visual up axis measures the in-plane (Z) rotation, including Y flips.
        bool rotationCorrect = Vector3.Angle(
            car.TransformVector(Vector3.up),
            place.TransformVector(Vector3.up)) <= AngleTolerance;

        // Spawn and Space both encode mirroring in the sign of localScale.x.
        bool flipCorrect = (car.localScale.x < 0f) == (place.localScale.x < 0f);

        if (!sizeCorrect || !rotationCorrect || !flipCorrect || !IsSameVisualDirection(car, place))
            return;

        if (gameObjectsScript != null) gameObjectsScript.inRightPlace = true;

        car.position = place.position;
        car.localScale = place.localScale;
        car.localRotation = place.localRotation;

        bool firstPlacement =
            draggedCar.ConfirmPlacement();

        PlayCorrectSound(draggedCar.tag);

        if (firstPlacement && gameManager != null)
        {
            gameManager.VehiclePlaced();
        }
    }

    private static bool IsSameVisualDirection(RectTransform car, RectTransform place)
    {
        // TransformVector includes both signed scale and rotation (unlike TransformDirection).
        // Negative X and a 180-degree Y rotation therefore give the same visual right axis.
        return Vector3.Angle(
            car.TransformVector(Vector3.right),
            place.TransformVector(Vector3.right)) <= AngleTolerance;
    }

    private void PlaySound(int index)
    {
        if (gameObjectsScript == null || gameObjectsScript.carSoundSource == null ||
            gameObjectsScript.sounds == null || index < 0 || index >= gameObjectsScript.sounds.Length ||
            gameObjectsScript.sounds[index] == null) return;
        gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[index]);
    }

    private void PlayCorrectSound(string objectTag)
    {
        switch (objectTag)
        {
            case "Garbage":

                PlaySound(1);

                break;


            case "Ambulance":

                PlaySound(2);

                break;


            case "School":

                PlaySound(3);

                break;


            case "Cement":

                PlaySound(4);

                break;


            case "Ugunsdzeseji":

                PlaySound(5);

                break;


            case "e61":

                PlaySound(6);

                break;


            case "e46":

                PlaySound(7);

                break;


            case "viegla":

                PlaySound(8);

                break;


            case "Eskavators":

                PlaySound(9);

                break;


            case "Traktors_Dzeltens":

                PlaySound(9);

                break;


            case "Traktors_Zals":

                PlaySound(10);

                break;


            case "Policija":

                PlaySound(11);

                break;


            default:

                Debug.Log(
                    "No sound for tag: " +
                    objectTag
                );

                break;
        }
    }
}

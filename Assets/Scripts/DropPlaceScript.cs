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
        if (eventData.pointerDrag == null ||
            !eventData.pointerDrag.TryGetComponent(out DragAndDropScript draggedCar))
            return;

        gameObjectsScript.inRightPlace = false;
        if (!draggedCar.CompareTag(tag))
        {
            // Return the actual dragged instance before playing feedback.
            draggedCar.ReturnToStart();
            gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[4]);
            return;
        }

        RectTransform car = draggedCar.GetComponent<RectTransform>();
        RectTransform place = GetComponent<RectTransform>();
        bool sizeCorrect =
            Mathf.Abs(Mathf.Abs(car.localScale.x) - Mathf.Abs(place.localScale.x)) <= SizeTolerance &&
            Mathf.Abs(Mathf.Abs(car.localScale.y) - Mathf.Abs(place.localScale.y)) <= SizeTolerance;

        // The visual up axis measures the in-plane (Z) rotation, including Y flips.
        bool rotationCorrect = Vector3.Angle(
            car.TransformVector(Vector3.up),
            place.TransformVector(Vector3.up)) <= AngleTolerance;

        if (!sizeCorrect || !rotationCorrect || !IsSameVisualDirection(car, place))
            return;

        gameObjectsScript.inRightPlace = true;

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

    private void PlayCorrectSound(string objectTag)
    {
        switch (objectTag)
        {
            case "Garbage":

                gameObjectsScript.carSoundSource.PlayOneShot(
                    gameObjectsScript.sounds[1]
                );

                break;


            case "Ambulance":

                gameObjectsScript.carSoundSource.PlayOneShot(
                    gameObjectsScript.sounds[2]
                );

                break;


            case "School":

                gameObjectsScript.carSoundSource.PlayOneShot(
                    gameObjectsScript.sounds[3]
                );

                break;


            case "Cement":

                gameObjectsScript.carSoundSource.PlayOneShot(
                    gameObjectsScript.sounds[4]
                );

                break;


            case "Ugunsdzeseji":

                gameObjectsScript.carSoundSource.PlayOneShot(
                    gameObjectsScript.sounds[5]
                );

                break;


            case "e61":

                gameObjectsScript.carSoundSource.PlayOneShot(
                    gameObjectsScript.sounds[6]
                );

                break;


            case "e46":

                gameObjectsScript.carSoundSource.PlayOneShot(
                    gameObjectsScript.sounds[7]
                );

                break;


            case "viegla":

                gameObjectsScript.carSoundSource.PlayOneShot(
                    gameObjectsScript.sounds[8]
                );

                break;


            case "Eskavators":

                gameObjectsScript.carSoundSource.PlayOneShot(
                    gameObjectsScript.sounds[9]
                );

                break;


            case "Traktors_Dzeltens":

                gameObjectsScript.carSoundSource.PlayOneShot(
                    gameObjectsScript.sounds[9]
                );

                break;


            case "Traktors_Zals":

                gameObjectsScript.carSoundSource.PlayOneShot(
                    gameObjectsScript.sounds[10]
                );

                break;


            case "Policija":

                gameObjectsScript.carSoundSource.PlayOneShot(
                    gameObjectsScript.sounds[11]
                );

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

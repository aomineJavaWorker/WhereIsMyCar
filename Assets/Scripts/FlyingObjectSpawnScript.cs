using UnityEngine;

public class FlyingObjectSpawnScript : MonoBehaviour
{
    private ScreenBounderiesScript screenBoundariesScript;
    private GameManager gameManager;

    [Header("Cloud Prefabs")]
    public GameObject[] cloudPrefabs;
    [Header("Plain Prefabs")]
    public GameObject[] plainPrefabs;
    [Header("Spawn Point")]
    public Transform spawnPoint;
    [Header("Spawn Interval")]
    public float cloudSpawnInterval = 2f;
    public float plainSpawnInterval = 3f;

    private float minY, maxY;

    [Header("Cloud Speed")]
    public float cloudMinSpeed = 1.5f;
    public float cloudMaxSpeed = 150f;
    [Header("Plain Speed")]
    public float plainMinSpeed = 2f;
    public float plainMaxSpeed = 150f;

    private void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        screenBoundariesScript = FindFirstObjectByType<ScreenBounderiesScript>();
        if (spawnPoint == null || screenBoundariesScript == null)
        {
            Debug.LogError("FlyingObjectSpawnScript: spawnPoint or ScreenBounderiesScript is missing.");
            enabled = false;
            return;
        }

        UpdateVerticalBounds();
        if (cloudSpawnInterval > 0f)
            InvokeRepeating(nameof(SpawnCloud), 0f, cloudSpawnInterval);
        if (plainSpawnInterval > 0f)
            InvokeRepeating(nameof(SpawnPlain), 0f, plainSpawnInterval);
    }

    private void UpdateVerticalBounds()
    {
        screenBoundariesScript.RecalculateBounds();
        minY = screenBoundariesScript.minY;
        maxY = screenBoundariesScript.maxY;
    }

    private void SpawnCloud()
    {
        if (gameManager != null && gameManager.GameEnded) return;
        if (cloudPrefabs == null || cloudPrefabs.Length == 0) return;
        UpdateVerticalBounds();
        GameObject prefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];
        if (prefab == null) return;
        Vector3 pos = new Vector3(spawnPoint.position.x,
            Random.Range(minY, maxY), spawnPoint.position.z);
        GameObject cloud = Instantiate(prefab, pos, Quaternion.identity, spawnPoint);
        FlyingObjectControllerScript controller = cloud.GetComponent<FlyingObjectControllerScript>();
        if (controller != null)
            controller.speed = Random.Range(cloudMinSpeed, cloudMaxSpeed);
    }

    private void SpawnPlain()
    {
        if (gameManager != null && gameManager.GameEnded) return;
        if (plainPrefabs == null || plainPrefabs.Length == 0) return;
        UpdateVerticalBounds();
        GameObject prefab = plainPrefabs[Random.Range(0, plainPrefabs.Length)];
        if (prefab == null) return;
        Vector3 pos = new Vector3(-spawnPoint.position.x,
            Random.Range(minY, maxY), spawnPoint.position.z);
        GameObject plain = Instantiate(prefab, pos, Quaternion.identity, spawnPoint);
        FlyingObjectControllerScript controller = plain.GetComponent<FlyingObjectControllerScript>();
        if (controller != null)
            controller.speed = -Random.Range(plainMinSpeed, plainMaxSpeed);
    }
}

using UnityEngine;

public class FlyingObjectSpawnScript : MonoBehaviour
{
    ScreenBounderiesScript screenBoundariesScript;
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
    public float plainMinSpeed = 2;
    public float plainMaxSpeed = 150f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        screenBoundariesScript = Object.FindFirstObjectByType<ScreenBounderiesScript>();
        minY = screenBoundariesScript.minY;
        maxY = screenBoundariesScript.maxY;
        InvokeRepeating(nameof(SpawnCloud), 0f, cloudSpawnInterval);
        InvokeRepeating(nameof(SpawnPlain), 0f, plainSpawnInterval);
    }

    void SpawnCloud()
    {
        if (gameManager != null && gameManager.GameEnded) return;
        if (cloudPrefabs.Length == 0) return;
        GameObject prefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];
        float y = Random.Range(minY, maxY);
        Vector3 pos = new Vector3(spawnPoint.position.x, y, spawnPoint.position.z);
        GameObject cloud = Instantiate(prefab, pos, Quaternion.identity, spawnPoint);
        float speed = Random.Range(cloudMinSpeed, cloudMaxSpeed);

        FlyingObjectControllerScript controller =
            cloud.GetComponent<FlyingObjectControllerScript>();

        if (controller != null)
            controller.speed = speed;
    }

    void SpawnPlain()
    {
        if (gameManager != null && gameManager.GameEnded) return;
        if (plainPrefabs.Length == 0) return;
        GameObject prefab = plainPrefabs[Random.Range(0, plainPrefabs.Length)];
        float y = Random.Range(minY, maxY);
        Vector3 pos = new Vector3(-spawnPoint.position.x, y, spawnPoint.position.z);
        GameObject plain = Instantiate(prefab, pos, Quaternion.identity, spawnPoint);
        float speed = Random.Range(plainMinSpeed, plainMaxSpeed);

        FlyingObjectControllerScript controller =
            plain.GetComponent<FlyingObjectControllerScript>();

        if (controller != null)
            controller.speed = -speed;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

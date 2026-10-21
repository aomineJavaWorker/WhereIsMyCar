using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Vehicles")]
    public int totalVehicles = 12;
    private int placedVehicles = 0;
    private int destroyedVehicles = 0;

    [Header("Timer")]
    public TMP_Text timerText;
    private float elapsedTime = 0f;

    [Header("Result UI")]
    public GameObject winPanel;
    public TMP_Text resultTitle;
    public TMP_Text winTimeText;

    [Header("Stars")]
    public Image star1;
    public Image star2;
    public Image star3;

    [Header("Lives")]
    public Image[] hearts;

    public Sprite yellowStar;
    public Sprite grayStar;
    public AudioClip winMusic;
    public AudioClip loseMusic;

    private AudioSource backgroundMusicSource;
    private bool gameFinished = false;
    private int displayedSeconds = -1;
    public bool GameEnded => gameFinished;

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Application.targetFrameRate = 60;
#endif
        Time.timeScale = 1f;
        elapsedTime = 0f;
        placedVehicles = 0;
        destroyedVehicles = 0;
        gameFinished = false;
        GameObjectsScript.isDragging = false;
        GameObjectsScript.lastDragged = null;
        UpdateHearts();
        if (winPanel != null) winPanel.SetActive(false);
        SetStars(0);
        UpdateTimerText();
        GameObject backgroundMusic = GameObject.Find("BackgroundMusic");
        if (backgroundMusic != null)
            backgroundMusicSource = backgroundMusic.GetComponent<AudioSource>();
        else
            Debug.LogWarning("BackgroundMusic object not found!");
    }

    private void PlayResultSound(AudioClip clip)
    {
        if (backgroundMusicSource == null || clip == null) return;
        backgroundMusicSource.Stop();
        backgroundMusicSource.clip = clip;
        backgroundMusicSource.loop = false;
        backgroundMusicSource.Play();
    }

    private void Update()
    {
        if (gameFinished) return;
        elapsedTime += Time.deltaTime;
        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        if (timerText == null) return;
        int seconds = Mathf.FloorToInt(elapsedTime);
        if (seconds == displayedSeconds) return;
        displayedSeconds = seconds;
        TimeSpan time = TimeSpan.FromSeconds(elapsedTime);
        timerText.text = time.ToString(@"hh\:mm\:ss");
    }

    public void VehiclePlaced()
    {
        if (gameFinished) return;
        placedVehicles++;
        Debug.Log("Placed: " + placedVehicles + " / " + GetRequiredVehicles());
        CheckWin();
    }

    public void VehicleDestroyed()
    {
        if (gameFinished) return;
        destroyedVehicles++;
        UpdateHearts();
        Debug.Log("Destroyed vehicles: " + destroyedVehicles);
        if (destroyedVehicles >= 4)
        {
            LoseGame();
            return;
        }
        CheckWin();
    }

    private int GetRequiredVehicles() => Mathf.Max(0, totalVehicles - destroyedVehicles);

    private void CheckWin()
    {
        if (!gameFinished && placedVehicles >= GetRequiredVehicles())
            WinGame();
    }

    private void WinGame()
    {
        if (gameFinished) return;
        gameFinished = true;
        StopInteraction();
        int stars = CalculateStars();
        if (resultTitle != null) resultTitle.text = "UZVARA!";
        SetFinalTime();
        SetStars(stars);
        ShowResultPanel();
        Debug.Log("UZVARA! Time: " + elapsedTime + " | Destroyed: " +
            destroyedVehicles + " | Stars: " + stars);
        PlayResultSound(winMusic);
        Time.timeScale = 0f;
    }

    private void LoseGame()
    {
        if (gameFinished) return;
        gameFinished = true;
        StopInteraction();
        if (resultTitle != null) resultTitle.text = "ZAUDĒJUMS!";
        SetFinalTime();
        SetStars(0);
        ShowResultPanel();
        Debug.Log("ZAUDĒJUMS! Destroyed vehicles: " + destroyedVehicles);
        PlayResultSound(loseMusic);
        Time.timeScale = 0f;
    }

    private void StopInteraction()
    {
        GameObjectsScript.isDragging = false;
        GameObjectsScript.lastDragged = null;
    }

    private void ShowResultPanel()
    {
        if (winPanel == null) return;
        winPanel.transform.SetAsLastSibling();
        winPanel.SetActive(true);
    }

    private int CalculateStars()
    {
        // Your original time thresholds: under 2m = 3, under 3m = 2, else 1.
        int starsByTime = elapsedTime < 120f ? 3 : (elapsedTime < 180f ? 2 : 1);
        // Your intended damage thresholds: first lost car caps at 2 stars,
        // second/third lost car caps at 1 star, fourth means defeat.
        int starsByDamage = destroyedVehicles == 0 ? 3 :
            (destroyedVehicles == 1 ? 2 : 1);
        return Mathf.Min(starsByTime, starsByDamage);
    }

    private void SetFinalTime()
    {
        if (winTimeText == null) return;
        TimeSpan finalTime = TimeSpan.FromSeconds(elapsedTime);
        winTimeText.text = finalTime.ToString(@"hh\:mm\:ss");
    }

    private void SetStars(int stars)
    {
        if (star1 != null) star1.sprite = stars >= 1 ? yellowStar : grayStar;
        if (star2 != null) star2.sprite = stars >= 2 ? yellowStar : grayStar;
        if (star3 != null) star3.sprite = stars >= 3 ? yellowStar : grayStar;
    }

    private void UpdateHearts()
    {
        if (hearts == null) return;
        int remainingLives = Mathf.Max(0, 4 - destroyedVehicles);
        for (int i = 0; i < hearts.Length; i++)
            if (hearts[i] != null) hearts[i].gameObject.SetActive(i < remainingLives);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}

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
    private bool gameFinished = false;

    [Header("Win Panel")]
    public GameObject winPanel;
    public TMP_Text winTimeText;

    [Header("Stars")]
    public Image star1;
    public Image star2;
    public Image star3;

    public Color activeStarColor = Color.yellow;
    public Color inactiveStarColor = Color.gray;


    private void Start()
    {
        Time.timeScale = 1f;

        elapsedTime = 0f;
        placedVehicles = 0;
        destroyedVehicles = 0;
        gameFinished = false;

        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        UpdateTimerText();
    }


    private void Update()
    {
        if (gameFinished)
            return;

        elapsedTime += Time.deltaTime;

        UpdateTimerText();
    }


    private void UpdateTimerText()
    {
        if (timerText == null)
            return;

        TimeSpan time = TimeSpan.FromSeconds(elapsedTime);

        timerText.text = time.ToString(@"hh\:mm\:ss");
    }


    public void VehiclePlaced()
    {
        if (gameFinished)
            return;

        placedVehicles++;

        Debug.Log(
            "Placed: " + placedVehicles +
            " / Required: " +
            (totalVehicles - destroyedVehicles)
        );

        CheckWin();
    }


    public void VehicleDestroyed()
    {
        if (gameFinished)
            return;

        destroyedVehicles++;

        Debug.Log(
            "Destroyed: " + destroyedVehicles +
            " / Total: " + totalVehicles
        );

        CheckWin();
    }


    private void CheckWin()
    {
        int requiredVehicles =
            totalVehicles - destroyedVehicles;

        if (placedVehicles >= requiredVehicles)
        {
            WinGame();
        }
    }


    private void WinGame()
    {
        gameFinished = true;

        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        if (winTimeText != null)
        {
            TimeSpan finalTime =
                TimeSpan.FromSeconds(elapsedTime);

            winTimeText.text =
                finalTime.ToString(@"hh\:mm\:ss");
        }

        int stars = CalculateStars();

        ShowStars(stars);

        Debug.Log(
            "UZVARA! Time: " +
            elapsedTime +
            " Stars: " +
            stars
        );

        Time.timeScale = 0f;
    }


    private int CalculateStars()
    {
        if (elapsedTime <= 60f)
        {
            return 3;
        }

        if (elapsedTime <= 120f)
        {
            return 2;
        }

        if (elapsedTime <= 180f)
        {
            return 1;
        }

        return 0;
    }


    private void ShowStars(int stars)
    {
        if (star1 != null)
        {
            star1.color =
                stars >= 1
                ? activeStarColor
                : inactiveStarColor;
        }

        if (star2 != null)
        {
            star2.color =
                stars >= 2
                ? activeStarColor
                : inactiveStarColor;
        }

        if (star3 != null)
        {
            star3.color =
                stars >= 3
                ? activeStarColor
                : inactiveStarColor;
        }
    }


    public void RestartGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }


    public void GoToMainMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("MainMenu");
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sound controller for the existing HanojasTornis scene.
/// Add only this component to the HanojasCanvas GameObject.
/// No changes to HanojasTornis.cs or HanoiDiskDrag.cs are required.
/// Sounds are loaded from Assets/Resources/HanoiSounds at runtime.
/// </summary>
[DisallowMultipleComponent]
public sealed class HanoiAudio : MonoBehaviour
{
    [Header("Optional overrides (empty = use Resources/HanoiSounds)")]
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private AudioClip dropClip;
    [SerializeField] private AudioClip invalidClip;
    [SerializeField] private AudioClip buttonClip;
    [SerializeField] private AudioClip victoryClip;
    [SerializeField] private AudioClip musicClip;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float soundEffectsVolume = 0.75f;
    [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.16f;
    [SerializeField, Range(0f, 1f)] private float winMusicVolumeMultiplier = 0.20f;

    private HanojasTornis game;
    private AudioSource soundSource;
    private AudioSource musicSource;
    private readonly List<Button> connectedButtons = new List<Button>();
    private int previousMoves;
    private bool previousWin;
    private bool previousBonus;
    private bool pausedForAd;
    private bool pausedByApplication;

    private void Awake()
    {
        game = GetComponent<HanojasTornis>();
        if (game == null) game = GetComponentInParent<HanojasTornis>();

        pickupClip = pickupClip != null ? pickupClip : Resources.Load<AudioClip>("HanoiSounds/click");
        dropClip = dropClip != null ? dropClip : Resources.Load<AudioClip>("HanoiSounds/drop");
        invalidClip = invalidClip != null ? invalidClip : Resources.Load<AudioClip>("HanoiSounds/click");
        buttonClip = buttonClip != null ? buttonClip : Resources.Load<AudioClip>("HanoiSounds/onButton");
        victoryClip = victoryClip != null ? victoryClip : Resources.Load<AudioClip>("HanoiSounds/victory");
        musicClip = musicClip != null ? musicClip : Resources.Load<AudioClip>("HanoiSounds/theTower");

        soundSource = gameObject.AddComponent<AudioSource>();
        soundSource.playOnAwake = false;
        soundSource.spatialBlend = 0f;
        soundSource.volume = soundEffectsVolume;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.loop = true;
        musicSource.volume = backgroundMusicVolume;
        musicSource.clip = musicClip;
    }

    private void Start()
    {
        if (game == null)
        {
            Debug.LogError("HanoiAudio: attach this component to HanojasCanvas, together with HanojasTornis.", this);
            enabled = false;
            return;
        }

        previousMoves = game.Puzzle.Moves;
        previousWin = game.Puzzle.IsWon;
        previousBonus = game.Puzzle.BonusUnlocked;

        // Automatically attach sound hooks to all 4/5/6 disk objects.
        if (game.disks != null)
        {
            for (int i = 0; i < game.disks.Length; i++)
            {
                Image disk = game.disks[i];
                if (disk == null) continue;
                HanoiDiskSound hook = disk.GetComponent<HanoiDiskSound>();
                if (hook == null) hook = disk.gameObject.AddComponent<HanoiDiskSound>();
                hook.Initialize(this, game, i + 1);
            }
        }

        // Includes the difficulty panel, Restart, Main Menu, and rewarded ad button.
        // HanojasTornis builds its dynamic buttons in Awake, before this Start.
        Button[] buttons = game.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null) continue;
            button.onClick.AddListener(PlayButton);
            connectedButtons.Add(button);
        }

        if (musicSource.clip != null) musicSource.Play();
        else Debug.LogWarning("HanoiAudio: theTower.mp3 missing in Assets/Resources/HanoiSounds.");
    }

    private void Update()
    {
        if (game == null) return;

        bool fullscreenAd = AdManager.Instance != null &&
            AdManager.Instance.bannerAd != null &&
            AdManager.Instance.bannerAd.IsFullscreenAdActive;

        if (fullscreenAd && !pausedForAd)
        {
            pausedForAd = true;
            if (musicSource.isPlaying) musicSource.Pause();
        }
        else if (!fullscreenAd && pausedForAd)
        {
            pausedForAd = false;
            if (!pausedByApplication && musicSource.clip != null) musicSource.UnPause();
        }

        bool won = game.Puzzle.IsWon;
        int moves = game.Puzzle.Moves;
        bool bonus = game.Puzzle.BonusUnlocked;

        if (won && !previousWin)
        {
            Play(victoryClip, 1f);
            musicSource.volume = backgroundMusicVolume * winMusicVolumeMultiplier;
        }
        else if (moves > previousMoves && !won)
        {
            Play(dropClip, 0.9f);
        }

        if (bonus && !previousBonus)
            Play(buttonClip, 1f);

        if (!won && previousWin)
            musicSource.volume = backgroundMusicVolume;

        previousMoves = moves;
        previousWin = won;
        previousBonus = bonus;
    }

    private void Play(AudioClip clip, float multiplier)
    {
        if (clip != null && soundSource != null && !pausedForAd && !pausedByApplication)
            soundSource.PlayOneShot(clip, multiplier);
    }

    public void PlayPickup() => Play(pickupClip, 0.75f);
    public void PlayInvalidMove() => Play(invalidClip, 0.45f);
    public void PlayButton() => Play(buttonClip, 0.85f);

    private void OnApplicationPause(bool paused)
    {
        pausedByApplication = paused;
        if (musicSource == null) return;
        if (paused) musicSource.Pause();
        else if (!pausedForAd && musicSource.clip != null) musicSource.UnPause();
    }

    private void OnDestroy()
    {
        foreach (Button button in connectedButtons)
            if (button != null) button.onClick.RemoveListener(PlayButton);
        connectedButtons.Clear();
    }
}

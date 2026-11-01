using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A companion to HanoiDiskDrag: listens to the same UI drag events,
/// but never changes the puzzle state or the position of a disk.
/// Installed automatically on each disk by HanoiAudio.
/// </summary>
public sealed class HanoiDiskSound : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    private HanoiAudio audioController;
    private HanojasTornis game;
    private int diskNumber;
    private int movesAtGrab;
    private bool validGrab;

    public void Initialize(HanoiAudio audio, HanojasTornis controller, int number)
    {
        audioController = audio;
        game = controller;
        diskNumber = number;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        validGrab = game != null && game.CanDragDisk(diskNumber, out _);
        if (!validGrab) return;
        movesAtGrab = game.Puzzle.Moves;
        if (audioController != null) audioController.PlayPickup();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!validGrab) return;
        validGrab = false;
        StartCoroutine(CheckDropNextFrame(movesAtGrab));
    }

    private IEnumerator CheckDropNextFrame(int before)
    {
        // Wait one frame so HanoiDiskDrag.OnEndDrag can apply the move,
        // regardless of the order Unity invokes the two handlers.
        yield return null;
        if (game != null && game.Puzzle.Moves == before &&
            audioController != null && !game.Puzzle.IsWon)
            audioController.PlayInvalidMove();
    }

    private void OnDisable() => validGrab = false;
}

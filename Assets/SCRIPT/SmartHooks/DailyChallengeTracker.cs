using UnityEngine;

// Bridges semantic gameplay events into the persisted Daily Challenge state.
// It owns no presentation and is installed once by ExtrasRuntimeWiring.
[DisallowMultipleComponent]
public sealed class DailyChallengeTracker : MonoBehaviour
{
    void OnEnable()
    {
        // Remove first so domain reload / enable cycles cannot double-count a
        // single gameplay event.
        GameEvents.OnMatchCompleted -= OnMatchCompleted;
        GameEvents.OnCorrectGuess -= OnCorrectGuess;
        GameEvents.OnRoomShared -= OnRoomShared;

        GameEvents.OnMatchCompleted += OnMatchCompleted;
        GameEvents.OnCorrectGuess += OnCorrectGuess;
        GameEvents.OnRoomShared += OnRoomShared;

        // Opening a new UTC day resets stale mission counters even before the
        // player visits the Daily Challenge screen.
        _ = DailyChallengeProgress.Current;
    }

    void OnDisable()
    {
        GameEvents.OnMatchCompleted -= OnMatchCompleted;
        GameEvents.OnCorrectGuess -= OnCorrectGuess;
        GameEvents.OnRoomShared -= OnRoomShared;
    }

    static void OnMatchCompleted(MatchOutcome outcome)
    {
        if (outcome.Outcome == MatchOutcome.Result.Win)
            DailyChallengeProgress.RecordWin();
    }

    static void OnCorrectGuess()
    {
        DailyChallengeProgress.RecordCorrectGuess();
    }

    static void OnRoomShared()
    {
        DailyChallengeProgress.RecordRoomShared();
    }
}

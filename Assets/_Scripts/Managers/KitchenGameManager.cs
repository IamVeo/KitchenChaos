using System;
using UnityEngine;

public class KitchenGameManager : MonoBehaviour {


    public static KitchenGameManager Instance { get; private set; }

    public class RunStartedEventArgs : EventArgs
    {
        public string runId;
    }

    public class RunEndedEventArgs : EventArgs
    {
        public string runId;
        public RunEndReason reason;
    }

    public event EventHandler OnStateChanged;
    public event EventHandler OnGamePaused;
    public event EventHandler OnGameUnpaused;
    public event EventHandler<RunStartedEventArgs> OnRunStarted;
    public event EventHandler<RunEndedEventArgs> OnRunEnded;

    public string CurrentRunId => currentRunId;

    private enum State {
        WaitingToStart,
        CountdownToStart,
        GamePlaying,
        GameOver,
    }


    private State state;
    private float countdownToStartTimer;
    private float gamePlayingTimer;
    private float gamePlayingTimerMax = 300f;
    private bool isGamePaused;
    private string currentRunId;


    private void Awake() {
        Instance = this;

        state = State.WaitingToStart;
    }

    private void Start() {
        GameInput.Instance.OnPauseAction += GameInput_OnPauseAction;
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e) {
        if (state == State.WaitingToStart) {
            state = State.CountdownToStart;
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void GameInput_OnPauseAction(object sender, EventArgs e) {
        TogglePauseGame();
    }

    private void Update() {
        switch (state) {
            case State.WaitingToStart:
                break;
            case State.CountdownToStart:
                countdownToStartTimer -= Time.deltaTime;
                if (countdownToStartTimer < 0f) {
                    state = State.GamePlaying;
                    gamePlayingTimer = gamePlayingTimerMax;
                    currentRunId = CreateNewRunId();
                    GameInput.Instance?.SetGameplayInputEnabled(true);
                    OnStateChanged?.Invoke(this, EventArgs.Empty);
                    OnRunStarted?.Invoke(this, new RunStartedEventArgs {
                        runId = currentRunId
                    });
                }
                break;
            case State.GamePlaying:
                gamePlayingTimer -= Time.deltaTime;
                if (gamePlayingTimer < 0f) {
                    EnterGameOverState(RunEndReason.GameOver);
                }
                break;
            case State.GameOver:
                break;
        }
    }

    public bool IsGamePlaying() {
        return state == State.GamePlaying;
    }

    public bool IsCountdownToStartActive() {
        return state == State.CountdownToStart;
    }

    public float GetCountdownToStartTimer() {
        return countdownToStartTimer;
    }

    public bool IsGameOver() {
        return state == State.GameOver;
    }

    public float GetGamePlayingTimerNormalized() {
        return 1 - (gamePlayingTimer / gamePlayingTimerMax);
    }

    public void TogglePauseGame() {
        isGamePaused = !isGamePaused;
        if (isGamePaused) {
            Time.timeScale = 0f;

            OnGamePaused?.Invoke(this, EventArgs.Empty);
        } else {
            Time.timeScale = 1f;

            OnGameUnpaused?.Invoke(this, EventArgs.Empty);
        }
    }

    public void SetGameOver() {
        if (state == State.GamePlaying) {
            EnterGameOverState(RunEndReason.GameOver);
        }
    }

    private void EnterGameOverState(RunEndReason reason)
    {
        if (state != State.GamePlaying)
        {
            return;
        }

        state = State.GameOver;
        GameInput.Instance?.SetGameplayInputEnabled(false);
        OnStateChanged?.Invoke(this, EventArgs.Empty);
        OnRunEnded?.Invoke(this, new RunEndedEventArgs {
            runId = currentRunId,
            reason = reason
        });
    }

    private static string CreateNewRunId()
    {
        return $"run_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";
    }
}
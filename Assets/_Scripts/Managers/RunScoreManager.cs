using UnityEngine;

public class RunScoreManager
{
    private static RunScoreManager _instance;

    public static RunScoreManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new RunScoreManager();
            }

            return _instance;
        }
    }

    private const string HIGH_SCORE_PLAYER_PREFS_KEY = "KC_HighScore";

    private int currentRunScore;
    private bool hasRunActive;

    public void BeginRun(string runId)
    {
        currentRunScore = 0;
        hasRunActive = true;
    }

    public void AddScore(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentRunScore += amount;
    }

    public int GetCurrentRunScore()
    {
        return currentRunScore;
    }

    public int GetHighScore()
    {
        return PlayerPrefs.GetInt(HIGH_SCORE_PLAYER_PREFS_KEY, 0);
    }

    public bool TrySetHighScore(int score)
    {
        int currentHighScore = GetHighScore();
        if (score <= currentHighScore)
        {
            return false;
        }

        PlayerPrefs.SetInt(HIGH_SCORE_PLAYER_PREFS_KEY, score);
        PlayerPrefs.Save();
        return true;
    }

    public void FinalizeRunOnGameOver()
    {
        if (!hasRunActive)
        {
            return;
        }

        TrySetHighScore(currentRunScore);
        hasRunActive = false;
    }

    public void ResetRunLocalState()
    {
        currentRunScore = 0;
        hasRunActive = false;
    }

    public void ResetHighScoreForDebug()
    {
        PlayerPrefs.DeleteKey(HIGH_SCORE_PLAYER_PREFS_KEY);
        PlayerPrefs.Save();
    }
}

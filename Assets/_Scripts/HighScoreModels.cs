using System;

[Serializable]
public class HighScoreUpdateRequest
{
    public int score;
}

[Serializable]
public class HighScoreUpdateResponse
{
    public string username;
    public int previousHighScore;
    public int currentHighScore;
    public bool updated;
}


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

[Serializable]
public class TopHighScoreEntry
{
    public string username;
    public int highScore;
}

[Serializable]
public class HighScoreMeResponse
{
    public string username;
    public int highScore;
    public int rank;
}

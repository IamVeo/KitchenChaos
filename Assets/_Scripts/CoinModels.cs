using System;

[Serializable]
public class UpdateCoinDeltaRequest
{
    public int delta;
}

[Serializable]
public class UpdateCoinDeltaResponse
{
    public int previousCoins;
    public int deltaApplied;
    public int currentCoins;
}


using System;

[Serializable]
public struct DeferredRewardTotals
{
    public int totalCoin;
    public int totalScore;

    public static DeferredRewardTotals Empty()
    {
        return new DeferredRewardTotals();
    }
}

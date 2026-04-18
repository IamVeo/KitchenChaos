using System;

[Serializable]
public sealed class DeferredRewardEntry
{
    public int coinAmount;
    public int scoreAmount;

    public static DeferredRewardEntry Create(int coinAmount, int scoreAmount)
    {
        return new DeferredRewardEntry
        {
            coinAmount = coinAmount,
            scoreAmount = scoreAmount
        };
    }
}

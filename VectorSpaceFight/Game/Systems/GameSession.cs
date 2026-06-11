namespace VectorSpaceFight.Game.Systems;

public sealed class GameSession
{
    public int HumanCount { get; set; }
    public bool[] IsHuman { get; } = new bool[PlayerRoster.Count];

    public void ApplyClaimedSlots(Func<int, bool> isPlayerConnected)
    {
        HumanCount = 0;
        for (var i = 0; i < PlayerRoster.Count; i++)
        {
            IsHuman[i] = isPlayerConnected(i);
            if (IsHuman[i])
                HumanCount++;
        }
    }
}

namespace Themis.Server
{
    public enum GameMessageType
    {
        PlayerMove,
        Ball,
        Player,
        GameState,
        PlayerHit,


#region Test

        ChangeBallSpeed,

#endregion

        Reset,
        Pause,
        Count
    }
}
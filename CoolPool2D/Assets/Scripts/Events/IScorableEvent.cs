public interface IScorableEvent : IGameEventArgs
{
    BallData BallData { get; set; }
    string ScoreTypeHeader { get; set; }
    float ScoreTypePoints { get; set; }
    bool IsFoul { get; set; }
}
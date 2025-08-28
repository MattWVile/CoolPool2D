public interface IScorableEvent : IGameEventArgs
{
    BallData Ball { get; set; }
    string ScoreTypeHeader { get; set; }
    float ScoreTypePoints { get; set; }
    bool IsFoul { get; set; }
}
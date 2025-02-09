public interface IScorableEvent : IGameEventArgs
{
    Ball Ball { get; set; }
    string ScoreTypeHeader { get; set; }
    float ScoreTypePoints { get; set; }
    bool IsFoul { get; set; }
}
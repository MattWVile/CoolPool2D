public class ScoreType
{
    public string ScoreTypeHeader { get; set; }
    public float ScoreTypePoints { get; set; }
    public float ScoreTypeMultiplierAddition { get; set; }
    public float NumberOfThisScoreType { get; set; }

    public bool IsScoreFoul { get; set; }

    public ScoreType(string scoreTypeHeader, float scoreTypePoints, bool isScoreFoul = false, float numberOfThisScoreType = 1f)
    {
        ScoreTypeHeader = scoreTypeHeader;
        ScoreTypePoints = scoreTypePoints;
        IsScoreFoul = isScoreFoul;
        NumberOfThisScoreType = numberOfThisScoreType;
    }
}

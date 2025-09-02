public class ScoreType
{
    public string ScoreTypeName { get; set; }
    public float ScoreTypePoints { get; set; }
    public float ScoreTypeMultiplierAddition { get; set; }
    public float NumberOfThisScoreType { get; set; }

    public bool IsScoreFoul { get; set; }

    public ScoreType(string scoreTypeName, float scoreTypePoints, bool isScoreFoul = false, float scoreTypeMultiplierAddition = 0, float numberOfThisScoreType = 1f)
    {
        ScoreTypeName = scoreTypeName;
        ScoreTypePoints = scoreTypePoints;
        IsScoreFoul = isScoreFoul;
        ScoreTypeMultiplierAddition = scoreTypeMultiplierAddition;
        NumberOfThisScoreType = numberOfThisScoreType;
    }
}

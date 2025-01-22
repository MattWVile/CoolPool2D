public class ScoreType
{
    public string ScoreTypeName { get; set; }
    public float ScoreTypePoints { get; set; }
    public float ScoreTypeMultiplierAddition { get; set; }
    public float NumberOfThisScoreType { get; set; }

    public bool IsScoreFoul { get; set; }



    public ScoreType(string scoreTypeName, float scoreTypePoints, float scoreTypeMultiplierAddition = 0, float numberOfThisScoreType = 1f, bool isScoreFoul = false)
    {
        ScoreTypeName = scoreTypeName;
        ScoreTypePoints = scoreTypePoints;
        ScoreTypeMultiplierAddition = scoreTypeMultiplierAddition;
        NumberOfThisScoreType = numberOfThisScoreType;
        IsScoreFoul = isScoreFoul;
    }
}

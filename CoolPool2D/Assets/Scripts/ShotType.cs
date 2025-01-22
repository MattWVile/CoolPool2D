public class ShotType
{
    public string ShotTypeName { get; set; }
    public float ShotTypePoints { get; set; }
    public float ShotTypeMultiplierAddition { get; set; }
    public float NumberOfThisShotType { get; set; }

    public bool IsShotFoul { get; set; }



    public ShotType(string shotTypeName, float shotTypePoints, float shotTypeMultiplierAddition = 0, float numberOfThisShotType = 1f, bool isShotFoul = false)
    {
        ShotTypeName = shotTypeName;
        ShotTypePoints = shotTypePoints;
        ShotTypeMultiplierAddition = shotTypeMultiplierAddition;
        NumberOfThisShotType = numberOfThisShotType;
        IsShotFoul = isShotFoul;
    }
}

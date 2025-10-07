
public class PocketBallDuplicator : BaseArtifact<BallPocketedEvent>
{

    public string name = "Pocket Ball Duplicator";
    public string description = "When pocketing a ball, that ball is duplicated next to the pocket on your next shot";

    private PocketLocation lastPocketLocation;
    private BallData lastDuplicatedBallData;
    public void Start()
    {
        EventBus.Subscribe<ScoringFinishedEvent>(OnScoringFinishedEvent);
    }

    protected override void OnEvent(BallPocketedEvent ballPocketedEvent)
    {
        lastPocketLocation = ballPocketedEvent.PocketLocation;
        lastDuplicatedBallData = ballPocketedEvent.BallData;
    }

    private void OnScoringFinishedEvent(ScoringFinishedEvent scoringFinishedEvent)
    {
        BallSpawner.SpawnSpecificColourBall(lastDuplicatedBallData.ballColour, ConvertPocketLocationToSpawnLocation(lastPocketLocation), lastDuplicatedBallData);
    }

    private BallSpawnLocations ConvertPocketLocationToSpawnLocation(PocketLocation pocketLocation)
    {
        return pocketLocation switch
        {
            PocketLocation.TopLeft => BallSpawnLocations.InFrontOfToTopLeftPocket,
            PocketLocation.TopRight => BallSpawnLocations.InFrontOfToTopRightPocket,
            PocketLocation.TopMiddle => BallSpawnLocations.InFrontOfToTopCenterPocket,
            PocketLocation.BottomMiddle => BallSpawnLocations.InFrontOfToBottomCenterPocket,
            PocketLocation.BottomLeft => BallSpawnLocations.InFrontOfToBottomLeftPocket,
            PocketLocation.BottomRight => BallSpawnLocations.InFrontOfToBottomRightPocket,
            _ => BallSpawnLocations.Random
        };
    }
}
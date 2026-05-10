using UnityEngine;

public class BallScoringData : MonoBehaviour
{
    [Header("Ball Settings")]
    public BallVariant ballVariant;

    public float ballPoints = 100f;
    public float ballMultiplier = 1f;

    // Example: expose readonly properties if you want safe access
    public BallVariant BallVariant => ballVariant;
    public float BallPoints => ballPoints;
    public float BallMultiplier => ballMultiplier;

}

public struct BallScoringDataSnapshot
{
    public BallVariant ballVariant;
    public float ballPoints;
    public float ballMultiplier;

    public BallScoringDataSnapshot(BallScoringData data)
    {
        ballVariant = data.ballVariant;
        ballPoints = data.ballPoints;
        ballMultiplier = data.ballMultiplier;
    }
}

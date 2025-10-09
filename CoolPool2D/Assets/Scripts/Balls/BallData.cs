using UnityEngine;

public class BallData : MonoBehaviour
{
    [Header("Ball Settings")]
    public BallColour ballColour;

    public float ballPoints = 100f;
    public float ballMultiplier = 1f;

    // Example: expose readonly properties if you want safe access
    public BallColour BallColour => ballColour;
    public float BallPoints => ballPoints;
    public float BallMultiplier => ballMultiplier;

}

using UnityEngine;

public class BallData
{
    public BallColour BallColour { get; set; }
    public GameObject BallGameObject { get; set; }
    public float BallPoints { get; set; }
    public float BallMultiplier { get; set; }

    public BallData(BallColour ballColour, GameObject ballGameObject, float ballPoints = 100, float ballMultiplier = 1)
    {
        BallColour = ballColour;
        BallGameObject = ballGameObject;
        BallPoints = ballPoints;
        BallMultiplier = ballMultiplier;
    }
}
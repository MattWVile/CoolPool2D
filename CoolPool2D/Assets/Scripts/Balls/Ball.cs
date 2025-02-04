using UnityEngine;

public class Ball
{
    public string BallName { get; set; }
    public GameObject BallGameObject { get; set; }
    public float BallPoints { get; set; }
    public float BallMultiplier { get; set; }

    public Ball(string ballName, GameObject ballGameObject, float ballPoints = 100, float ballMultiplier = 1)
    {
        BallName = ballName;
        BallGameObject = ballGameObject;
        BallPoints = ballPoints;
        BallMultiplier = ballMultiplier;
    }
}
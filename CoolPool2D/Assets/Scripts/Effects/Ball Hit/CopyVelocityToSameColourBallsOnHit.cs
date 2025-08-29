using UnityEngine;

public class CopyVelocityToSameColourBallsOnHit : MonoBehaviour, IOnBallHitEffect
{
    public void OnBallHit(GameObject self, GameObject other)
    {
        foreach (GameObject gameObject in GameManager.Instance.ballGameObjects)
        {
            BallData ballData = gameObject.GetComponent<BallData>();
            DeterministicBall deterministicBall = gameObject.GetComponent<DeterministicBall>();

            if (ballData.BallColour == self.GetComponent<BallData>().BallColour && gameObject != self && gameObject != other)
            {
                deterministicBall.velocity = self.GetComponent<DeterministicBall>().velocity;
            }
        }
    }
}
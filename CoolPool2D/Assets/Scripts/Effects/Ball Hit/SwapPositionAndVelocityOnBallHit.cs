using UnityEngine;

public class SwapPositionAndVelocityOnBallHit : MonoBehaviour, IOnBallHitEffect
{
    public void OnBallHit(GameObject self, GameObject other)
    {
        Vector3 tempPosition = self.transform.position;
        Vector2 tempVelocity = self.GetComponent<DeterministicBall>().velocity;

        self.transform.position = other.transform.position;
        self.GetComponent<DeterministicBall>().velocity = other.GetComponent<DeterministicBall>().velocity;

        other.transform.position = tempPosition;
        other.GetComponent<DeterministicBall>().velocity = tempVelocity;
    }
}

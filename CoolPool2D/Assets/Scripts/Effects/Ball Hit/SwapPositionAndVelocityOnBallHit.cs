using UnityEngine;

public class SwapPositionAndVelocityOnBallHit : MonoBehaviour, IOnBallHitEffect
{
    public float nudgeFactor = 20f;
    public void OnBallHit(GameObject self, GameObject other)
    {
        Vector2 tempPosition = self.transform.position;
        Vector2 tempVelocity = self.GetComponent<DeterministicBall>().velocity;

        Vector2 smallNudge = other.GetComponent<DeterministicBall>().velocity / nudgeFactor;
        Vector2 otherSmallNudge = tempVelocity /nudgeFactor;

        self.transform.position = new Vector2(other.transform.position.x, other.transform.position.y) + smallNudge;
        self.GetComponent<DeterministicBall>().velocity = other.GetComponent<DeterministicBall>().velocity;

        other.transform.position = tempPosition + otherSmallNudge;
        other.GetComponent<DeterministicBall>().velocity = tempVelocity;
    }
}

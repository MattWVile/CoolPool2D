using UnityEngine;

public class SwapPositionAndVelocityOnBallHit : MonoBehaviour, IOnBallHitEffect
{
    public void OnBallHit(GameObject self, GameObject other)
    {
        BallData selfBallData = self.GetComponent<BallData>();

        DeterministicBall selfDeterministicBall = self.GetComponent<DeterministicBall>();
        DeterministicBall otherDeterministicBall = other.GetComponent<DeterministicBall>();

        if (selfBallData.numberOfOnBallHitEffectsTriggeredThisTurn == selfBallData.numberOfOnBallHitEffects)
            return;

        other.transform.position = selfDeterministicBall.stationaryPosition;
        otherDeterministicBall.velocity = selfDeterministicBall.velocity;

        self.transform.position = otherDeterministicBall.stationaryPosition;
        selfDeterministicBall.velocity = otherDeterministicBall.initialVelocity * .9f;

        selfBallData.numberOfOnBallHitEffectsTriggeredThisTurn ++;
    }
}

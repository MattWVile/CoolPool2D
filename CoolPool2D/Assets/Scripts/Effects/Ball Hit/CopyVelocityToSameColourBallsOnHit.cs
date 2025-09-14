using UnityEngine;

public class CopyVelocityToSameColourBallsOnHit : MonoBehaviour, IOnBallHitEffect
{
    public void OnBallHit(GameObject self, GameObject other)
    {
        var selfBallData = self.GetComponent<BallData>();
        if (selfBallData.numberOfOnBallHitEffectsTriggeredThisTurn >= selfBallData.numberOfOnBallHitEffects) return;
        foreach (GameObject gameObject in GameManager.Instance.ballGameObjects)
        {
            BallData ballData = gameObject.GetComponent<BallData>();
            DeterministicBall deterministicBall = gameObject.GetComponent<DeterministicBall>();

            if (ballData.BallColour == selfBallData.BallColour && gameObject != self && gameObject != other)
            {
                deterministicBall.velocity = self.GetComponent<DeterministicBall>().velocity;
            }
        }
        selfBallData.numberOfOnBallHitEffectsTriggeredThisTurn++;
    }
}
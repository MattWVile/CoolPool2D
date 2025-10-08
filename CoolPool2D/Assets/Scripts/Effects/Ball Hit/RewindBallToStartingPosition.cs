
using UnityEngine;

public class RewindBallToStartingPosition : MonoBehaviour, IOnBallHitEffect
{

    //private DeterministicBall deterministicBall;
    public void OnBallHit(GameObject self, GameObject other)
    {
        var selfBallData = self.GetComponent<BallData>();
        DeterministicBall selfDeterministicBall = self.GetComponent<DeterministicBall>();

        if (selfBallData.numberOfOnBallHitEffectsTriggeredThisTurn >= selfBallData.numberOfOnBallHitEffects) return;

        selfBallData.numberOfOnBallHitEffectsTriggeredThisTurn++;
    }
}

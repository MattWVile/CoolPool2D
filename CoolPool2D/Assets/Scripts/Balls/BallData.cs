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

    public void TriggerBallHitEffect(GameObject other)
    {
        var ballHitEffects = GetComponents<IOnBallHitEffect>();
        foreach (var effect in ballHitEffects)
            effect.OnBallHit(base.gameObject, other);
    }

    //public void TriggerPotEffect()
    //{
    //    var effects = GetComponents<IOnBallPotEffect>();
    //    foreach (var effect in effects)
    //        effect.OnBallPot(base.gameObject);
    //}

    //public void TriggerRailEffect(RailLocation rail)
    //{
    //    var effects = GetComponents<IOnBallRailEffect>();
    //    foreach (var effect in effects)
    //        effect.OnBallRailBounce(base.gameObject, rail);
    //}
}

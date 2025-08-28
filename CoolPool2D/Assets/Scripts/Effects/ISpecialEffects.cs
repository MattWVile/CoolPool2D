
using UnityEngine;

public interface IOnBallHitEffect
{
    void OnBallHit(GameObject self, GameObject other);
}

public interface IOnPotEffect
{
    void OnPot(BallData self);
}

public interface IOnRailBounceEffect
{
    void OnRailBounce(BallData self);
}

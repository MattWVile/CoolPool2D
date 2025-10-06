using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class PinballMachine : BaseArtifact<BallCollidedWithRailEvent> {

    public string name = "Pinball Machine";
    public string description = "Balls bounce off the rails";

    public float BOUNCE_STRENGTH = 1.3f;

    protected override void OnEvent(BallCollidedWithRailEvent e)
    {
        e.BallData.gameObject.GetComponent<DeterministicBall>().velocity *= BOUNCE_STRENGTH;

    }
}
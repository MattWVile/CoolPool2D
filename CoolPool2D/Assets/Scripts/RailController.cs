using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RailController : MonoBehaviour
{
    public Rail rail;

    public void OnCollisionEnter2D(Collision2D collision) {
        EventBus.Publish(new BallCollidedWithRailEvent {
            Sender = this,
            Rail = rail,
            Ball = collision.gameObject
        });
    }
}

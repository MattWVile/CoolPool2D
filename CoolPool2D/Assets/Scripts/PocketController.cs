using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PocketController : MonoBehaviour
{
    public Pocket pocket;
    public void OnTriggerEnter2D(Collider2D other) {
        EventBus.Publish(new BallPocketedEvent()
        {
            Ball = other.gameObject,
            Pocket = pocket,
            Sender = this
        });
        
    }
}

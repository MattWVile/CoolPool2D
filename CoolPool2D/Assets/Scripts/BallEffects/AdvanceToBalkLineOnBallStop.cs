using UnityEngine;
using static PoolWorld;

public class AdvanceToBalkLineOnBallStop : BaseBallEffect<BallStoppedEvent>
{
    public float advanceForceMagnitude = 10f;
    public float explosionRadius = 5f;
    public float explosionForce = 10f;
    protected override void Start()
    {
        base.Start();
        EventBus.Subscribe<NewGameStateEvent>(OnNewGameStateEvent);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventBus.Unsubscribe<NewGameStateEvent>(OnNewGameStateEvent);
    }

    public void OnNewGameStateEvent(NewGameStateEvent @event)
    {
        if (@event.NewGameState == GameState.Aiming)
        {
            hasEffectTriggeredThisShot = false;
        }
        else if (@event.NewGameState == GameState.PrepareNextTurn)
        {
            var balkLine = GameObject.Find("BalkLine");
            Vector3 directionToBalkLine = (balkLine.transform.position - gameObject.transform.position).normalized;
            if (directionToBalkLine.x >= 0)
            {
                BallHasStoppedBeyondBalkLine();
                return;
            }
        }
    }

    protected override void OnEvent(BallStoppedEvent ballStoppedEvent)
    {
        if (!hasEffectTriggeredThisShot && TurnManager.Instance.shouldBallsAdvance)
        {
            DeterministicBall deterministicBall = gameObject.GetComponent<DeterministicBall>();
            var balkLine = GameObject.Find("BalkLine");
            Vector3 directionToBalkLine = (balkLine.transform.position - gameObject.transform.position).normalized;

            if (balkLine != null && deterministicBall != null)
            {
                if(directionToBalkLine.x >= 0)
                {
                    BallHasStoppedBeyondBalkLine();
                    return;
                }
                directionToBalkLine.y = 0; 
                deterministicBall.velocity = directionToBalkLine.normalized * advanceForceMagnitude;
            }
        }
    }

    public void BallHasStoppedBeyondBalkLine()
    {
        GameManager.Instance.ballGameObjects.Remove(gameObject);
        Destroy(gameObject);
        EventBus.Publish(new BallStoppedBeyondBalkLineEvent());
    }

    //public void ExplodeBall()
    //{ 
    //    var explosion = Instantiate(Resources.Load<GameObject>("Prefabs/vfx_Explosion_02"), gameObject.transform.position, Quaternion.identity);
    //    explosion.GetComponent<ParticleSystem>().Play();
    //    Destroy(explosion, explosion.GetComponent<ParticleSystem>().main.duration);

    //    // push all other balls away from this ball in a circle with a certain radius and force the closer the ball is the more force it receives
    //    foreach (GameObject ball in GameManager.Instance.ballGameObjects)
    //    {
    //        if (ball.GetComponent<BallScoringData>().ballVariant == BallVariant.Cue)
    //            continue;

    //        Vector3 offset = ball.transform.position - transform.position;

    //        float distance = offset.magnitude;

    //        if (distance > explosionRadius)
    //            continue;

    //        DeterministicBall ballToMoveDeterministicBall = ball.GetComponent<DeterministicBall>();
    //        if (ballToMoveDeterministicBall == null)
    //            continue;
    //        ballToMoveDeterministicBall.velocity = offset.normalized * explosionForce;
    //    }
    //    GameManager.Instance.ballGameObjects.Remove(gameObject);
    //    Destroy(gameObject);
    //}
}

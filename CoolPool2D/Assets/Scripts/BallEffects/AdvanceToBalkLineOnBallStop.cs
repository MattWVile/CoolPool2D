using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class AdvanceToBalkLineOnBallStop : BaseBallEffect<BallStoppedEvent>
{
    public float forceMagnitude = 10f;
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
                EventBus.Publish(new BallStoppedBeyondBalkLineEvent());
                return;
            }
        }
    }

    protected override void OnEvent(BallStoppedEvent ballStoppedEvent)
    {
        if (!hasEffectTriggeredThisShot)
        {
            DeterministicBall deterministicBall = gameObject.GetComponent<DeterministicBall>();
            var balkLine = GameObject.Find("BalkLine");
            Vector3 directionToBalkLine = (balkLine.transform.position - gameObject.transform.position).normalized;

            if (balkLine != null && deterministicBall != null)
            {
                if(directionToBalkLine.x >= 0)
                {
                    EventBus.Publish(new BallStoppedBeyondBalkLineEvent());
                    return;
                }
                directionToBalkLine.y = 0; 
                deterministicBall.velocity = directionToBalkLine.normalized * forceMagnitude;
            }
        }
    }
}

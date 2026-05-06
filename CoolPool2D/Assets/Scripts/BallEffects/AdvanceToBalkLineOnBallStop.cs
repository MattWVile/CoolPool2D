using UnityEngine;

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
    }

    protected override void OnEvent(BallStoppedEvent ballStoppedEvent)
    {
        if (!hasEffectTriggeredThisShot)
        {
            var balkLine = GameObject.Find("BalkLine");
            DeterministicBall deterministicBall = gameObject.GetComponent<DeterministicBall>();

            if (balkLine != null && deterministicBall != null)
            {
                Vector3 directionToBalkLine = (balkLine.transform.position - gameObject.transform.position).normalized;
                if(directionToBalkLine.x >= 0)
                {
                    return;
                }
                directionToBalkLine.y = 0; 
                deterministicBall.velocity = directionToBalkLine * forceMagnitude;
            }
        }
        hasEffectTriggeredThisShot = true;
    }
}

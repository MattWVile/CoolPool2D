using UnityEngine;
using Debug = UnityEngine.Debug;

public class DebuggingTools : MonoBehaviour
{
    private GameObject cueBall;

    void Start()
    {

        //EventBus.Subscribe<BallPocketedEvent>(@event =>
        //    Debug.Log($"[DEBUG] [Event] BallPocketedEvent: {@event.Ball.gameObject.name} in {@event.Pocket}"));
        //EventBus.Subscribe<BallCollidedWithRailEvent>(@event =>
        //    Debug.Log($"[DEBUG] [Event] BallCollidedWithRailEvent: {@event.Ball.gameObject.name} with {@event.Rail}"));
    }

    // Update is called once per frame
    void Update()
    {
        HandleTimeControl();
        HandleGameTools();
        HandleDeleteAllBalls();
    }

    private void HandleDeleteAllBalls()
    {
        if (Input.GetKeyDown(KeyCode.F6))
        {

            cueBall = GameObject.FindWithTag("CueBall");
            var balls = GameManager.Instance.ballGameObjects;
            foreach (var ball in balls)
            {
                if (ball == cueBall) continue;
                Destroy(ball);
            }
            Debug.Log($"[DEBUG] Deleted all balls");
        }
    }
    private void HandleGameTools()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            GameManager.Instance.RetryLastShot();
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            GameManager.Instance.ResetGame();
        }
    }

    private static void HandleTimeControl()
    {
        if (Input.GetKey(KeyCode.F1))
        {
            Time.timeScale *= 0.995f;
            Debug.Log($"[DEBUG] Decreased Time Scale [{Time.timeScale}]");
        }
        if (Input.GetKey(KeyCode.F2))
        {
            Time.timeScale /= 0.995f;
            Debug.Log($"[DEBUG] Increased Time Scale [{Time.timeScale}]");
        }
        if (Input.GetKey(KeyCode.F3))
        {
            Time.timeScale = 1f;
            Debug.Log($"[DEBUG] Reset Time Scale [{Time.timeScale}]");
        }
        if (Input.GetKey(KeyCode.F4))
        {
            Time.timeScale = 0.001f;
            Debug.Log($"[DEBUG] set Time Scale [{Time.timeScale}]");
        }
        if (Input.GetKey(KeyCode.F5))
        {
            Time.timeScale = 0.01f;
            Debug.Log($"[DEBUG]     set Time Scale [{Time.timeScale}]");
        }
    }
}

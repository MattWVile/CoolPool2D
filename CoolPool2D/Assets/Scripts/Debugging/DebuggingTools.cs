using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class DebuggingTools : MonoBehaviour
{
    private bool showLazer;
    private CueMovement cueMovement;
    private GameObject cueBall;

    void Start()
    {
        cueMovement = GameObject.FindFirstObjectByType<CueMovement>();
        cueBall = GameObject.Find("CueBall");

        EventBus.Subscribe<BallPocketedEvent>(@event =>
            Debug.Log($"[DEBUG] [Event] BallPocketedEvent: {@event.Ball.gameObject.name} in {@event.Pocket}"));
        EventBus.Subscribe<BallCollidedWithRailEvent>(@event =>
            Debug.Log($"[DEBUG] [Event] BallCollidedWithRailEvent: {@event.Ball.gameObject.name} with {@event.Rail}"));
    }

    // Update is called once per frame
    void Update()
    {
        HandleTimeControl();
        HandleGameTools();
    }

    private void HandleGameTools()
    {
        if (Input.GetKeyDown(KeyCode.F4)) {
            showLazer = !showLazer;
            Debug.Log($"[DEBUG] Show Lazer: {showLazer}");
        }
        if (showLazer) DrawLaser();


        if (Input.GetKeyDown(KeyCode.F5)) {
            cueBall.transform.position = new Vector3(-3.6f, 0, 0);
            Debug.Log($"[DEBUG] Reset CueBall position");
        }
    }

    private void DrawLaser()
    {
        var cuePosition = cueMovement.transform.position;
        var aimingAngleInRads = cueMovement.AimingAngle;
        var targetPosition = cuePosition + new Vector3(Mathf.Cos(aimingAngleInRads), Mathf.Sin(aimingAngleInRads), 0);

        // extrapolate the target position to make it further
        var extendedTargetPosition = targetPosition + (targetPosition - cuePosition) * 20;

        Debug.DrawLine(cuePosition, extendedTargetPosition, Color.red);
    }

    private static void HandleTimeControl()
    {
        if (Input.GetKey(KeyCode.F1))
        {
            Time.timeScale *= 0.998f;
            Debug.Log($"[DEBUG] Decreased Time Scale [{Time.timeScale}]");
        }
        if (Input.GetKey(KeyCode.F2))
        {
            Time.timeScale /= 0.998f;
            Debug.Log($"[DEBUG] Increased Time Scale [{Time.timeScale}]");
        }
        if (Input.GetKey(KeyCode.F3)) {
            Time.timeScale = 1f;
            Debug.Log($"[DEBUG] Reset Time Scale [{Time.timeScale}]");
        }
    }
}
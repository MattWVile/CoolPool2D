using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class DebuggingTools : MonoBehaviour
{
    private bool showLazer;
    private CueMovement cueMovement;

    void Start()
    {
        cueMovement = GameObject.FindFirstObjectByType<CueMovement>();

        EventBus.Subscribe<BallPocketedEvent>(@event =>
            Debug.Log($"[Event] BallPocketedEvent: {@event.Ball.gameObject.name} in {@event.Pocket}"));
        EventBus.Subscribe<BallCollidedWithRailEvent>(@event =>
            Debug.Log($"[Event] BallCollidedWithRailEvent: {@event.Ball.gameObject.name} with {@event.Rail}"));
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKey(KeyCode.F1))
        {
            Time.timeScale *= 0.998f;
            Debug.Log($"Decreased Time Scale [{Time.timeScale}]");
        }
        if (Input.GetKey(KeyCode.F2))
        {
            Time.timeScale /= 0.998f;
            Debug.Log($"Increased Time Scale [{Time.timeScale}]");
        }
        if (Input.GetKey(KeyCode.F3)) {
            Time.timeScale = 1f;
            Debug.Log($"Reset Time Scale [{Time.timeScale}]");
        }
        if (Input.GetKeyDown(KeyCode.F4)) {
            showLazer = !showLazer;
            Debug.Log($"Show Lazer: {showLazer}");

        }

        // Draw a line from the cue to the target
        if (showLazer) {
            var cuePosition = cueMovement.transform.position;
            var aimingAngleInRads = cueMovement.AimingAngle;
            var targetPosition = cuePosition + new Vector3(Mathf.Cos(aimingAngleInRads), Mathf.Sin(aimingAngleInRads), 0);

            // extrapolate the target position to make it further
            var extendedTargetPosition = targetPosition + (targetPosition - cuePosition) * 20;

            Debug.DrawLine(cuePosition, extendedTargetPosition, Color.red);
        }

    }
}
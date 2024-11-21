using UnityEngine;

public class CueSpawner : MonoBehaviour
{
    private GameObject cuePrefab;
    private GameObject cueToDelete;
    public Vector3 initialPosition;
    void Start()
    {
        EventBus.Subscribe<BallStoppedEvent>(OnBallStoppedEvent);
        EventBus.Subscribe<BallHasBeenShotEvent>(OnBallHasBeenShotEvent);
        cuePrefab = Resources.Load<GameObject>("Prefabs/CueStick");
        cueToDelete = Instantiate(cuePrefab, initialPosition, Quaternion.identity);
    }

    public void OnBallStoppedEvent(BallStoppedEvent ballStoppedEvent)
    {
        if (GameObject.FindGameObjectWithTag("CueStick") != null) return;
        var cueBall = ballStoppedEvent.Sender;
        cueToDelete = Instantiate(cuePrefab, cueBall.transform.position, Quaternion.identity);
    }

    public void OnBallHasBeenShotEvent(BallHasBeenShotEvent ballHasBeenShotEvent)
    {
        if (cueToDelete == null) return;
        Destroy(cueToDelete, 0.2f);
    }
}
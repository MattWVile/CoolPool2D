using UnityEngine;

public class CueMovement : MonoBehaviour
{
    private BallController cueBallControllerScript;
    private Vector2 cueBallPosition;
    public float distance = -4f; // Distance of the cue from the cue ball

    private void Start()
    {
        cueBallControllerScript = GameObject.Find("CueBall").GetComponent<BallController>();
        cueBallPosition = cueBallControllerScript.transform.position;
    }

    private void Update()
    {
        float aimingAngle = cueBallControllerScript.aimingAngle;

        Vector2 offset = getOffset(distance, aimingAngle);
        transform.position = cueBallPosition + offset;
        // rotate the cue to face the direction the ball is going to move in
        transform.rotation = Quaternion.Euler(0, 0, aimingAngle * Mathf.Rad2Deg);

        // move the cue left and right
        float cueMovement = Input.GetAxis("Horizontal");
        transform.Translate(Vector2.right * cueMovement * Time.deltaTime);
    }

    public Vector2 getOffset(float distance, float angle)
    {
        float x = distance * Mathf.Cos(angle);
        float y = distance * Mathf.Sin(angle);
        return new Vector2(x, y);
    }
}

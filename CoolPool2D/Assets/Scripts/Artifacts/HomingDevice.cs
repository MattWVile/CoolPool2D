using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class HomingDevice : BaseArtifact<BallKissedEvent> {

    public string name = "Homing Device";
    public string description = "When a ball gets kissed by the cueball it has a 20% chance to get nudged towards the closest pocket.";

    protected override void OnEvent(BallKissedEvent e)
    {
        //if (Random.Range(0, 100) > 20) return;

        var closestPocket = FindClosestPocket(e.CollisionBallData.transform.position);
        if (closestPocket == null) {
            Debug.LogWarning("No pockets found in the scene.");
            return;
        }

        Vector2 directionToPocket = (closestPocket.transform.position - e.CollisionBallData.transform.position).normalized;
        float nudgeStrength = 15.0f; // total strength distributed over 0.5 seconds
        float duration = 0.5f;

        var ball = e.CollisionBallData.gameObject.GetComponent<DeterministicBall>();
        CoroutineRunner.Instance.StartCoroutine(GradualNudge(ball, directionToPocket, nudgeStrength, duration));

        Debug.Log($"Gradually nudging ball: {e.CollisionBallData.BallColour} to {closestPocket.gameObject.name}");
    }
    private IEnumerator GradualNudge(DeterministicBall ball, Vector2 dir, float totalStrength, float duration) {
        float elapsed = 0f;
        while (elapsed < duration) {
            float step = Time.deltaTime / duration; // fraction of time passed this frame
            // Apply incremental nudge
            ball.velocity += dir * (totalStrength * step);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    private GameObject FindClosestPocket(Vector2 transformPosition)
    {
        var pockets = GameObject.FindGameObjectsWithTag("Pocket");
        GameObject closestPocket = null;
        float closestDistance = Mathf.Infinity;

        foreach (var pocket in pockets) {
            float distance = Vector2.Distance(transformPosition, pocket.transform.position);
            if (distance < closestDistance) {
                closestDistance = distance;
                closestPocket = pocket;
            }
        }

        return closestPocket;
    }
}
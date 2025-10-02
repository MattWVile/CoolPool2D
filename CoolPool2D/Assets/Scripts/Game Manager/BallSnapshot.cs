using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BallSnapshot
{
    public int InstanceId;
    public BallColour Colour;
    public Vector2 Position;
    public Vector2 Velocity;
    public bool Active;
}

public class ShotRecorder
{
    private readonly List<BallSnapshot> _lastShotSnapshot = new List<BallSnapshot>();

    public void SaveSnapshot(IEnumerable<GameObject> balls)
    {
        _lastShotSnapshot.Clear();
        if (balls == null) return;

        foreach (var ballGameObject in balls)
        {
            if (ballGameObject == null) continue;
            var data = ballGameObject.GetComponent<BallData>();
            if (data == null) continue;
            var det = ballGameObject.GetComponent<DeterministicBall>();

            var snap = new BallSnapshot
            {
                InstanceId = ballGameObject.GetInstanceID(),
                Colour = data.BallColour,
                Position = (Vector2)ballGameObject.transform.position,
                Velocity = det != null ? det.velocity : Vector2.zero,
                Active = det != null ? det.active : ballGameObject.activeInHierarchy
            };
            _lastShotSnapshot.Add(snap);
        }
    }

    public IReadOnlyList<BallSnapshot> GetLastSnapshot() => _lastShotSnapshot.AsReadOnly();

    // Restore by setting existing objects (match by instance id). Useful if you never destroyed originals.
    public void RestoreSnapshotToExistingObjects()
    {
        if (_lastShotSnapshot == null || _lastShotSnapshot.Count == 0) return;

        var map = _lastShotSnapshot.ToDictionary(s => s.InstanceId);
        foreach (var det in UnityEngine.Object.FindObjectsOfType<DeterministicBall>())
        {
            if (det == null) continue;
            var go = det.gameObject;
            if (map.TryGetValue(go.GetInstanceID(), out var snap))
            {
                go.transform.position = snap.Position;
                det.velocity = snap.Velocity;
                det.active = snap.Active;
            }
        }
    }
}


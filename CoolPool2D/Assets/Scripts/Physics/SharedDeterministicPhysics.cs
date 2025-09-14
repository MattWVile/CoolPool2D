using System.Collections.Generic;
using UnityEngine;

public static class SharedDeterministicPhysics
{
    public const float MIN_DIRECTION_EPSILON = 1e-9f;
    public const float MIN_VELOCITY_THRESHOLD = 1e-12f;

    // Sweep a ball center along ballVelocity and test against all rail segments (capsule tests).
    // Returns earliest hit time <= maxSimulationTime and the collision normal (segment -> ball).
    public static RailLocation CalculateTimeToRailCollision(
        Vector2 ballPosition, Vector2 ballVelocity, float ballRadius,
        Dictionary<RailLocation, List<RailSegment>> rails,
        float maxSimulationTime, out float timeToCollision, out Vector2 collisionNormal)
    {

        RailLocation collidedRail = default;
        timeToCollision = maxSimulationTime;
        collisionNormal = Vector2.zero;

        if (rails == null || rails.Count == 0) return collidedRail;

        foreach (var railKV in rails)
        {
            var segList = railKV.Value;
            if (segList == null) continue;
            foreach (var seg in segList)
            {
                if (CalculateTimeToSegmentCollision(
                    ballPosition, ballVelocity, ballRadius,
                    seg.start, seg.end,
                    maxSimulationTime, out float candidateTime, out Vector2 candidateNormal))
                {
                    if (candidateTime < timeToCollision)
                    {
                        timeToCollision = candidateTime;
                        collisionNormal = candidateNormal;
                        collidedRail = railKV.Key;
                    }
                }
            }
        }
        return collidedRail;
    }

    // Swept circle vs capsule (segment + caps).
    // Returns earliest hit time <= maxSimulationTime, normal from segment -> ball at contact.
    public static bool CalculateTimeToSegmentCollision(
        Vector2 ballPosition, Vector2 ballVelocity, float ballRadius,
        Vector2 segA, Vector2 segB,
        float maxSimulationTime, out float timeToCollision, out Vector2 collisionNormal)
    {
        timeToCollision = maxSimulationTime;
        collisionNormal = Vector2.zero;
        bool found = false;

        Vector2 seg = segB - segA;
        float segLenSq = seg.sqrMagnitude;
        if (segLenSq <= MIN_DIRECTION_EPSILON) return false; // degenerate

        Vector2 segUnit = seg / Mathf.Sqrt(segLenSq);
        Vector2 segNormal = new Vector2(-segUnit.y, segUnit.x); // perpendicular

        // 1) line-core collision
        float distToLine = Vector2.Dot(ballPosition - segA, segNormal);
        float relVelAlongNormal = Vector2.Dot(ballVelocity, segNormal);

        if (Mathf.Abs(relVelAlongNormal) > MIN_VELOCITY_THRESHOLD)
        {
            float t1 = (ballRadius - distToLine) / relVelAlongNormal;
            float t2 = (-ballRadius - distToLine) / relVelAlongNormal;

            float tLine = float.PositiveInfinity;
            if (t1 >= 0f && t1 <= maxSimulationTime) tLine = Mathf.Min(tLine, t1);
            if (t2 >= 0f && t2 <= maxSimulationTime) tLine = Mathf.Min(tLine, t2);

            if (!float.IsPositiveInfinity(tLine))
            {
                Vector2 posAtT = ballPosition + ballVelocity * tLine;
                float projParam = Vector2.Dot(posAtT - segA, seg) / segLenSq;
                if (projParam >= 0f && projParam <= 1f)
                {
                    Vector2 closestPoint = segA + projParam * seg;
                    Vector2 normalVec = posAtT - closestPoint;
                    float nlen = normalVec.magnitude;
                    if (nlen > MIN_DIRECTION_EPSILON)
                    {
                        Vector2 normalDir = normalVec / nlen;
                        if (tLine < timeToCollision)
                        {
                            timeToCollision = tLine;
                            collisionNormal = normalDir;
                            found = true;
                        }
                    }
                }
            }
        }

        // 2) endpoint caps (quadratic)
        float vDotV = Vector2.Dot(ballVelocity, ballVelocity);
        if (vDotV > MIN_VELOCITY_THRESHOLD)
        {
            // endpoint A
            {
                Vector2 s = ballPosition - segA;
                float c = Vector2.Dot(s, s) - ballRadius * ballRadius;
                float b = 2f * Vector2.Dot(s, ballVelocity);
                float disc = b * b - 4f * vDotV * c;
                if (disc >= 0f)
                {
                    float sqrtD = Mathf.Sqrt(disc);
                    float r0 = (-b - sqrtD) / (2f * vDotV);
                    float r1 = (-b + sqrtD) / (2f * vDotV);
                    float tCandidate = float.PositiveInfinity;
                    if (r0 >= 0f && r0 <= maxSimulationTime) tCandidate = Mathf.Min(tCandidate, r0);
                    if (r1 >= 0f && r1 <= maxSimulationTime) tCandidate = Mathf.Min(tCandidate, r1);
                    if (!float.IsPositiveInfinity(tCandidate) && tCandidate < timeToCollision)
                    {
                        Vector2 posAtT = ballPosition + ballVelocity * tCandidate;
                        Vector2 normalVec = posAtT - segA;
                        float nlen = normalVec.magnitude;
                        if (nlen > MIN_DIRECTION_EPSILON)
                        {
                            collisionNormal = normalVec / nlen;
                            timeToCollision = tCandidate;
                            found = true;
                        }
                    }
                }
            }

            // endpoint B
            {
                Vector2 s = ballPosition - segB;
                float c = Vector2.Dot(s, s) - ballRadius * ballRadius;
                float b = 2f * Vector2.Dot(s, ballVelocity);
                float disc = b * b - 4f * vDotV * c;
                if (disc >= 0f)
                {
                    float sqrtD = Mathf.Sqrt(disc);
                    float r0 = (-b - sqrtD) / (2f * vDotV);
                    float r1 = (-b + sqrtD) / (2f * vDotV);
                    float tCandidate = float.PositiveInfinity;
                    if (r0 >= 0f && r0 <= maxSimulationTime) tCandidate = Mathf.Min(tCandidate, r0);
                    if (r1 >= 0f && r1 <= maxSimulationTime) tCandidate = Mathf.Min(tCandidate, r1);
                    if (!float.IsPositiveInfinity(tCandidate) && tCandidate < timeToCollision)
                    {
                        Vector2 posAtT = ballPosition + ballVelocity * tCandidate;
                        Vector2 normalVec = posAtT - segB;
                        float nlen = normalVec.magnitude;
                        if (nlen > MIN_DIRECTION_EPSILON)
                        {
                            collisionNormal = normalVec / nlen;
                            timeToCollision = tCandidate;
                            found = true;
                        }
                    }
                }
            }
        }

        return found && timeToCollision <= maxSimulationTime;
    }

    // Compute reflection and nudge position (same semantics as your ComputeWallReflection)
    public static void ComputeWallReflection(Vector2 incomingDirection, Vector2 hitNormal, float railBounciness, Vector2 contactCenter, float separationNudge, float stepOffset, out Vector2 reflectedDirectionNormalized, out Vector2 newPositionAfterNudge)
    {
        float normalComponent = Vector2.Dot(incomingDirection, hitNormal);
        Vector2 normalVector = normalComponent * hitNormal;
        Vector2 tangentialVector = incomingDirection - normalVector;

        Vector2 newDirUnnormalized = tangentialVector - normalVector * railBounciness;
        if (newDirUnnormalized.sqrMagnitude <= MIN_VELOCITY_THRESHOLD)
            newDirUnnormalized = Vector2.Reflect(incomingDirection, hitNormal);

        reflectedDirectionNormalized = newDirUnnormalized.normalized;
        newPositionAfterNudge = contactCenter + hitNormal * separationNudge + reflectedDirectionNormalized * stepOffset;
    }

    // Equal-mass 1D normal swap with restitution; used by both ball solver and aiming preview.
    public static void ResolveEqualMassBallCollision(Vector2 velocityA, Vector2 velocityB, Vector2 collisionNormal, float restitution, out Vector2 velocityAAfter, out Vector2 velocityBAfter)
    {
        Vector2 normalUnit = collisionNormal.normalized;
        Vector2 tangentUnit = new Vector2(-normalUnit.y, normalUnit.x);

        float velocityANormal = Vector2.Dot(velocityA, normalUnit);
        float velocityBNormal = Vector2.Dot(velocityB, normalUnit);
        float velocityATangent = Vector2.Dot(velocityA, tangentUnit);
        float velocityBTangent = Vector2.Dot(velocityB, tangentUnit);

        float velocityANormalAfter = velocityBNormal * restitution;
        float velocityBNormalAfter = velocityANormal * restitution;

        velocityAAfter = normalUnit * velocityANormalAfter + tangentUnit * velocityATangent;
        velocityBAfter = normalUnit * velocityBNormalAfter + tangentUnit * velocityBTangent;
    }
}

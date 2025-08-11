using UnityEngine;

public class Physics2DIterationProfiler : MonoBehaviour
{
    public int velocityIterations = 8;
    public int positionIterations = 3;

    private float timer;
    private int frameCount;
    private float fps;

    void Update()
    {
        // Simple FPS counter
        frameCount++;
        timer += Time.unscaledDeltaTime;

        if (timer >= 1f)
        {
            fps = frameCount / timer;
            frameCount = 0;
            timer = 0;
            Debug.Log($"FPS: {fps} | Vel: {velocityIterations}, Pos: {positionIterations}");
        }
    }

    void FixedUpdate()
    {
        // Apply new iteration values every physics step
        Physics2D.velocityIterations = velocityIterations;
        Physics2D.positionIterations = positionIterations;
    }
}

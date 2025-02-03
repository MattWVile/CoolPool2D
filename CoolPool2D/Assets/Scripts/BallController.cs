using UnityEngine;

public class BallController : MonoBehaviour
{
    private Rigidbody2D rb;

    private Vector2 initalPosition = new Vector2(-2.66f, -0.12f);

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            rb.velocity = Vector2.zero;
            transform.position = initalPosition;
        }
    }

}

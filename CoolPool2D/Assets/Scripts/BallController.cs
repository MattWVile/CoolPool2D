using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    private Rigidbody2D rb;

    public float normalRotationSpeed = 5f;
    public float slowedRotationSpeed = 2f;

    private Vector2 initalPosition = new Vector2(-4.81400299f, -0.119999997f);

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

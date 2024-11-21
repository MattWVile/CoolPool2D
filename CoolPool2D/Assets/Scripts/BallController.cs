using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    private Rigidbody2D rb;

    public float aimingAngle = 0f;
    public float normalRotationSpeed = 5f;
    public float slowedRotationSpeed = 2f;
    public float amountOfForceToApplyToBall = 10f;

    private Vector2 initalPosition = new Vector2(-4.81400299f, -0.119999997f);

    void Start()
    {

        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        // on space key press
        if (Input.GetKeyDown(KeyCode.Space)) {
            ShootBall();
        }
        HandleAiming();
        if (Input.GetKeyDown(KeyCode.R))
        {
            aimingAngle = 0f;
            rb.velocity = Vector2.zero;
            transform.position = initalPosition;

        }
    }

    private void ShootBall() {
        Vector2 force = new Vector2(Mathf.Cos(aimingAngle), Mathf.Sin(aimingAngle)) * amountOfForceToApplyToBall;
        rb.velocity = force;
    }
    private void HandleAiming()
    {
        float rotationSpeed = Input.GetKey(KeyCode.UpArrow) ? slowedRotationSpeed : normalRotationSpeed;

        if (Input.GetKey(KeyCode.RightArrow))
        {
            aimingAngle += rotationSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {

            aimingAngle -= rotationSpeed * Time.deltaTime;
        }
    }
}

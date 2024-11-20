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
    }

    private void ShootBall() {
        Vector2 force = new Vector2(Mathf.Sin(aimingAngle), Mathf.Cos(aimingAngle)) * amountOfForceToApplyToBall;
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

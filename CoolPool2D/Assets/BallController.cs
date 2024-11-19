using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    private Rigidbody2D rb;
    void Start()
    {

        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        // on space key press
        if (Input.GetKeyDown(KeyCode.Space)) {
            rb.AddForce(new Vector3(100, 0, 0));
        }
    }


}

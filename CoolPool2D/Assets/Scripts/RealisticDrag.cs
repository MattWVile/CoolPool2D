using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RealisticDrag : MonoBehaviour
{
    private Rigidbody2D rb;
    public float drag = 0.5f;

    void Start() => rb = GetComponent<Rigidbody2D>();
    void Update() => SetRealisticDrag();

    private void SetRealisticDrag() {
        float epsilon = 0.01f;  // Small constant to avoid division by zero
        float calculatedDrag = drag / (rb.velocity.magnitude + epsilon);
        rb.drag = calculatedDrag;
    }
}

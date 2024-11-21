using UnityEngine;
using System.Collections;
public enum GameEventTypes
{
    BallCollision = 1,
    BallHasBeenShot = 2,
    BallStopped = 3,
    BallReadyToBeShot = 4,
    BallIsBeingMoved = 5,
    BallCollidedWithRail = 6
}

public class BallController : MonoBehaviour
{
    //public variables
    public float aimingAngle = 0f;
    public float normalRotationSpeed = 5f;
    public float slowedRotationSpeed = 2f;
    public float amountOfForceToApplyToBall = 0f;
    public UnityEngine.UI.Image powerBarFillImage;
    public float maxAmountOfBallForce;
    public float forceMultiplier;
    public GameEventTypes LastPublishedState;

    //private variables
    private GameObject powerBarPrefab;
    private GameObject currentPowerBar;

    private bool hasBallJustBeenShot = false;
    private bool isballStopped = true;    

    private Canvas canvas;

    private Rigidbody2D rb;

    private Vector2 initalPosition = new Vector2(-4.81400299f, -0.119999997f);

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        canvas = FindObjectOfType<Canvas>();
        powerBarPrefab = Resources.Load<GameObject>("Prefabs/PowerBar");
    }

    void Update()
    {
        HandleShoot();
        HandleAiming();
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetBall();
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
    private void ResetBall()
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;
        transform.position = initalPosition;
        aimingAngle = 0;
    }

    private void HandleShoot()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {

            if (currentPowerBar == null)
            {
                currentPowerBar = Instantiate(powerBarPrefab, canvas.transform);
                powerBarFillImage = GameObject.FindWithTag("PowerBar").GetComponent<UnityEngine.UI.Image>();
            }

            StartCoroutine(ShootCoroutine());
        }
    }
    private IEnumerator ShootCoroutine()
    {
        while (Input.GetKey(KeyCode.Space) && amountOfForceToApplyToBall < maxAmountOfBallForce)
        {
            EventBus.Publish(new BallIsBeingChargedEvent { Sender = this });
            amountOfForceToApplyToBall += Time.deltaTime * forceMultiplier;
            powerBarFillImage.fillAmount = amountOfForceToApplyToBall / maxAmountOfBallForce;

            yield return null; // Yield control back to Unity to allow other tasks to execute
        }

        EventBus.Publish(new BallIsChargedEvent { Sender = this });
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        Vector2 force = new Vector2(Mathf.Cos(aimingAngle), Mathf.Sin(aimingAngle)) * amountOfForceToApplyToBall;
        rb.velocity = force;
        EventBus.Publish(new BallHasBeenShotEvent { Sender = this });
        isballStopped = false;
        hasBallJustBeenShot = true;

        Destroy(currentPowerBar, 0.5f);

        LastPublishedState = GameEventTypes.BallHasBeenShot;
        Invoke("SetHasBallJustBeenShotToFalse", 0.5f);
        amountOfForceToApplyToBall = 0;
    }
    private void SetHasBallJustBeenShotToFalse()
    {
        hasBallJustBeenShot = false;
    }
}

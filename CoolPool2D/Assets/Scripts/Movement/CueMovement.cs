using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Runtime.InteropServices;
#endif

public class CueMovement : MonoBehaviour
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKeyCode);

    private const int virtualKeyCodeRightMouseButton = 0x02;
#endif

    public SpriteRenderer spriteRenderer;
    public float distanceFromTarget = 4f;
    public GameObject target;
    public DeterministicBall targetBall;
    public float aimingAngle;
    public bool fineAimingActive = false;
    public float shotStrength = 1f;
    public float aimingSpeed = 1f;

    [Header("Drag Shot")]
    [SerializeField] private float maxDragDistance = 3f;
    [SerializeField] private float minimumShotStrength = 0.2f;

    [Tooltip("Closest cue centre/pivot distance from the cue ball while aiming. 3.8 is the confirmed good minimum-power position.")]
    [SerializeField] private float cueCentreDistanceFromCueBallAtMinimumPower = 3.8f;

    [Tooltip("The player must press this close to the cue ball to start aiming.")]
    [SerializeField] private float dragStartMaximumDistanceFromCueBall = 1f;

    [Header("Cue Strike Animation")]
    [SerializeField] private float cueStrikeAnimationDuration = 0.08f;

    private float? isChargingStart = null;

    // Kept so other code that references chargeTime still compiles.
    // The drag-shot system does not use time-based charging.
    private float chargeTime => 0f;

    private float currentShotPower = 0f;
    private bool isDraggingCue = false;
    private bool isCueStrikeAnimationPlaying = false;
    private bool shouldIgnorePrimaryInputUntilReleased = false;

    private int activePrimaryTouchFingerId = -1;

    private Vector2 currentCueCentreWorldPosition;
    private Vector2 cueStrikeAnimationCueCentreWorldPosition;

    private Coroutine cueStrikeAnimationCoroutine;

    private void Update()
    {
        if (isDraggingCue && IsCancelInputPressed())
        {
            CancelAimedShot();
            SetPosition();
            return;
        }

        HandleInput();
        SetPosition();
        ShowAimingLineIfNeeded();
    }

    public void RunDisableRoutine(IEnumerator routine)
    {
        StartCoroutine(routine);
    }

    public IEnumerator Disable(float delay = 0f)
    {
        yield return new WaitForSeconds(delay);

        target = null;
        targetBall = null;

        ResetCueState();
    }

    public void Enable(GameObject targetObject)
    {
        target = targetObject;
        targetBall = target != null ? target.GetComponent<DeterministicBall>() : null;

        ResetCueState();

        if (target == null) return;

        currentCueCentreWorldPosition = GetMinimumPowerCueCentreWorldPosition(target.transform.position);
        cueStrikeAnimationCueCentreWorldPosition = currentCueCentreWorldPosition;
    }

    private void HandleInput()
    {
        if (target == null || isCueStrikeAnimationPlaying) return;

        Camera camera = Camera.main ?? Camera.current;
        if (camera == null) return;

        if (shouldIgnorePrimaryInputUntilReleased)
        {
            if (!IsPrimaryInputHeld())
            {
                shouldIgnorePrimaryInputUntilReleased = false;
                activePrimaryTouchFingerId = -1;
            }

            return;
        }

        Vector2 inputWorldPosition = GetPrimaryInputWorldPosition(camera);
        Vector2 cueBallWorldPosition = target.transform.position;

        if (!isDraggingCue)
        {
            if (WasPrimaryInputPressedThisFrame())
            {
                TryStartCueDrag(inputWorldPosition, cueBallWorldPosition);
            }

            return;
        }

        if (IsCancelInputPressed())
        {
            CancelAimedShot();
            return;
        }

        if (WasPrimaryInputReleasedThisFrame())
        {
            ShootAimedShot();
            return;
        }

        if (IsPrimaryInputHeld())
        {
            UpdateAimAndPowerFromInputWorldPosition(inputWorldPosition, cueBallWorldPosition);
        }
    }

    private void TryStartCueDrag(Vector2 inputWorldPosition, Vector2 cueBallWorldPosition)
    {
        if (!IsInputCloseEnoughToCueBallToStartDrag(inputWorldPosition, cueBallWorldPosition)) return;

        activePrimaryTouchFingerId = GetPrimaryTouchFingerId();
        isDraggingCue = true;
        isChargingStart = GetUnscaledTime();
        currentShotPower = 0f;

        SetCueVisible(true);
        UpdateAimAndPowerFromInputWorldPosition(inputWorldPosition, cueBallWorldPosition);
    }

    private void CancelAimedShot()
    {
        StopCueStrikeAnimation();

        isDraggingCue = false;
        isCueStrikeAnimationPlaying = false;
        shouldIgnorePrimaryInputUntilReleased = true;
        isChargingStart = null;
        currentShotPower = 0f;

        if (target != null)
        {
            currentCueCentreWorldPosition = GetMinimumPowerCueCentreWorldPosition(target.transform.position);
            cueStrikeAnimationCueCentreWorldPosition = currentCueCentreWorldPosition;
        }

        SetCueVisible(false);
    }

    private void ShootAimedShot()
    {
        float finalShotStrength = Mathf.Lerp(
            minimumShotStrength,
            shotStrength,
            currentShotPower
        );

        isDraggingCue = false;
        isChargingStart = null;

        StartCueStrikeAnimation(finalShotStrength);
    }

    private void StartCueStrikeAnimation(float finalShotStrength)
    {
        StopCueStrikeAnimation();

        cueStrikeAnimationCoroutine = StartCoroutine(
            PlayCueStrikeAnimationThenShoot(finalShotStrength)
        );
    }

    private void StopCueStrikeAnimation()
    {
        if (cueStrikeAnimationCoroutine == null) return;

        StopCoroutine(cueStrikeAnimationCoroutine);
        cueStrikeAnimationCoroutine = null;
    }

    private bool IsPrimaryInputHeld()
    {
        return activePrimaryTouchFingerId != -1
            ? IsTouchStillHeld(activePrimaryTouchFingerId)
            : IsLeftMouseButtonPressed();
    }

    private bool WasPrimaryInputPressedThisFrame()
    {
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).phase == TouchPhase.Began;
        }

        return WasLeftMouseButtonPressedThisFrame();
    }

    private bool WasPrimaryInputReleasedThisFrame()
    {
        return activePrimaryTouchFingerId != -1
            ? WasTouchReleased(activePrimaryTouchFingerId)
            : WasLeftMouseButtonReleasedThisFrame();
    }

    private bool IsCancelInputPressed()
    {
        return IsRightMouseButtonPressed() || IsSecondFingerPressed();
    }

    private bool IsLeftMouseButtonPressed()
    {
        if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Mouse0)) return true;

#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        return false;
#endif
    }

    private bool WasLeftMouseButtonPressedThisFrame()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Mouse0)) return true;

#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return false;
#endif
    }

    private bool WasLeftMouseButtonReleasedThisFrame()
    {
        if (Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Mouse0)) return true;

#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
        return false;
#endif
    }

    private bool IsRightMouseButtonPressed()
    {
        if (Input.GetMouseButton(1) || Input.GetKey(KeyCode.Mouse1)) return true;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if ((GetAsyncKeyState(virtualKeyCodeRightMouseButton) & 0x8000) != 0) return true;
#endif

#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
        return false;
#endif
    }

    private bool IsSecondFingerPressed()
    {
        if (Input.touchCount >= 2)
        {
            if (activePrimaryTouchFingerId == -1) return true;

            for (int touchIndex = 0; touchIndex < Input.touchCount; touchIndex++)
            {
                Touch touch = Input.GetTouch(touchIndex);

                if (touch.fingerId != activePrimaryTouchFingerId &&
                    touch.phase != TouchPhase.Ended &&
                    touch.phase != TouchPhase.Canceled)
                {
                    return true;
                }
            }
        }

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current == null) return false;

        int pressedTouchCount = 0;

        foreach (TouchControl touchControl in Touchscreen.current.touches)
        {
            if (++pressedTouchCount >= 2 && touchControl.press.isPressed)
            {
                return true;
            }
        }
#endif

        return false;
    }

    private int GetPrimaryTouchFingerId()
    {
        return Input.touchCount > 0 ? Input.GetTouch(0).fingerId : -1;
    }

    private bool IsTouchStillHeld(int fingerId)
    {
        for (int touchIndex = 0; touchIndex < Input.touchCount; touchIndex++)
        {
            Touch touch = Input.GetTouch(touchIndex);

            if (touch.fingerId == fingerId)
            {
                return touch.phase != TouchPhase.Ended &&
                       touch.phase != TouchPhase.Canceled;
            }
        }

        return false;
    }

    private bool WasTouchReleased(int fingerId)
    {
        for (int touchIndex = 0; touchIndex < Input.touchCount; touchIndex++)
        {
            Touch touch = Input.GetTouch(touchIndex);

            if (touch.fingerId == fingerId)
            {
                return touch.phase == TouchPhase.Ended ||
                       touch.phase == TouchPhase.Canceled;
            }
        }

        return true;
    }

    private Vector2 GetPrimaryInputWorldPosition(Camera camera)
    {
        Vector3 inputScreenPosition = GetPrimaryInputScreenPosition();

        float distanceFromCameraToTarget = Mathf.Abs(
            camera.transform.position.z - target.transform.position.z
        );

        Vector3 inputWorldPosition3D = camera.ScreenToWorldPoint(
            new Vector3(
                inputScreenPosition.x,
                inputScreenPosition.y,
                distanceFromCameraToTarget
            )
        );

        return new Vector2(inputWorldPosition3D.x, inputWorldPosition3D.y);
    }

    private Vector3 GetPrimaryInputScreenPosition()
    {
        if (activePrimaryTouchFingerId != -1)
        {
            for (int touchIndex = 0; touchIndex < Input.touchCount; touchIndex++)
            {
                Touch touch = Input.GetTouch(touchIndex);

                if (touch.fingerId == activePrimaryTouchFingerId)
                {
                    return touch.position;
                }
            }
        }

        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            return new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, 0f);
        }
#endif

        return Input.mousePosition;
    }

    private bool IsInputCloseEnoughToCueBallToStartDrag(Vector2 inputWorldPosition, Vector2 cueBallWorldPosition)
    {
        return Vector2.Distance(inputWorldPosition, cueBallWorldPosition) <= dragStartMaximumDistanceFromCueBall;
    }

    private void UpdateAimAndPowerFromInputWorldPosition(Vector2 inputWorldPosition, Vector2 cueBallWorldPosition)
    {
        Vector2 directionFromInputToCueBall = cueBallWorldPosition - inputWorldPosition;
        Vector2 shotDirection = directionFromInputToCueBall.sqrMagnitude <= 0.0001f
            ? GetCurrentShotDirection()
            : directionFromInputToCueBall.normalized;

        aimingAngle = Mathf.Atan2(shotDirection.y, shotDirection.x);

        float clampedPullBackDistance = Mathf.Clamp(
            directionFromInputToCueBall.magnitude,
            0f,
            maxDragDistance
        );

        currentShotPower = Mathf.Clamp01(clampedPullBackDistance / maxDragDistance);

        currentCueCentreWorldPosition = cueBallWorldPosition -
            shotDirection * (cueCentreDistanceFromCueBallAtMinimumPower + clampedPullBackDistance);
    }

    private IEnumerator PlayCueStrikeAnimationThenShoot(float finalShotStrength)
    {
        isCueStrikeAnimationPlaying = true;
        SetCueVisible(true);

        Vector2 cueBallWorldPosition = target != null ? target.transform.position : Vector2.zero;
        Vector2 cueStrikeStartCueCentreWorldPosition = currentCueCentreWorldPosition;
        Vector2 cueStrikeEndCueCentreWorldPosition = GetMinimumPowerCueCentreWorldPosition(cueBallWorldPosition);

        for (float animationElapsedTime = 0f; animationElapsedTime < cueStrikeAnimationDuration; animationElapsedTime += Time.unscaledDeltaTime)
        {
            cueStrikeAnimationCueCentreWorldPosition = Vector2.Lerp(
                cueStrikeStartCueCentreWorldPosition,
                cueStrikeEndCueCentreWorldPosition,
                Mathf.Clamp01(animationElapsedTime / cueStrikeAnimationDuration)
            );

            yield return null;
        }

        cueStrikeAnimationCueCentreWorldPosition = cueStrikeEndCueCentreWorldPosition;

        ShootTargetBall(finalShotStrength);
        FinishCueStrikeAnimation();
    }

    private void ShootTargetBall(float finalShotStrength)
    {
        if (targetBall != null)
        {
            targetBall.Shoot(aimingAngle, finalShotStrength);
        }

        EventBus.Publish(new BallHasBeenShotEvent
        {
            Sender = this,
            Target = target
        });
    }

    private void FinishCueStrikeAnimation()
    {
        isCueStrikeAnimationPlaying = false;
        cueStrikeAnimationCoroutine = null;
        currentShotPower = 0f;
        activePrimaryTouchFingerId = -1;

        if (target != null)
        {
            currentCueCentreWorldPosition = GetMinimumPowerCueCentreWorldPosition(target.transform.position);
        }

        SetCueVisible(false);
    }

    private void ResetCueState()
    {
        StopCueStrikeAnimation();

        isDraggingCue = false;
        isCueStrikeAnimationPlaying = false;
        shouldIgnorePrimaryInputUntilReleased = false;
        isChargingStart = null;
        currentShotPower = 0f;
        activePrimaryTouchFingerId = -1;

        if (target != null)
        {
            currentCueCentreWorldPosition = GetMinimumPowerCueCentreWorldPosition(target.transform.position);
            cueStrikeAnimationCueCentreWorldPosition = currentCueCentreWorldPosition;
        }

        SetCueVisible(false);
    }

    private void SetPosition()
    {
        if (target == null) return;

        if (!isDraggingCue && !isCueStrikeAnimationPlaying)
        {
            currentCueCentreWorldPosition = GetMinimumPowerCueCentreWorldPosition(target.transform.position);
        }

        transform.position = isCueStrikeAnimationPlaying
            ? cueStrikeAnimationCueCentreWorldPosition
            : currentCueCentreWorldPosition;

        transform.rotation = Quaternion.Euler(0f, 0f, aimingAngle * Mathf.Rad2Deg);
    }

    private void ShowAimingLineIfNeeded()
    {
        if (target == null || !isDraggingCue || isCueStrikeAnimationPlaying) return;
        if (GameStateManager.Instance.CurrentGameState != GameState.Aiming) return;

        BallAimingLineController lineController = target.GetComponent<BallAimingLineController>();
        if (lineController == null) return;

        lineController.ShowTrajectory(target.transform.position, GetCurrentShotDirection());
    }

    private Vector2 GetMinimumPowerCueCentreWorldPosition(Vector2 cueBallWorldPosition)
    {
        return cueBallWorldPosition - GetCurrentShotDirection() * cueCentreDistanceFromCueBallAtMinimumPower;
    }

    private Vector2 GetCurrentShotDirection()
    {
        return new Vector2(Mathf.Cos(aimingAngle), Mathf.Sin(aimingAngle));
    }

    private void SetCueVisible(bool shouldBeVisible)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = shouldBeVisible;
        }
    }

    public Vector2 getOffset(float distance, float angle)
    {
        return GetOffset(distance, angle);
    }

    public Vector2 GetOffset(float distance, float angle)
    {
        return new Vector2(
            distance * Mathf.Cos(angle),
            distance * Mathf.Sin(angle)
        );
    }

    private static float GetUnscaledTime()
    {
        return Time.unscaledTime;
    }
}

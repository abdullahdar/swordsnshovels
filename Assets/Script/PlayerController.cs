using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;

    public enum PlayerControlMode { FirstPerson, ThirdPerson }
    public PlayerControlMode mode;

    // References
    [Space(20)]
    [SerializeField] private CharacterController characterController;
    [Header("First person camera")]
    [SerializeField] private Transform fpCameraTransform;
    [Header("Third person camera")]
    [SerializeField] private Transform cameraPole;
    [SerializeField] private Transform tpCameraTransform;
    [SerializeField] private Transform graphics;
    [Space(20)]

    // Player settings
    [Header("Settings")]
    [SerializeField] private float cameraSensitivity;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float moveInputDeadZone;

    [Header("Third person camera settings")]
    [SerializeField] private LayerMask cameraObstacleLayers;
    private float maxCameraDistance;
    private bool isMoving;

    // Touch detection
    private int leftFingerId, rightFingerId;
    private float halfScreenWidth;
    private Vector2 fingerDown;
    private Vector2 fingerUp;
    public bool detectSwipeOnlyAfterRelease = true;
    public float SWIPE_THRESHOLD = 20f;
    private bool swipeUp = false, swipeDown = false;
    private float rotateTill = 0;
    bool setRotateTill = true;

    // Camera control
    private Vector2 lookInput;
    private float cameraPitch;

    // Player movement
    private Vector2 moveTouchStartPosition;
    private Vector2 moveInput;

    private Animator anim;
    public bool isAttacking = false;

    private void Awake()
    {
        anim = graphics.GetComponent<Animator>();

        if (instance == null) instance = this;
        else if (instance != this) Destroy(gameObject);
    }

    private void Start()
    {
        // id = -1 means the finger is not being tracked
        leftFingerId = -1;
        rightFingerId = -1;

        // only calculate once
        halfScreenWidth = Screen.width / 2;

        // calculate the movement input dead zone
        moveInputDeadZone = Mathf.Pow(Screen.height / moveInputDeadZone, 2);

        if (mode == PlayerControlMode.ThirdPerson)
        {

            // Get the initial angle for the camera pole
            cameraPitch = cameraPole.localRotation.eulerAngles.x;

            // Set max camera distance to the distance the camera is from the player in the editor
            maxCameraDistance = tpCameraTransform.localPosition.z;
        }
    }

    private void Update()
    {
        // Handles input
        GetTouchInput();


        if (rightFingerId != -1)
        {
            // Ony look around if the right finger is being tracked
            //Debug.Log("Rotating");
            LookAround();
        }

        if (leftFingerId != -1)
        {
            // Ony move if the left finger is being tracked
            //Debug.Log("Moving");
            Move();
        }

        if(isMoving)
            anim.SetFloat("Speed", 1f);
        else
            anim.SetFloat("Speed", 0f);

        if(swipeDown)
        {
            if (transform.eulerAngles.y <= rotateTill)
            {
                transform.Rotate(0, Time.deltaTime * 120, 0, Space.Self);
            }
        }
    }

    private void FixedUpdate()
    {
        if (mode == PlayerControlMode.ThirdPerson) MoveCamera();
    }

    private void GetTouchInput()
    {
        // Iterate through all the detected touches
        for (int i = 0; i < Input.touchCount; i++)
        {
            var yPos = 0f;
            Touch t = Input.GetTouch(i);

            // Check each touch's phase
            switch (t.phase)
            {
                case TouchPhase.Began:
                    if (t.position.x < halfScreenWidth && leftFingerId == -1)
                    {
                        // Start tracking the left finger if it was not previously being tracked
                        leftFingerId = t.fingerId;

                        // Set the start position for the movement control finger
                        moveTouchStartPosition = t.position;

                        yPos = t.position.y;
                    }
                    else if (t.position.x > halfScreenWidth && rightFingerId == -1)
                    {
                        // Start tracking the rightfinger if it was not previously being tracked
                        rightFingerId = t.fingerId;
                    }

                    fingerUp = t.position;
                    fingerDown = t.position;

                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:

                    if (t.fingerId == leftFingerId)
                    {
                        // Stop tracking the left finger
                        leftFingerId = -1;
                        //Debug.Log("Stopped tracking left finger");
                        isMoving = false;
                    }
                    else if (t.fingerId == rightFingerId)
                    {
                        // Stop tracking the right finger
                        rightFingerId = -1;
                        //Debug.Log("Stopped tracking right finger");
                    }

                    fingerDown = t.position;
                    //checkSwipe();

                    break;
                case TouchPhase.Moved:

                    // Get input for looking around
                    if (t.fingerId == rightFingerId)
                    {
                        lookInput = t.deltaPosition * cameraSensitivity * Time.deltaTime;
                    }
                    else if (t.fingerId == leftFingerId)
                    {
                        // calculating the position delta from the start position
                        moveInput = t.position - moveTouchStartPosition;
                    }

                    if (!detectSwipeOnlyAfterRelease)
                    {
                        fingerDown = t.position;
                        checkSwipe();
                    }

                    break;
                case TouchPhase.Stationary:
                    // Set the look input to zero if the finger is still
                    if (t.fingerId == rightFingerId)
                    {
                        lookInput = Vector2.zero;
                    }
                    break;
            }
        }
    }

    private void LookAround()
    {

        switch (mode)
        {
            case PlayerControlMode.FirstPerson:
                // vertical (pitch) rotation is applied to the first person camera
                cameraPitch = Mathf.Clamp(cameraPitch - lookInput.y, -90f, 90f);
                fpCameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
                break;
            case PlayerControlMode.ThirdPerson:
                // vertical (pitch) rotation is applied to the third person camera pole
                cameraPitch = Mathf.Clamp(cameraPitch - lookInput.y, -90f, 90f);
                cameraPole.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
                break;
        }

        if (mode == PlayerControlMode.ThirdPerson && !isMoving)
        {
            // Rotate the graphics in the opposite direction when stationary
            graphics.Rotate(graphics.up, -lookInput.x);
        }
        // horizontal (yaw) rotation
        transform.Rotate(transform.up, lookInput.x);
    }

    private void MoveCamera()
    {

        Vector3 rayDir = tpCameraTransform.position - cameraPole.position;

        Debug.DrawRay(cameraPole.position, rayDir, Color.red);
        // Check if the camera would be colliding with any obstacle
        if (Physics.Raycast(cameraPole.position, rayDir, out RaycastHit hit, Mathf.Abs(maxCameraDistance), cameraObstacleLayers))
        {
            // Move the camera to the impact point
            tpCameraTransform.position = hit.point;
        }
        else
        {
            // Move the camera to the max distance on the local z axis
            tpCameraTransform.localPosition = new Vector3(0, 0, maxCameraDistance);
        }
    }

    private void Move()
    {
        Vector2 movementDirection = moveInput.normalized * moveSpeed * Time.deltaTime;

        //if (movementDirection.x > 0 && movementDirection.y < 0)
        //    transform.Rotate(0, -180, 0);


        // Don't move if the touch delta is shorter than the designated dead zone
        if (moveInput.sqrMagnitude <= moveInputDeadZone)
        {
            isMoving = false;
            return;
        }

        if (!isMoving)
        {
            graphics.localRotation = Quaternion.Euler(0, 0, 0);
            isMoving = true;
        }
        // Multiply the normalized direction by the speed
        // Move relatively to the local transform's direction
        characterController.Move(transform.right * movementDirection.x + transform.forward * movementDirection.y);

        //Debug.Log(string.Concat("TransformRight: ", transform.right, "movementDirectionX:", movementDirection.x, "transformForward:", transform.forward, "movementDirectionY:", movementDirection.y));
    }

    public void ResetInput()
    {
        // id = -1 means the finger is not being tracked
        leftFingerId = -1;
        rightFingerId = -1;
    }

    public void Attack()
    {
        isAttacking = true;
        anim.SetTrigger("StartAttack");
        anim.SetTrigger("StopAttack");
    }


    void checkSwipe()
    {
        //Check if Vertical swipe
        if (verticalMove() > SWIPE_THRESHOLD && verticalMove() > horizontalValMove())
        {
            //Debug.Log("Vertical");
            if (fingerDown.y - fingerUp.y > 0)//up swipe
            {
                OnSwipeUp();
                swipeUp = true;
            }
            else if (fingerDown.y - fingerUp.y < 0)//Down swipe
            {
                OnSwipeDown();
                swipeDown = true;

                var yRotation = transform.eulerAngles.y;
                rotateTill = yRotation == 0 ? 180 : 0;
            }
            fingerUp = fingerDown;
        }

        //Check if Horizontal swipe
        else if (horizontalValMove() > SWIPE_THRESHOLD && horizontalValMove() > verticalMove())
        {
            //Debug.Log("Horizontal");
            if (fingerDown.x - fingerUp.x > 0)//Right swipe
            {
                OnSwipeRight();
            }
            else if (fingerDown.x - fingerUp.x < 0)//Left swipe
            {
                OnSwipeLeft();
            }
            fingerUp = fingerDown;
        }

        //No Movement at-all
        else
        {
            //Debug.Log("No Swipe!");
        }
    }

    float verticalMove()
    {
        return Mathf.Abs(fingerDown.y - fingerUp.y);
    }

    float horizontalValMove()
    {
        return Mathf.Abs(fingerDown.x - fingerUp.x);
    }

    //////////////////////////////////CALLBACK FUNCTIONS/////////////////////////////
    void OnSwipeUp()
    {
        Debug.Log("Swipe UP");
    }

    void OnSwipeDown()
    {
        Debug.Log("Swipe Down");
    }

    void OnSwipeLeft()
    {
        Debug.Log("Swipe Left");
    }

    void OnSwipeRight()
    {
        Debug.Log("Swipe Right");
    }
}
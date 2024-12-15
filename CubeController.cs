using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
#if UNITY_EDITOR
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
#endif

public class JakeController : MonoBehaviour
{
    public float forwardSpeed = 5f; // initial forward speed
    public float accelerationRate = 0.01f; // small rate of acceleration
    public float maxSpeed = 15f; // Max forward speed

    public float laneDistance = 2f;
    public float forwardShiftDistance = 1f;
    public int currentLane = 0;

    public float laneSwitchSpeed = 5f;
    public Vector3 targetPosition;
    public bool isSwitchingLanes = false;

    public bool isJumping = false;
    private Rigidbody rb;
    public float jumpForce = 5f;
    public float forwardJumpBoost = 2f;
    public float moreGravity = 40f;

    public bool isSliding = false;
    private float slideCooldown = 1f;
    private float lastSlideTime;

    private bool isGameOver = false;
    public GameOverManager gameOverManager;

    public float slopeAngleThreshold = 45f; 
    public LayerMask groundLayer;

    private bool isOnSlope = false;
    public float fallThresholdHeight = 2.4f; 

    private bool isStickingToPlane;


    private bool isMovingForward = false;

    private Animator animator;


    public AudioClip coinSound; // Assign the coin sound in the Inspector
    private AudioSource audioSource;
    

    public Material glowMaterial; // Drag your new GoldGlowMaterial here
    private Material originalMaterial; // Store the original material to reset later
    private Renderer playerRenderer; // Reference to the player's renderer

    public bool flagHuH = false;

    private BoxCollider boxCollider;
    private Vector3 originalSize;
    private Vector3 originalCenter;

    public float slideHeight = 0.5f; // New height of the collider during slide
    private float originalHeight;



    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;


        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = coinSound;

        // Get the player's renderer
        playerRenderer = GetComponentInChildren<Renderer>();

        // Save the player's original material
        if (playerRenderer != null)
        {
            originalMaterial = playerRenderer.material;
        }


        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            originalSize = boxCollider.size;
            originalCenter = boxCollider.center;
            originalHeight = boxCollider.size.y;
        }
        else
        {
            Debug.LogError("No BoxCollider found on the character!");
        }



        targetPosition = transform.position;
        rb = GetComponent<Rigidbody>();  // Get Rigidbody component
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();  // Add Rigidbody if missing
        }

        rb.drag = 0.03f;
        rb.angularDrag = 0.05f;

        lastSlideTime = -slideCooldown;
        animator = GetComponent<Animator>();  // Get Animator component

        // **Set the initial position to the left lane**
        float startXPosition = 35f;
        transform.position = new Vector3(startXPosition, transform.position.y, (laneDistance));
        

        targetPosition = transform.position;

        // **Trigger the kneeling animation at the start**
        StartCoroutine(lobbyAnimation());

        StartCoroutine(PreloadAssets());
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isGameOver) return;

        if (Input.GetKeyDown(KeyCode.Space) && !isMovingForward)
        {
            flagHuH = true;
            
            StartCoroutine(DelayStartMovingForward());

        }

        // Move the character forward
        if (isMovingForward)
        {
            // Gradually increase the speed by a very small amount over time
            forwardSpeed += accelerationRate * Time.deltaTime;

            // Clamp the speed to ensure it doesn't exceed the maximum
            forwardSpeed = Mathf.Clamp(forwardSpeed, 0f, maxSpeed);

            // Move the character forward
            transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime);
        }

        // Update animation states
        animator.SetBool("isRunning", !isJumping && !isSliding);

        // Handle lane movement
        if (Input.GetKeyDown(KeyCode.LeftArrow) && currentLane > 0)
        {
            currentLane--;
            SetTargetPosition();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) && currentLane < 2)
        {
            currentLane++;
            SetTargetPosition();
        }

        // Jump logic
        if (Input.GetKeyDown(KeyCode.UpArrow) && !isJumping)
        {
            StartCoroutine(Jump());
        }

        // Slide logic
        if (Input.GetKeyDown(KeyCode.DownArrow) && !isSliding && !isJumping && Time.time - lastSlideTime > slideCooldown)
        {
            lastSlideTime = Time.time;
            Slide();
        }


        if (Input.GetKeyDown(KeyCode.S))
        {
            SnapToClosestLane();
        }


        // Lane switching logic
        if (isSwitchingLanes)
        {
            Vector3 newPosition = new Vector3(transform.position.x, transform.position.y, Mathf.Lerp(transform.position.z, targetPosition.z, Time.deltaTime * laneSwitchSpeed));
            transform.position = newPosition;

            if (Vector3.Distance(newPosition, targetPosition) < 0.1f)
            {
                transform.position = targetPosition;
                isSwitchingLanes = false;
            }
        }


    }


    





    void FixedUpdate()
    {


        if (isJumping && rb.velocity.y < 0)
        {

            EndJump();

        }


        if (isStickingToPlane)
        {
            StickToPlane(); // Call the method to stick to the plane
        }

        CheckForSlope();


    }


    private IEnumerator lobbyAnimation()
    {
        // Trigger the looping animation
        animator.SetTrigger("kneel");

        // Wait for the specified loop duration
        yield return new WaitForSeconds(10f);

        // Trigger the next animation
        animator.SetTrigger("raptrig");
    }


    void SnapToClosestLane()
    {
        // Get the current z position using transform
        float currentZ = transform.position.z;

        // Determine the closest lane position
        if (Mathf.Abs(currentZ - 0) < Mathf.Abs(currentZ - 2.8f) && Mathf.Abs(currentZ - 0) < Mathf.Abs(currentZ + 2.8f))
        {
            currentLane = 1; // Middle lane
        }
        else if (Mathf.Abs(currentZ + 2.8f) < Mathf.Abs(currentZ - 2.8f))
        {
            currentLane = 2; // Left lane
        }
        else
        {
            currentLane = 0; // Right lane
        }

        // Set the target position based on the snapped lane
        SetTargetPosition();

        // Snap the character to the new lane position using transform
        transform.position = new Vector3(transform.position.x, transform.position.y, targetPosition.z);

        // Reset rotation to face forward
        transform.rotation = Quaternion.Euler(0, 90, 0); // Adjust the angle as necessary to face forward

        // Allow for lane switching immediately after snapping
        isSwitchingLanes = false; // Allow movement after snapping
    }


    void StickToPlane()
    {

        // Check if the character is not jumping
        if (!isJumping)
        {
            // Raycast downwards to check for the plane below
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.5f)) // Adjust distance if needed
            {
                // Set the Y position to the height of the plane only when landing
                if (transform.position.y > hit.point.y) // Check if character is above the plane
                {
                    Vector3 pos = transform.position;
                    pos.y = hit.point.y; // Set Y position to the height of the plane
                    transform.position = pos;
                }

                // Freeze Y and Z positions to prevent floating
                rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionY;
            }
            else
            {
                // If not on a plane, remove constraints or handle differently if needed
                rb.constraints = RigidbodyConstraints.None; // or whatever logic you need
            }
        }

    }


    void StickToSlope()
    {
        // Only adjust if the character is not jumping
        if (!isJumping)
        {
            RaycastHit hit;
            // Raycast downwards from the character's position to detect the slope
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.5f, groundLayer))
            {
                Vector3 pos = transform.position;

                // Smoothly move character's Y position to match the slope
                pos.y = Mathf.Lerp(transform.position.y, hit.point.y, Time.deltaTime * 10f); // Adjust speed factor if needed
                transform.position = pos;

                // Optionally apply a small downward force to keep the character "grounded"
                Vector3 downwardForce = new Vector3(0, -Physics.gravity.y * 2f, 0); // Adjust multiplier for smoothness
                rb.AddForce(downwardForce, ForceMode.Acceleration);
            }
            else
            {
                // Reset constraints if the character is not on a slope
                rb.constraints = RigidbodyConstraints.None;
            }
        }
    }






    void FreezeFallingRotation()
    {
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionZ;
    }

    void ResetRotationConstraints()
    {
        rb.constraints = RigidbodyConstraints.None; // Reset constraints after the fall
    }


    void SetTargetPosition()
    {
        targetPosition = new Vector3(transform.position.x, transform.position.y, (1 - currentLane) * laneDistance);
        isSwitchingLanes = true;
    }

    IEnumerator Jump()
    {
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.velocity = new Vector3(rb.velocity.x + forwardJumpBoost, jumpForce, rb.velocity.z);
        isJumping = true;

        // Trigger the jump animation
        animator.SetTrigger("flip");
        yield return new WaitForSeconds(1.6f);

        StartCoroutine(ApplyExtraGravity());
    }

    IEnumerator ApplyExtraGravity()
    {
        yield return new WaitForSeconds(0.05f);
        while (isJumping)
        {
            rb.AddForce(Vector3.down * moreGravity * Time.deltaTime, ForceMode.Acceleration);
            yield return null;
        }
    }

    void EndJump()
    {
        isJumping = false;
        animator.SetBool("isRunning", true); // Return to running animation
    }


    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
        {
            Debug.Log("Collided with: " + collision.gameObject.name);


        }
        if (collision.gameObject.CompareTag("StickPlane"))
        {
            isStickingToPlane = true; // Enable sticking to the plane
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
        {
            GameOver();
        }
    }


    private void OnCollisionExit(Collision collision)
    {
        // Check if the character exited a collision with a "StickPlane"
        if (collision.gameObject.CompareTag("StickPlane"))
        {
            isStickingToPlane = false; // Disable sticking to the plane
        }
    }


    void Slide()
    {
        isSliding = true;
        StartBoxcolliderSlide();
        animator.SetTrigger("slide");


        StartCoroutine(EndSlide());
    }

    IEnumerator EndSlide()
    {
        yield return new WaitForSeconds(1f);
        isSliding = false;
        animator.SetBool("isRunning", true); // Return to running animation
        EndBoxcolliderSlide();
    }

    public void StartBoxcolliderSlide()
    {
        if (boxCollider != null)
        {
            float heightReduction = originalHeight - slideHeight;
            boxCollider.size = new Vector3(boxCollider.size.x, slideHeight, boxCollider.size.z);
            boxCollider.center = new Vector3(originalCenter.x, originalCenter.y - (heightReduction / 2), originalCenter.z);
        }
    }

    public void EndBoxcolliderSlide()
    {
        if (boxCollider != null)
        {
            boxCollider.size = originalSize;
            boxCollider.center = originalCenter;
        }
    }

    public void GameOver()
    {
        isGameOver = true;
        forwardSpeed = 0;
        rb.velocity = Vector3.zero;
        animator.SetBool("isRunning", false);
        animator.speed = 0;

        gameOverManager.Part2GameOver();
    }

    IEnumerator PreloadAssets()
    {
        yield return new WaitForSeconds(4f);
    }

    private bool IsGrounded()
    {
        Debug.Log("isGrounded");
        return Physics.Raycast(transform.position, Vector3.down, 1.1f); // Adjust distance as necessary
    }


    void CheckForSlope()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.5f, groundLayer))
        {
            Vector3 normal = hit.normal;
            isOnSlope = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < (1 - slopeAngleThreshold);

            if (isOnSlope)
            {
                // Only freeze rotations, but allow movement on the slope
                rb.constraints = RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezePositionZ;
                StickToSlope();

            }
            else
            {
                // Release constraints when off the slope
                rb.constraints = RigidbodyConstraints.None;
            }
        }
    }



    public void StopForwardMovement()
    {
        forwardSpeed = 0f;  // Stop the forward speed
    }





    public void TriggerGlowEffect()
    {
        if (playerRenderer != null)
        {
            // Set the glow material
            playerRenderer.material = glowMaterial;

            // Start coroutine to remove the glow after 0.2 seconds
            StartCoroutine(RemoveGlowEffect());
        }
    }

    // Coroutine to remove the glow effect after a short delay
    IEnumerator RemoveGlowEffect()
    {
        yield return new WaitForSeconds(0.2f);

        // Reset the material back to the original
        playerRenderer.material = originalMaterial;
    }



    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that collided is tagged as "Coin"
        if (other.gameObject.CompareTag("Coin"))
        {
            // Play the coin sound
            audioSource.Play();
            TriggerGlowEffect();
        }
    }

    private IEnumerator DelayStartMovingForward()
    {
        animator.SetTrigger("scared");
        yield return new WaitForSeconds(1.5f); // Wait for 1.5 seconds

        isMovingForward = true;  // Start moving forward
        transform.rotation = Quaternion.Euler(0, 90, 0);  // Face forward
        animator.SetTrigger("run");
        currentLane = 1;
        Invoke("SetTargetPosition", 1f);
    }

}

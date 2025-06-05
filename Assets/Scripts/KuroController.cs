using UnityEngine;

public class KuroController : MonoBehaviour
{
    public enum KuroState
    {
        Following,
        Fetching,
        ReturningWithBall,
        Idle
    }
    
    [Header("Movement")]
    [SerializeField] private float followSpeed = 1f;
    [SerializeField] private float fetchSpeed = 2f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float stopDistance = 1.5f;
    [SerializeField] private float ballPickupDistance = 0.3f;
    
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform ballHoldPoint; // Create empty child GameObject for this
    
    private Transform playerTransform;
    private KuroState currentState = KuroState.Following;
    private GameObject targetBall;
    private bool isMoving = false;
    
    void Start()
    {
        playerTransform = Camera.main.transform;
        if (!animator) animator = GetComponent<Animator>();
        if (!rb) rb = GetComponent<Rigidbody>();
        
        // Create ball hold point if doesn't exist
        if (!ballHoldPoint)
        {
            GameObject holdPoint = new GameObject("BallHoldPoint");
            holdPoint.transform.SetParent(transform);
            holdPoint.transform.localPosition = new Vector3(0, 0.05f, 0.1f); // In front of Kuro
            ballHoldPoint = holdPoint.transform;
        }
    }
    
    void Update()
    {
        if (playerTransform == null) return;
        
        switch (currentState)
        {
            case KuroState.Following:
                UpdateFollowing();
                break;
            case KuroState.Fetching:
                UpdateFetching();
                break;
            case KuroState.ReturningWithBall:
                UpdateReturning();
                break;
            case KuroState.Idle:
                UpdateIdle();
                break;
        }
        
        // Update animator
        animator.SetFloat("Speed", isMoving ? 1f : 0f);
    }
    
    void UpdateFollowing()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        if (distanceToPlayer > stopDistance)
        {
            MoveTowards(playerTransform.position, followSpeed);
            isMoving = true;
        }
        else
        {
            StopMoving();
            isMoving = false;
        }
    }
    
    void UpdateFetching()
    {
        if (targetBall == null)
        {
            currentState = KuroState.Following;
            return;
        }
        
        float distanceToBall = Vector3.Distance(transform.position, targetBall.transform.position);
        
        if (distanceToBall < ballPickupDistance)
        {
            PickupBall();
        }
        else
        {
            MoveTowards(targetBall.transform.position, fetchSpeed);
            isMoving = true;
        }
    }
    
    void UpdateReturning()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        if (distanceToPlayer < stopDistance)
        {
            DropBall();
        }
        else
        {
            MoveTowards(playerTransform.position, followSpeed);
            isMoving = true;
        }
    }
    
    void UpdateIdle()
    {
        StopMoving();
        isMoving = false;
        
        // Return to following after idle
        if (Vector3.Distance(transform.position, playerTransform.position) > stopDistance * 2)
        {
            currentState = KuroState.Following;
        }
    }
    
    void MoveTowards(Vector3 targetPosition, float speed)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Keep movement on ground plane
        
        // Move using rigidbody
        Vector3 targetVelocity = direction * speed;
        targetVelocity.y = rb.velocity.y; // Preserve gravity
        rb.velocity = targetVelocity;
        
        // Rotate towards movement direction
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    void StopMoving()
    {
        Vector3 velocity = rb.velocity;
        velocity.x = 0;
        velocity.z = 0;
        rb.velocity = velocity;
    }
    
    public void OnObjectThrown(GameObject thrownObject)
    {
        if (currentState == KuroState.Following || currentState == KuroState.Idle)
        {
            targetBall = thrownObject;
            currentState = KuroState.Fetching;
            Debug.Log("Kuro is fetching the ball!");
        }
    }
    
    void PickupBall()
    {
        if (targetBall != null)
        {
            // Attach ball to Kuro
            targetBall.transform.SetParent(ballHoldPoint);
            targetBall.transform.localPosition = Vector3.zero;
            
            // Disable ball physics while carrying
            Rigidbody ballRb = targetBall.GetComponent<Rigidbody>();
            if (ballRb) ballRb.isKinematic = true;
            
            currentState = KuroState.ReturningWithBall;
            Debug.Log("Kuro picked up the ball!");
        }
    }
    
    void DropBall()
    {
        if (targetBall != null)
        {
            // Detach ball
            targetBall.transform.SetParent(null);
            
            // Re-enable physics
            Rigidbody ballRb = targetBall.GetComponent<Rigidbody>();
            if (ballRb)
            {
                ballRb.isKinematic = false;
                ballRb.velocity = Vector3.zero;
            }
            
            Debug.Log("Kuro dropped the ball!");
            targetBall = null;
            currentState = KuroState.Idle;
        }
    }
}
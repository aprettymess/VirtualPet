using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Controls Kuro's intelligent companion behavior including following, exploration, and interaction
/// Integrates navigation, animation, and emotional systems for believable pet behavior
/// </summary>
public class KuroCompanionController : MonoBehaviour
{
    [Header("Player Tracking")]
    [SerializeField] private Transform playerTarget; // Will be set to camera/head position
    [SerializeField] private float followDistance = 2.0f; // How close Kuro stays to player
    [SerializeField] private float maxFollowDistance = 4.0f; // Distance that triggers urgent following
    
    [Header("Movement Behavior")]
    [SerializeField] private float walkingSpeed = 1.5f; // Normal movement speed
    [SerializeField] private float runningSpeed = 3.5f; // Fast movement speed for catching up
    [SerializeField] private float rotationSpeed = 120f; // How quickly Kuro turns
    [SerializeField] private float stoppingDistance = 1.8f; // Distance where Kuro stops following
    
    [Header("Behavior Timing")]
    [SerializeField] private float idleCheckInterval = 2.0f; // How often to check if player moved
    [SerializeField] private float explorationChance = 0.3f; // Probability of independent exploration
    [SerializeField] private float attentionDuration = 5.0f; // How long Kuro pays attention to player
    
    [Header("Animation Integration")]
    [SerializeField] private Animator kuroAnimator; // Reference to animation controller
    [SerializeField] private string speedParameterName = "Speed"; // Animation parameter for movement
    [SerializeField] private string isHappyParameterName = "IsHappy"; // Animation parameter for emotions
    
    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGizmos = true;
    
    // Core Components
    private NavMeshAgent navigationAgent;
    private Rigidbody kuroRigidbody;
    private KuroInitializer initializationController;
    
    // Behavior State
    public enum CompanionState
    {
        Initializing,    // Waiting for systems to be ready
        Following,       // Actively following the player
        Idle,           // Standing still, watching player
        Exploring,      // Independent movement and investigation
        Returning,      // Coming back to player after exploration
        Excited         // High-energy response to player interaction
    }
    
    [Header("Current State")]
    [SerializeField] private CompanionState currentState = CompanionState.Initializing;
    
    // Internal State Management
    private Vector3 lastPlayerPosition;
    private Vector3 currentDestination;
    private float timeSincePlayerMoved;
    private float timeSinceStateChange;
    private bool isPlayerNearby;
    private bool hasValidNavigation;
    
    // Performance Optimization
    private WaitForSeconds stateUpdateDelay;
    private Coroutine behaviorUpdateCoroutine;
    
    /// <summary>
    /// Initialize all components and references
    /// </summary>
    void Start()
    {
        InitializeComponents();
        InitializePlayerTracking();
        InitializeAnimationSystem();
        
        // Start the main behavior loop
        stateUpdateDelay = new WaitForSeconds(0.1f); // Update behavior 10 times per second
        behaviorUpdateCoroutine = StartCoroutine(BehaviorUpdateLoop());
        
        if (showDebugInfo) Debug.Log("KuroCompanionController: Initialization complete");
    }
    
    /// <summary>
    /// Set up references to all required components
    /// </summary>
    private void InitializeComponents()
    {
        // Get essential components
        navigationAgent = GetComponent<NavMeshAgent>();
        kuroRigidbody = GetComponent<Rigidbody>();
        initializationController = GetComponent<KuroInitializer>();
        
        // Validate critical components
        if (navigationAgent == null)
        {
            Debug.LogError("KuroCompanionController: NavMeshAgent component required!");
            enabled = false;
            return;
        }
        
        if (kuroAnimator == null)
        {
            kuroAnimator = GetComponentInChildren<Animator>();
            if (kuroAnimator == null)
            {
                Debug.LogWarning("KuroCompanionController: No Animator found - animations will not work");
            }
        }
        
        // Configure navigation agent for pet behavior
        SetupNavigationAgent();
    }
    
    /// <summary>
    /// Configure the NavMeshAgent for realistic pet movement
    /// </summary>
    private void SetupNavigationAgent()
    {
        navigationAgent.speed = walkingSpeed;
        navigationAgent.angularSpeed = rotationSpeed;
        navigationAgent.acceleration = 8.0f; // Quick acceleration for responsive movement
        navigationAgent.stoppingDistance = stoppingDistance;
        navigationAgent.autoBraking = true; // Smooth deceleration when approaching destinations
        
        // Configure avoidance for natural movement through spaces
        navigationAgent.avoidancePriority = 50; // Medium priority for navigation
        navigationAgent.radius = 0.3f; // Match our physics collider radius
        navigationAgent.height = 1.0f; // Match our physics collider height
    }
    
    /// <summary>
    /// Set up player tracking reference
    /// </summary>
    private void InitializePlayerTracking()
    {
        // Use the main camera (player's head) as the follow target
        if (playerTarget == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                playerTarget = mainCamera.transform;
            }
            else
            {
                Debug.LogError("KuroCompanionController: No player target found! Assign manually or ensure Camera.main exists");
                enabled = false;
                return;
            }
        }
        
        // Initialize tracking variables
        lastPlayerPosition = playerTarget.position;
        currentDestination = transform.position;
    }
    
    /// <summary>
    /// Prepare animation system integration
    /// </summary>
    private void InitializeAnimationSystem()
    {
        if (kuroAnimator != null)
        {
            // Set initial animation state
            UpdateAnimationParameters(0f, false);
        }
    }
    
    /// <summary>
    /// Main behavior update loop - runs continuously to manage Kuro's behavior
    /// </summary>
    private IEnumerator BehaviorUpdateLoop()
    {
        while (enabled)
        {
            // Wait for initialization to complete before starting behavior
            if (initializationController != null && !initializationController.IsInitialized())
            {
                yield return stateUpdateDelay;
                continue;
            }
            
            // Check if navigation is available
            if (!CheckNavigationValidity())
            {
                yield return stateUpdateDelay;
                continue;
            }
            
            // Update behavior based on current state
            UpdateBehaviorState();
            UpdatePlayerTracking();
            UpdateAnimationBasedOnMovement();
            
            yield return stateUpdateDelay;
        }
    }
    
    /// <summary>
    /// Verify that navigation system is ready and functional
    /// </summary>
    private bool CheckNavigationValidity()
    {
        // Check if we're on a valid NavMesh
        NavMeshHit hit;
        hasValidNavigation = NavMesh.SamplePosition(transform.position, out hit, 1.0f, NavMesh.AllAreas);
        
        if (!hasValidNavigation && showDebugInfo)
        {
            Debug.LogWarning("KuroCompanionController: Not on valid NavMesh - waiting for navigation");
        }
        
        return hasValidNavigation;
    }
    
    /// <summary>
    /// Update behavior based on current state and conditions
    /// </summary>
    private void UpdateBehaviorState()
    {
        // Calculate important metrics for decision making
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        float playerMovementMagnitude = Vector3.Distance(playerTarget.position, lastPlayerPosition);
        
        // Update timing variables
        timeSinceStateChange += Time.deltaTime;
        
        if (playerMovementMagnitude > 0.1f)
        {
            timeSincePlayerMoved = 0f;
        }
        else
        {
            timeSincePlayerMoved += Time.deltaTime;
        }
        
        // Determine if player is nearby
        isPlayerNearby = distanceToPlayer <= maxFollowDistance;
        
        // State transition logic
        switch (currentState)
        {
            case CompanionState.Initializing:
                HandleInitializingState();
                break;
                
            case CompanionState.Following:
                HandleFollowingState(distanceToPlayer, playerMovementMagnitude);
                break;
                
            case CompanionState.Idle:
                HandleIdleState(distanceToPlayer, timeSincePlayerMoved);
                break;
                
            case CompanionState.Exploring:
                HandleExploringState(distanceToPlayer);
                break;
                
            case CompanionState.Returning:
                HandleReturningState(distanceToPlayer);
                break;
                
            case CompanionState.Excited:
                HandleExcitedState();
                break;
        }
    }
    
    /// <summary>
    /// Handle behavior during initialization phase
    /// </summary>
    private void HandleInitializingState()
    {
        // Stay still until all systems are ready
        navigationAgent.isStopped = true;
        
        if (hasValidNavigation)
        {
            ChangeState(CompanionState.Idle);
        }
    }
    
    /// <summary>
    /// Handle active following behavior
    /// </summary>
    private void HandleFollowingState(float distanceToPlayer, float playerMovementMagnitude)
    {
        // If player stopped moving and we're close enough, transition to idle
        if (playerMovementMagnitude < 0.05f && distanceToPlayer <= followDistance)
        {
            ChangeState(CompanionState.Idle);
            return;
        }
        
        // If player is very far away, run to catch up
        if (distanceToPlayer > maxFollowDistance)
        {
            SetMovementSpeed(runningSpeed);
        }
        else
        {
            SetMovementSpeed(walkingSpeed);
        }
        
        // Update destination to follow player
        MoveTowardsPlayer();
    }
    
    /// <summary>
    /// Handle idle/waiting behavior
    /// </summary>
    private void HandleIdleState(float distanceToPlayer, float timeSincePlayerMoved)
    {
        // Stop moving and face the player
        navigationAgent.isStopped = true;
        
        // If player moved or is getting far away, start following
        if (timeSincePlayerMoved < 0.5f || distanceToPlayer > followDistance)
        {
            ChangeState(CompanionState.Following);
            return;
        }
        
        // After being idle for a while, occasionally explore
        if (timeSinceStateChange > idleCheckInterval && Random.value < explorationChance)
        {
            ChangeState(CompanionState.Exploring);
        }
    }
    
    /// <summary>
    /// Handle independent exploration behavior
    /// </summary>
    private void HandleExploringState(float distanceToPlayer)
    {
        // If player moved far away, return immediately
        if (distanceToPlayer > maxFollowDistance)
        {
            ChangeState(CompanionState.Returning);
            return;
        }
        
        // If we've been exploring for a while, return to player
        if (timeSinceStateChange > 8.0f)
        {
            ChangeState(CompanionState.Returning);
            return;
        }
        
        // Continue exploration movement
        if (!navigationAgent.pathPending && navigationAgent.remainingDistance < 0.5f)
        {
            FindExplorationDestination();
        }
    }
    
    /// <summary>
    /// Handle returning to player after exploration
    /// </summary>
    private void HandleReturningState(float distanceToPlayer)
    {
        SetMovementSpeed(walkingSpeed);
        MoveTowardsPlayer();
        
        // When close enough to player, return to idle
        if (distanceToPlayer <= followDistance)
        {
            ChangeState(CompanionState.Idle);
        }
    }
    
    /// <summary>
    /// Handle excited/happy behavior state
    /// </summary>
    private void HandleExcitedState()
    {
        // Excited state is temporary - return to following after a short time
        if (timeSinceStateChange > 3.0f)
        {
            ChangeState(CompanionState.Following);
        }
    }
    
    /// <summary>
    /// Change to a new behavior state with proper cleanup
    /// </summary>
    private void ChangeState(CompanionState newState)
    {
        if (currentState == newState) return;
        
        if (showDebugInfo)
        {
            Debug.Log($"KuroCompanionController: State change from {currentState} to {newState}");
        }
        
        currentState = newState;
        timeSinceStateChange = 0f;
        
        // State-specific initialization
        switch (newState)
        {
            case CompanionState.Following:
                navigationAgent.isStopped = false;
                UpdateAnimationParameters(navigationAgent.speed, false);
                break;
                
            case CompanionState.Idle:
                navigationAgent.isStopped = true;
                UpdateAnimationParameters(0f, false);
                break;
                
            case CompanionState.Exploring:
                navigationAgent.isStopped = false;
                FindExplorationDestination();
                UpdateAnimationParameters(walkingSpeed, true);
                break;
                
            case CompanionState.Excited:
                UpdateAnimationParameters(0f, true);
                break;
        }
    }
    
    /// <summary>
    /// Move towards the player's position
    /// </summary>
    private void MoveTowardsPlayer()
    {
        if (playerTarget == null) return;
        
        Vector3 targetPosition = playerTarget.position;
        
        // Calculate a position near the player but not exactly on top of them
        Vector3 directionToPlayer = (transform.position - targetPosition).normalized;
        Vector3 desiredPosition = targetPosition + directionToPlayer * followDistance;
        
        // Make sure the destination is on the NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(desiredPosition, out hit, 2.0f, NavMesh.AllAreas))
        {
            currentDestination = hit.position;
            navigationAgent.SetDestination(currentDestination);
            navigationAgent.isStopped = false;
        }
    }
    
    /// <summary>
    /// Find a nearby location for exploration
    /// </summary>
    private void FindExplorationDestination()
    {
        // Look for a random nearby location to explore
        Vector3 randomDirection = Random.insideUnitSphere * 3.0f;
        randomDirection += transform.position;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, 3.0f, NavMesh.AllAreas))
        {
            currentDestination = hit.position;
            navigationAgent.SetDestination(currentDestination);
            SetMovementSpeed(walkingSpeed * 0.7f); // Slower speed for exploration
        }
    }
    
    /// <summary>
    /// Update movement speed and corresponding animation
    /// </summary>
    private void SetMovementSpeed(float speed)
    {
        navigationAgent.speed = speed;
        UpdateAnimationParameters(speed, currentState == CompanionState.Excited);
    }
    
    /// <summary>
    /// Update player position tracking
    /// </summary>
    private void UpdatePlayerTracking()
    {
        lastPlayerPosition = playerTarget.position;
    }
    
    /// <summary>
    /// Update animation parameters based on movement and emotional state
    /// </summary>
    private void UpdateAnimationBasedOnMovement()
    {
        if (kuroAnimator == null) return;
        
        // Calculate current movement speed for animation
        float currentSpeed = navigationAgent.velocity.magnitude;
        bool isHappy = (currentState == CompanionState.Excited || currentState == CompanionState.Exploring);
        
        UpdateAnimationParameters(currentSpeed, isHappy);
    }
    
    /// <summary>
    /// Send parameters to the animation system
    /// </summary>
    private void UpdateAnimationParameters(float speed, bool isHappy)
    {
        if (kuroAnimator == null) return;
        
        // Update speed parameter for movement animations
        kuroAnimator.SetFloat(speedParameterName, speed);
        
        // Update emotional state
        kuroAnimator.SetBool(isHappyParameterName, isHappy);
    }
    
    /// <summary>
    /// Public method to trigger excited behavior (for external interactions)
    /// </summary>
    public void TriggerExcitedState()
    {
        ChangeState(CompanionState.Excited);
    }
    
    /// <summary>
    /// Public method to check current behavior state
    /// </summary>
    public CompanionState GetCurrentState()
    {
        return currentState;
    }
    
    /// <summary>
    /// Visualization for debugging in Scene view
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
    
        // Draw follow distance circle using line segments (compatible with all Unity versions)
        Gizmos.color = Color.green;
        DrawWireCircleCompatible(transform.position, followDistance, 32);
    
        // Draw max follow distance circle
        Gizmos.color = Color.yellow;
        DrawWireCircleCompatible(transform.position, maxFollowDistance, 32);
    
        // Draw line to current destination
        if (currentDestination != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, currentDestination);
            Gizmos.DrawWireCube(currentDestination, Vector3.one * 0.2f);
        }
    
        // Draw line to player
        if (playerTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTarget.position);
        }
    }

    /// <summary>
    /// Draw a wire circle using line segments - compatible with all Unity versions
    /// </summary>
    private void DrawWireCircleCompatible(Vector3 center, float radius, int segments)
    {
        if (segments < 3) segments = 3;
    
        float angleStep = 360f / segments;
        Vector3 previousPoint = center + new Vector3(radius, 0, 0);
    
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 currentPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
        
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    }
    
    /// <summary>
    /// Clean up coroutines when disabled
    /// </summary>
    private void OnDisable()
    {
        if (behaviorUpdateCoroutine != null)
        {
            StopCoroutine(behaviorUpdateCoroutine);
        }
    }
}
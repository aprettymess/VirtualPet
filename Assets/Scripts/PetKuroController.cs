using UnityEngine;
using UnityEngine.AI;

public class PetKuroController : MonoBehaviour
{
    [Header("Following Behavior")]
    public Transform player;
    public float followDistance = 3f;
    public float updateRate = 0.1f;
    
    [Header("Animation References")]
    public AnimationClip walkAnimation;
    public AnimationClip idleAnimation;
    
    [Header("Components")]
    private NavMeshAgent agent;
    private Animator animator;
    
    private float lastUpdateTime;
    private bool isMoving = false;
    
    void Start()
    {
        // Get components
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // Find player (camera)
        if (player == null)
        {
            // Look for Meta building blocks camera
            GameObject cameraRig = GameObject.Find("[BuildingBlock] Camera Rig");
            if (cameraRig == null)
                cameraRig = GameObject.Find("OVRCameraRig");
            if (cameraRig == null)
                cameraRig = GameObject.Find("XR Origin");
                
            if (cameraRig != null)
            {
                // Try to find the center eye camera
                Transform centerEye = cameraRig.transform.Find("TrackingSpace/CenterEyeAnchor");
                if (centerEye != null)
                    player = centerEye;
                else
                    player = cameraRig.transform;
            }
        }
        
        if (player == null)
        {
            Debug.LogError("No player/camera found for Kuro to follow!");
        }
        
        // Validate animations
        if (walkAnimation == null)
            Debug.LogWarning("Walk animation not assigned to Kuro!");
        if (idleAnimation == null)
            Debug.LogWarning("Idle animation not assigned to Kuro!");
            
        Debug.Log("Kuro started! Following: " + (player ? player.name : "NOBODY"));
    }
    
    void Update()
    {
        if (player == null) return;
        
        // Update pathfinding at intervals
        if (Time.time - lastUpdateTime > updateRate)
        {
            UpdateFollowBehavior();
            lastUpdateTime = Time.time;
        }
        
        // Update animations
        UpdateAnimations();
    }
    
    void UpdateFollowBehavior()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer > followDistance)
        {
            // Calculate position to move to
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            Vector3 targetPosition = player.position - directionToPlayer * (followDistance - 1f);
            
            agent.SetDestination(targetPosition);
            isMoving = true;
        }
        else
        {
            // Close enough, stop
            agent.ResetPath();
            isMoving = false;
        }
    }
    
    void UpdateAnimations()
    {
        if (animator == null) return;
        
        // Check if actually moving
        bool actuallyMoving = agent.velocity.magnitude > 0.1f;
        
        if (actuallyMoving && walkAnimation != null)
        {
            animator.Play(walkAnimation.name);
        }
        else if (!actuallyMoving && idleAnimation != null)
        {
            animator.Play(idleAnimation.name);
        }
    }
    
    // Visual debug
    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(player.position, followDistance);
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}
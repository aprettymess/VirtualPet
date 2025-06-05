using UnityEngine;

public class KuroHandInteraction : MonoBehaviour
{
    [Header("Hand Detection")]
    [SerializeField] private float detectionRadius = 0.4f;
    [SerializeField] private float pettingRadius = 0.25f;
    [SerializeField] private LayerMask handLayers = -1;
    
    [Header("Interaction Settings")]
    [SerializeField] private float pettingCooldown = 2f;
    [SerializeField] private float handNearDuration = 1f;
    
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip happySound;
    [SerializeField] private AudioClip curiousSound;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugSpheres = true;
    
    private Transform leftHand;
    private Transform rightHand;
    private float lastPetTime = 0f;
    private float handNearStartTime = 0f;
    private bool isHandNear = false;
    private bool isPetting = false;
    private KuroController kuroController;
    
    void Start()
    {
        // Find hand tracking objects
        FindHandReferences();
        
        if (!animator) animator = GetComponent<Animator>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        kuroController = GetComponent<KuroController>();
    }
    
    void FindHandReferences()
{
    // Method 1: Look for Hand Tracking building blocks
    GameObject leftHandAnchor = GameObject.Find("LeftHandAnchor");
    GameObject rightHandAnchor = GameObject.Find("RightHandAnchor");
    
    if (leftHandAnchor) leftHand = leftHandAnchor.transform;
    if (rightHandAnchor) rightHand = rightHandAnchor.transform;
    
    // Method 2: Look for OVRHand components with correct API
    if (!leftHand || !rightHand)
    {
        OVRHand[] hands = FindObjectsOfType<OVRHand>();
        foreach (var hand in hands)
        {
            // Use the correct property name for v76
            if (hand.IsDataValid && hand.GetFingerConfidence(OVRHand.HandFinger.Index) > (OVRHand.TrackingConfidence)0.5f)
            {
                // Check if it's left or right hand by name or position
                if (hand.name.ToLower().Contains("left") || hand.transform.position.x < 0)
                {
                    leftHand = hand.transform;
                }
                else if (hand.name.ToLower().Contains("right") || hand.transform.position.x > 0)
                {
                    rightHand = hand.transform;
                }
            }
        }
    }
    
    // Method 3: Alternative - Find by GameObject names
    if (!leftHand)
    {
        GameObject leftHandObj = GameObject.Find("LeftHand");
        if (!leftHandObj) leftHandObj = GameObject.Find("Left Hand");
        if (!leftHandObj) leftHandObj = GameObject.Find("HandLeft");
        if (leftHandObj) leftHand = leftHandObj.transform;
    }
    
    if (!rightHand)
    {
        GameObject rightHandObj = GameObject.Find("RightHand");
        if (!rightHandObj) rightHandObj = GameObject.Find("Right Hand");
        if (!rightHandObj) rightHandObj = GameObject.Find("HandRight");
        if (rightHandObj) rightHand = rightHandObj.transform;
    }
    
    Debug.Log($"Hand tracking found - Left: {leftHand != null}, Right: {rightHand != null}");
    
    if (leftHand) Debug.Log($"Left hand object: {leftHand.name}");
    if (rightHand) Debug.Log($"Right hand object: {rightHand.name}");
}
    
    void Update()
    {
        CheckHandProximity();
        HandlePettingInteraction();
    }
    
    void CheckHandProximity()
    {
        bool handCurrentlyNear = false;
        
        // Check both hands
        if (leftHand && Vector3.Distance(transform.position, leftHand.position) < detectionRadius)
        {
            handCurrentlyNear = true;
        }
        
        if (rightHand && Vector3.Distance(transform.position, rightHand.position) < detectionRadius)
        {
            handCurrentlyNear = true;
        }
        
        // Hand near state management
        if (handCurrentlyNear && !isHandNear)
        {
            // Hand just entered detection area
            isHandNear = true;
            handNearStartTime = Time.time;
            OnHandApproaching();
        }
        else if (!handCurrentlyNear && isHandNear)
        {
            // Hand left detection area
            isHandNear = false;
            OnHandLeaving();
        }
    }
    
    void HandlePettingInteraction()
    {
        if (!isHandNear) return;
        
        bool inPettingRange = false;
        
        // Check if hand is in petting range
        if (leftHand && Vector3.Distance(transform.position, leftHand.position) < pettingRadius)
        {
            inPettingRange = true;
        }
        
        if (rightHand && Vector3.Distance(transform.position, rightHand.position) < pettingRadius)
        {
            inPettingRange = true;
        }
        
        // Trigger petting if in range and cooldown passed
        if (inPettingRange && !isPetting && Time.time - lastPetTime > pettingCooldown)
        {
            StartPetting();
        }
        else if (!inPettingRange && isPetting)
        {
            StopPetting();
        }
    }
    
    void OnHandApproaching()
    {
        Debug.Log("Hand approaching Kuro - showing curiosity");
        
        // Trigger curious animation
        if (animator)
        {
            animator.SetInteger("AnimationID", 4); // Curious/alert animation
        }
        
        // Play curious sound
        if (audioSource && curiousSound)
        {
            audioSource.PlayOneShot(curiousSound);
        }
        
        // Stop current movement to focus on hand
        if (kuroController)
        {
            kuroController.enabled = false; // Temporarily disable movement
        }
    }
    
    void OnHandLeaving()
    {
        Debug.Log("Hand leaving Kuro's area");
        
        // Return to normal behavior
        if (kuroController)
        {
            kuroController.enabled = true; // Re-enable movement
        }
        
        StopPetting();
    }
    
    void StartPetting()
    {
        if (isPetting) return;
        
        isPetting = true;
        lastPetTime = Time.time;
        
        Debug.Log("Petting Kuro - he's happy!");
        
        // Trigger happy animation
        if (animator)
        {
            animator.SetInteger("AnimationID", 2); // Happy animation
        }
        
        // Play happy sound
        if (audioSource && happySound)
        {
            audioSource.PlayOneShot(happySound);
        }
        
        // Visual feedback could go here (particles, etc.)
    }
    
    void StopPetting()
    {
        if (!isPetting) return;
        
        isPetting = false;
        
        Debug.Log("Stopped petting Kuro");
        
        // Return to idle
        if (animator)
        {
            animator.SetInteger("AnimationID", 0); // Idle
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugSpheres) return;
        
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Draw petting radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pettingRadius);
    }
}
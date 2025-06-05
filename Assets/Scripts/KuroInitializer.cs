using System.Collections;
using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Manages safe initialization of Kuro in mixed reality environments
/// Ensures proper positioning and component activation timing
/// </summary>
public class KuroInitializer : MonoBehaviour
{
    [Header("Initialization Settings")]
    [SerializeField] private float safeSpawnHeight = 0.5f;
    [SerializeField] private float initializationDelay = 1.0f;
    
    private Rigidbody kuroRigidbody;
    private bool isInitialized = false;
    
    void Start()
    {
        // Get reference to Kuro's rigidbody
        kuroRigidbody = GetComponent<Rigidbody>();
        
        // Set initial safe position and kinematic state
        InitializeSafePosition();
        
        // Wait for navigation system to be ready
        MRUK.Instance.RegisterSceneLoadedCallback(BeginSafeInitialization);
    }
    
    /// <summary>
    /// Sets Kuro to a safe initial position and prevents physics conflicts
    /// </summary>
    private void InitializeSafePosition()
    {
        // Place Kuro at a safe height to prevent falling through floors
        Vector3 safePosition = transform.position;
        safePosition.y = safeSpawnHeight;
        transform.position = safePosition;
        
        // Make Kuro kinematic to prevent physics conflicts during initialization
        if (kuroRigidbody != null)
        {
            kuroRigidbody.isKinematic = true;
        }
        
        Debug.Log("Kuro positioned safely for initialization");
    }
    
    /// <summary>
    /// Called when room scanning completes - begins safe physics activation
    /// </summary>
    private void BeginSafeInitialization()
    {
        StartCoroutine(SafePhysicsActivation());
    }
    
    /// <summary>
    /// Safely activates Kuro's physics after all systems are ready
    /// </summary>
    private IEnumerator SafePhysicsActivation()
    {
        // Wait for navigation mesh and other systems to stabilize
        yield return new WaitForSeconds(initializationDelay);
        
        // Re-enable physics for natural behavior
        if (kuroRigidbody != null)
        {
            kuroRigidbody.isKinematic = false;
        }
        
        isInitialized = true;
        Debug.Log("Kuro initialization complete - physics activated");
    }
    public bool IsInitialized()
    {
        return isInitialized;
    }
}
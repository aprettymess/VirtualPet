using System.Collections;
using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Simplified Kuro initializer with hard-coded settings to avoid serialization issues
/// </summary>
public class SimpleKuroInitializer : MonoBehaviour
{
    private Rigidbody kuroRigidbody;
    private bool isInitialized = false;
    
    // Hard-coded values to avoid serialization conflicts
    private const float SAFE_SPAWN_HEIGHT = 0.5f;
    private const float INITIALIZATION_DELAY = 1.0f;
    
    void Start()
    {
        kuroRigidbody = GetComponent<Rigidbody>();
        
        if (kuroRigidbody == null)
        {
            Debug.LogWarning("SimpleKuroInitializer: No Rigidbody found");
            return;
        }
        
        InitializeSafePosition();
        
        if (MRUK.Instance != null)
        {
            MRUK.Instance.RegisterSceneLoadedCallback(BeginSafeInitialization);
        }
        else
        {
            StartCoroutine(FallbackInitialization());
        }
    }
    
    private void InitializeSafePosition()
    {
        Vector3 safePosition = transform.position;
        safePosition.y = SAFE_SPAWN_HEIGHT;
        transform.position = safePosition;
        
        kuroRigidbody.isKinematic = true;
        Debug.Log("SimpleKuroInitializer: Safe position set");
    }
    
    private void BeginSafeInitialization()
    {
        StartCoroutine(SafePhysicsActivation());
    }
    
    private IEnumerator FallbackInitialization()
    {
        yield return new WaitForSeconds(2.0f);
        ActivatePhysics();
    }
    
    private IEnumerator SafePhysicsActivation()
    {
        yield return new WaitForSeconds(INITIALIZATION_DELAY);
        ActivatePhysics();
    }
    
    private void ActivatePhysics()
    {
        if (kuroRigidbody != null)
        {
            kuroRigidbody.isKinematic = false;
        }
        
        isInitialized = true;
        Debug.Log("SimpleKuroInitializer: Physics activated");
    }
    
    /// <summary>
    /// Public method to check if initialization is complete
    /// Other scripts can call this to verify Kuro is ready for behavior
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized;
    }
}
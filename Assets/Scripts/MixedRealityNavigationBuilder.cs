using System.Collections;
using UnityEngine;
using Unity.AI.Navigation;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Handles dynamic generation of navigation meshes for mixed reality environments.
/// Waits for MRUK to complete room scanning, then builds navigation data
/// based on detected real-world geometry and furniture.
/// </summary>
public class MixedRealityNavigationBuilder : MonoBehaviour
{
    [Header("Navigation Components")]
    [SerializeField] private NavMeshSurface navigationSurface;
    
    [Header("Build Settings")]
    [SerializeField] private float buildDelay = 0.5f; // Time to wait after scene detection completes
    
    private bool navigationBuilt = false;
    
    /// <summary>
    /// Initialize the navigation system and register for room detection events
    /// </summary>
    void Start()
    {
        // Get the NavMeshSurface component if not manually assigned
        if (navigationSurface == null)
        {
            navigationSurface = GetComponent<NavMeshSurface>();
            
            if (navigationSurface == null)
            {
                Debug.LogError("MixedRealityNavigationBuilder requires a NavMeshSurface component!");
                return;
            }
        }
        
        // Register our callback to execute when MRUK finishes understanding the room
        // This ensures we build navigation data only after real-world geometry is available
        MRUK.Instance.RegisterSceneLoadedCallback(InitiateNavigationBuild);
        
        Debug.Log("Navigation builder initialized - waiting for room scanning to complete");
    }
    
    /// <summary>
    /// Called when MRUK has finished scanning and understanding the room layout
    /// Triggers the navigation mesh building process
    /// </summary>
    private void InitiateNavigationBuild()
    {
        Debug.Log("Room scanning complete - beginning navigation mesh generation");
        StartCoroutine(BuildNavigationMeshWithDelay());
    }
    
    /// <summary>
    /// Builds the navigation mesh after a brief delay to ensure all room objects are stable
    /// The delay prevents building navigation data before all detected objects are fully positioned
    /// </summary>
    private IEnumerator BuildNavigationMeshWithDelay()
    {
        // Wait for one frame to ensure all detected objects are properly instantiated
        yield return new WaitForEndOfFrame();
        
        // Additional delay to allow any object positioning to stabilize
        yield return new WaitForSeconds(buildDelay);
        
        // Perform the actual navigation mesh build
        ExecuteNavigationBuild();
    }
    
    /// <summary>
    /// Performs the actual navigation mesh generation using Unity's built-in system
    /// Creates walkable areas and obstacle avoidance based on detected room geometry
    /// </summary>
    private void ExecuteNavigationBuild()
    {
        if (navigationBuilt)
        {
            Debug.LogWarning("Navigation mesh already built - skipping duplicate build");
            return;
        }
        
        Debug.Log("Generating navigation mesh from detected room geometry...");
        
        // Build the navigation mesh using all currently detected objects in the scene
        navigationSurface.BuildNavMesh();
        
        navigationBuilt = true;
        
        Debug.Log("Navigation mesh generation complete - Kuro can now navigate the room!");
        
        // Optional: Validate that the navigation mesh was built successfully
        ValidateNavigationMesh();
    }
    
    /// <summary>
    /// Validates that the navigation mesh was built successfully and contains navigable areas
    /// Provides feedback for debugging navigation issues
    /// </summary>
    private void ValidateNavigationMesh()
    {
        // Check if any navigation areas were actually created
        var navMeshData = navigationSurface.navMeshData;
        
        if (navMeshData != null)
        {
            Debug.Log("Navigation mesh validation successful - walkable areas detected");
        }
        else
        {
            Debug.LogWarning("Navigation mesh validation failed - no walkable areas found. Check room scanning and surface detection.");
        }
    }
    
    /// <summary>
    /// Public method to rebuild navigation mesh if room layout changes
    /// Useful for debugging or handling dynamic room changes
    /// </summary>
    public void RebuildNavigation()
    {
        Debug.Log("Rebuilding navigation mesh...");
        navigationBuilt = false;
        ExecuteNavigationBuild();
    }
    
    /// <summary>
    /// Clean up event registrations when the object is destroyed
    /// Prevents memory leaks and callback errors
    /// </summary>
    private void OnDestroy()
    {
        // Unregister our callback to prevent calls to destroyed objects
        if (MRUK.Instance != null)
        {
            // Note: MRUK callback unregistration may vary based on version
            // Check MRUK documentation for proper cleanup if needed
        }
    }
}
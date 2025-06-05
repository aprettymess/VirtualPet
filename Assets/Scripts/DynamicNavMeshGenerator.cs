using Meta.XR.MRUtilityKit;
using UnityEngine;
using Unity.AI.Navigation;
using System.Collections;
using UnityEngine.AI;

public class DynamicNavMeshGenerator : MonoBehaviour
{
    private NavMeshSurface meshSurface;
    
    void Start()
    {
        meshSurface = GetComponent<NavMeshSurface>();
        Debug.Log("DynamicNavMeshGenerator: Waiting for MRUK scene to load...");
        MRUK.Instance.RegisterSceneLoadedCallback(GenerateNavigation);
    }

    private void GenerateNavigation()
    {
        Debug.Log("DynamicNavMeshGenerator: MRUK scene loaded, starting NavMesh generation...");
        StartCoroutine(BuildNavigationMesh());
    }

    private IEnumerator BuildNavigationMesh()
    {
        // Wait longer for MRUK to fully instantiate all surfaces
        yield return new WaitForSeconds(2f);
        
        Debug.Log("DynamicNavMeshGenerator: Building NavMesh...");
        
        // Ensure we're using the right geometry source
        meshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        meshSurface.layerMask = -1; // Include all layers
        
        meshSurface.BuildNavMesh();
        
        // Log NavMesh statistics
        var navMeshData = UnityEngine.AI.NavMesh.CalculateTriangulation();
        Debug.Log($"Kuro's NavMesh generated successfully! Vertices: {navMeshData.vertices.Length}, Triangles: {navMeshData.indices.Length / 3}");
        
        if (navMeshData.vertices.Length == 0)
        {
            Debug.LogWarning("NavMesh has no vertices! Trying alternative approach...");
            yield return StartCoroutine(TryAlternativeNavMeshBuild());
        }
    }
    
    private IEnumerator TryAlternativeNavMeshBuild()
    {
        // Try with Physics Colliders in case MRUK added them
        meshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        yield return new WaitForSeconds(1f);
        meshSurface.BuildNavMesh();
        
        var navMeshData = UnityEngine.AI.NavMesh.CalculateTriangulation();
        Debug.Log($"Alternative NavMesh build - Vertices: {navMeshData.vertices.Length}, Triangles: {navMeshData.indices.Length / 3}");
        
        if (navMeshData.vertices.Length == 0)
        {
            Debug.LogError("NavMesh still empty! Check MRUK surface setup and ensure room surfaces are being generated.");
        }
    }
}
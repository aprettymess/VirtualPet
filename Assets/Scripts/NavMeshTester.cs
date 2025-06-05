using UnityEngine;
using UnityEngine.AI;

public class NavMeshTester : MonoBehaviour
{
    private NavMeshAgent testAgent;
    
    void Start()
    {
        testAgent = GetComponent<NavMeshAgent>();
        
        // Wait a few seconds for NavMesh to build, then test
        Invoke("TestNavMesh", 3f);
    }
    
    void TestNavMesh()
    {
        // Try to find a valid position on NavMesh
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            Debug.Log($"NavMesh Test: Found valid position at {hit.position}");
            transform.position = hit.position;
        }
        else
        {
            Debug.LogWarning("NavMesh Test: No valid NavMesh position found nearby!");
        }
    }
}
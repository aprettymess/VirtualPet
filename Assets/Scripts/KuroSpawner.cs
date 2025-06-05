using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.AI;

public class KuroSpawner : MonoBehaviour
{
    [Header("Kuro Configuration")]
    [SerializeField] private GameObject kuroPrefab;
    [SerializeField] private bool spawnOnStart = true;
    
    [Header("Spawn Settings")]
    [SerializeField] private float minDistanceFromEdge = 0.3f;
    [SerializeField] private float surfaceOffset = 0.1f;
    [SerializeField] private MRUKAnchor.SceneLabels targetSurface = MRUKAnchor.SceneLabels.FLOOR;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private GameObject spawnedKuro;
    
    void Start()
    {
        if (spawnOnStart)
        {
            // Wait for MRUK to load, then spawn Kuro
            MRUK.Instance.RegisterSceneLoadedCallback(SpawnKuro);
        }
    }
    
    public void SpawnKuro()
    {
        if (showDebugLogs) Debug.Log("KuroSpawner: Attempting to spawn Kuro...");
    
        MRUKRoom currentRoom = MRUK.Instance.GetCurrentRoom();
        if (currentRoom == null)
        {
            Debug.LogError("KuroSpawner: No room found! Make sure MRUK has loaded properly.");
            return;
        }
    
        LabelFilter floorFilter = new LabelFilter(targetSurface);
    
        bool foundPosition = currentRoom.GenerateRandomPositionOnSurface(
            MRUK.SurfaceType.FACING_UP, 
            minDistanceFromEdge, 
            floorFilter, 
            out Vector3 spawnPosition, 
            out Vector3 surfaceNormal
        );
    
        if (foundPosition)
        {
            Vector3 finalSpawnPosition = spawnPosition + (surfaceNormal * surfaceOffset);
        
            // Spawn Kuro and IMMEDIATELY set correct scale
            spawnedKuro = Instantiate(kuroPrefab, finalSpawnPosition, Quaternion.identity);
            spawnedKuro.transform.localScale = Vector3.one * .99f; // FORCE correct scale
        
            if (showDebugLogs) Debug.Log($"KuroSpawner: Kuro spawned at {finalSpawnPosition} with scale {spawnedKuro.transform.localScale}");
        
            // Disable NavMeshAgent 
            NavMeshAgent agent = spawnedKuro.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;
        }
        else
        {
            Debug.LogWarning("KuroSpawner: Could not find suitable spawn position!");
        }
    }
    
    public GameObject GetSpawnedKuro()
    {
        return spawnedKuro;
    }
}
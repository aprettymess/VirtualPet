using UnityEngine;
using Meta.XR.MRUtilityKit;

public class BallThrower : MonoBehaviour
{
    [Header("Ball Settings")]
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private float throwForce = 8f;
    [SerializeField] private float minDistanceFromEdge = 0.2f;
    [SerializeField] private float surfaceOffset = 0.1f;
    
    [Header("Input")]
    [SerializeField] private KeyCode throwKey = KeyCode.Space;
    
    private Camera playerCamera;
    
    void Start()
    {
        playerCamera = Camera.main;
    }
    
    void Update()
    {
        if (Input.GetKeyDown(throwKey))
        {
            SpawnBallOnSurface();
        }
    }
    
    void SpawnBallOnSurface()
    {
        MRUKRoom currentRoom = MRUK.Instance.GetCurrentRoom();
        if (currentRoom == null)
        {
            Debug.LogWarning("No room found for ball spawning!");
            return;
        }
        
        // Try to spawn on floor first
        LabelFilter floorFilter = new LabelFilter(MRUKAnchor.SceneLabels.FLOOR);
        
        bool foundPosition = currentRoom.GenerateRandomPositionOnSurface(
            MRUK.SurfaceType.FACING_UP,
            minDistanceFromEdge,
            floorFilter,
            out Vector3 spawnPosition,
            out Vector3 surfaceNormal
        );
        
        if (foundPosition)
        {
            // Spawn ball slightly above surface
            Vector3 finalPosition = spawnPosition + (surfaceNormal * surfaceOffset);
            GameObject newBall = Instantiate(ballPrefab, finalPosition, Quaternion.identity);
            
            // Give ball initial velocity toward player's look direction
            Rigidbody ballRb = newBall.GetComponent<Rigidbody>();
            if (ballRb && playerCamera)
            {
                Vector3 throwDirection = playerCamera.transform.forward;
                throwDirection.y = Mathf.Max(0.2f, throwDirection.y); // Slight upward arc
                ballRb.AddForce(throwDirection * throwForce, ForceMode.Impulse);
            }
            
            // Notify Kuro
            FetchBall ballScript = newBall.GetComponent<FetchBall>();
            if (ballScript)
            {
                FindObjectOfType<KuroController>()?.OnObjectThrown(newBall);
            }
            
            Debug.Log($"Ball spawned on surface at {finalPosition}");
        }
        else
        {
            Debug.LogWarning("Could not find suitable surface to spawn ball!");
        }
    }
}
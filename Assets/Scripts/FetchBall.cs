using UnityEngine;

public class FetchBall : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private float despawnTime = 30f;
    [SerializeField] private LayerMask groundLayers = -1;
    
    private Rigidbody ballRigidbody;
    private bool hasLanded = false;
    
    void Start()
    {
        ballRigidbody = GetComponent<Rigidbody>();
        
        // Auto-despawn after time
        Destroy(gameObject, despawnTime);
        
        // Ensure proper physics settings for MR
        ballRigidbody.mass = 0.1f;
        ballRigidbody.drag = 1f;
        ballRigidbody.angularDrag = 1f;
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (!hasLanded)
        {
            hasLanded = true;
            
            // Reduce bouncing after first contact
            ballRigidbody.drag = 3f;
            ballRigidbody.angularDrag = 3f;
            
            Debug.Log("Ball landed and is ready for fetch!");
        }
    }
    
    void Update()
    {
        // Safety: If ball falls too low, reset to reasonable height
        if (transform.position.y < -2f)
        {
            transform.position = new Vector3(transform.position.x, 0.5f, transform.position.z);
            ballRigidbody.velocity = Vector3.zero;
        }
    }
}
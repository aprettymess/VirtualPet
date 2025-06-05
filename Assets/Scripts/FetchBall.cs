// Create FetchBall.cs script:
using UnityEngine;

public class FetchBall : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private float throwForce = 5f;
    [SerializeField] private float despawnTime = 30f;
    
    private Rigidbody ballRigidbody;
    private bool hasBeenThrown = false;
    
    void Start()
    {
        ballRigidbody = GetComponent<Rigidbody>();
        
        // Auto-despawn after time
        Destroy(gameObject, despawnTime);
    }
    
    public void ThrowBall(Vector3 direction, float force)
    {
        if (!hasBeenThrown)
        {
            ballRigidbody.AddForce(direction * force, ForceMode.Impulse);
            hasBeenThrown = true;
            
            // Notify Kuro about thrown object
            FindObjectOfType<KuroController>()?.OnObjectThrown(gameObject);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Make ball less bouncy after first bounce
        if (hasBeenThrown && ballRigidbody.drag < 2f)
        {
            ballRigidbody.drag = 2f;
            ballRigidbody.angularDrag = 2f;
        }
    }
}
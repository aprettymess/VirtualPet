using UnityEngine;

public class BallThrower : MonoBehaviour
{
    [Header("Ball Settings")]
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private float throwForce = 8f;
    [SerializeField] private float spawnDistance = 0.5f;
    
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
            ThrowBall();
        }
    }
    
    void ThrowBall()
    {
        if (ballPrefab && playerCamera)
        {
            // Spawn ball in front of player
            Vector3 spawnPosition = playerCamera.transform.position + playerCamera.transform.forward * spawnDistance;
            GameObject newBall = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
            
            // Throw ball forward
            FetchBall ballScript = newBall.GetComponent<FetchBall>();
            if (ballScript)
            {
                ballScript.ThrowBall(playerCamera.transform.forward, throwForce);
            }
            
            Debug.Log("Ball thrown! Kuro should fetch it.");
        }
    }
}
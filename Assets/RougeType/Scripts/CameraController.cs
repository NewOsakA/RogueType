using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Positions")]
    public Vector3 basePosition = new Vector3(-15f, 0f, -10f);
    public Vector3 wavePosition = new Vector3(0f, 0f, -10f);

    [Header("Smooth Transition")]
    public float moveSpeed = 5f;

    [Header("Manual Pan (Base Phase Only)")]
    public float panSpeed = 10f;
    public float minX = -30f;
    public float maxX = 0f;

    private Vector3 targetPosition;
    private bool isTransitioning = false;

    void Start()
    {
        targetPosition = basePosition;
        transform.position = basePosition;
    }

    void Update()
    {
        if (isTransitioning)
        {
            // Smooth camera movement
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);

            // End transition if close enough
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                isTransitioning = false;
            }
        }

        if (GameManager.Instance.IsBasePhase() && !isTransitioning)
        {
            float inputX = Input.GetAxis("Horizontal");
            Vector3 move = new Vector3(inputX, 0f, 0f) * panSpeed * Time.deltaTime;
            transform.position += move;

            // Clamp horizontal movement
            float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
        }
    }

    public void MoveToBase()
    {
        targetPosition = basePosition;
        isTransitioning = true;
    }

    public void MoveToWave()
    {
        targetPosition = wavePosition;
        isTransitioning = true;
    }
}

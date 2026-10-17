using UnityEngine;

public class CameraFollow : MonoBehaviour {
    public Transform PlayerObject;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);

    private void Start()
    {
        if (PlayerObject == null)
        {
            Debug.LogError("CameraFollow: PlayerObject not assigned!", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (PlayerObject == null)
            return;

        // Target position follows player on X and Y
        Vector3 targetPosition = PlayerObject.position + offset;

        // Smoothly interpolate camera position
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
    }
};

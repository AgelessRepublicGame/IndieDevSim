using UnityEngine;

public class TutorialArrow : MonoBehaviour
{
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed = 4f;

    private Transform target;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            gameObject.SetActive(false);
            return;
        }

        float bob = Mathf.Sin(Time.unscaledTime * bobSpeed) * bobHeight;
        transform.position = target.position + worldOffset + Vector3.up * bob;
    }

    public void PointTo(Transform newTarget)
    {
        target = newTarget;
        gameObject.SetActive(target != null);
    }

    public void Hide()
    {
        target = null;
        gameObject.SetActive(false);
    }
}

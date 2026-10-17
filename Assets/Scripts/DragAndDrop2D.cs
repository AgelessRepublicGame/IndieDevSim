using UnityEngine;

public class DragAndDrop2D : MonoBehaviour
{
    private bool dragging;
    private Vector3 offset;
    public Camera cam;
    void Update()
    {
        if (StageEventSystem.IsBlocking)
        {
            if (dragging)
            {
                dragging = false;
            }

            return;
        }

        if (Input.GetMouseButtonDown(0))
        {

            Vector3 mouseWorld = GetMouseWorld();

            Collider2D hit = Physics2D.OverlapPoint(mouseWorld);

            if (hit&& hit.transform == transform)
            {
                dragging= true;
                offset = transform.position - mouseWorld;
            }
        }

        if (Input.GetMouseButton(0) && dragging)
        {
            Vector3 target = GetMouseWorld() + offset;
            float dragSpeed = RandomEventSystem.GetDragSpeedMultiplier();
            transform.position = Vector3.Lerp(transform.position, target, dragSpeed);
        }

        if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
        }
    }

    Vector3 GetMouseWorld()
    {
        Vector3 mouse = Input.mousePosition;

        // Important for 2D camera at Z -10 and object at Z 0
        mouse.z = Mathf.Abs(cam.transform.position.z - transform.position.z);

        Vector3 world = cam.ScreenToWorldPoint(mouse);
        world.z = transform.position.z;

        return world;
    }
}
using UnityEngine;

public class PurchaseScript : MonoBehaviour
{
    private void Awake()
    {
        if (GetComponent<HoverScaleEffect>() == null)
        {
            gameObject.AddComponent<HoverScaleEffect>();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnEnable()
    {
        GetComponent<InteractiveObj>().OnInteractionAccept += Purchase;
    }

    private void OnDisable()
    {
        GetComponent<InteractiveObj>().OnInteractionAccept -= Purchase;
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void Purchase(GameObject obj)
    {

        DeveloperSimulation developer = obj.GetComponent<DeveloperSimulation>();
        if (developer != null)
        {
            developer.puchase();
        }

        ManagerSimulation manager = GetComponent<ManagerSimulation>();
        if (manager != null && manager.taskBalloon != null)
        {
            manager.taskBalloon.SetActive(false);
        }

        // Show emotion for 2s, then exit.
        if (manager != null)
        {
            manager.PlayEmotionThenExit();
        }
    }
}

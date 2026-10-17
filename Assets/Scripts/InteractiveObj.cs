using UnityEngine;
using System;

public class InteractiveObj: MonoBehaviour
{
    public static event Action<InteractiveObj, GameObject> AnyInteractionAccepted;

    [SerializeField] private bool addHoverHighlight = true;

    public event Action<GameObject> OnInteractionAccept;

    private void Awake()
    {
        if (addHoverHighlight && GetComponent<InteractiveHoverHighlight>() == null)
        {
            gameObject.AddComponent<InteractiveHoverHighlight>();
        }
    }

    public void Interact(GameObject sender)
    {
        OnInteractionAccept?.Invoke(sender);
        AnyInteractionAccepted?.Invoke(this, sender);
    }
}

using UnityEngine;

public class LocalOnly : MonoBehaviour
{
    public bool EnableOnLocal = true;

    void Awake()
    {
        var lot = GetComponent<LocalOnlyToggler>();
        lot ??= GetComponentInParent<LocalOnlyToggler>();

        if (lot != null)
        {
            if (lot.photonView.IsMine ^ EnableOnLocal)
            {
                DestroyImmediate(gameObject);
                return;
            }
            else
            {
                Destroy(this);
            }
        }

    }
}

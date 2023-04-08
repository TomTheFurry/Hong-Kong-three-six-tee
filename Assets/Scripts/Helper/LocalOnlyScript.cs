using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalOnlyScript : MonoBehaviour
{
    public bool EnableOnLocal = true;
    public MonoBehaviour[] Scripts;

    void Start()
    {
        var lot = GetComponent<LocalOnlyToggler>();
        lot ??= GetComponentInParent<LocalOnlyToggler>();

        if (lot != null)
        {
            if (lot.photonView.IsMine ^ EnableOnLocal)
            {
                foreach (var script in Scripts)
                    DestroyImmediate(script);
            }
            Destroy(this);
        }
    }
}

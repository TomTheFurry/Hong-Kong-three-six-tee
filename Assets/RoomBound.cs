using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RoomBound : MonoBehaviour
{
    private BoxCollider _collider;
    public static RoomBound Instance;

    public void Start()
    {
        Instance = this;
        _collider = GetComponent<BoxCollider>();
        _collider.isTrigger = true;
    }

    public static bool IsOutside(Transform t)
    {
        return !Instance._collider.bounds.Contains(t.position);
    }
}

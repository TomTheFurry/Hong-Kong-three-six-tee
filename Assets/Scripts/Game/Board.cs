using System.Collections;
using System.Collections.Generic;
using UnityEditor;

using UnityEngine;

public class Board : MonoBehaviour
{
    public static GameTile StartTile => Instance.StartingTile;
    static Board Instance;
    public GameTile StartingTile = null;
    
    private void Awake()
    {
        Instance = this;
    }

    public void OnDrawGizmos()
    {
        if (StartingTile != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(StartingTile.transform.position + Vector3.up * 1f, 0.5f);
        }
    }



}

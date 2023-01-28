using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;

using JetBrains.Annotations;

using Photon.Pun;
using Photon.Realtime;

using Unity.VisualScripting;

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SpringJoint))]
public class Piece : MonoBehaviourPun
{
    public GameTile CurrentTile = null;
    private Rigidbody rb;
    private SpringJoint sj;
    public IPlayer Owner = null;

    //private ArticulationBody pin = null;

    public void Teleport()
    {
        if (CurrentTile == null) return;
        rb.position = CurrentTile.transform.position + Vector3.up * 1f;
        rb.angularVelocity = Vector3.zero;
        rb.velocity = Vector3.zero;
        rb.rotation = Quaternion.AngleAxis(Random.value * Mathf.PI, Vector3.up);
    }
    
    public void Start()
    {
        rb = GetComponent<Rigidbody>();
        sj = GetComponent<SpringJoint>();
        sj.autoConfigureConnectedAnchor = false;
        Teleport();
        UpdatePin();
    }

    private void UpdatePin()
    {
        sj.connectedAnchor = CurrentTile.transform.position;

        // if (pin != null) Destroy(pin);
        // if (CurrentTile == null)
        // {
        //     sj.connectedArticulationBody = null;
        // }
        // else
        // {
        //     pin = CurrentTile.AddComponent<ArticulationBody>();
        //     pin.immovable = true;
        //     pin.enabled = false;
        //     sj.connectedArticulationBody = pin;
        // }
    }

    public void MoveForward(int number)
    {
        while (number-- != 0)
        {
            CurrentTile = CurrentTile.NextTile;
        }
        UpdatePin();
    }

    private int t = 0;
    public void Update()
    {
        if (Time.timeSinceLevelLoad - t > 0)
        {
            t++;
            MoveForward(1);
            //Teleport();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(GrabInteractableBase))]
public class PcGrab : MonoBehaviour
{
    GrabInteractableBase gb;

    void Start()
    {
        gb = GetComponent<GrabInteractableBase>();
    }

    void FixedUpdate()
    {
        Rigidbody target = GetComponent<Rigidbody>();
        if (gb.CurrentLocalGrabber != null)
        {
            var posdiff = gb.CurrentLocalGrabber.position - target.position;
            var vel = target.velocity;
            var force = posdiff * gb.positionP - vel * gb.positionD;
            target.AddForce(force, ForceMode.VelocityChange);

            var x = Vector3.Cross(target.transform.forward, gb.CurrentLocalGrabber.forward);
            float theta = Mathf.Asin(x.magnitude);
            if (float.IsNormal(x.sqrMagnitude))
            {
                if (x.sqrMagnitude < 0.1f)
                {
                    // Sync roll
                    Vector3 targetUp = gb.CurrentLocalGrabber.up;
                    Vector3 axis = Vector3.Cross(target.transform.up, targetUp);
                    float angle = Vector3.Angle(target.transform.up, targetUp) * 1f;
                    x += axis * angle;
                }
                var w = x.normalized * theta / Time.deltaTime;
                target.AddTorque(gb.rotationP * w - gb.rotationD * target.angularVelocity, ForceMode.VelocityChange);
            }
            if (gb.noGravityAfterGrab) target.useGravity = false;
        }
        else if (gb.IsGrabbed)
        {
            if (gb.noGravityAfterGrab) target.useGravity = false;
        }
        else
        {
            if (gb.noGravityAfterGrab) target.useGravity = true;
        }
    }
}

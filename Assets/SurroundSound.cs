using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SurroundSound : MonoBehaviour
{
    public Camera camera;
    public AudioSource audioSource { get; private set; }

    public Vector3 defaultForward;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        defaultForward = defaultForward.normalized;
    }

    private void Update()
    {
        transform.rotation = Quaternion.identity; // force at front
        transform.position = camera.transform.position + Vector3.forward;

        /*
        float angleDiff = Vector3.SignedAngle(transform.forward + defaultForward, camera.transform.forward, Vector3.up);

        float leftRightPan;
        if (MathF.Abs(angleDiff) > 90)
        { // at the back.
            transform.position = transform.position - camera.transform.forward;
            if (angleDiff > 0)
            {
                leftRightPan = 180 - 
            }
            else
            {
                leftRightPan = 1;
            }

        }*/
    }

}

using UnityEngine;

public class SmoothLazer : MonoBehaviour
{
    Rigidbody rb;

    public Transform mover;

    public float rotationP = 0.1f;
    public float rotationD = 0.9f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        rb.position = mover.position;

        var x = Vector3.Cross(rb.transform.forward, mover.forward);
        float theta = Mathf.Asin(x.magnitude);
        var w = x.normalized * theta / Time.deltaTime;
        var q = transform.rotation * rb.inertiaTensorRotation;
        var t = q * Vector3.Scale(rb.inertiaTensor, Quaternion.Inverse(q) * w);
        rb.AddTorque(rotationP * t - rotationD * rb.angularVelocity, ForceMode.Impulse);
        
    }
}

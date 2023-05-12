using UnityEngine;

public class XrSmoothAttach : MonoBehaviour
{
    public Rigidbody target;

    public float rotationP = 0.2f;
    public float rotationD = 0.4f;
    public float positionP = 4f;
    public float positionD = 0.5f;

    void Update()
    {
        var posdiff = transform.position - target.position;
        var vel = target.velocity;
        var force = posdiff * positionP - vel * positionD;
        target.AddForce(force, ForceMode.VelocityChange);

        var x = Vector3.Cross(target.transform.forward, transform.forward);
        float theta = Mathf.Asin(x.magnitude);
        if (float.IsNormal(x.sqrMagnitude)) {
            if (x.sqrMagnitude < 0.1f) {
                // Sync roll
                Vector3 targetUp = transform.up;
                Vector3 axis = Vector3.Cross(target.transform.up, targetUp);
                float angle = Vector3.Angle(target.transform.up, targetUp) * 1f;
                x += axis * angle;
            }
            var w = x.normalized * theta / Time.deltaTime;
            target.AddTorque(rotationP * w - rotationD * target.angularVelocity, ForceMode.VelocityChange);
        }        
    }
}

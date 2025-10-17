using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyMotor : MonoBehaviour
{
    Rigidbody rb;

    public Vector3 LinearVelocity
    {
        get => rb.linearVelocity;
        set => rb.linearVelocity = value;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public void TickHorizontal(Vector3 desiredDir, float targetSpeed, bool grounded, MovementConfig cfg, float dt)
    {
        Vector3 vel = rb.linearVelocity;
        Vector3 velH = Vector3.ProjectOnPlane(vel, Vector3.up);
        Vector3 targetVelH = desiredDir * targetSpeed;

        Vector3 delta = targetVelH - velH;
        float usedAccel = (Vector3.Dot(delta, velH) < 0f) ? cfg.decelGround : (grounded ? cfg.accelGround : cfg.accelAir);
        float maxChange = usedAccel * dt;

        Vector3 accelVec = delta;
        if (accelVec.magnitude > maxChange) accelVec = accelVec.normalized * maxChange;

        rb.linearVelocity = velH + accelVec + Vector3.up * vel.y;
    }

    public void SetVerticalSpeed(float y)
    {
        var v = rb.linearVelocity; v.y = y; rb.linearVelocity = v;
    }

    public void AddExtraGravity(float accelDown)
    {
        rb.AddForce(Vector3.down * accelDown, ForceMode.Acceleration);
    }
}

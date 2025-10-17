using UnityEngine;

public class SlopeHandler
{
    public void HandleSlide(RigidbodyMotor motor, MovementConfig cfg, bool grounded, Vector3 groundNormal)
    {
        if (!grounded) return;
        float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
        if (slopeAngle > cfg.slopeLimit)
        {
            Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
            // 가속도 형태로 미끄러짐
            motor.GetComponent<Rigidbody>().AddForce(slideDir * 20f, ForceMode.Acceleration);
        }
    }
}

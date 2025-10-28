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

    /// <summary>
    /// 지면 법선을 고려해 이동 (오르막 감속 / 내리막 가속 포함)
    /// </summary>
    public void TickHorizontal(Vector3 desiredDir, float targetSpeed, bool grounded, MovementConfig cfg, float dt)
    {
        Vector3 vel = rb.linearVelocity;

        // 바닥 법선 감지
        Vector3 groundNormal = Vector3.up;
        bool hasGround = Physics.SphereCast(
            transform.position + Vector3.up * 0.05f,
            cfg.groundCheckRadius,
            Vector3.down,
            out RaycastHit hit,
            cfg.groundCheckRadius + cfg.groundCheckOffset + 0.1f,
            cfg.groundMask
        );

        if (hasGround)
            groundNormal = hit.normal;

        // 경사면 기준 이동 방향
        Vector3 desiredDirOnSlope = Vector3.ProjectOnPlane(desiredDir, groundNormal).normalized;

        // 경사각에 따라 속도 조정 (오르막 감속 / 내리막 가속)
        float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
        float dirDot = Vector3.Dot(
            desiredDirOnSlope,
            Vector3.ProjectOnPlane(Vector3.up, groundNormal).normalized * -1f
        );
        // dirDot > 0 → 오르막 / dirDot < 0 → 내리막

        float slopeFactor;
        if (dirDot > 0)
        {
            // 오르막: 감속폭 완화 (0.9~1.0)
            slopeFactor = Mathf.Lerp(1f, 0.9f, slopeAngle / cfg.slopeLimit);
        }
        else
        {
            // 내리막: 가속폭 완화 (1.0~1.15)
            slopeFactor = Mathf.Lerp(1f, 1.15f, slopeAngle / cfg.slopeLimit);
        }

        // 최종 목표 속도
        Vector3 targetVelH = desiredDirOnSlope * targetSpeed * slopeFactor;

        // 수평 속도
        Vector3 velH = Vector3.ProjectOnPlane(vel, groundNormal);

        // 가속 계산
        Vector3 delta = targetVelH - velH;
        float usedAccel = (Vector3.Dot(delta, velH) < 0f)
            ? cfg.decelGround
            : (grounded ? cfg.accelGround : cfg.accelAir);

        // 최소 반응값 추가 (짧은 입력에도 약간의 반응)
        float minAccel = 0.5f;
        float maxChange = Mathf.Max(usedAccel * dt, minAccel * dt);

        Vector3 accelVec = delta;
        if (accelVec.magnitude > maxChange)
            accelVec = accelVec.normalized * maxChange;

        // ✅ 최종 속도 (지면 법선 기준 유지)
        Vector3 newVel = velH + accelVec;
        newVel += groundNormal * Vector3.Dot(vel, groundNormal);
        rb.linearVelocity = newVel;
    }

    public void SetVerticalSpeed(float y)
    {
        var v = rb.linearVelocity;
        v.y = y;
        rb.linearVelocity = v;
    }

    public void AddExtraGravity(float accelDown)
    {
        rb.AddForce(Vector3.down * accelDown, ForceMode.Acceleration);
    }
}

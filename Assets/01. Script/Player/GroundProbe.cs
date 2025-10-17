using UnityEngine;

public class GroundProbe
{
    public bool Grounded { get; private set; }
    public Vector3 GroundNormal { get; private set; } = Vector3.up;
    public Vector3 ProbeCenter { get; private set; } // 디버그용

    public void Sample(CapsuleCollider col, MovementConfig cfg)
    {
        Bounds b = col.bounds;
        Vector3 sphereCenter = new Vector3(b.center.x, b.min.y + cfg.groundCheckRadius + cfg.groundCheckOffset, b.center.z);

        Grounded = Physics.CheckSphere(sphereCenter, cfg.groundCheckRadius, cfg.groundMask, QueryTriggerInteraction.Ignore);
        GroundNormal = Vector3.up;
        ProbeCenter = sphereCenter;

        if (Grounded)
        {
            if (Physics.Raycast(sphereCenter + Vector3.up * 0.1f, Vector3.down, out var hit, 0.5f + cfg.groundCheckOffset, cfg.groundMask, QueryTriggerInteraction.Ignore))
                GroundNormal = hit.normal;
        }
    }
}

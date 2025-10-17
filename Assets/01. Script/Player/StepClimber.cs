using UnityEngine;

public class StepClimber
{
    public void TryStep(Rigidbody rb, CapsuleCollider col, MovementConfig cfg)
    {
        if (!cfg.enableStep) return;

        Vector3 vel = rb.linearVelocity;
        Vector3 velH = Vector3.ProjectOnPlane(vel, Vector3.up);
        if (velH.sqrMagnitude < 0.0001f) return;

        Vector3 dir = velH.normalized;
        Vector3 p = rb.position;
        float skin = 0.02f;
        float castRadius = Mathf.Max(cfg.groundCheckRadius, col.radius * 0.9f);

        Vector3 lowOrigin = p + Vector3.up * (cfg.stepHeight * 0.25f);
        bool lowHit = Physics.SphereCast(lowOrigin, castRadius, dir, out RaycastHit hitLow, cfg.stepCheckDistance, cfg.groundMask, QueryTriggerInteraction.Ignore);
        if (!lowHit) return;

        Vector3 highOrigin = p + Vector3.up * (cfg.stepHeight + castRadius + skin);
        bool highBlocked = Physics.SphereCast(highOrigin, castRadius, dir, out _, cfg.stepCheckDistance, cfg.groundMask, QueryTriggerInteraction.Ignore);
        if (!highBlocked)
        {
            rb.position += Vector3.up * (cfg.stepHeight - hitLow.distance + skin);
        }
    }
}

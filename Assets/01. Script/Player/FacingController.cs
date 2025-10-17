using UnityEngine;

public class FacingController
{
    public void Face(Transform t, Vector3 linearVelocity, Vector3 inputDir, float rotateSharpness, float dt)
    {
        Vector3 fwd = Vector3.ProjectOnPlane(linearVelocity, Vector3.up);
        if (fwd.sqrMagnitude < 0.0001f) fwd = inputDir;

        if (fwd.sqrMagnitude < 0.0001f) return;
        fwd.y = 0;

        Quaternion target = Quaternion.LookRotation(fwd.normalized, Vector3.up);
        float s = 1f - Mathf.Pow(1f - rotateSharpness, dt * 60f);
        t.rotation = Quaternion.Slerp(t.rotation, target, s);
    }
}

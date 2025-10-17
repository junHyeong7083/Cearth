using UnityEngine;
public interface IMoveDirectionProvider
{
    Vector3 GetDesiredDirection(Vector2 inputMove, Camera cam, bool grounded, Vector3 groundNormal);
}
public class CameraDirectionProvider : MonoBehaviour, IMoveDirectionProvider
{
    public Vector3 GetDesiredDirection(Vector2 inputMove, Camera cam, bool grounded, Vector3 groundNormal)
    {
        Vector3 f = cam ? cam.transform.forward : Vector3.forward;
        Vector3 r = cam ? cam.transform.right : Vector3.right;
        f.y = 0; r.y = 0; f.Normalize(); r.Normalize();

        Vector3 dir = (f * inputMove.y + r * inputMove.x);
        if (dir.sqrMagnitude > 0f) dir.Normalize();

        if (grounded && dir.sqrMagnitude > 0f)
            dir = Vector3.ProjectOnPlane(dir, groundNormal).normalized;

        return dir;
    }
}

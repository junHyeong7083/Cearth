using UnityEditor;
using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    [SerializeField] Transform Player;
    [SerializeField] Vector3 Offset;
    [SerializeField] float followSpeed;

    private void LateUpdate()
    {
        if (Player == null) return;

        Vector3 targetPos = Player.position + Offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed*Time.deltaTime);
    }
}

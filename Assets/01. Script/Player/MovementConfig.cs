using UnityEngine;

[CreateAssetMenu(menuName = "Configs/MovementConfig", fileName = "MovementConfig")]
public class MovementConfig : ScriptableObject
{
    [Header("Move")]
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 6.0f;
    public float accelGround = 30f;
    public float accelAir = 10f;
    public float decelGround = 60f;
    [Range(0f, 1f)] public float rotateSharpness = 0.25f;

    [Header("Jump")]
    public float jumpHeight = 1.2f;
    public float coyoteTime = 0.12f;
    public float jumpBuffer = 0.12f;
    public float extraGravity = 15f;

    [Header("Grounding")]
    public float groundCheckRadius = 0.25f;
    public float groundCheckOffset = 0.05f;
    public float slopeLimit = 50f;
    public LayerMask groundMask = ~0;

    [Header("Step")]
    public bool enableStep = true;
    public float stepHeight = 0.35f;
    public float stepCheckDistance = 0.3f;
}

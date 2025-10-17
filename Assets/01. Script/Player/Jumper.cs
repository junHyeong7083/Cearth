using UnityEngine;

public class Jumper
{
    float lastGroundedTime;
    float lastJumpPressedTime = -999f;
    bool jumpConsumed;

    public void CacheJump(bool jumpPressed, float now)
    {
        if (jumpPressed) lastJumpPressedTime = now;
    }

    public void NotifyGrounded(float now)
    {
        lastGroundedTime = now;
        jumpConsumed = false;
    }

    public bool TryJump(RigidbodyMotor motor, MovementConfig cfg, bool grounded, float now)
    {
        bool canCoyote = now - lastGroundedTime <= cfg.coyoteTime;
        bool buffered = now - lastJumpPressedTime <= cfg.jumpBuffer;

        if (!jumpConsumed && buffered && (grounded || canCoyote))
        {
            jumpConsumed = true;
            lastJumpPressedTime = -999f;

            float gTotal = Physics.gravity.magnitude + cfg.extraGravity;
            float jumpV = Mathf.Sqrt(2f * gTotal * cfg.jumpHeight);
            motor.SetVerticalSpeed(jumpV);
            return true;
        }
        return false;
    }
}

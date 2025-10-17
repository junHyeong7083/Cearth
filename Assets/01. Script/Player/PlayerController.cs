using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(RigidbodyMotor))]
[RequireComponent(typeof(PlayerInput))]          
[RequireComponent(typeof(CameraDirectionProvider))]    
public class PlayerController : MonoBehaviour
{
    [SerializeField] MovementConfig config;   // so
    [SerializeField] Camera cam;           // 비우면 MainCamera

    // 의존성
    Rigidbody rb;
    CapsuleCollider col;
    RigidbodyMotor motor;
    IPlayerInput input;
    IMoveDirectionProvider dirProvider;

    // 순수 로직 객체들
    readonly GroundProbe ground = new();
    readonly Jumper jumper = new();
    readonly SlopeHandler slope = new();
    readonly StepClimber step = new();
    readonly FacingController facing = new();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        motor = GetComponent<RigidbodyMotor>();
        input = GetComponent<IPlayerInput>();
        dirProvider = GetComponent<IMoveDirectionProvider>();
        if (!cam) cam = Camera.main;
        if (!config) Debug.LogWarning("MovementConfig가 할당되지 않았습니다. 기본값으로 동작합니다.");
    }

    void Update()
    {
        input.Poll();                                         // 입력만
        jumper.CacheJump(input.JumpPressedThisFrame, Time.time);
    }

    void FixedUpdate()
    {
        // 1) 접지/노멀
        ground.Sample(col, config);
        if (ground.Grounded) jumper.NotifyGrounded(Time.time);

        // 2) 방향 + 속도 목표
        Vector3 desiredDir = dirProvider.GetDesiredDirection(input.Move, cam, ground.Grounded, ground.GroundNormal);
        float targetSpeed = (input.Sprint ? config.sprintSpeed : config.walkSpeed) * Mathf.Clamp01(input.Move.magnitude);

        // 3) 수평 가감속
        motor.TickHorizontal(desiredDir, targetSpeed, ground.Grounded, config, Time.fixedDeltaTime);

        // 4) 점프
        jumper.TryJump(motor, config, ground.Grounded, Time.time);

        // 5) 추가 중력
        motor.AddExtraGravity(config.extraGravity);

        // 6) 급경사 슬라이드
        slope.HandleSlide(motor, config, ground.Grounded, ground.GroundNormal);

        // 7) 회전
        facing.Face(transform, motor.LinearVelocity, desiredDir, config.rotateSharpness, Time.fixedDeltaTime);

        // 8) 스텝 보조
        if (config.enableStep) step.TryStep(rb, col, config);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!config) return;
        if (!TryGetComponent(out CapsuleCollider c)) return;
        Bounds b = c.bounds;
        Vector3 center = new Vector3(b.center.x, b.min.y + config.groundCheckRadius + config.groundCheckOffset, b.center.z);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, config.groundCheckRadius);
    }
#endif
}

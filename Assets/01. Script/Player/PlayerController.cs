using UnityEngine;

[RequireComponent(typeof(Rigidbody))]         
[RequireComponent(typeof(CapsuleCollider))]  
public class PlayerController : MonoBehaviour 
{
    [Header("Camera")]                                            
    [SerializeField] Camera cam; 

    [Header("Move")]                                              // 이동 관련 파라미터
    [SerializeField] float walkSpeed = 3.5f;                      // 기본 이동 속도
    [SerializeField] float sprintSpeed = 6.0f;                    // 달리기 속도
    [SerializeField] float accelGround = 30f;                     // 지상 가속(속도 변화 상한/초)
    [SerializeField] float accelAir = 10f;                        // 공중 가속
    [SerializeField] float decelGround = 60f;                     // 지상 감속(브레이크 세기)
    [SerializeField, Range(0f, 1f)] float rotateSharpness = 0.25f;// 회전 보간 민감도(0~1)

    [Header("Jump")]                                              // 점프 관련 파라미터
    [SerializeField] KeyCode jumpKey = KeyCode.Space;             // 점프 키
    [SerializeField] float jumpHeight = 1.2f;                     // 목표 점프 높이(미터)
    [SerializeField] float coyoteTime = 0.12f;                    // 코요테 타임(땅 떠난 직후 허용 시간)
    [SerializeField] float jumpBuffer = 0.12f;                    // 점프 입력 버퍼(선입력 허용 시간)
    [SerializeField] float extraGravity = 15f;                    // 추가 중력(낙하 가속 보정)

    [Header("Grounding")]                                         // 접지 판정 파라미터
    [SerializeField] float groundCheckRadius = 0.25f;             // 발밑 구체 반경
    [SerializeField] float groundCheckOffset = 0.05f;             // 발바닥과 체크 구체 사이 여유
    [SerializeField] float slopeLimit = 50f;                      // 오를 수 있는 최대 경사 각도(도)
    [SerializeField] LayerMask groundMask = ~0;                   // 지면 레이어 마스크(기본: 전체)

    [Header("Step (Optional)")]                                   // 턱/계단 보조 옵션
    [SerializeField] bool enableStep = true;                      // 스텝 업 기능 사용 여부
    [SerializeField] float stepHeight = 0.35f;                    // 오를 수 있는 턱의 최대 높이
    [SerializeField] float stepCheckDistance = 0.3f;              // 턱 탐지 전방 거리

    [Header("Key Mapping")]                                       // 기타 입력 키
    [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;       // 달리기 키

    Rigidbody rb;             
    CapsuleCollider col;      

    Vector2 inputMove;        // 이동 입력(수평/수직)
    bool inputSprint;         // 달리기 입력 여부
    bool inputJumpPressed;    // 이번 프레임 점프 키 눌림 여부

    Vector3 groundNormal = Vector3.up; // 현재 접지면 노멀(기본은 위 방향)
    bool grounded;                     // 접지 여부
    float lastGroundedTime;            // 마지막으로 접지했던 시간
    float lastJumpPressedTime;         // 마지막으로 점프 키를 눌렀던 시간(버퍼용)
    bool jumpConsumed;                 // 이번 착지 전 점프를 이미 소모했는지

    void Awake() // 초기화: 컴포넌트 캐시 및 물리 설정
    {
        rb = GetComponent<Rigidbody>();            // Rigidbody 획득
        col = GetComponent<CapsuleCollider>();     // CapsuleCollider 획득
        if (!cam) cam = Camera.main;               // cam 미할당 시 MainCamera 자동 할당

        rb.useGravity = true;                      // 기본 중력 사용
        rb.interpolation = RigidbodyInterpolation.Interpolate; // 보간으로 움직임 부드럽게
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // 고속 이동 충돌 정확도 향상
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // 좌우 앞뒤 회전 고정(넘어짐 방지)
        // Drag(Linear Damping)는 인스펙터에서 0 권장                   // 선형 감쇠는 0이 반응성 좋음
    } // Awake 끝

    void Update() // 프레임 기반 입력 처리(키다운 정확성 위해)
    {
        HandleInput();           // 입력 수집
        CacheJumpBufferIfNeeded();// 점프 버퍼(선입력) 기록 및 소진
    } // Update 끝

    void FixedUpdate() // 물리 프레임 처리
    {
        GroundCheck();           // ✅ 접지 먼저 갱신(점프/가속 로직의 전제)
        Move();                  // 이동 가속/감속 및 방향 적용
        Jump();                  // 코요테 + 버퍼 기반 점프 처리
        ApplyExtraGravity();     // 추가 중력으로 낙하감 보정
        HandleSlopeSlide();      // 급경사면 슬라이드
        RotateToFace();          // 속도/입력 방향을 향해 회전
        TryStepUpIfNeeded();     // 턱/계단 보조
    } // FixedUpdate 끝

    // -------- Input --------
    void HandleInput() // 키보드 입력 수집
    {
        float h = Input.GetAxisRaw("Horizontal"); // 좌우 입력(-1~1, 즉시 반응)
        float v = Input.GetAxisRaw("Vertical");   // 전후 입력(-1~1, 즉시 반응)
        inputMove = new Vector2(h, v);            // 2D 벡터로 저장
        inputSprint = Input.GetKey(sprintKey);    // 달리기 키 눌림 여부

        if (Input.GetKeyDown(jumpKey))            // 이번 프레임에 점프 키가 눌렸다면
            inputJumpPressed = true;              // 플래그 설정(버퍼에서 소비)
    } // HandleInput 끝

    void CacheJumpBufferIfNeeded() // 점프 버퍼 기록/소진
    {
        if (inputJumpPressed)                      // 플래그가 서 있으면
        {
            lastJumpPressedTime = Time.time;       // 마지막 점프 입력 시간 기록
            inputJumpPressed = false;              // 즉시 소진(중복 기록 방지)
        }
    } // CacheJumpBufferIfNeeded 끝

    // -------- Move --------
    void Move() 
    {
        Vector3 f = cam ? cam.transform.forward : Vector3.forward; // 카메라 전방
        Vector3 r = cam ? cam.transform.right : Vector3.right;   // 카메라 오른쪽
        f.y = 0; r.y = 0; f.Normalize(); r.Normalize();            // 수평면 투영 후 정규화

        Vector3 desiredDir = (f * inputMove.y + r * inputMove.x).normalized; // 입력을 카메라 기준 방향으로 변환
        if (grounded && desiredDir.sqrMagnitude > 0.0001f)                    // 접지 상태이고 입력이 있으면
            desiredDir = Vector3.ProjectOnPlane(desiredDir, groundNormal).normalized; // 경사면 평면에 투영해 발이 미끄러지지 않게

        float targetSpeed = (inputSprint ? sprintSpeed : walkSpeed) * Mathf.Clamp01(inputMove.magnitude); // 목표 수평 속도 계산

        Vector3 vel = rb.linearVelocity;                                      // 현재 전체 속도(선형)
        Vector3 velH = Vector3.ProjectOnPlane(vel, Vector3.up);               // 수평 성분만 분리
        Vector3 targetVelH = desiredDir * targetSpeed;                        // 목표 수평 속도 벡터

        Vector3 delta = targetVelH - velH;                                    // 현재 수평 속도와 목표 속도의 차이
        float usedAccel = (Vector3.Dot(delta, velH) < 0f)                     // 현재 진행방향과 반대면 감속, 아니면 가속
            ? decelGround
            : (grounded ? accelGround : accelAir);
        float maxChange = usedAccel * Time.fixedDeltaTime;                    // 이번 물리 프레임에서 허용되는 최대 속도 변화량

        Vector3 accelVec = delta;                                             // 변화시킬 벡터(원하는 만큼)
        if (accelVec.magnitude > maxChange) accelVec = accelVec.normalized * maxChange; // 상한 클램프로 급격한 변화 방지

        rb.linearVelocity = velH + accelVec + Vector3.up * vel.y;             // 수평 속도 갱신 + 기존 수직 속도 유지
    } 

    
    void Jump() 
    {
        bool canCoyote = Time.time - lastGroundedTime <= coyoteTime;          // 최근 접지 시점이 코요테 타임 이내인지
        bool buffered = Time.time - lastJumpPressedTime <= jumpBuffer;       // 최근 점프 입력이 버퍼 시간 이내인지

        if (!jumpConsumed && buffered && (grounded || canCoyote))             // 점프 미소모 + 버퍼 유효 + 접지/코요테 허용
        {
            jumpConsumed = true;                                            
            lastJumpPressedTime = -999f;                                   

            float gTotal = Physics.gravity.magnitude + extraGravity;          // 실제 적용할 총 중력(추가 중력 포함)
            float jumpV = Mathf.Sqrt(2f * gTotal * jumpHeight);               // v = sqrt(2 g h)로 초기 상승 속도 계산

            var v = rb.linearVelocity;                                        // 현재 속도 가져오고
            v.y = jumpV;                                                      // 수직 속도만 점프 속도로 세팅
            rb.linearVelocity = v;                                            // 적용
        }
    } 

   
    void ApplyExtraGravity() // 낙하감을 위한 추가 중력 가속도 적용
    {
        rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);     // 아래 방향 가속도 추가(질량 무관)
    } 

  
    void HandleSlopeSlide() 
    {
        if (!grounded) return;                                                // 공중이면 스킵
        float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);           // 현재 바닥 경사 각도
        if (slopeAngle > slopeLimit)                                          // 한계각 초과 시
        {
            Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized; // 경사면을 따라 아래로 미끄러질 방향
            rb.AddForce(slideDir * 20f, ForceMode.Acceleration);              // 가속도 형태로 미끄러짐 부여
        }
    }

  
    void RotateToFace() 
    {
        Vector3 fwd = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);  
        if (fwd.sqrMagnitude < 0.0001f)                                    
        {
            if (inputMove.sqrMagnitude < 0.0001f) return;                    

            Vector3 cf = cam ? cam.transform.forward : Vector3.forward;      
            Vector3 cr = cam ? cam.transform.right : Vector3.right;         
            cf.y = 0; cr.y = 0; cf.Normalize(); cr.Normalize();               
            fwd = (cf * inputMove.y + cr * inputMove.x);                     
        }

        fwd.y = 0;                                                           
        if (fwd.sqrMagnitude < 0.0001f) return;                        

        Quaternion target = Quaternion.LookRotation(fwd.normalized, Vector3.up); 
        float t = 1f - Mathf.Pow(1f - rotateSharpness, Time.fixedDeltaTime * 60f); 
        transform.rotation = Quaternion.Slerp(transform.rotation, target, t);
    } 
  
    void GroundCheck() 
    {
        Bounds b = col.bounds;                                              
        Vector3 sphereCenter = new Vector3(
            b.center.x,                                                       
            b.min.y + groundCheckRadius + groundCheckOffset,                  // Y: 바운즈 바닥에서 반경+오프셋만큼 위
            b.center.z                                                       
        );

        grounded = Physics.CheckSphere(                                      
            sphereCenter,
            groundCheckRadius,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
        groundNormal = Vector3.up;                                           

        if (grounded)                                                         
        {
            // 노멀 보정을 위해 짧은 다운 레이 쏨(경사 정보 확보)
            if (Physics.Raycast(
                sphereCenter + Vector3.up * 0.1f,                             // 레이 시작점을 살짝 위로
                Vector3.down,                                                
                out RaycastHit hit,                                          
                0.5f + groundCheckOffset,                                    
                groundMask,
                QueryTriggerInteraction.Ignore))
                groundNormal = hit.normal;                                    

            lastGroundedTime = Time.time;                                    
            jumpConsumed = false;                                           
        }
    } 

    void TryStepUpIfNeeded() // 낮은 턱/계단을 부드럽게 오르게 보조
    {
        if (!enableStep || !grounded) return;                                 // 비활성/공중이면 스킵

        Vector3 velH = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up); // 수평 속도
        if (velH.sqrMagnitude < 0.0001f) return;                              // 거의 정지면 스킵

        Vector3 dir = velH.normalized;                                        // 진행 방향

        Vector3 p = transform.position;                                       // 현재 위치
        float skin = 0.02f;                                                   // 여유 거리
        float castRadius = Mathf.Max(groundCheckRadius, col.radius * 0.9f);   // 캐스트 반경(몸통에 맞게)

        Vector3 lowOrigin = p + Vector3.up * (stepHeight * 0.25f);            // 낮은 위치(발목 근처)에서
        bool lowHit = Physics.SphereCast(
            lowOrigin, castRadius, dir, out RaycastHit hitLow,                // 전방에 낮은 장애물이 있는지
            stepCheckDistance, groundMask, QueryTriggerInteraction.Ignore
        );
        if (!lowHit) return;                                                  // 없으면 스킵

        Vector3 highOrigin = p + Vector3.up * (stepHeight + castRadius + skin); // 높은 위치(발등 위)에서
        bool highBlocked = Physics.SphereCast(
            highOrigin, castRadius, dir, out _,                               // 위쪽 공간이 막혔는지 확인
            stepCheckDistance, groundMask, QueryTriggerInteraction.Ignore
        );
        if (!highBlocked)                                                     // 위가 비어 있으면
        {
            rb.position += Vector3.up * (stepHeight - hitLow.distance + skin);// 살짝 위로 올려 턱을 넘김
        }
    } 

#if UNITY_EDITOR
    void OnDrawGizmosSelected() // 에디터에서 접지 체크 구체 시각화
    {
        if (!col) col = GetComponent<CapsuleCollider>();                      // 콜라이더 캐시 보장
        Bounds b = col.bounds;                                                // 바운즈 획득
        Vector3 sphereCenter = new Vector3(                                   // 구체 중심 계산(발밑)
            b.center.x,
            b.min.y + groundCheckRadius + groundCheckOffset,
            b.center.z
        );
        Gizmos.color = Color.yellow;                                          // 색상 설정
        Gizmos.DrawWireSphere(sphereCenter, groundCheckRadius);               // 와이어 구체 그리기
    } // OnDrawGizmosSelected 끝
#endif
} // PlayerController 클래스 끝

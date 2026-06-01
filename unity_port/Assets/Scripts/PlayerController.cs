// 플레이어 — Python Player(Actor) Phase 1 코어 이식.
// ⚠️ Rigidbody2D 안 씀. 커스텀 AABB(OverlapBox 수동 충돌)로 셀레스트식 정밀 제어 유지.
// ⚠️ y축 위=+y. 점프 vy 양수, 중력은 vy에서 빼기. 이동은 FixedUpdate(60Hz).
// ⚠️ 속도 단위는 '픽셀/프레임' → 이동 시 /PPU로 월드 유닛 변환.
//
// [씬 세팅] 빈 GameObject에 이 스크립트 + BoxCollider2D(크기는 자동 안 맞춤, 시각용) 부착.
//   transform.position = 중심. solidMask에 바닥/벽 레이어(예: "Solid") 지정.
using UnityEngine;

namespace HypeJumper
{
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("바닥/벽/천장으로 막을 레이어 (예: Solid)")]
        public LayerMask solidMask;

        // ── 속도 (픽셀/프레임) — Python의 vx_input/vx_external/vy 분리 유지 ──
        private float vxInput;     // 입력 속도 (-MaxSpeed~+MaxSpeed)
        private float vxExternal;  // 외적 속도 (대시/월점프 반발 등, 상한 없음)
        private float vy;          // 수직 (양수=위)
        private float Vx => vxInput + vxExternal;

        // ── 상태 플래그 ──
        public PlayerState State = PlayerState.Normal;
        private bool onGround, onWall, wallSliding;
        private int wallDir, lastWallDir;     // -1 좌, +1 우
        private int ceilingStick;             // 천장 밀착 남은 프레임
        private bool nearGround;
        private bool wjInputLock;             // 월점프 후 '벽 쪽' 입력 무시
        private float curHeight = GameSettings.NormalHitboxHeight;

        // ── 충돌 결과 (매 Move 리셋) ──
        private bool cUp, cDown, cLeft, cRight;

        // ── 통합 버퍼 ──
        private readonly InputBuffer groundCoyote = new InputBuffer(GameSettings.CoyoteTime);
        private readonly InputBuffer wallCoyote = new InputBuffer(GameSettings.WallCoyoteTime);
        private readonly InputBuffer jumpBuffer = new InputBuffer(GameSettings.JumpBuffer);
        private readonly InputBuffer wallJumpBuffer = new InputBuffer(GameSettings.WallJumpBuffer);

        // ── 입력 엣지 (Update에서 잡아 FixedUpdate에서 소비) ──
        private bool jumpEdge;

        // ───────────────────────── Unity 루프 ─────────────────────────
        private void Update()
        {
            // 엣지 입력은 Update(GetKeyDown)에서만 안정적으로 잡힘 → 다음 FixedUpdate까지 보존
            if (Input.GetKeyDown(KeyCode.C)) jumpEdge = true;
        }

        private void FixedUpdate()
        {
            PlayerInputState inp = ReadInput();
            if (State == PlayerState.Normal) UpdateNormal(inp);
            jumpEdge = false;   // 엣지 소비
        }

        private PlayerInputState ReadInput()
        {
            return new PlayerInputState
            {
                Left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow),
                Right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow),
                Up = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow),
                Down = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow),
                JumpPressed = jumpEdge,
                JumpHeld = Input.GetKey(KeyCode.C),
            };
        }

        // ───────────────────────── NORMAL 한 프레임 ─────────────────────────
        private void UpdateNormal(PlayerInputState inp)
        {
            PreState(inp);          // 슬라이드/패스트폴 판정
            UpdateHorizontal(inp);
            HandleJump(inp);
            ApplyVertical(inp);
            Move();                 // 축분리 AABB 충돌
            ResolveLanding();
            UpdateContacts();       // 1px 프로브로 접지/벽 + 코요테
            TickBuffers();
        }

        private void PreState(PlayerInputState inp)
        {
            bool pressingWall = (inp.Right && wallDir == 1) || (inp.Left && wallDir == -1);
            bool descending = vy < 0f;            // 위=+y라 하강은 음수
            wallSliding = onWall && descending && pressingWall;
        }

        // ── 수평 (Approach) ──
        private void UpdateHorizontal(PlayerInputState inp)
        {
            int dir = (inp.Right ? 1 : 0) - (inp.Left ? 1 : 0);
            float accel, decel;
            if (onGround)
            {
                accel = GameSettings.GroundAccel; decel = GameSettings.GroundDecel;
                vxExternal = Approach(vxExternal, 0f, GameSettings.SpeedReduce);   // 빠른 감속
            }
            else
            {
                accel = GameSettings.GroundAccel * GameSettings.AirMult;
                decel = GameSettings.GroundDecel * GameSettings.AirMult;
                vxExternal *= GameSettings.AirFriction;                            // 공기 저항
            }
            if (wjInputLock)   // 월점프 후 벽 쪽 연속 홀드는 정점(vy<=0)까지 무시
            {
                if (vy <= 0f || dir != lastWallDir) wjInputLock = false;
                else dir = 0;
            }
            if (dir != 0)
            {
                if (vxExternal * dir > GameSettings.PlayerMaxSpeed)
                    vxInput = Approach(vxInput, 0f, accel);                        // 외적 초과: 추가 가속 불가
                else
                    vxInput = Approach(vxInput, dir * GameSettings.PlayerMaxSpeed, accel);
            }
            else vxInput = Approach(vxInput, 0f, decel);
        }

        // ── 점프 / 월점프 ──
        private void HandleJump(PlayerInputState inp)
        {
            if (inp.JumpPressed) { jumpBuffer.Set(); wallJumpBuffer.Set(); }

            if (jumpBuffer.IsActive() && (onGround || groundCoyote.IsActive() || nearGround))
            {
                vy = GameSettings.JumpSpeed;   // 위(양수)
                jumpBuffer.Consume(); wallJumpBuffer.Consume(); groundCoyote.Consume();
            }
            else if (wallJumpBuffer.IsActive() && WallJumpReady())
            {
                vy = GameSettings.WallJumpV;
                vxExternal = -lastWallDir * GameSettings.WallJumpH;
                vxInput = 0f;
                wjInputLock = true;
                wallJumpBuffer.Consume(); jumpBuffer.Consume(); wallCoyote.Consume();
            }
        }

        private bool WallJumpReady()
        {
            if (nearGround) return false;   // 바닥 근처는 일반 점프 우선
            return onWall || wallCoyote.IsActive();
        }

        // ── 수직 / 중력 ──
        private void ApplyVertical(PlayerInputState inp)
        {
            if (ceilingStick > 0) { ceilingStick--; vy = 0f; return; }
            if (wallSliding)
            {
                vy = Mathf.Max(vy - GameSettings.WallSlideGravity, -GameSettings.WallSlideMaxFall);
                return;
            }
            vy -= GravityValue(inp);                       // 위=+y라 중력은 빼기
            if (vy < -GameSettings.MaxFallSpeed) vy = -GameSettings.MaxFallSpeed;
        }

        private float GravityValue(PlayerInputState inp)
        {
            if (vy > 0f) return inp.JumpHeld ? GameSettings.GravityUp : GameSettings.GravityUpRelease;
            return GameSettings.GravityDown;
        }

        // ── 이동 + 축분리 AABB 충돌 ──
        private void Move()
        {
            cUp = cDown = cLeft = cRight = false;
            MoveAxis(Vx / GameSettings.PPU, true);
            MoveAxis(vy / GameSettings.PPU, false);
        }

        private void MoveAxis(float delta, bool horizontal)
        {
            Vector3 p = transform.position;
            if (horizontal) p.x += delta; else p.y += delta;
            transform.position = p;

            Vector2 size = BoxSize();
            Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, size * 0.98f, 0f, solidMask);
            foreach (var h in hits)
            {
                Bounds b = h.bounds;
                p = transform.position;
                if (horizontal)
                {
                    float halfW = size.x * 0.5f;
                    if (delta > 0f) { p.x = b.min.x - halfW; cRight = true; }
                    else if (delta < 0f) { p.x = b.max.x + halfW; cLeft = true; }
                }
                else
                {
                    float halfH = size.y * 0.5f;
                    if (delta > 0f) { p.y = b.min.y - halfH; cUp = true; }      // 위로 이동 → 천장
                    else if (delta < 0f) { p.y = b.max.y + halfH; cDown = true; } // 아래로 → 바닥
                }
                transform.position = p;
            }
        }

        private void ResolveLanding()
        {
            if (cDown) vy = 0f;
            if (cUp)
            {
                ceilingStick = (int)Mathf.Min(Mathf.Abs(vy) / GameSettings.CeilingStickDivisor,
                                              GameSettings.MaxCeilingStickFrames);
                vy = 0f;
            }
            if (cLeft) { vxInput = Mathf.Max(vxInput, 0f); vxExternal = Mathf.Max(vxExternal, 0f); }
            if (cRight) { vxInput = Mathf.Min(vxInput, 0f); vxExternal = Mathf.Min(vxExternal, 0f); }
        }

        // ── 접지/벽 프로브 (1px) + 코요테 충전 ──
        private void UpdateContacts()
        {
            float px = 1f / GameSettings.PPU;
            onGround = OverlapAt(new Vector2(0f, -px));
            nearGround = vy <= 0f && OverlapAt(new Vector2(0f, -GameSettings.NearGroundDistance / GameSettings.PPU));
            if (onGround) { groundCoyote.Set(); wjInputLock = false; }

            bool tl = OverlapAt(new Vector2(-px, 0f));
            bool tr = OverlapAt(new Vector2(px, 0f));
            onWall = !onGround && (tl || tr);
            wallDir = tl ? -1 : (tr ? 1 : 0);
            if (onWall) { wallCoyote.Set(); lastWallDir = wallDir; }
        }

        private bool OverlapAt(Vector2 offset)
        {
            return Physics2D.OverlapBox((Vector2)transform.position + offset, BoxSize() * 0.98f, 0f, solidMask) != null;
        }

        private void TickBuffers()
        {
            if (!onGround) groundCoyote.Tick();
            if (!onWall) wallCoyote.Tick();
            jumpBuffer.Tick();
            wallJumpBuffer.Tick();
        }

        // ── 유틸 ──
        private Vector2 BoxSize()
        {
            return new Vector2(GameSettings.PlayerWidth / GameSettings.PPU, curHeight / GameSettings.PPU);
        }

        private static float Approach(float value, float target, float amount)
        {
            if (value < target) return Mathf.Min(value + amount, target);
            return Mathf.Max(value - amount, target);
        }

        // 씬뷰에서 히트박스 확인용
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, BoxSize());
        }
    }
}

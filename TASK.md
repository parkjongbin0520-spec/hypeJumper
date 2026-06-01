# TASK.md — 현재 세션 작업 목록

> 이 파일은 한 세션에 한 가지 작업만 포함한다.
> Claude Code는 이 파일에 명시된 작업만 수행하며, 임의로 다음 단계로 넘어가지 않는다.

---

## 진행 규칙

1. 이 파일의 현재 세션 작업만 수행한다
2. 작업 완료 후 WORK_LOG.md에 기록한다
3. QA 자체 테스트 + QA 보고서 작성
4. 비니의 최종 검수 요청
5. 비니 승인 시 → 다음 세션 작업을 이 파일에 추가

---

## 현재 상태 (2026-06-01 기준)

**중대 결정: Pygame/Python → Unity/C# 재구성.** (메모리 `unity-port-pivot` 참조)

### 파이썬 프로토타입 진행도 (= 레퍼런스용으로 동결)
- Phase 1(이동) / Phase 2(대시) / Phase 2.5(슈퍼·하이퍼·월바운스) — 완료
- Phase 3A(가시/체크포인트/텍스트맵) / 3B(점프패드·스프링) — 완료
- Phase 3C 잡기 — 재설계 완료(Z누름 윈도우+슬로우, 아래=대시, 가시차단, 줄NTT). **최신 사양 = WORK_LOG (3)~(8) + PLANNING [재설계] 섹션**
- 3C-2 적(Enemy/ArmoredEnemy), 3C-3 줄NTT — 완료
- DASH_STRIKE — 구현 후 **제거**(비니 결정)
- 미완: 투사체 적, 타일맵 파일 로더, 카메라, 폴리싱

### 유니티 포팅 (`unity_port/`)
- 완료: README_SETUP.md(초보 셋업·실행·구조), C# 코어 5종(GameSettings/PlayerState/InputBuffer/PlayerInputState/PlayerController = Phase 1 이동)
- 다음: 유니티에서 실행 확인(비니) → 대시 → 고급무브 → 잡기(WORK_LOG 사양) → 적/오브젝트 → 씬 전환(Title/Tutorial/1-1) → 스프라이트/카메라

---

## 현재 세션 작업

### [정리/포팅] 핸드오프 정리 + 유니티 코어 이식

- [x] DASH_STRIKE 제거 (player.py / enemy.py)
- [x] PLANNING.md 잡기 [재설계] 섹션 추가 (추가만)
- [x] TASK.md 현재 상태로 갱신
- [x] 유니티 포팅 산출물 생성 (README_SETUP + C# 코어 5종)
- [ ] (선택) 파이썬 tests/ 영구 회귀 스위트 — **유니티 전환으로 가치 하락, 보류 권장**

**비니 확인 요청**: 유니티 설치 → README_SETUP.md 따라 Player 박스가 바닥에서 이동/점프 되는지.

---

## 다음 세션 예고 (참고)

- 유니티에서 Phase 1 이동 손맛 확인 → 대시(Phase 2) C# 이식

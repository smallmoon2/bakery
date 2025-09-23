using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIController : MonoBehaviour
{
    public enum State { Pick, Pack, Table, Eat, Exit, hall }

    [Header("Refs")]
    public NavMeshAgent agent;
    public Animator anim;

    [Header("Targets (Arrays)")]
    public Transform[] pickPoints;   // 줍기 후보들
    public Transform[] packPoints;   // 포장 후보들
    public Transform[] hallPoints;   // 홀 후보들
    public Transform[] tablePoints;  // 테이블 후보들
    public Transform[] exitPoints;   // 퇴장 후보들

    [Header("PreTargets (One)")]
    public Transform prePickPoint;   // 줍기 경유지(단일)
    public Transform prePackPoint;   // 포장 경유지(단일)
    public Transform preHallPoint;   // 홀 경유지(단일)
    public Transform preTablePoint;  // 테이블 경유지(단일)
    public Transform preExitPoint;   // 퇴장 경유지(단일)

    [Header("Use Index (0-based)")]
    public int pickIdx;
    public int packIdx;
    public int hallIdx;
    public int tableIdx;
    public int exitIdx;

    [Header("Look Targets (optional)")]
    public Transform pickLook;   // 없으면 선택된 point를 바라봄
    public Transform packLook;
    public Transform hallLook;
    public Transform tableLook;
    public Transform exitLook;

    [Header("Durations (sec)")]
    public float pickTime = 1.0f;
    public float packTime = 1.0f;
    public float eatTime = 2.0f;

    [Header("Facing")]
    public float faceSpeedDegPerSec = 540f;
    public float faceDoneAngleDeg = 5f;

    public State state = State.Pick;

    // 내부 상태
    float timer;
    float targetMoveParam;
    bool prePhase;          // 현재 상태에서 경유지로 이동 중인지
    bool waitInit;          // FaceThenWaitNext 타이머 초기화 여부
    private State _lastState; // 외부에서 state 직접 변경 감지용

    // 도착+바라봄+대기 완료 신호
    public bool readyForNext { get; private set; }
    public void ClearReady() => readyForNext = false;

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        Reset();
        Go(State.Pick);
        _lastState = state; // 초기 상태 동기화
    }

    void Update()
    {
        // 외부에서 state를 직접 바꿨을 때도 항상 진입 처리(+ ready 플래그 리셋)
        if (state != _lastState)
        {
            EnterState(state);
            _lastState = state;
        }

        switch (state)
        {
            case State.Pick:
                {
                    // GameManager.Instance.ai.Pick[pickIdx] = true; // 필요 시 사용
                    var pre = GetPrePoint(State.Pick);
                    var p = GetPoint(pickPoints, pickIdx);
                    MoveViaPreThen(pre, p);
                    if (!prePhase && Arrived()) FaceThenWaitNext(pickLook ?? p, pickTime, State.Pack);
                    break;
                }
            case State.Pack:
                {
                    var pre = GetPrePoint(State.Pack);
                    var p = GetPoint(packPoints, packIdx);
                    MoveViaPreThen(pre, p);
                    if (!prePhase && Arrived()) FaceThenWaitNext(packLook ?? p, packTime, State.Table);
                    break;
                }
            case State.hall:
                {
                    var pre = GetPrePoint(State.hall);
                    var p = GetPoint(hallPoints, hallIdx);
                    MoveViaPreThen(pre, p);
                    if (!prePhase && Arrived()) FaceThenWaitNext(hallLook ?? p, packTime, State.Table);
                    break;
                }
            case State.Table:
                {
                    var pre = GetPrePoint(State.Table);
                    var p = GetPoint(tablePoints, tableIdx);
                    MoveViaPreThen(pre, p);
                    if (!prePhase && Arrived()) FaceThenWaitNext(tableLook ?? p, packTime, State.Eat);
                    break;
                }
            case State.Eat:
                timer -= Time.deltaTime;
                if (timer <= 0f) Go(State.Exit); // 필요 시 외부 제어로 바꿔도 됨
                break;

            case State.Exit:
                {
                    var pre = GetPrePoint(State.Exit);
                    var p = GetPoint(exitPoints, exitIdx);
                    MoveViaPreThen(pre, p);
                    if (!prePhase && Arrived())
                    {
                        if (FaceTowards(exitLook ?? p))
                        {
                            agent.isStopped = true;
                            agent.updateRotation = true;
                        }
                    }
                    break;
                }
        }

        // 이동 애니메이션 보간 (사용자 제공식)
        float animValue = anim.GetFloat("Move");
        animValue = Mathf.Lerp(animValue, targetMoveParam, 10f * Time.deltaTime);
        anim.SetFloat("Move", animValue);
    }

    // ---- 상태 변경 공용 진입 ----
    void EnterState(State s)
    {
        prePhase = HasPrePoint(s); // 이 상태에서 경유지 사용 여부
        waitInit = false;          // 바라봄-대기 초기화
        readyForNext = false;      // 진입 시 신호 리셋

        agent.ResetPath();

        switch (s)
        {
            case State.Eat:
                agent.isStopped = true;
                agent.updateRotation = false;
                timer = eatTime;
                break;

            default:
                agent.isStopped = false;
                agent.updateRotation = true;
                break;
        }
    }

    public void Go(State next)
    {
        state = next;
        EnterState(next);  // 일관된 진입 처리
        _lastState = next;
    }

    // ---------- 경유 이동 핵심 ----------
    void MoveViaPreThen(Transform pre, Transform main)
    {
        if (prePhase && pre != null)
        {
            MoveTo(pre);
            if (Arrived())
            {
                prePhase = false;          // 프리 도착 → 이제 메인으로
                agent.ResetPath();         // 경로 초기화
                agent.isStopped = false;   // 이동 재개
                agent.updateRotation = true;
                readyForNext = false;      // 이동 재개 시 신호 내려둠(안전)
            }
        }
        else
        {
            MoveTo(main);
        }
    }

    // ---------- 선택 유틸 ----------
    Transform GetPoint(Transform[] arr, int idx)
    {
        if (arr == null || arr.Length == 0) return null;
        if (idx < 0) idx = 0;
        if (idx >= arr.Length) idx = arr.Length - 1;
        return arr[idx];
    }

    Transform GetPrePoint(State s)
    {
        switch (s)
        {
            case State.Pick: return prePickPoint;
            case State.Pack: return prePackPoint;
            case State.hall: return preHallPoint;
            case State.Table: return preTablePoint;
            case State.Exit: return preExitPoint;
            default: return null;
        }
    }

    bool HasPrePoint(State s) => GetPrePoint(s) != null;

    void MoveTo(Transform t)
    {
        if (!t) return;

        // 이동 시작 전에 잠금 해제 보장
        if (agent.isStopped) agent.isStopped = false;
        if (!agent.updateRotation) agent.updateRotation = true;   // 회전 자동 복구

        // 목적지 갱신 (부동소수 오차 대비)
        if (!agent.hasPath || (agent.destination - t.position).sqrMagnitude > 0.0001f)
            agent.SetDestination(t.position);

        // 이동 애니 판정은 desiredVelocity/velocity 둘 다 고려
        targetMoveParam = (agent.desiredVelocity.sqrMagnitude > 0.01f || agent.velocity.sqrMagnitude > 0.01f) ? 1f : 0f;
    }

    bool Arrived()
    {
        if (agent.pathPending) return false;
        if (agent.remainingDistance > agent.stoppingDistance + 0.05f) return false;
        return !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;
    }

    // ---- 바라보기 ----
    bool FaceTowards(Transform lookTarget)
    {
        if (!lookTarget) return true;
        Vector3 to = lookTarget.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return true;

        Quaternion targetRot = Quaternion.LookRotation(to);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            faceSpeedDegPerSec * Time.deltaTime
        );

        float angle = Quaternion.Angle(transform.rotation, targetRot);
        return angle <= faceDoneAngleDeg;
    }

    // ---- 도착 → 바라봄 → 대기 (여기선 자동 Go 안함) ----
    void FaceThenWaitNext(Transform lookTarget, float wait, State next /* 외부 전환이면 next 무시 가능 */)
    {
        agent.isStopped = true;
        agent.updateRotation = false;

        bool faced = FaceTowards(lookTarget);

        // 처음 진입 시 대기시간 초기화
        if (!waitInit)
        {
            timer = wait;
            waitInit = true;
        }

        if (faced)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                agent.updateRotation = true;
                waitInit = false;     // 다음 라운드 대비
                readyForNext = true;  // 외부에서 Go(next) 호출 가능 상태
            }
        }
        // else: 아직 방향 맞추는 중이면 타이머 유지
    }
}

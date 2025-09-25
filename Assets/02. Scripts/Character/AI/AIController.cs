using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.AI;

public class AIController : MonoBehaviour
{
    public enum State { Pick, Pack, Table, Eat, Exit, Hall, Exit2 }

    [Header("Refs")]
    public NavMeshAgent agent;
    public Animator anim;
    [SerializeField] private BreadBasket Basket;
    [SerializeField] private AIObjectController aIObjectController;

    [Header("Stack Check")]
    [SerializeField] private Transform handStackPoint;

    [Header("Eat")]
    public Transform[] EatPoint;
    [SerializeField] private string eatTag = "Eat";
    private bool goToEatPoint;
    public bool eatingLogicStarted = false;   // 중복 실행 방지
    public bool eatingLogicSFinshed = false;  // 중복 실행 방지

    [Header("Targets (Arrays)")]
    public Transform[] pickPoints;   // 줍기 후보들
    public Transform[] packPoints;   // 포장 후보들
    public Transform[] hallPoints;   // 홀 후보들
    public Transform[] tablePoints;  // 테이블 후보들
    public Transform[] exitPoints;   // Exit 포인트
    public Transform[] exit2Points;  // ★ Exit2 전용 포인트

    [Header("PreTargets (One)")]
    public Transform prePickPoint;   // 줍기 경유지(단일)
    public Transform prePackPoint;   // 포장 경유지(단일)
    public Transform preHallPoint;   // 홀 경유지(단일)
    public Transform preTablePoint;  // 테이블 경유지(단일)
    public Transform preExitPoint;   // Exit 경유지(단일)
    public Transform preExit2Point;  // ★ Exit2 전용 경유지(단일)

    [Header("Use Index (0-based)")]
    public int pickIdx;
    public int packIdx;
    public int hallIdx;
    public int tableIdx;
    public int exitIdx;
    public int exit2Idx;             // ★ Exit2 전용 인덱스

    [Header("Look Targets (optional)")]
    public Transform pickLook;   // 없으면 선택된 point를 바라봄
    public Transform packLook;
    public Transform hallLook;
    public Transform tableLook;
    public Transform exitLook;   // Exit용
    public Transform exit2Look;  // ★ Exit2 전용

    [Header("Durations (sec)")]
    public float pickTime = 1.0f;
    public float packTime = 1.0f;
    public float eatTime = 2.0f;

    [Header("Facing")]
    public float faceSpeedDegPerSec = 540f;
    public float faceDoneAngleDeg = 5f;

    public State state = State.Pick;

    // 내부 상태
    public bool isHall;
    float timer;
    float targetMoveParam;
    bool prePhase;          // 현재 상태에서 경유지로 이동 중인지
    bool waitInit;          // FaceThenWaitNext 타이머 초기화 여부
    private State _lastState; // 외부에서 state 직접 변경 감지용
    public int breadCount;

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
        breadCount = Random.Range(1, 4);

        ClaimPickSlot();
        isHall = Random.value < 0.4f;

        Debug.Log(isHall);
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
                    var pre = GetPrePoint(State.Pick);
                    var p = GetPoint(pickPoints, pickIdx);
                    MoveViaPreThen(pre, p);
                    if (!prePhase && Arrived()) FaceThenWaitNext(pickLook ?? p, pickTime, State.Pack);

                    if (aIObjectController.PickupFinish)
                    {
                        var pickList = GameManager.Instance.ai.Pick;
                        pickList[pickIdx] = false;

                        if (isHall)
                        {
                            GameManager.Instance.ai.AddToList(this, AIManager.ListState.Hall);
                            state = State.Hall;
                        }
                        else
                        {
                            GameManager.Instance.ai.AddToList(this, AIManager.ListState.Pack);
                            state = State.Pack;
                        }
                    }
                    break;
                }

            case State.Pack:
                {
                    var pre = GetPrePoint(State.Pack);
                    var p = GetPoint(packPoints, packIdx);
                    MoveViaPreThen(pre, p);
                    if (!prePhase && Arrived()) FaceThenWaitNext(packLook ?? p, packTime, State.Table);

                    if (aIObjectController.BagFinish)
                    {
                        GameManager.Instance.ai.RemoveFromList(this, AIManager.ListState.Pack);
                        state = State.Exit;
                    }
                    break;
                }

            case State.Hall:
                {
                    var pre = GetPrePoint(State.Hall);
                    var p = GetPoint(hallPoints, hallIdx);
                    MoveViaPreThen(pre, p);
                    if (!prePhase && Arrived()) FaceThenWaitNext(hallLook ?? p, packTime, State.Table);

                    if (aIObjectController.BagFinish && GameManager.Instance.ai.isHallOpen && GameManager.Instance.ai.isTableempty && GameManager.Instance.ai.Trash == null)
                    {
                        Debug.Log("table 이동");
                        GameManager.Instance.ai.RemoveFromList(this, AIManager.ListState.Hall);
                        state = State.Table;
                        GameManager.Instance.ai.isTableempty = false;
                    }
                    break;
                }

            case State.Table:
                {
                    var pre = GetPrePoint(State.Table);
                    var p = GetPoint(tablePoints, tableIdx);

                    if (goToEatPoint)
                    {
                        p = GetPoint(EatPoint, tableIdx);
                        if (!eatingLogicStarted)
                            StartCoroutine(EatingLogic());
                    }

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

            case State.Exit2:
                {
                    var pre = GetPrePoint(State.Exit2);
                    var p = GetPoint(exit2Points, exit2Idx); // ★ Exit2 전용 포인트/인덱스 사용
                    MoveViaPreThen(pre, p);
                    if (!prePhase && Arrived())
                    {
                        if (FaceTowards(exit2Look ?? p)) // ★ Exit2 전용 룩 타겟 사용(없으면 p)
                        {
                            agent.isStopped = true;
                            agent.updateRotation = true;
                        }
                    }
                    break;
                }
        }

        // 이동 애니메이션 보간
        float animValue = anim.GetFloat("Move");
        animValue = Mathf.Lerp(animValue, targetMoveParam, 10f * Time.deltaTime);
        anim.SetFloat("Move", animValue);

        // HandStackPoint 상태에 따른 Stack Bool 제어
        bool hasStack = handStackPoint && handStackPoint.childCount > 0;
        anim.SetBool("Stack", hasStack);
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
            case State.Hall: return preHallPoint;
            case State.Table: return preTablePoint;
            case State.Exit: return preExitPoint;
            case State.Exit2: return preExit2Point;   // ★ Exit2 전용 프리포인트
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

    // ---- 도착 → 바라봄 → 대기 ----
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

    private void ClaimPickSlot()
    {
        // GameManager/AIManager 존재/리스트 유효성 체크
        if (GameManager.Instance == null || GameManager.Instance.ai == null)
        {
            Debug.LogWarning("[AIController] GameManager.Instance.ai 없음");
            pickIdx = Mathf.Clamp(pickIdx, 0, (pickPoints?.Length ?? 1) - 1);
            return;
        }

        var pickList = GameManager.Instance.ai.Pick;
        if (pickList == null)
        {
            Debug.LogWarning("[AIController] AIManager.Pick 리스트 없음");
            pickIdx = Mathf.Clamp(pickIdx, 0, (pickPoints?.Length ?? 1) - 1);
            return;
        }

        // (선택) pickPoints 길이에 맞춰 리스트 크기 보정
        EnsureListSize(pickList, pickPoints != null ? pickPoints.Length : pickList.Count);

        // 첫 false 슬롯 탐색
        int chosen = -1;
        for (int i = 0; i < pickList.Count; i++)
        {
            if (!pickList[i])
            {
                chosen = i;
                break;
            }
        }

        if (chosen >= 0)
        {
            pickIdx = chosen;
            pickList[chosen] = true; // 점유!
        }
        else
        {
            // 빈 슬롯이 없을 때의 폴백
            pickIdx = Mathf.Clamp(pickIdx, 0, (pickPoints?.Length ?? 1) - 1);
            Debug.LogWarning("[AIController] 빈 Pick 슬롯이 없어 기본 인덱스로 시작합니다. pickIdx=" + pickIdx);
        }
    }

    private IEnumerator EatingLogic()
    {
        eatingLogicStarted = true;

        // 1초 후 isEating = true
        yield return new WaitForSeconds(1f);
        if (anim) anim.SetBool("isEating", true);

        // 6초 후 isEating = false
        yield return new WaitForSeconds(6f);
        if (anim) anim.SetBool("isEating", false);

        eatingLogicSFinshed = true;

        // 테이블 비움 플래그
        GameManager.Instance.ai.isTableempty = true;

        // 종료: Exit/Exit2로 이동 (원하는 쪽 호출)
        Go(State.Exit2); // ★ Exit2로 나가도록 설정
        // 필요 시 Exit로 바꾸려면: Go(State.Exit);
    }

    // 리스트를 지정 크기까지 false로 채워 확장
    private void EnsureListSize(List<bool> list, int size)
    {
        if (list == null) return;
        if (size < 0) size = 0;
        if (list.Count < size)
        {
            int add = size - list.Count;
            for (int i = 0; i < add; i++) list.Add(false);
        }
    }

    public void SetPackIndex(int idx) => packIdx = idx;
    public void SetHallIndex(int idx) => hallIdx = idx;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(eatTag))
        {
            // Eat 트리거 밟으면 Table 단계에서 목표를 EatPoint로 강제
            goToEatPoint = true;
        }
    }
}

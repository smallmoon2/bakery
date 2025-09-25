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
    public bool eatingLogicStarted = false;   // �ߺ� ���� ����
    public bool eatingLogicSFinshed = false;  // �ߺ� ���� ����

    [Header("Targets (Arrays)")]
    public Transform[] pickPoints;   // �ݱ� �ĺ���
    public Transform[] packPoints;   // ���� �ĺ���
    public Transform[] hallPoints;   // Ȧ �ĺ���
    public Transform[] tablePoints;  // ���̺� �ĺ���
    public Transform[] exitPoints;   // Exit ����Ʈ
    public Transform[] exit2Points;  // �� Exit2 ���� ����Ʈ

    [Header("PreTargets (One)")]
    public Transform prePickPoint;   // �ݱ� ������(����)
    public Transform prePackPoint;   // ���� ������(����)
    public Transform preHallPoint;   // Ȧ ������(����)
    public Transform preTablePoint;  // ���̺� ������(����)
    public Transform preExitPoint;   // Exit ������(����)
    public Transform preExit2Point;  // �� Exit2 ���� ������(����)

    [Header("Use Index (0-based)")]
    public int pickIdx;
    public int packIdx;
    public int hallIdx;
    public int tableIdx;
    public int exitIdx;
    public int exit2Idx;             // �� Exit2 ���� �ε���

    [Header("Look Targets (optional)")]
    public Transform pickLook;   // ������ ���õ� point�� �ٶ�
    public Transform packLook;
    public Transform hallLook;
    public Transform tableLook;
    public Transform exitLook;   // Exit��
    public Transform exit2Look;  // �� Exit2 ����

    [Header("Durations (sec)")]
    public float pickTime = 1.0f;
    public float packTime = 1.0f;
    public float eatTime = 2.0f;

    [Header("Facing")]
    public float faceSpeedDegPerSec = 540f;
    public float faceDoneAngleDeg = 5f;

    public State state = State.Pick;

    // ���� ����
    public bool isHall;
    float timer;
    float targetMoveParam;
    bool prePhase;          // ���� ���¿��� �������� �̵� ������
    bool waitInit;          // FaceThenWaitNext Ÿ�̸� �ʱ�ȭ ����
    private State _lastState; // �ܺο��� state ���� ���� ������
    public int breadCount;

    // ����+�ٶ�+��� �Ϸ� ��ȣ
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
        _lastState = state; // �ʱ� ���� ����ȭ
    }

    void Update()
    {
        // �ܺο��� state�� ���� �ٲ��� ���� �׻� ���� ó��(+ ready �÷��� ����)
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
                        Debug.Log("table �̵�");
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
                if (timer <= 0f) Go(State.Exit); // �ʿ� �� �ܺ� ����� �ٲ㵵 ��
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
                    var p = GetPoint(exit2Points, exit2Idx); // �� Exit2 ���� ����Ʈ/�ε��� ���
                    MoveViaPreThen(pre, p);
                    if (!prePhase && Arrived())
                    {
                        if (FaceTowards(exit2Look ?? p)) // �� Exit2 ���� �� Ÿ�� ���(������ p)
                        {
                            agent.isStopped = true;
                            agent.updateRotation = true;
                        }
                    }
                    break;
                }
        }

        // �̵� �ִϸ��̼� ����
        float animValue = anim.GetFloat("Move");
        animValue = Mathf.Lerp(animValue, targetMoveParam, 10f * Time.deltaTime);
        anim.SetFloat("Move", animValue);

        // HandStackPoint ���¿� ���� Stack Bool ����
        bool hasStack = handStackPoint && handStackPoint.childCount > 0;
        anim.SetBool("Stack", hasStack);
    }

    // ---- ���� ���� ���� ���� ----
    void EnterState(State s)
    {
        prePhase = HasPrePoint(s); // �� ���¿��� ������ ��� ����
        waitInit = false;          // �ٶ�-��� �ʱ�ȭ
        readyForNext = false;      // ���� �� ��ȣ ����

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
        EnterState(next);  // �ϰ��� ���� ó��
        _lastState = next;
    }

    // ---------- ���� �̵� �ٽ� ----------
    void MoveViaPreThen(Transform pre, Transform main)
    {
        if (prePhase && pre != null)
        {
            MoveTo(pre);
            if (Arrived())
            {
                prePhase = false;          // ���� ���� �� ���� ��������
                agent.ResetPath();         // ��� �ʱ�ȭ
                agent.isStopped = false;   // �̵� �簳
                agent.updateRotation = true;
                readyForNext = false;      // �̵� �簳 �� ��ȣ ������(����)
            }
        }
        else
        {
            MoveTo(main);
        }
    }

    // ---------- ���� ��ƿ ----------
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
            case State.Exit2: return preExit2Point;   // �� Exit2 ���� ��������Ʈ
            default: return null;
        }
    }

    bool HasPrePoint(State s) => GetPrePoint(s) != null;

    void MoveTo(Transform t)
    {
        if (!t) return;

        // �̵� ���� ���� ��� ���� ����
        if (agent.isStopped) agent.isStopped = false;
        if (!agent.updateRotation) agent.updateRotation = true;   // ȸ�� �ڵ� ����

        // ������ ���� (�ε��Ҽ� ���� ���)
        if (!agent.hasPath || (agent.destination - t.position).sqrMagnitude > 0.0001f)
            agent.SetDestination(t.position);

        // �̵� �ִ� ������ desiredVelocity/velocity �� �� ���
        targetMoveParam = (agent.desiredVelocity.sqrMagnitude > 0.01f || agent.velocity.sqrMagnitude > 0.01f) ? 1f : 0f;
    }

    bool Arrived()
    {
        if (agent.pathPending) return false;
        if (agent.remainingDistance > agent.stoppingDistance + 0.05f) return false;
        return !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;
    }

    // ---- �ٶ󺸱� ----
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

    // ---- ���� �� �ٶ� �� ��� ----
    void FaceThenWaitNext(Transform lookTarget, float wait, State next /* �ܺ� ��ȯ�̸� next ���� ���� */)
    {
        agent.isStopped = true;
        agent.updateRotation = false;

        bool faced = FaceTowards(lookTarget);

        // ó�� ���� �� ���ð� �ʱ�ȭ
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
                waitInit = false;     // ���� ���� ���
                readyForNext = true;  // �ܺο��� Go(next) ȣ�� ���� ����
            }
        }
        // else: ���� ���� ���ߴ� ���̸� Ÿ�̸� ����
    }

    private void ClaimPickSlot()
    {
        // GameManager/AIManager ����/����Ʈ ��ȿ�� üũ
        if (GameManager.Instance == null || GameManager.Instance.ai == null)
        {
            Debug.LogWarning("[AIController] GameManager.Instance.ai ����");
            pickIdx = Mathf.Clamp(pickIdx, 0, (pickPoints?.Length ?? 1) - 1);
            return;
        }

        var pickList = GameManager.Instance.ai.Pick;
        if (pickList == null)
        {
            Debug.LogWarning("[AIController] AIManager.Pick ����Ʈ ����");
            pickIdx = Mathf.Clamp(pickIdx, 0, (pickPoints?.Length ?? 1) - 1);
            return;
        }

        // (����) pickPoints ���̿� ���� ����Ʈ ũ�� ����
        EnsureListSize(pickList, pickPoints != null ? pickPoints.Length : pickList.Count);

        // ù false ���� Ž��
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
            pickList[chosen] = true; // ����!
        }
        else
        {
            // �� ������ ���� ���� ����
            pickIdx = Mathf.Clamp(pickIdx, 0, (pickPoints?.Length ?? 1) - 1);
            Debug.LogWarning("[AIController] �� Pick ������ ���� �⺻ �ε����� �����մϴ�. pickIdx=" + pickIdx);
        }
    }

    private IEnumerator EatingLogic()
    {
        eatingLogicStarted = true;

        // 1�� �� isEating = true
        yield return new WaitForSeconds(1f);
        if (anim) anim.SetBool("isEating", true);

        // 6�� �� isEating = false
        yield return new WaitForSeconds(6f);
        if (anim) anim.SetBool("isEating", false);

        eatingLogicSFinshed = true;

        // ���̺� ��� �÷���
        GameManager.Instance.ai.isTableempty = true;

        // ����: Exit/Exit2�� �̵� (���ϴ� �� ȣ��)
        Go(State.Exit2); // �� Exit2�� �������� ����
        // �ʿ� �� Exit�� �ٲٷ���: Go(State.Exit);
    }

    // ����Ʈ�� ���� ũ����� false�� ä�� Ȯ��
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
            // Eat Ʈ���� ������ Table �ܰ迡�� ��ǥ�� EatPoint�� ����
            goToEatPoint = true;
        }
    }
}

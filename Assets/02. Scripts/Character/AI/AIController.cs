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
    public Transform[] pickPoints;   // �ݱ� �ĺ���
    public Transform[] packPoints;   // ���� �ĺ���
    public Transform[] hallPoints;   // Ȧ �ĺ���
    public Transform[] tablePoints;  // ���̺� �ĺ���
    public Transform[] exitPoints;   // ���� �ĺ���

    [Header("PreTargets (One)")]
    public Transform prePickPoint;   // �ݱ� ������(����)
    public Transform prePackPoint;   // ���� ������(����)
    public Transform preHallPoint;   // Ȧ ������(����)
    public Transform preTablePoint;  // ���̺� ������(����)
    public Transform preExitPoint;   // ���� ������(����)

    [Header("Use Index (0-based)")]
    public int pickIdx;
    public int packIdx;
    public int hallIdx;
    public int tableIdx;
    public int exitIdx;

    [Header("Look Targets (optional)")]
    public Transform pickLook;   // ������ ���õ� point�� �ٶ�
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

    // ���� ����
    float timer;
    float targetMoveParam;
    bool prePhase;          // ���� ���¿��� �������� �̵� ������
    bool waitInit;          // FaceThenWaitNext Ÿ�̸� �ʱ�ȭ ����
    private State _lastState; // �ܺο��� state ���� ���� ������

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
                    // GameManager.Instance.ai.Pick[pickIdx] = true; // �ʿ� �� ���
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
        }

        // �̵� �ִϸ��̼� ���� (����� ������)
        float animValue = anim.GetFloat("Move");
        animValue = Mathf.Lerp(animValue, targetMoveParam, 10f * Time.deltaTime);
        anim.SetFloat("Move", animValue);
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

    // ---- ���� �� �ٶ� �� ��� (���⼱ �ڵ� Go ����) ----
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
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIObjectController : MonoBehaviour
{
    [Header("Refs (Pickup / Drop)")]
    [SerializeField] private AIController aIController;

    [SerializeField] private GameObject BagPrefab;
    [SerializeField] private BreadBasket pickupBasket;   // ���⼭ pickUp
    [SerializeField] private BreadTable dropTable;       // ����� dropOff

    [SerializeField] private Transform stackPoint;
    [SerializeField] private Transform prestackPoint;
    [SerializeField] private Transform PaperBagPoint;

    [SerializeField] private Transform moneyTablePoint;

    [Header("Tags")]
    [SerializeField] private string pickUpTag = "Basket";   // �Ⱦ� ��
    [SerializeField] private string dropOffTag = "Table";   // ��� ��

    [Header("Motion")]
    [SerializeField] private float stepHeight = 0.25f;   // �� ����
    [SerializeField] private float stackMoveSpeed = 8f;  // �̵� �ӵ�
    [SerializeField] private float rotLerp = 8f;         // ȸ�� ����
    [SerializeField] private float delay = 0.1f;         // �Ⱦ�/��� �� ����

    [Header("Limits")]
    [SerializeField] private int maxStack = 8;

    private Stack<GameObject> stacking = new Stack<GameObject>(); // �տ� �� �͵�

    private GameObject currentBag;
    private Coroutine bagCo;

    private bool moneyCreated = false;

    private bool canStack;
    private bool canDrop;
    private float nextMove = 0f;
    public bool PickupFinish;
    public bool BagFinish;
    public bool DropFinish;   // �� ��� �Ϸ� �÷��� �߰�

    // ���� ���� ����
    private bool isPicking = false;
    private bool isDropping = false;

    private void Update()
    {
        if (!aIController || !aIController.readyForNext) return;

        int maxCarry = Mathf.Min(maxStack, aIController.breadCount);

        if (canStack && Time.time >= nextMove && stacking.Count < maxCarry && !isPicking)
        {
            StartPickupOne();
            nextMove = Time.time + delay;
        }

        if (canDrop && Time.time >= nextMove && stacking.Count > 0 && !isDropping &&
            GameManager.Instance && GameManager.Instance.ai && GameManager.Instance.ai.isCalculated)
        {
            if (aIController.isHall)
            {
                if (!moneyCreated)
                {
                    GameManager.Instance.ai.Moneycreate(moneyTablePoint, aIController.breadCount * 5);
                    BagFinish = true;
                    moneyCreated = true;   // �� �ѹ���
                }
            }
            else
            {
                StartDropOne();
                nextMove = Time.time + delay;
            }
        }

        if (stacking.Count == aIController.breadCount && !isPicking)
        {
            StartCoroutine(SetPickupFinishWithDelay(0.5f));
        }
    }

    // ---------- PICKUP ----------
    private void StartPickupOne()
    {
        if (!pickupBasket || !stackPoint || pickupBasket.playerdropOff) return;
        if (pickupBasket.breads == null || pickupBasket.breads.Count == 0) return;

        var picked = pickupBasket.breads.Pop();
        if (!picked) return;

        int slotIndex = stacking.Count;
        StartCoroutine(PickupOneRoutine(picked, slotIndex));
    }

    private IEnumerator PickupOneRoutine(GameObject picked, int slotIndex)
    {
        isPicking = true;

        EnsureKinematic(picked);
        var t = picked.transform;

        Vector3 basePos = prestackPoint ? prestackPoint.position : (stackPoint ? stackPoint.position : t.position);
        Vector3 prePos = basePos + Vector3.up * (stepHeight * slotIndex);

        Vector3 handPos = (stackPoint ? stackPoint.position : t.position) + Vector3.up * (stepHeight * slotIndex);
        Quaternion handRot = stackPoint ? stackPoint.rotation : t.rotation;

        while ((t.position - prePos).sqrMagnitude > 0.0001f)
        {
            t.position = Vector3.MoveTowards(t.position, prePos, stackMoveSpeed * Time.deltaTime);
            if (stackPoint) t.rotation = Quaternion.Slerp(t.rotation, stackPoint.rotation, rotLerp * Time.deltaTime);
            yield return null;
        }

        while ((t.position - handPos).sqrMagnitude > 0.0001f)
        {
            t.position = Vector3.MoveTowards(t.position, handPos, stackMoveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, handRot, rotLerp * Time.deltaTime);
            yield return null;
        }

        if (stackPoint)
        {
            t.SetParent(stackPoint, true);
            t.position = handPos;
            t.rotation = handRot;
        }

        stacking.Push(picked);
        isPicking = false;
    }

    // ---------- DROPOFF ----------
    private void StartDropOne()
    {
        // ���� ������ �� ���� ��ŸƮ
        if (bagCo == null)
            bagCo = StartCoroutine(Baglogic());

        if (!dropTable || stacking.Count == 0) return;

        var slots = dropTable.Rslots;
        if (slots == null || slots.Count == 0) return;

        int nextIndex = dropTable.breads.Count;
        int maxCapacity = Mathf.Min(slots.Count, 8);
        if (nextIndex >= maxCapacity) return;

        var bread = stacking.Pop();
        if (!bread) return;

        var slotT = slots[nextIndex];
        if (!slotT) return;

        StartCoroutine(DropOneRoutine(bread, slotT, nextIndex));
    }

    private IEnumerator DropOneRoutine(GameObject bread, Transform slotT, int slotIndex)
    {
        isDropping = true;

        EnsureKinematic(bread);
        var t = bread.transform;

        Vector3 fromPos = t.position;

        Vector3 basePos = prestackPoint ? prestackPoint.position : fromPos;
        Vector3 prePos = basePos + Vector3.up * (stepHeight * slotIndex);

        while ((t.position - prePos).sqrMagnitude > 0.0001f)
        {
            t.position = Vector3.MoveTowards(t.position, prePos, stackMoveSpeed * Time.deltaTime);
            if (stackPoint) t.rotation = Quaternion.Slerp(t.rotation, stackPoint.rotation, rotLerp * Time.deltaTime);
            yield return null;
        }

        Vector3 tablePos = slotT.position;
        Quaternion tableRot = slotT.rotation;

        t.SetParent(slotT, true);

        while ((t.position - tablePos).sqrMagnitude > 0.0001f)
        {
            t.position = Vector3.MoveTowards(t.position, tablePos, stackMoveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, tableRot, rotLerp * Time.deltaTime);
            yield return null;
        }

        t.SetParent(slotT, false);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        dropTable.breads.Push(bread);

        Destroy(bread);

        isDropping = false;
    }

    // ---------- TRIGGERS ----------
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(pickUpTag)) canStack = true;     // Basket ��
        if (other.CompareTag(dropOffTag)) canDrop = true;     // Table ��
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(pickUpTag)) canStack = false;
        if (other.CompareTag(dropOffTag)) canDrop = false;
    }

    // ---------- UTIL ----------
    private void EnsureKinematic(GameObject go)
    {
        if (go && go.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    private IEnumerator SetPickupFinishWithDelay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        PickupFinish = true;
    }

    // ---------- BAG ----------
    private IEnumerator Baglogic()
    {
        if (!BagPrefab || !PaperBagPoint)
        {
            Debug.LogWarning("[AIObjectController] BagPrefab �Ǵ� PaperBagPoint ���Ҵ�");
            bagCo = null;
            yield break;
        }

        // ������ ���� (PaperBagPoint���� ����)
        if (currentBag == null)
            currentBag = Instantiate(BagPrefab, PaperBagPoint.position, PaperBagPoint.rotation, PaperBagPoint);

        // 1.2�� ���
        yield return new WaitForSeconds(1.2f);

        // �ִ� Ʈ����
        var anim = currentBag.GetComponent<Animator>();
        if (!anim) anim = currentBag.GetComponentInChildren<Animator>();
        if (anim)
        {
            anim.ResetTrigger("BagClose");
            anim.SetTrigger("BagClose");

            // �ִϸ��̼� ���� ��� (Ÿ�Ӿƿ� ������ġ ����)
            yield return StartCoroutine(WaitForAnimatorFinishOrTimeout(anim, 0, 0.98f, 3f));
        }
        else
        {
            Debug.LogWarning("[AIObjectController] BagPrefab���� Animator�� ã�� ���߽��ϴ�. �ִ� ��� ���� �����մϴ�.");
            yield return new WaitForSeconds(0.5f);
        }

        // �� �ִ� ������ ������ prestackPoint �� stackPoint(���� ����)�� �̵�
        yield return StartCoroutine(MoveBagToStackRoutine());

        bagCo = null;
    }

    // �ִϸ��̼� ���� ��� (���� ��ȯ �� + normalizedTime ����, Ÿ�Ӿƿ� ����)
    private IEnumerator WaitForAnimatorFinishOrTimeout(Animator anim, int layer, float minNormalizedTime, float timeout)
    {
        float t = 0f;

        // ��ȯ ���̸� ��ȯ ���� ���
        while (anim.IsInTransition(layer))
        {
            if ((t += Time.deltaTime) >= timeout) yield break;
            yield return null;
        }

        var info = anim.GetCurrentAnimatorStateInfo(layer);
        int stateHash = info.shortNameHash;

        // ���� ���°� minNormalizedTime ���� ������
        while (info.shortNameHash == stateHash && info.normalizedTime < minNormalizedTime)
        {
            if ((t += Time.deltaTime) >= timeout) yield break;
            yield return null;
            info = anim.GetCurrentAnimatorStateInfo(layer);
        }
    }

    // ������ prestack �� stackPoint(���� �� ���� ����)�� �̵�
    private IEnumerator MoveBagToStackRoutine()
    {
        if (!currentBag)
            yield break;

        if (!stackPoint)
        {
            Debug.LogWarning("[AIObjectController] stackPoint ���Ҵ�: ���� �̵��� �ߴ��մϴ�.");
            yield break;
        }

        // �տ� ����ִ� �� ������ŭ ���� �״� ������ ��Ģ ���
        int slotIndex = stacking.Count;

        EnsureKinematic(currentBag);
        var t = currentBag.transform;

        // ��ǥ�� ���
        Vector3 basePos = prestackPoint ? prestackPoint.position : stackPoint.position;
        Vector3 prePos = basePos + Vector3.up * (stepHeight * slotIndex);

        Vector3 handPos = stackPoint.position + Vector3.up * (stepHeight * slotIndex);
        Quaternion handRot = stackPoint.rotation * Quaternion.Euler(0f, 90f, 0f);

        // ���� ��ġ �� ��������
        while ((t.position - prePos).sqrMagnitude > 0.0001f)
        {
            t.position = Vector3.MoveTowards(t.position, prePos, stackMoveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, handRot, rotLerp * Time.deltaTime);
            yield return null;
        }

        // �������� �� �� ����
        while ((t.position - handPos).sqrMagnitude > 0.0001f)
        {
            t.position = Vector3.MoveTowards(t.position, handPos, stackMoveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, handRot, rotLerp * Time.deltaTime);
            yield return null;
        }

        // �տ� ���̱�
        t.SetParent(stackPoint, true);
        t.position = handPos;
        t.rotation = handRot;

        
        GameManager.Instance.ai.Moneycreate(moneyTablePoint,aIController.breadCount * 5);

        BagFinish = true;
    }

}


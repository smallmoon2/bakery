using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization; // �� �ν����� ������
// using Unity.Burst.Intrinsics; // �ʿ� ������ ������ ��

public class AIObjectController : MonoBehaviour
{
    // ������������������������������������������������������������������ Refs (Core)
    [Header("Refs (Core)")]
    [SerializeField, FormerlySerializedAs("aIController")] private AIController aiController;

    // ������������������������������������������������������������������ Basket (PickUp)
    [Header("Basket (PickUp)")]
    [SerializeField, FormerlySerializedAs("pickupBasket")] private BreadBasket basketPickup;   // pick up from
    [SerializeField, FormerlySerializedAs("pickUpTag")] private string basketTag = "Basket"; // Ʈ���� �±�

    // ������������������������������������������������������������������ Table (DropOff)
    [Header("Table (DropOff)")]
    [SerializeField, FormerlySerializedAs("dropTable")] private BreadTable tableDrop;        // drop to
    [SerializeField, FormerlySerializedAs("dropOffTag")] private string tableTag = "Table";   // Ʈ���� �±�

    [Header("Food Placement (Eat)")]
    [SerializeField] private Transform foodPoint;     // ���� ���� ��ġ
    [SerializeField] private Transform preFoodPoint;  // ���� ����(�ɼ�)
    [SerializeField] private GameObject chair;  // ���� ����(�ɼ�)
    [SerializeField] private GameObject trashPrefab;  // ������ ������Ʈ
    public bool trashSpawned = false; // ������ ��ü 1ȸ ����
    private bool movedToFood = false;

    // ������������������������������������������������������������������ Hand Stack / Points
    [Header("Hand Stack / Points")]
    [SerializeField, FormerlySerializedAs("stackPoint")] private Transform stackPoint;
    [SerializeField, FormerlySerializedAs("prestackPoint")] private Transform preStackPoint;

    // ������������������������������������������������������������������ Bag
    [Header("Bag")]
    [SerializeField, FormerlySerializedAs("BagPrefab")] private GameObject bagPrefab;
    [SerializeField, FormerlySerializedAs("PaperBagPoint")] private Transform bagPoint;

    // ������������������������������������������������������������������ Money (Hall ����)
    [Header("Money (Hall Only)")]
    [SerializeField, FormerlySerializedAs("moneyTablePoint")] private Transform moneyPoint;

    // ������������������������������������������������������������������ Motion & Limits
    [Header("Motion")]
    [SerializeField] private float stepHeight = 0.25f;  // �� ����
    [SerializeField] private float stackMoveSpeed = 8f; // �̵� �ӵ�
    [SerializeField] private float rotLerp = 8f;        // ȸ�� ����
    [SerializeField, FormerlySerializedAs("delay")] private float actionDelay = 0.1f; // �Ⱦ�/��� �� ����

    [Header("Limits")]
    [SerializeField] private int maxStack = 8;

    // ������������������������������������������������������������������ Runtime
    private readonly Stack<GameObject> stacking = new Stack<GameObject>(); // �տ� �� �͵�
    private GameObject currentBag;
    private Coroutine bagCo;

    private bool canStack;
    private bool canDrop;
    private bool isPicking = false;
    private bool isDropping = false;
    private bool moneyCreated = false;

    private float nextMove = 0f;

    // �ܺο��� ���� �÷���
    public bool PickupFinish;
    public bool BagFinish;
    public bool DropFinish;

    [SerializeField] private bool debugDropGate = false;

    // ������������������������������������������������������������������ Unity Loop
    private void Update()
    {

        // ��� ����

        if (aiController.eatingLogicSFinshed && !trashSpawned)  // �� ����ڰ� ���� ������ �״��
        {
            Debug.Log("����ó��");
            StartCoroutine(ReplaceFoodWithTrashOnce());
        }

        /// �ൿ �� ó��

        if (!aiController || !aiController.readyForNext) return;

        bool cond_canDrop = canDrop;
        bool cond_timeOK = Time.time >= nextMove;
        int stackCnt = stacking.Count;
        bool cond_hasStack = stackCnt > 0;
        bool cond_notDropping = !isDropping;

        var gm = GameManager.Instance;
        bool cond_hasGM = gm != null;
        bool cond_hasAI = cond_hasGM && gm.ai != null;
        bool cond_isCalc = cond_hasAI && gm.ai.isCalculated;

        if (debugDropGate)
        {
            Debug.Log(
                $"[DropGate] canDrop={cond_canDrop} | timeOK={cond_timeOK} (t={Time.time:F2}, next={nextMove:F2}) | " +
                $"hasStack={cond_hasStack} (count={stackCnt}) | notDropping={cond_notDropping} | " +
                $"GM={(cond_hasGM ? "OK" : "NULL")} AI={(cond_hasAI ? "OK" : "NULL")} isCalculated={(cond_hasAI ? gm.ai.isCalculated.ToString() : "n/a")}"
            );
        }
        // ��������������������������������������������������������������������������������������������������������

        if (cond_canDrop && cond_timeOK && cond_hasStack && cond_notDropping && cond_isCalc)
        {
            if (aiController.isHall)
            {
                if (debugDropGate) Debug.Log($"[DropGate] PASS �� Hall �б� (moneyCreated={moneyCreated})");
                if (!moneyCreated)
                {
                    gm.ai.Moneycreate(moneyPoint, aiController.breadCount * 5);
                    BagFinish = true;
                    moneyCreated = true;
                }
            }
            else
            {
                if (debugDropGate) Debug.Log("[DropGate] PASS �� Table ��� �б� StartDropOne()");
                StartDropOne();
                nextMove = Time.time + actionDelay;
            }
        }
        else
        {
            // � ������ ���Ҵ��� �� ���
            if (debugDropGate)
            {
                if (!cond_canDrop) Debug.LogWarning("[DropGate] BLOCKED: canDrop == false (Table Ʈ���� �ȿ� �ƴ�?)");
                if (!cond_timeOK) Debug.LogWarning($"[DropGate] BLOCKED: Time.time < nextMove ({Time.time:F2} < {nextMove:F2})");
                if (!cond_hasStack) Debug.LogWarning("[DropGate] BLOCKED: stacking.Count == 0 (�տ� ���� ����)");
                if (!cond_notDropping) Debug.LogWarning("[DropGate] BLOCKED: isDropping == true (��� ��)");
                if (!cond_hasGM) Debug.LogWarning("[DropGate] BLOCKED: GameManager.Instance == null");
                else if (!cond_hasAI) Debug.LogWarning("[DropGate] BLOCKED: GameManager.Instance.ai == null");
                else if (!cond_isCalc) Debug.LogWarning("[DropGate] BLOCKED: ai.isCalculated == false");
            }
        }
        int maxCarry = Mathf.Min(maxStack, aiController.breadCount);

        // Basket �� ��(����)
        if (canStack && Time.time >= nextMove && stacking.Count < maxCarry && !isPicking)
        {
            StartPickupOne();
            nextMove = Time.time + actionDelay;
        }

        // ��(����) �� Table or Hall Money
        if (canDrop && Time.time >= nextMove && stacking.Count > 0 && !isDropping &&
            GameManager.Instance && GameManager.Instance.ai && GameManager.Instance.ai.isCalculated)
        {
            if (aiController.isHall)
            {
                Debug.Log(!moneyCreated);
                if (!moneyCreated)
                {
                    GameManager.Instance.ai.Moneycreate(moneyPoint, aiController.breadCount * 5);
                    BagFinish = true;
                    moneyCreated = true;   // �� ����
                }
            }
            else
            {
                StartDropOne();
                nextMove = Time.time + actionDelay;
            }
        }

        if (aiController.eatingLogicStarted && !movedToFood)
        {
            StartCoroutine(MoveHandStackToFoodPointOnce());
        }



        // ��� �������� �ణ ��� �� �Ϸ�
        if (stacking.Count == aiController.breadCount && !isPicking)
        {
            StartCoroutine(SetPickupFinishWithDelay(0.5f));
        }
    }

    // ������������������������������������������������������������������ PICKUP
    private void StartPickupOne()
    {
        if (!basketPickup || !stackPoint || basketPickup.playerdropOff) return;
        if (basketPickup.breads == null || basketPickup.breads.Count == 0) return;

        var picked = basketPickup.breads.Pop();
        if (!picked) return;

        int slotIndex = stacking.Count;
        StartCoroutine(PickupOneRoutine(picked, slotIndex));
    }

    private IEnumerator PickupOneRoutine(GameObject picked, int slotIndex)
    {
        isPicking = true;

        EnsureKinematic(picked);
        var t = picked.transform;

        Vector3 basePos = preStackPoint ? preStackPoint.position : (stackPoint ? stackPoint.position : t.position);
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

    // ������������������������������������������������������������������ DROPOFF (Table)
    private void StartDropOne()
    {
        // ���� ������ �� ���� ��ŸƮ
        if (bagCo == null)
            bagCo = StartCoroutine(Baglogic());

        if (!tableDrop || stacking.Count == 0) return;

        var slots = tableDrop.Rslots;
        if (slots == null || slots.Count == 0) return;

        int nextIndex = tableDrop.breads.Count;
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

        Vector3 basePos = preStackPoint ? preStackPoint.position : fromPos;
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

        tableDrop.breads.Push(bread);
        Destroy(bread);

        isDropping = false;
    }

    // ������������������������������������������������������������������ TRIGGERS
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(basketTag)) canStack = true; // Basket ��
        if (other.CompareTag(tableTag)) canDrop = true; // Table  ��
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(basketTag)) canStack = false;
        if (other.CompareTag(tableTag)) canDrop = false;
    }

    // ������������������������������������������������������������������ Utils
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

    // ������������������������������������������������������������������ Bag Logic
    private IEnumerator Baglogic()
    {
        if (!bagPrefab || !bagPoint)
        {
            Debug.LogWarning("[AIObjectController] bagPrefab �Ǵ� bagPoint ���Ҵ�");
            bagCo = null;
            yield break;
        }

        // ������ ���� (bagPoint���� ����)
        if (currentBag == null)
            currentBag = Instantiate(bagPrefab, bagPoint.position, bagPoint.rotation, bagPoint);

        // 1.2�� ���
        yield return new WaitForSeconds(1.2f);

        // �ִ� Ʈ����
        var anim = currentBag.GetComponent<Animator>() ?? currentBag.GetComponentInChildren<Animator>();
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

        // �ִ� ������ ���� �̵�
        yield return StartCoroutine(MoveBagToStackRoutine());

        bagCo = null;
    }

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

    private IEnumerator MoveBagToStackRoutine()
    {
        if (!currentBag) yield break;
        if (!stackPoint)
        {
            Debug.LogWarning("[AIObjectController] stackPoint ���Ҵ�: ���� �̵��� �ߴ��մϴ�.");
            yield break;
        }

        int slotIndex = stacking.Count;

        EnsureKinematic(currentBag);
        var t = currentBag.transform;

        Vector3 basePos = preStackPoint ? preStackPoint.position : stackPoint.position;
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

        // Hall�� �� �� ���� (���� ���� ���� �� ���� ȣ��)
        GameManager.Instance.ai.Moneycreate(moneyPoint, aiController.breadCount * 5);

        BagFinish = true;
    }

    private IEnumerator MoveHandStackToFoodPointOnce()
    {
        movedToFood = true;

        if (!stackPoint || !foodPoint)
            yield break;

        // ���� �տ� �鸰(=stackPoint�� �ڽ�) ������Ʈ���� ����Ʈ�� ���� ����
        var items = new List<Transform>();
        for (int i = 0; i < stackPoint.childCount; i++)
            items.Add(stackPoint.GetChild(i));

        // ����������/�Ʒ��������� ��� ������ ���ϴ� ���.
        // ���⼱ 0,1,2... ������� foodPoint�� 0,1,2...�� ����
        for (int i = 0; i < items.Count; i++)
        {
            var tr = items[i];
            if (!tr) continue;

            // ���� ���� ����
            EnsureKinematic(tr.gameObject);

            // ��ǥ ���� ���
            Vector3 prePos = (preFoodPoint ? preFoodPoint.position : foodPoint.position) + Vector3.up * (stepHeight * i);
            Vector3 targetPos = foodPoint.position + Vector3.up * (stepHeight * i);
            Quaternion targetRot = foodPoint.rotation;

            // ���� ��ġ �� preFoodPoint
            while ((tr.position - prePos).sqrMagnitude > 0.0001f)
            {
                tr.position = Vector3.MoveTowards(tr.position, prePos, stackMoveSpeed * Time.deltaTime);
                tr.rotation = Quaternion.Slerp(tr.rotation, targetRot, rotLerp * Time.deltaTime);
                yield return null;
            }

            // preFoodPoint �� foodPoint(����)
            // ���� �θ� foodPoint�� �ٲ㵵 �ǰ�, ������ �ٲ㵵 ��. ���⼱ ������ ��Ȯ�� ����.
            while ((tr.position - targetPos).sqrMagnitude > 0.0001f)
            {
                tr.position = Vector3.MoveTowards(tr.position, targetPos, stackMoveSpeed * Time.deltaTime);
                tr.rotation = Quaternion.Slerp(tr.rotation, targetRot, rotLerp * Time.deltaTime);
                yield return null;
            }

            tr.SetParent(foodPoint, true);
            tr.position = targetPos;
            tr.rotation = targetRot;
        }
    }
    private IEnumerator ReplaceFoodWithTrashOnce()
    {
        trashSpawned = true;

        if (!foodPoint)
        {
            Debug.LogWarning("[AIObjectController] foodPoint ���Ҵ�");
            yield break;
        }
        if (!trashPrefab)
        {
            Debug.LogWarning("[AIObjectController] trashPrefab ���Ҵ�");
            yield break;
        }

        // �ڽ� ������
        var children = new List<Transform>();
        for (int i = 0; i < foodPoint.childCount; i++)
            children.Add(foodPoint.GetChild(i));

        // �߾�(centroid) ��ġ ���
        Vector3 spawnPos;
        if (children.Count > 0)
        {
            Vector3 sum = Vector3.zero;
            foreach (var c in children) if (c) sum += c.position;
            spawnPos = sum / children.Count;

            // �� ���� "ù ��° �ڽ� ��ġ"�� ���� �ʹٸ� �Ʒ� �� �ٷ� ��ü:
            // spawnPos = children[0].position;
        }
        else
        {
            // �ڽ��� ������ foodPoint ��ü ��ġ ���
            spawnPos = foodPoint.position;
        }

        Quaternion spawnRot = foodPoint.rotation;

        // �ڽĵ� ����
        foreach (var c in children)
            if (c) Destroy(c.gameObject);

        // ������ �� 1�� ����
        GameManager.Instance.ai.Trash = Instantiate(trashPrefab, spawnPos, spawnRot, foodPoint);
        GameManager.Instance.ai.Chair = chair;
        chair.transform.eulerAngles = new Vector3(
            chair.transform.eulerAngles.x,
            chair.transform.eulerAngles.y + 45f,
            chair.transform.eulerAngles.z
        );
        yield return null;
    }

}

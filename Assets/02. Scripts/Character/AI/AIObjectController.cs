using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization; // ← 인스펙터 유지용
// using Unity.Burst.Intrinsics; // 필요 없으면 지워도 됨

public class AIObjectController : MonoBehaviour
{
    // ───────────────────────────────── Refs (Core)
    [Header("Refs (Core)")]
    [SerializeField, FormerlySerializedAs("aIController")] private AIController aiController;

    // ───────────────────────────────── Basket (PickUp)
    [Header("Basket (PickUp)")]
    [SerializeField, FormerlySerializedAs("pickupBasket")] private BreadBasket basketPickup;   // pick up from
    [SerializeField, FormerlySerializedAs("pickUpTag")] private string basketTag = "Basket"; // 트리거 태그

    // ───────────────────────────────── Table (DropOff)
    [Header("Table (DropOff)")]
    [SerializeField, FormerlySerializedAs("dropTable")] private BreadTable tableDrop;        // drop to
    [SerializeField, FormerlySerializedAs("dropOffTag")] private string tableTag = "Table";   // 트리거 태그

    [Header("Food Placement (Eat)")]
    [SerializeField] private Transform foodPoint;     // 최종 놓을 위치
    [SerializeField] private Transform preFoodPoint;  // 경유 지점(옵션)
    [SerializeField] private GameObject chair;  // 경유 지점(옵션)
    [SerializeField] private GameObject trashPrefab;  // 쓰레기 오브젝트
    public bool trashSpawned = false; // 쓰레기 교체 1회 가드
    private bool movedToFood = false;

    // ───────────────────────────────── Hand Stack / Points
    [Header("Hand Stack / Points")]
    [SerializeField, FormerlySerializedAs("stackPoint")] private Transform stackPoint;
    [SerializeField, FormerlySerializedAs("prestackPoint")] private Transform preStackPoint;

    // ───────────────────────────────── Bag
    [Header("Bag")]
    [SerializeField, FormerlySerializedAs("BagPrefab")] private GameObject bagPrefab;
    [SerializeField, FormerlySerializedAs("PaperBagPoint")] private Transform bagPoint;

    // ───────────────────────────────── Money (Hall 전용)
    [Header("Money (Hall Only)")]
    [SerializeField, FormerlySerializedAs("moneyTablePoint")] private Transform moneyPoint;

    // ───────────────────────────────── Motion & Limits
    [Header("Motion")]
    [SerializeField] private float stepHeight = 0.25f;  // 층 간격
    [SerializeField] private float stackMoveSpeed = 8f; // 이동 속도
    [SerializeField] private float rotLerp = 8f;        // 회전 보간
    [SerializeField, FormerlySerializedAs("delay")] private float actionDelay = 0.1f; // 픽업/드롭 간 간격

    [Header("Limits")]
    [SerializeField] private int maxStack = 8;

    // ───────────────────────────────── Runtime
    private readonly Stack<GameObject> stacking = new Stack<GameObject>(); // 손에 든 것들
    private GameObject currentBag;
    private Coroutine bagCo;

    private bool canStack;
    private bool canDrop;
    private bool isPicking = false;
    private bool isDropping = false;
    private bool moneyCreated = false;

    private float nextMove = 0f;

    // 외부에서 보는 플래그
    public bool PickupFinish;
    public bool BagFinish;
    public bool DropFinish;

    [SerializeField] private bool debugDropGate = false;

    // ───────────────────────────────── Unity Loop
    private void Update()
    {

        // 상시 감지

        if (aiController.eatingLogicSFinshed && !trashSpawned)  // ← 사용자가 말한 변수명 그대로
        {
            Debug.Log("음식처리");
            StartCoroutine(ReplaceFoodWithTrashOnce());
        }

        /// 행동 후 처리

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
        // ────────────────────────────────────────────────────

        if (cond_canDrop && cond_timeOK && cond_hasStack && cond_notDropping && cond_isCalc)
        {
            if (aiController.isHall)
            {
                if (debugDropGate) Debug.Log($"[DropGate] PASS → Hall 분기 (moneyCreated={moneyCreated})");
                if (!moneyCreated)
                {
                    gm.ai.Moneycreate(moneyPoint, aiController.breadCount * 5);
                    BagFinish = true;
                    moneyCreated = true;
                }
            }
            else
            {
                if (debugDropGate) Debug.Log("[DropGate] PASS → Table 드롭 분기 StartDropOne()");
                StartDropOne();
                nextMove = Time.time + actionDelay;
            }
        }
        else
        {
            // 어떤 조건이 막았는지 상세 경고
            if (debugDropGate)
            {
                if (!cond_canDrop) Debug.LogWarning("[DropGate] BLOCKED: canDrop == false (Table 트리거 안에 아님?)");
                if (!cond_timeOK) Debug.LogWarning($"[DropGate] BLOCKED: Time.time < nextMove ({Time.time:F2} < {nextMove:F2})");
                if (!cond_hasStack) Debug.LogWarning("[DropGate] BLOCKED: stacking.Count == 0 (손에 물건 없음)");
                if (!cond_notDropping) Debug.LogWarning("[DropGate] BLOCKED: isDropping == true (드롭 중)");
                if (!cond_hasGM) Debug.LogWarning("[DropGate] BLOCKED: GameManager.Instance == null");
                else if (!cond_hasAI) Debug.LogWarning("[DropGate] BLOCKED: GameManager.Instance.ai == null");
                else if (!cond_isCalc) Debug.LogWarning("[DropGate] BLOCKED: ai.isCalculated == false");
            }
        }
        int maxCarry = Mathf.Min(maxStack, aiController.breadCount);

        // Basket → 손(스택)
        if (canStack && Time.time >= nextMove && stacking.Count < maxCarry && !isPicking)
        {
            StartPickupOne();
            nextMove = Time.time + actionDelay;
        }

        // 손(스택) → Table or Hall Money
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
                    moneyCreated = true;   // 한 번만
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



        // 모두 집었으면 약간 대기 후 완료
        if (stacking.Count == aiController.breadCount && !isPicking)
        {
            StartCoroutine(SetPickupFinishWithDelay(0.5f));
        }
    }

    // ───────────────────────────────── PICKUP
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

    // ───────────────────────────────── DROPOFF (Table)
    private void StartDropOne()
    {
        // 가방 로직은 한 번만 스타트
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

    // ───────────────────────────────── TRIGGERS
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(basketTag)) canStack = true; // Basket 존
        if (other.CompareTag(tableTag)) canDrop = true; // Table  존
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(basketTag)) canStack = false;
        if (other.CompareTag(tableTag)) canDrop = false;
    }

    // ───────────────────────────────── Utils
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

    // ───────────────────────────────── Bag Logic
    private IEnumerator Baglogic()
    {
        if (!bagPrefab || !bagPoint)
        {
            Debug.LogWarning("[AIObjectController] bagPrefab 또는 bagPoint 미할당");
            bagCo = null;
            yield break;
        }

        // 없으면 생성 (bagPoint에서 시작)
        if (currentBag == null)
            currentBag = Instantiate(bagPrefab, bagPoint.position, bagPoint.rotation, bagPoint);

        // 1.2초 대기
        yield return new WaitForSeconds(1.2f);

        // 애니 트리거
        var anim = currentBag.GetComponent<Animator>() ?? currentBag.GetComponentInChildren<Animator>();
        if (anim)
        {
            anim.ResetTrigger("BagClose");
            anim.SetTrigger("BagClose");

            // 애니메이션 종료 대기 (타임아웃 안전장치 포함)
            yield return StartCoroutine(WaitForAnimatorFinishOrTimeout(anim, 0, 0.98f, 3f));
        }
        else
        {
            Debug.LogWarning("[AIObjectController] BagPrefab에서 Animator를 찾지 못했습니다. 애니 대기 없이 진행합니다.");
            yield return new WaitForSeconds(0.5f);
        }

        // 애니 끝나면 가방 이동
        yield return StartCoroutine(MoveBagToStackRoutine());

        bagCo = null;
    }

    private IEnumerator WaitForAnimatorFinishOrTimeout(Animator anim, int layer, float minNormalizedTime, float timeout)
    {
        float t = 0f;

        // 전환 중이면 전환 종료 대기
        while (anim.IsInTransition(layer))
        {
            if ((t += Time.deltaTime) >= timeout) yield break;
            yield return null;
        }

        var info = anim.GetCurrentAnimatorStateInfo(layer);
        int stateHash = info.shortNameHash;

        // 같은 상태가 minNormalizedTime 지날 때까지
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
            Debug.LogWarning("[AIObjectController] stackPoint 미할당: 가방 이동을 중단합니다.");
            yield break;
        }

        int slotIndex = stacking.Count;

        EnsureKinematic(currentBag);
        var t = currentBag.transform;

        Vector3 basePos = preStackPoint ? preStackPoint.position : stackPoint.position;
        Vector3 prePos = basePos + Vector3.up * (stepHeight * slotIndex);

        Vector3 handPos = stackPoint.position + Vector3.up * (stepHeight * slotIndex);
        Quaternion handRot = stackPoint.rotation * Quaternion.Euler(0f, 90f, 0f);

        // 현재 위치 → 프리스택
        while ((t.position - prePos).sqrMagnitude > 0.0001f)
        {
            t.position = Vector3.MoveTowards(t.position, prePos, stackMoveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, handRot, rotLerp * Time.deltaTime);
            yield return null;
        }

        // 프리스택 → 손 슬롯
        while ((t.position - handPos).sqrMagnitude > 0.0001f)
        {
            t.position = Vector3.MoveTowards(t.position, handPos, stackMoveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, handRot, rotLerp * Time.deltaTime);
            yield return null;
        }

        // 손에 붙이기
        t.SetParent(stackPoint, true);
        t.position = handPos;
        t.rotation = handRot;

        // Hall일 때 돈 생성 (여긴 가드 없이 한 번만 호출)
        GameManager.Instance.ai.Moneycreate(moneyPoint, aiController.breadCount * 5);

        BagFinish = true;
    }

    private IEnumerator MoveHandStackToFoodPointOnce()
    {
        movedToFood = true;

        if (!stackPoint || !foodPoint)
            yield break;

        // 현재 손에 들린(=stackPoint의 자식) 오브젝트들을 리스트로 먼저 복사
        var items = new List<Transform>();
        for (int i = 0; i < stackPoint.childCount; i++)
            items.Add(stackPoint.GetChild(i));

        // 위에서부터/아래에서부터 어느 순서든 원하는 대로.
        // 여기선 0,1,2... 순서대로 foodPoint에 0,1,2...로 쌓음
        for (int i = 0; i < items.Count; i++)
        {
            var tr = items[i];
            if (!tr) continue;

            // 물리 영향 제거
            EnsureKinematic(tr.gameObject);

            // 목표 포즈 계산
            Vector3 prePos = (preFoodPoint ? preFoodPoint.position : foodPoint.position) + Vector3.up * (stepHeight * i);
            Vector3 targetPos = foodPoint.position + Vector3.up * (stepHeight * i);
            Quaternion targetRot = foodPoint.rotation;

            // 현재 위치 → preFoodPoint
            while ((tr.position - prePos).sqrMagnitude > 0.0001f)
            {
                tr.position = Vector3.MoveTowards(tr.position, prePos, stackMoveSpeed * Time.deltaTime);
                tr.rotation = Quaternion.Slerp(tr.rotation, targetRot, rotLerp * Time.deltaTime);
                yield return null;
            }

            // preFoodPoint → foodPoint(최종)
            // 먼저 부모를 foodPoint로 바꿔도 되고, 끝나고 바꿔도 됨. 여기선 끝나고 정확히 스냅.
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
            Debug.LogWarning("[AIObjectController] foodPoint 미할당");
            yield break;
        }
        if (!trashPrefab)
        {
            Debug.LogWarning("[AIObjectController] trashPrefab 미할당");
            yield break;
        }

        // 자식 스냅샷
        var children = new List<Transform>();
        for (int i = 0; i < foodPoint.childCount; i++)
            children.Add(foodPoint.GetChild(i));

        // 중앙(centroid) 위치 계산
        Vector3 spawnPos;
        if (children.Count > 0)
        {
            Vector3 sum = Vector3.zero;
            foreach (var c in children) if (c) sum += c.position;
            spawnPos = sum / children.Count;

            // ★ 만약 "첫 번째 자식 위치"로 쓰고 싶다면 아래 한 줄로 교체:
            // spawnPos = children[0].position;
        }
        else
        {
            // 자식이 없으면 foodPoint 자체 위치 사용
            spawnPos = foodPoint.position;
        }

        Quaternion spawnRot = foodPoint.rotation;

        // 자식들 제거
        foreach (var c in children)
            if (c) Destroy(c.gameObject);

        // 쓰레기 단 1개 생성
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

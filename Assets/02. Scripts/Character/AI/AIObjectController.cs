using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIObjectController : MonoBehaviour
{
    [Header("Refs (Pickup / Drop)")]
    [SerializeField] private BreadBasket pickupBasket;   // 여기서 pickUp
    [SerializeField] private BreadTable dropTable;       // 여기로 dropOff

    [SerializeField] private Transform stackPoint;
    [SerializeField] private Transform prestackPoint;

    [Header("Tags")]
    [SerializeField] private string pickUpTag = "Basket";   // 픽업 존
    [SerializeField] private string dropOffTag = "Table";   // 드롭 존

    [Header("Motion")]
    [SerializeField] private float stepHeight = 0.25f;   // 층 간격
    [SerializeField] private float stackMoveSpeed = 8f;  // 이동 속도
    [SerializeField] private float rotLerp = 8f;         // 회전 보간
    [SerializeField] private float delay = 0.1f;         // 픽업/드롭 간 간격

    [Header("Limits")]
    [SerializeField] private int maxStack = 8;

    private Stack<GameObject> stacking = new Stack<GameObject>();                    // 손에 든 것들
    private readonly Dictionary<GameObject, Transform> dropping = new Dictionary<GameObject, Transform>(); // 드롭 이동 중

    private bool canStack;
    private bool canDrop;
    private float nextMove = 0f;

    private void Update()
    {
        // 픽업: 바스켓 안 + 간격 + 손 제한
        if (canStack && Time.time >= nextMove && stacking.Count < maxStack)
        {
            TryPickupOneFromBasket();
            nextMove = Time.time + delay;
        }

        // 드롭: 테이블 안 + 현재 드롭 없음 + 간격
        if (canDrop && dropping.Count == 0 && Time.time >= nextMove)
        {
            TryDropOneToTable();
            nextMove = Time.time + delay;
        }

        ProcessDropping();           // 드롭 중 이동 처리
        MoveAllInHandToSlots();      // 손에 든 것들 정렬
    }

    // ---------- PICKUP ----------
    private void TryPickupOneFromBasket()
    {
        if (!pickupBasket || !stackPoint) return;

        // Basket에서 하나 꺼내오기 (Stack<GameObject> 가정)
        if (pickupBasket.breads == null || pickupBasket.breads.Count == 0) return;

        GameObject picked = null;
        // 최상단에서 Pop
        picked = pickupBasket.breads.Pop();
        if (!picked) return;

        int slotIndex = stacking.Count;

        // 부모를 stackPoint로 (월드좌표 유지)
        var t = picked.transform;
        t.SetParent(stackPoint, true);

        // 프리스택 → 손으로 자연스럽게 정렬
        Vector3 basePos = prestackPoint ? prestackPoint.position : stackPoint.position;
        Vector3 prePos = basePos + Vector3.up * (stepHeight * slotIndex);
        t.position = prePos;

        EnsureKinematic(picked);
        stacking.Push(picked);
    }

    // 손에 든 것들 정렬(항상 부드럽게 붙이기)
    private void MoveAllInHandToSlots()
    {
        if (!stackPoint) return;

        var arr = stacking.ToArray(); // top-first
        int n = arr.Length;
        int slot = 0;

        for (int i = n - 1; i >= 0; i--)
        {
            var go = arr[i];
            if (!go) continue;

            Vector3 targetPos = stackPoint.position + Vector3.up * (stepHeight * slot);
            var t = go.transform;

            t.position = Vector3.MoveTowards(t.position, targetPos, stackMoveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, stackPoint.rotation, rotLerp * Time.deltaTime);
            slot++;
        }
    }

    // ---------- DROPOFF ----------
    private void TryDropOneToTable()
    {
        if (!dropTable) return;

        var slots = dropTable.Rslots; // IReadOnlyList<Transform> 가정
        if (slots == null || slots.Count == 0) return;

        if (dropping.Count > 0) return;     // 한 번에 하나만
        if (stacking.Count == 0) return;

        int nextIndex = dropTable.breads.Count;                  // 테이블에 이미 놓인 개수
        int maxCapacity = Mathf.Min(slots.Count, 8);             // 안전 상한선
        if (nextIndex >= maxCapacity) return;

        var bread = stacking.Pop();
        if (!bread) return;

        var slotT = slots[nextIndex];
        if (!slotT) return;

        var t = bread.transform;

        // 드롭 시작 위치(프리스택 → 슬롯)
        Vector3 startPos = prestackPoint ? prestackPoint.position
                                         : (stackPoint ? stackPoint.position : t.position);
        t.position = startPos;

        // 이동 동안 슬롯을 부모로(월드 좌표 유지)
        t.SetParent(slotT, true);
        EnsureKinematic(bread);

        // 이동 처리 테이블에 등록
        dropping[bread] = slotT;
    }

    private void ProcessDropping()
    {
        if (dropping.Count == 0) return;

        var finalize = new List<GameObject>();

        foreach (var kv in dropping)
        {
            var go = kv.Key;
            var slotT = kv.Value;
            if (!go || !slotT) { finalize.Add(go); continue; }

            var t = go.transform;
            t.position = Vector3.MoveTowards(t.position, slotT.position, stackMoveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, slotT.rotation, rotLerp * Time.deltaTime);

            if ((t.position - slotT.position).sqrMagnitude < 0.0001f)
                finalize.Add(go);
        }

        foreach (var go in finalize)
        {
            if (go && dropping.TryGetValue(go, out var slotT) && slotT)
            {
                var t = go.transform;
                t.SetParent(slotT, false);          // 로컬로 스냅
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;

                // 테이블 스택에 쌓기
                dropTable.breads.Push(go);
            }
            dropping.Remove(go);
        }
    }

    // ---------- TRIGGERS ----------
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(pickUpTag)) canStack = true;     // Basket 존
        if (other.CompareTag(dropOffTag)) canDrop = true;     // Table 존
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
}

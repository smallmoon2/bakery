using System.Collections;
using System.Collections.Generic;
// using Unity.VisualScripting;            // 불필요하면 제거
// using UnityEditor.VersionControl;       // 에디터 전용: 삭제하거나 #if UNITY_EDITOR 로 감싸세요
using UnityEngine;

public class PlayerObjectController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BreadSpawner spawner;
    [SerializeField] private BreadBasket Basket;

    [SerializeField] private Transform stackPoint;
    [SerializeField] private Transform prestackPoint;

    [Header("Tags")]
    [SerializeField] private string pickUpTag = "Oven";
    [SerializeField] private string dropOffTag = "Basket";

    [Header("Motion")]
    [SerializeField] private float stepHeight = 0.25f;     // 층 간격
    [SerializeField] private float stackMoveSpeed = 8f;    // 부드러운 이동 속도
    [SerializeField] private float rotLerp = 8f;           // 회전 보간 속도
    [SerializeField] private float delay = 0.1f;           // 픽업/드롭 시작 간격

    [Header("Limits")]
    [SerializeField] private int maxStack = 8;

    private Stack<GameObject> stacking = new Stack<GameObject>();
    private readonly Dictionary<GameObject, Transform> dropping = new Dictionary<GameObject, Transform>();

    private bool canStack;
    private bool canDrop;

    private float nextMove = 0f;

    private void Update()
    {
        // 픽업: 오븐 안 + 간격 지남 + 손 제한 미초과
        if (canStack && Time.time >= nextMove && stacking.Count < maxStack)
        {
            TryPickupOneToStackPoint();
            nextMove = Time.time + delay;
        }

        // 드롭: 바스켓 안 + 현재 드롭 없음 + 간격 지남  (픽업 여부와 독립)
        if (canDrop && dropping.Count == 0 && Time.time >= nextMove)
        {
            TryDropOneToBasket();
            nextMove = Time.time + delay;
        }

        // 드롭 진행 중 이동
        ProcessDropping();

        // 손에 든 빵 정렬(항상 수행)
        MoveAllInHandToSlots();
    }

    private void TryPickupOneToStackPoint()
    {
        if (!spawner || !stackPoint) return;
        if (spawner.breads == null || spawner.breads.Count == 0) return;

        spawner.breads.RemoveAll(b => b == null);
        if (spawner.breads.Count == 0) return;

        GameObject picked = null;
        for (int i = 0; i < spawner.breads.Count; i++)
        {
            var c = spawner.breads[i];
            if (c != null)
            {
                picked = c;
                spawner.breads.RemoveAt(i);
                break;
            }
        }
        if (!picked) return;

        // 이 빵이 들어갈 슬롯 인덱스
        int slotIndex = stacking.Count;

        // 부모를 stackPoint로 (월드 좌표 유지)
        var t = picked.transform;
        t.SetParent(stackPoint, true);

        // prestack 기준(없으면 stackPoint로 폴백)
        Vector3 basePos = prestackPoint ? prestackPoint.position : stackPoint.position;

        // 프리스택 위치로 즉시 이동 (층 간격 적용)
        Vector3 prePos = basePos + Vector3.up * (stepHeight * slotIndex);
        t.position = prePos;

        EnsureKinematic(picked);

        // 손 스택에 보관
        stacking.Push(picked);
    }

    private void MoveAllInHandToSlots()
    {
        if (!stackPoint) return;

        var arr = stacking.ToArray(); // top-first
        int n = arr.Length;
        int slot = 0;

        // bottom -> top 순으로 목표 계산
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

    private void TryDropOneToBasket()
    {
        if (!Basket) return;

        var slots = Basket.Rslots; // IReadOnlyList<Transform>
        if (slots == null || slots.Count == 0) return;

        // 한 번에 하나만
        if (dropping.Count > 0) return;

        int nextIndex = Basket.breads.Count;
        int maxCapacity = Mathf.Min(slots.Count, 8);
        if (nextIndex >= maxCapacity) return;
        if (stacking.Count == 0) return;

        var bread = stacking.Pop();
        if (!bread) return;

        var slotT = slots[nextIndex];
        if (!slotT) return;

        var t = bread.transform;

        // 드롭 시작 위치: prestackPoint(없으면 stackPoint, 그것도 없으면 현재 위치)
        Vector3 startPos = prestackPoint ? prestackPoint.position
                                         : (stackPoint ? stackPoint.position : t.position);
        t.position = startPos;

        // 이동 중에는 슬롯을 부모로 두되 월드 좌표 유지
        t.SetParent(slotT, true);

        EnsureKinematic(bread);

        // 드롭 테이블에 등록(이동 처리 대상)
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
                t.SetParent(slotT, false);          // 로컬 좌표로 스냅하기 위해 false
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;

                //Basket.breads.Push(go);
                Basket.AddBread(go);
            }
            dropping.Remove(go);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(pickUpTag)) canStack = true;
        if (other.CompareTag(dropOffTag)) canDrop = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(pickUpTag)) canStack = false;
        if (other.CompareTag(dropOffTag)) canDrop = false;
    }

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

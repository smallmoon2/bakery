using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIObjectController : MonoBehaviour
{
    [Header("Refs (Pickup / Drop)")]
    [SerializeField] private BreadBasket pickupBasket;   // ���⼭ pickUp
    [SerializeField] private BreadTable dropTable;       // ����� dropOff

    [SerializeField] private Transform stackPoint;
    [SerializeField] private Transform prestackPoint;

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

    private Stack<GameObject> stacking = new Stack<GameObject>();                    // �տ� �� �͵�
    private readonly Dictionary<GameObject, Transform> dropping = new Dictionary<GameObject, Transform>(); // ��� �̵� ��

    private bool canStack;
    private bool canDrop;
    private float nextMove = 0f;

    private void Update()
    {
        // �Ⱦ�: �ٽ��� �� + ���� + �� ����
        if (canStack && Time.time >= nextMove && stacking.Count < maxStack)
        {
            TryPickupOneFromBasket();
            nextMove = Time.time + delay;
        }

        // ���: ���̺� �� + ���� ��� ���� + ����
        if (canDrop && dropping.Count == 0 && Time.time >= nextMove)
        {
            TryDropOneToTable();
            nextMove = Time.time + delay;
        }

        ProcessDropping();           // ��� �� �̵� ó��
        MoveAllInHandToSlots();      // �տ� �� �͵� ����
    }

    // ---------- PICKUP ----------
    private void TryPickupOneFromBasket()
    {
        if (!pickupBasket || !stackPoint) return;

        // Basket���� �ϳ� �������� (Stack<GameObject> ����)
        if (pickupBasket.breads == null || pickupBasket.breads.Count == 0) return;

        GameObject picked = null;
        // �ֻ�ܿ��� Pop
        picked = pickupBasket.breads.Pop();
        if (!picked) return;

        int slotIndex = stacking.Count;

        // �θ� stackPoint�� (������ǥ ����)
        var t = picked.transform;
        t.SetParent(stackPoint, true);

        // �������� �� ������ �ڿ������� ����
        Vector3 basePos = prestackPoint ? prestackPoint.position : stackPoint.position;
        Vector3 prePos = basePos + Vector3.up * (stepHeight * slotIndex);
        t.position = prePos;

        EnsureKinematic(picked);
        stacking.Push(picked);
    }

    // �տ� �� �͵� ����(�׻� �ε巴�� ���̱�)
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

        var slots = dropTable.Rslots; // IReadOnlyList<Transform> ����
        if (slots == null || slots.Count == 0) return;

        if (dropping.Count > 0) return;     // �� ���� �ϳ���
        if (stacking.Count == 0) return;

        int nextIndex = dropTable.breads.Count;                  // ���̺� �̹� ���� ����
        int maxCapacity = Mathf.Min(slots.Count, 8);             // ���� ���Ѽ�
        if (nextIndex >= maxCapacity) return;

        var bread = stacking.Pop();
        if (!bread) return;

        var slotT = slots[nextIndex];
        if (!slotT) return;

        var t = bread.transform;

        // ��� ���� ��ġ(�������� �� ����)
        Vector3 startPos = prestackPoint ? prestackPoint.position
                                         : (stackPoint ? stackPoint.position : t.position);
        t.position = startPos;

        // �̵� ���� ������ �θ��(���� ��ǥ ����)
        t.SetParent(slotT, true);
        EnsureKinematic(bread);

        // �̵� ó�� ���̺� ���
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
                t.SetParent(slotT, false);          // ���÷� ����
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;

                // ���̺� ���ÿ� �ױ�
                dropTable.breads.Push(go);
            }
            dropping.Remove(go);
        }
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
}

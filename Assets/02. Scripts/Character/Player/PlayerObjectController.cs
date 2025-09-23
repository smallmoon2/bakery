using System.Collections;
using System.Collections.Generic;
// using Unity.VisualScripting;            // ���ʿ��ϸ� ����
// using UnityEditor.VersionControl;       // ������ ����: �����ϰų� #if UNITY_EDITOR �� ���μ���
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
    [SerializeField] private float stepHeight = 0.25f;     // �� ����
    [SerializeField] private float stackMoveSpeed = 8f;    // �ε巯�� �̵� �ӵ�
    [SerializeField] private float rotLerp = 8f;           // ȸ�� ���� �ӵ�
    [SerializeField] private float delay = 0.1f;           // �Ⱦ�/��� ���� ����

    [Header("Limits")]
    [SerializeField] private int maxStack = 8;

    private Stack<GameObject> stacking = new Stack<GameObject>();
    private readonly Dictionary<GameObject, Transform> dropping = new Dictionary<GameObject, Transform>();

    private bool canStack;
    private bool canDrop;

    private float nextMove = 0f;

    private void Update()
    {
        // �Ⱦ�: ���� �� + ���� ���� + �� ���� ���ʰ�
        if (canStack && Time.time >= nextMove && stacking.Count < maxStack)
        {
            TryPickupOneToStackPoint();
            nextMove = Time.time + delay;
        }

        // ���: �ٽ��� �� + ���� ��� ���� + ���� ����  (�Ⱦ� ���ο� ����)
        if (canDrop && dropping.Count == 0 && Time.time >= nextMove)
        {
            TryDropOneToBasket();
            nextMove = Time.time + delay;
        }

        // ��� ���� �� �̵�
        ProcessDropping();

        // �տ� �� �� ����(�׻� ����)
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

        // �� ���� �� ���� �ε���
        int slotIndex = stacking.Count;

        // �θ� stackPoint�� (���� ��ǥ ����)
        var t = picked.transform;
        t.SetParent(stackPoint, true);

        // prestack ����(������ stackPoint�� ����)
        Vector3 basePos = prestackPoint ? prestackPoint.position : stackPoint.position;

        // �������� ��ġ�� ��� �̵� (�� ���� ����)
        Vector3 prePos = basePos + Vector3.up * (stepHeight * slotIndex);
        t.position = prePos;

        EnsureKinematic(picked);

        // �� ���ÿ� ����
        stacking.Push(picked);
    }

    private void MoveAllInHandToSlots()
    {
        if (!stackPoint) return;

        var arr = stacking.ToArray(); // top-first
        int n = arr.Length;
        int slot = 0;

        // bottom -> top ������ ��ǥ ���
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

        // �� ���� �ϳ���
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

        // ��� ���� ��ġ: prestackPoint(������ stackPoint, �װ͵� ������ ���� ��ġ)
        Vector3 startPos = prestackPoint ? prestackPoint.position
                                         : (stackPoint ? stackPoint.position : t.position);
        t.position = startPos;

        // �̵� �߿��� ������ �θ�� �ε� ���� ��ǥ ����
        t.SetParent(slotT, true);

        EnsureKinematic(bread);

        // ��� ���̺� ���(�̵� ó�� ���)
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
                t.SetParent(slotT, false);          // ���� ��ǥ�� �����ϱ� ���� false
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

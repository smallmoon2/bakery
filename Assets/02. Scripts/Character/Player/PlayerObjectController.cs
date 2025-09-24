using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerObjectController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BreadSpawner spawner;
    [SerializeField] private BreadBasket Basket;

    [SerializeField] private Transform stackPoint;
    [SerializeField] private Transform prestackPoint;

    [Header("Money")]
    [SerializeField] private Transform MoneyPoint;
    [SerializeField] private Transform preMoneyPoint;

    [Header("Tags")]
    [SerializeField] private string pickUpTag = "Oven";
    [SerializeField] private string dropOffTag = "Basket";
    [SerializeField] private string moneyTag = "Money";

    [Header("Motion")]
    [SerializeField] private float stepHeight = 0.25f;   // �� ����
    [SerializeField] private float stackMoveSpeed = 8f;  // �̵� �ӵ�
    [SerializeField] private float rotLerp = 8f;         // ȸ�� ����
    [SerializeField] private float delay = 0.1f;         // �� �Ⱦ�/��� �� ����

    [Header("Limits")]
    [SerializeField] private int maxStack = 8;

    private Stack<GameObject> stacking = new Stack<GameObject>();

    private bool canStack;
    private bool canDrop;
    private bool canMoney;                 // Money �� �ȿ� ���� ��
    private float nextMove = 0f;

    // ���� ���� ���� (�� ����)
    private bool isPicking = false;
    private bool isDropping = false;

    // �Ӵ� ����Ʈ ���� �ߺ� ����
    private bool isMoneyBurstRunning = false;

    private void Update()
    {
        // �� �Ⱦ�
        if (canStack && Time.time >= nextMove && stacking.Count < maxStack && !isPicking)
        {
            StartPickupOne();
            nextMove = Time.time + delay;
        }

        // �� ���
        if (canDrop && Time.time >= nextMove && stacking.Count > 0 && !isDropping)
        {
            StartDropOne();
            nextMove = Time.time + delay;
        }

        // �� �Ⱦ�: 0.05�� �������� ���� ���� (canMoney�� ���� ����)
        if (canMoney && !isMoneyBurstRunning)
        {
            StartCoroutine(MoneyBurstRoutine());
        }
    }

    // ===================== BREAD: PICKUP =====================
    private void StartPickupOne()
    {
        if (!spawner || !stackPoint) return;
        if (spawner.breads == null || spawner.breads.Count == 0) return;

        // null ���� �� ù ��ȿ �� �ϳ� ��������
        spawner.breads.RemoveAll(b => b == null);
        if (spawner.breads.Count == 0) return;

        GameObject picked = null;
        for (int i = 0; i < spawner.breads.Count; i++)
        {
            var c = spawner.breads[i];
            if (c)
            {
                picked = c;
                spawner.breads.RemoveAt(i);
                break;
            }
        }
        if (!picked) return;

        int slotIndex = stacking.Count;
        StartCoroutine(PickupOneRoutine(picked, slotIndex));
    }

    private IEnumerator PickupOneRoutine(GameObject picked, int slotIndex)
    {
        isPicking = true;
        EnsureKinematic(picked);
        var t = picked.transform;
        const float EPS = 0.0001f;

        // ���� ��ġ �� prestack (�ǽð� ��ǥ)
        while (true)
        {
            Vector3 preBase = prestackPoint ? prestackPoint.position : (stackPoint ? stackPoint.position : t.position);
            Vector3 prePos = preBase + Vector3.up * (stepHeight * slotIndex);
            Quaternion preRot = stackPoint ? stackPoint.rotation : t.rotation;

            t.position = Vector3.MoveTowards(t.position, prePos, stackMoveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, preRot, rotLerp * Time.deltaTime);

            if ((t.position - prePos).sqrMagnitude <= EPS) break;
            yield return null;
        }

        // prestack �� �� ���� (�ǽð� ��ǥ)
        while (true)
        {
            Vector3 handPos = (stackPoint ? stackPoint.position : t.position) + Vector3.up * (stepHeight * slotIndex);
            Quaternion handRot = stackPoint ? stackPoint.rotation : t.rotation;

            t.position = Vector3.MoveTowards(t.position, handPos, stackMoveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, handRot, rotLerp * Time.deltaTime);

            if ((t.position - handPos).sqrMagnitude <= EPS) break;
            yield return null;
        }

        if (stackPoint)
        {
            t.SetParent(stackPoint, true);
            t.position = stackPoint.position + Vector3.up * (stepHeight * slotIndex);
            t.rotation = stackPoint.rotation;
        }

        stacking.Push(picked);
        isPicking = false;
    }

    // ===================== BREAD: DROP =====================
    private void StartDropOne()
    {
        if (!Basket || stacking.Count == 0) return;

        var slots = Basket.Rslots;
        if (slots == null || slots.Count == 0) return;

        int nextIndex = Basket.breads.Count;
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

        Vector3 basePos = prestackPoint ? prestackPoint.position : t.position;
        Vector3 prePos = basePos + Vector3.up * (stepHeight * slotIndex);

        while ((t.position - prePos).sqrMagnitude > 0.0001f)
        {
            t.position = Vector3.MoveTowards(t.position, prePos, stackMoveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, slotT.rotation, rotLerp * Time.deltaTime);
            yield return null;
        }

        Vector3 targetPos = slotT.position;
        Quaternion targetRot = slotT.rotation;

        t.SetParent(slotT, true);

        while ((t.position - targetPos).sqrMagnitude > 0.0001f)
        {
            t.position = Vector3.MoveTowards(t.position, targetPos, stackMoveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, targetRot, rotLerp * Time.deltaTime);
            yield return null;
        }

        t.SetParent(slotT, false);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        Basket.AddBread(bread);
        isDropping = false;
    }

    // ===================== MONEY: BURST PICKUP =====================
    // 0.05�� �������� ����Ʈ ������(pop) �ϳ��� ���� �Ⱦ� ����
    private IEnumerator MoneyBurstRoutine()
    {
        isMoneyBurstRunning = true;
        const float interval = 0.05f;

        if (GameManager.Instance == null || GameManager.Instance.ai == null)
        {
            isMoneyBurstRunning = false;
            yield break;
        }

        var list = GameManager.Instance.ai.TableMoney;

        while (canMoney) // Money �� �ȿ� �ִ� ���ȸ�
        {
            if (list == null || list.Count == 0) break;

            // ����Ʈ ���������� pop (null �ǳʶٱ�)
            GameObject target = null;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var go = list[i];
                list.RemoveAt(i);          // pop
                GameManager.Instance.myMoney++; // ���� ���� ���� ����(�ʿ� ������ ����)
                if (go != null)
                {
                    target = go;
                    break;
                }
                // null �̸� ��� ��������
            }

            if (target != null)
            {
                // ���ķ� �̵� ����(��ٸ��� ����)
                StartCoroutine(PickupMoneyRoutine(target));
            }
            else
            {
                // ��� null�̾��� �� �̻� ���� �� ���ٸ� ����
                if (list == null || list.Count == 0) break;
            }

            // ���� �Ӵ� ���۱��� ����
            yield return new WaitForSeconds(interval);
        }

        isMoneyBurstRunning = false;
    }

    // Money�� preMoneyPoint �� MoneyPoint "���� �ڸ�"�� ��ġ�� �̵�
    private IEnumerator PickupMoneyRoutine(GameObject money)
    {
        EnsureKinematic(money);
        var t = money.transform;
        const float EPS = 0.0001f;

        if (!MoneyPoint)
        {
            Debug.LogWarning("[PlayerObjectController] MoneyPoint ���Ҵ�");
            yield break;
        }

        // 1) ���� ��ġ �� preMoneyPoint (������ MoneyPoint)
        while (true)
        {
            Vector3 prePos = preMoneyPoint ? preMoneyPoint.position : MoneyPoint.position;
            Quaternion preRot = MoneyPoint.rotation;

            t.position = Vector3.MoveTowards(t.position, prePos, stackMoveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, preRot, rotLerp * Time.deltaTime);

            if ((t.position - prePos).sqrMagnitude <= EPS) break;
            yield return null;
        }

        // 2) preMoneyPoint �� MoneyPoint ��Ȯ�� ���� �ڸ�(��ħ)
        while (true)
        {
            Vector3 targetPos = MoneyPoint.position;
            Quaternion targetRot = MoneyPoint.rotation;

            t.position = Vector3.MoveTowards(t.position, targetPos, stackMoveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, targetRot, rotLerp * Time.deltaTime);

            if ((t.position - targetPos).sqrMagnitude <= EPS) break;
            yield return null;
        }

        // 3) �θ� ���̱�: ���� (0,0,0)���� ���� ���� �� ���� �ڸ��� ��ħ
        t.SetParent(MoneyPoint, false);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        Destroy(t.gameObject);
    }

    // ===================== TRIGGERS =====================
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(pickUpTag)) canStack = true;
        if (other.CompareTag(dropOffTag)) canDrop = true;
        if (other.CompareTag(moneyTag)) canMoney = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(pickUpTag)) canStack = false;
        if (other.CompareTag(dropOffTag)) canDrop = false;
        if (other.CompareTag(moneyTag)) canMoney = false;
    }

    // ===================== UTIL =====================
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

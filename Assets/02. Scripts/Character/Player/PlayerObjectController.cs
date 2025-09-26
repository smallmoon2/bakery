using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class PlayerObjectController : MonoBehaviour
{
    // ===================== MONEY2 =====================
    [Header("Money2")]
    [SerializeField] private Transform MoneyPoint2;
    [SerializeField] private Transform preMoneyPoint2;
    [SerializeField] private string money2Tag = "Money2";   // �� Money2 ���� �±�

    // ===================== MONEY =====================
    [Header("Money")]
    [SerializeField] private Transform MoneyPoint;
    [SerializeField] private Transform preMoneyPoint;
    [SerializeField] private string moneyTag = "Money";     // Money ���� �±�

    // ===================== MONEY USE =====================
    [Header("Money Use")]
    [SerializeField] private Transform MoneyUsePoint;
    [SerializeField] private Transform preMoneyUsePoint;
    [SerializeField] private string moneyUseTag = "MoneyUse";

    // ===================== OVEN (Bread Pick) =====================
    [Header("Oven")]
    [SerializeField] private BreadSpawner spawner;     // ���쿡�� ���� �� �ҽ�
    [SerializeField] public Transform stackPoint;      // �տ� ���� ������
    [SerializeField] private string pickUpTag = "Oven";

    // ===================== BASKET (Bread Drop) =====================
    [Header("Basket")]
    [SerializeField] private BreadBasket Basket;
    [SerializeField] private string dropOffTag = "Basket";

    // ===================== COMMON MOTION/LIMITS =====================
    [Header("Motion")]
    [SerializeField] private float stepHeight = 0.25f;   // �� ����
    [SerializeField] private float stackMoveSpeed = 8f;  // �̵� �ӵ�
    [SerializeField] private float rotLerp = 8f;         // ȸ�� ����
    [SerializeField] private float delay = 0.1f;         // �� �Ⱦ�/��� �� ����

    [Header("Limits")]
    [SerializeField] private int maxStack = 8;

    [Header("Drop Arc (No Prestack)")]
    [SerializeField] private float ArcHeight = 1f;   // ������ ����
    [SerializeField] private float ArcSpeed = 10f;  // �̵� �ӵ�(�Ÿ�/��)

    // ===================== STATE =====================
    private Stack<GameObject> stacking = new Stack<GameObject>();

    private OpenLock openLock;

    private bool canStack;
    private bool canDrop;

    private bool canMoney;                  // Money �� ��
    private bool canMoney2;                 // Money2 �� ��
    private bool canMoneyUse;               // MoneyUse �� ��

    private float nextMove = 0f;

    // ���� ���� ���� (�� ����)
    private bool isPicking = false;
    private bool isDropping = false;

    // �Ӵ� ����Ʈ ���� �ߺ� ����
    private bool isMoneyBurstRunning = false;
    private bool isMoney2BurstRunning = false;
    private bool isMoneyUseBurstRunning = false;

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

        // ��(Money) �Ⱦ�: 0.05�� �������� ���� ���� (canMoney�� ���� ����)
        if (canMoney && !isMoneyBurstRunning)
        {
            StartCoroutine(MoneyBurstRoutine());
        }

        // ��(Money2) �Ⱦ�
        if (canMoney2 && !isMoney2BurstRunning)
        {
            StartCoroutine(Money2BurstRoutine());
        }

        // �� ���
        if (canMoneyUse && !isMoneyUseBurstRunning)
        {
            StartCoroutine(MoneyUseBurstRoutine());
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

        if (!stackPoint)
        {
            isPicking = false;
            yield break;
        }

        // ���� ������
        Vector3 startPos = t.position;
        Quaternion startRot = t.rotation;

        // �ð� ���(�ʱ� �Ÿ� ����)  Ÿ���� �������� duration�� ����
        float dist = Vector3.Distance(startPos, stackPoint.position + Vector3.up * (stepHeight * slotIndex));
        float duration = Mathf.Max(0.05f, dist / Mathf.Max(0.01f, ArcSpeed));

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (!stackPoint) break; // �߰��� Ÿ�� ������� Ż��

            float u = elapsed / duration;   // 0..1
            Vector3 endPos = stackPoint.position + Vector3.up * (stepHeight * slotIndex);
            Quaternion endRot = stackPoint.rotation;

            // ���� ���� + ������ ������(�ִ� ArcHeight, �߰����� ��ũ)
            Vector3 line = Vector3.Lerp(startPos, endPos, u);
            float arc = 4f * u * (1f - u) * ArcHeight;   // 0��peak��0
            Vector3 pos = line + Vector3.up * arc;

            t.position = pos;
            t.rotation = Quaternion.Slerp(startRot, endRot, u);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // ������ ���� & �θ� ����(���尪 �����ؼ� ������ ���� ����)
        if (stackPoint)
        {
            Vector3 endPos = stackPoint.position + Vector3.up * (stepHeight * slotIndex);
            Quaternion endRot = stackPoint.rotation;

            t.position = endPos;
            t.rotation = endRot;
            t.SetParent(stackPoint, true); // worldPositionStays = true
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

        if (!slotT || !bread)
        {
            isDropping = false;
            yield break;
        }

        // ����/���� ������
        Vector3 startPos = t.position;
        Quaternion startRot = t.rotation;
        Vector3 endPos = slotT.position;
        Quaternion endRot = slotT.rotation;

        // ��Ʈ�� ����Ʈ: �߰� �������� ���� �÷� ������ ����
        Vector3 mid = (startPos + endPos) * 0.5f;
        Vector3 control = mid + Vector3.up * ArcHeight;

        // �Ÿ� ��� �ð� ���
        float dist = Vector3.Distance(startPos, endPos);
        float duration = Mathf.Max(0.05f, dist / Mathf.Max(0.01f, ArcSpeed));

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float u = elapsed / duration;       // 0..1
            float uu = 1f - u;

            // Quadratic Bezier: B(u) = (1-u)^2*A + 2(1-u)u*C + u^2*B
            Vector3 pos =
                uu * uu * startPos +
                2f * uu * u * control +
                u * u * endPos;

            t.position = pos;
            t.rotation = Quaternion.Slerp(startRot, endRot, u); // ȸ���� �ڿ�������

            elapsed += Time.deltaTime;
            yield return null;
        }

        // ���� ���� & �θ� ����
        t.position = endPos;
        t.rotation = endRot;
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
                list.RemoveAt(i);                 // pop
                GameManager.Instance.myMoney++;   // �ʿ� ������ ����
                if (go != null)
                {
                    target = go;
                    break;
                }
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

        // �ʿ� �� ����/���� ó��
        money.SetActive(false);
    }

    // ===================== MONEY2: BURST PICKUP =====================
    private IEnumerator Money2BurstRoutine()
    {
        isMoney2BurstRunning = true;
        const float interval = 0.05f;

        if (GameManager.Instance == null || GameManager.Instance.ai == null)
        {
            isMoney2BurstRunning = false;
            yield break;
        }

        var list = GameManager.Instance.ai.TableMoney2; // �� �� ��° ����Ʈ

        while (canMoney2) // Money2 �� �ȿ� �ִ� ���ȸ�
        {
            if (list == null || list.Count == 0) break;

            GameObject target = null;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var go = list[i];
                list.RemoveAt(i);                 // pop
                GameManager.Instance.myMoney++;   // �ʿ� ������ ���� ����
                if (go != null)
                {
                    target = go;
                    break;
                }
            }

            if (target != null)
            {
                StartCoroutine(PickupMoney2Routine(target));
            }
            else
            {
                if (list == null || list.Count == 0) break;
            }

            yield return new WaitForSeconds(interval);
        }

        isMoney2BurstRunning = false;
    }

    private IEnumerator PickupMoney2Routine(GameObject money)
    {
        EnsureKinematic(money);
        var t = money.transform;
        const float EPS = 0.0001f;

        if (!MoneyPoint2)
        {
            Debug.LogWarning("[PlayerObjectController] MoneyPoint2 ���Ҵ�");
            yield break;
        }

        // 1) ���� ��ġ �� preMoneyPoint2 (������ MoneyPoint2)
        while (true)
        {
            Vector3 prePos = preMoneyPoint2 ? preMoneyPoint2.position : MoneyPoint2.position;
            Quaternion preRot = MoneyPoint2.rotation;

            t.position = Vector3.MoveTowards(t.position, prePos, stackMoveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, preRot, rotLerp * Time.deltaTime);

            if ((t.position - prePos).sqrMagnitude <= EPS) break;
            yield return null;
        }

        // 2) preMoneyPoint2 �� MoneyPoint2 ��Ȯ�� ���� �ڸ�(��ħ)
        while (true)
        {
            Vector3 targetPos = MoneyPoint2.position;
            Quaternion targetRot = MoneyPoint2.rotation;

            t.position = Vector3.MoveTowards(t.position, targetPos, stackMoveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, targetRot, rotLerp * Time.deltaTime);

            if ((t.position - targetPos).sqrMagnitude <= EPS) break;
            yield return null;
        }

        // 3) �θ�/����(��ġ��)
        t.SetParent(MoneyPoint2, false);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        money.SetActive(false);
    }

    // ===================== MONEY USE: BURST =====================
    private IEnumerator MoneyUseBurstRoutine()
    {
        isMoneyUseBurstRunning = true;
        const float interval = 0.05f;

        // ���� üũ
        if (!MoneyPoint)
        {
            Debug.LogWarning("[PlayerObjectController] MoneyPoint ���Ҵ�");
            isMoneyUseBurstRunning = false;
            yield break;
        }

        while (canMoneyUse)
        {
            if (int.Parse(openLock.lockCounttext.text) > 0)
            {
                // MoneyPoint �ؿ� �ڽ�(��) ������ ����
                if (MoneyPoint.childCount == 0) break;

                // ��(���� ������) �ڽ��� ����
                Transform child = MoneyPoint.GetChild(MoneyPoint.childCount - 1);
                GameObject money = child.gameObject;

                if (money != null)
                {
                    // �θ� �и�(����� ������ �̵�)
                    child.SetParent(null, true);

                    // ��Ȱ�� ���¿��ٸ� Ȱ��ȭ
                    if (!money.activeSelf) money.SetActive(true);

                    openLock.decreaseLockCount();
                    GameManager.Instance.myMoney--;
                    StartCoroutine(UseMoneyMoveRoutine(money));
                }
            }

            yield return new WaitForSeconds(interval);
        }

        isMoneyUseBurstRunning = false;
    }

    private IEnumerator UseMoneyMoveRoutine(GameObject money)
    {
        EnsureKinematic(money);
        var t = money.transform;
        const float EPS = 0.0001f;

        if (!MoneyUsePoint)
        {
            Debug.LogWarning("[PlayerObjectController] MoneyUsePoint ���Ҵ�");
            yield break;
        }

        // 1) ���� �� preMoneyUsePoint(������) / ������ MoneyUsePoint
        while (true)
        {
            Vector3 prePos = preMoneyUsePoint ? preMoneyUsePoint.position : MoneyUsePoint.position;
            Quaternion preRot = MoneyUsePoint.rotation;

            t.position = Vector3.MoveTowards(t.position, prePos, stackMoveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, preRot, rotLerp * Time.deltaTime);

            if ((t.position - prePos).sqrMagnitude <= EPS) break;
            yield return null;
        }

        // 2) pre �� MoneyUsePoint(���� ��ħ)
        while (true)
        {
            Vector3 targetPos = MoneyUsePoint.position;
            Quaternion targetRot = MoneyUsePoint.rotation;

            t.position = Vector3.MoveTowards(t.position, targetPos, stackMoveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, targetRot, rotLerp * Time.deltaTime);

            if ((t.position - targetPos).sqrMagnitude <= EPS) break;
            yield return null;
        }

        // 3) �θ�/����(��ġ��)
        t.SetParent(MoneyUsePoint, false);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        money.SetActive(false);
    }

    // ===================== TRIGGERS =====================
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(pickUpTag)) canStack = true;
        if (other.CompareTag(dropOffTag)) canDrop = true;

        if (other.CompareTag(moneyTag)) canMoney = true;  // "Money"
        if (other.CompareTag(money2Tag)) canMoney2 = true;  // "Money2"

        if (other.CompareTag(moneyUseTag))
        {
            canMoneyUse = true;
            var found = other.GetComponent<OpenLock>()
                   ?? other.GetComponentInParent<OpenLock>()
                   ?? other.GetComponentInChildren<OpenLock>();

            if (found != null)
            {
                openLock = found;
            }
        }

        if (other.CompareTag("trashClear"))
        {
            var ai = GameManager.Instance != null ? GameManager.Instance.ai : null;
            if (ai != null && ai.Trash != null)
            {
                Destroy(ai.Trash);
                ai.Trash = null; // ������ ����
                var chair = ai.Chair;
                chair.transform.eulerAngles = new Vector3(
                    chair.transform.eulerAngles.x,
                    chair.transform.eulerAngles.y - 45f,
                    chair.transform.eulerAngles.z
                );
            }
            else
            {
                Debug.LogWarning("[AIObjectController] ai.Trash �� ���ų� �̹� ���ŵǾ����ϴ�.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(pickUpTag))
        {
            GameManager.Instance.ui.SetGuide(UIManager.Guidestate.Basket);
            canStack = false;
        }
        if (other.CompareTag(dropOffTag)) canDrop = false;

        if (other.CompareTag(moneyTag)) canMoney = false; // "Money"
        if (other.CompareTag(money2Tag)) canMoney2 = false; // "Money2"

        if (other.CompareTag(moneyUseTag)) canMoneyUse = false; // "MoneyUse"
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

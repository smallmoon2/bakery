using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreadStack : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BreadSpawner spawner;
    [SerializeField] private Transform stackPoint;
    [SerializeField] private Transform PrestackPoint; // �� �߰�: �������� ��ġ ����

    [Header("Stack Settings")]
    [SerializeField] private float stepHeight = 0.25f; // �� �� ����
    [SerializeField] private float moveSpeed = 10f;     // ���� �̵� �ӵ�
    [SerializeField] private float rotLerp = 8f;       // ȸ�� ���� �ӵ�
    [SerializeField] private bool makeKinematicOnStack = true;

    [Header("Pickup Gate")]
    [SerializeField] private string pickupTag = "Pickup";
    private bool canStack;

    [Header("Stack Flow")]
    [SerializeField] private float stackDelay = 0.1f;         // 1�� �������� �ϳ���
    private Stack<GameObject> stacking = new Stack<GameObject>();
    private Coroutine stackRoutine;

    private void Awake()
    {
        if (!spawner) spawner = FindObjectOfType<BreadSpawner>();
    }

    private void OnEnable()
    {
        stackRoutine = StartCoroutine(StackLoop());
    }

    private void OnDisable()
    {
        if (stackRoutine != null) StopCoroutine(stackRoutine);
        stackRoutine = null;
    }

    // 1�ʸ���: Pickup �ȿ� ������ spawner.breads���� �ϳ��� ����
    // stackPoint�� �ڽ����� ���̰�, PrestackPoint ���� ��ġ�� �����̵� �� ���ÿ� Push
    private IEnumerator StackLoop()
    {
        var wait = new WaitForSeconds(stackDelay);

        while (true)
        {
            if (spawner && canStack)
            {
                // ���� ���� ����
                spawner.breads.RemoveAll(b => b == null);

                // ù ��ȿ �� �ϳ� ���� & ����Ʈ���� ����
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

                if (picked != null && !stacking.Contains(picked))
                {
                    var t = picked.transform;

                    // �θ� stackPoint�� (������ǥ ����)
                    t.SetParent(stackPoint, true);

                    // �� ���� �ö� "���� ����" (Ǫ�� ���̹Ƿ� ���� ������ ����)
                    int slot = stacking.Count;

                    // �� PrestackPoint ���� �������� ���� ��ġ ���
                    //    (PrestackPoint�� ������ stackPoint �������� ����)
                    Vector3 basePos = (PrestackPoint ? PrestackPoint.position : stackPoint.position);
                    Vector3 prestackWorldPos = basePos + Vector3.up * (stepHeight * slot);

                    // ��� �����̵�
                    t.position = prestackWorldPos;

                    // ���� ���� ����(�ɼ�)
                    EnsureKinematic(picked);

                    // ���� ��� (�ֽ� = top)
                    stacking.Push(picked);
                }
            }

            yield return wait;
        }
    }

    private void Update()
    {
        if (!stackPoint) return;

        // bottom(������) �� top(�ֱ�) ������ ����
        var arr = stacking.ToArray(); // top-first �迭 ��ȯ
        int n = arr.Length;
        int slot = 0;

        for (int i = n - 1; i >= 0; i--)
        {
            var go = arr[i];
            if (!go) continue;

            Vector3 targetPos = stackPoint.position + Vector3.up * (stepHeight * slot);
            MoveStack(go, targetPos);
            slot++;
        }
    }

    // ���� Ʈ���� ����(�÷��̾� �ʿ� Trigger Collider �ʿ�) ����
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(pickupTag))
            canStack = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(pickupTag))
            canStack = false;
    }

    // --- Helpers ---
    private void EnsureKinematic(GameObject go)
    {
        if (go.TryGetComponent<Rigidbody>(out var rb) && makeKinematicOnStack)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    private void MoveStack(GameObject go, Vector3 targetPos)
    {
        var t = go.transform;
        t.position = Vector3.MoveTowards(t.position, targetPos, moveSpeed * Time.deltaTime);
        t.rotation = Quaternion.Slerp(t.rotation, stackPoint.rotation, rotLerp * Time.deltaTime);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreadStack : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BreadSpawner spawner;
    [SerializeField] private Transform stackPoint;
    [SerializeField] private Transform PrestackPoint; // ★ 추가: 프리스택 위치 기준

    [Header("Stack Settings")]
    [SerializeField] private float stepHeight = 0.25f; // 한 층 높이
    [SerializeField] private float moveSpeed = 10f;     // 정렬 이동 속도
    [SerializeField] private float rotLerp = 8f;       // 회전 보간 속도
    [SerializeField] private bool makeKinematicOnStack = true;

    [Header("Pickup Gate")]
    [SerializeField] private string pickupTag = "Pickup";
    private bool canStack;

    [Header("Stack Flow")]
    [SerializeField] private float stackDelay = 0.1f;         // 1초 간격으로 하나씩
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

    // 1초마다: Pickup 안에 있으면 spawner.breads에서 하나를 꺼내
    // stackPoint의 자식으로 붙이고, PrestackPoint 기준 위치로 순간이동 → 스택에 Push
    private IEnumerator StackLoop()
    {
        var wait = new WaitForSeconds(stackDelay);

        while (true)
        {
            if (spawner && canStack)
            {
                // 유령 참조 제거
                spawner.breads.RemoveAll(b => b == null);

                // 첫 유효 빵 하나 선택 & 리스트에서 제거
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

                    // 부모를 stackPoint로 (월드좌표 유지)
                    t.SetParent(stackPoint, true);

                    // 이 빵이 올라갈 "최종 슬롯" (푸시 전이므로 현재 개수가 슬롯)
                    int slot = stacking.Count;

                    // ★ PrestackPoint 기준 프리스택 월드 위치 계산
                    //    (PrestackPoint가 없으면 stackPoint 기준으로 폴백)
                    Vector3 basePos = (PrestackPoint ? PrestackPoint.position : stackPoint.position);
                    Vector3 prestackWorldPos = basePos + Vector3.up * (stepHeight * slot);

                    // 즉시 순간이동
                    t.position = prestackWorldPos;

                    // 물리 영향 제거(옵션)
                    EnsureKinematic(picked);

                    // 스택 등록 (최신 = top)
                    stacking.Push(picked);
                }
            }

            yield return wait;
        }
    }

    private void Update()
    {
        if (!stackPoint) return;

        // bottom(오래된) → top(최근) 순으로 정렬
        var arr = stacking.ToArray(); // top-first 배열 반환
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

    // ── 트리거 감지(플레이어 쪽에 Trigger Collider 필요) ──
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

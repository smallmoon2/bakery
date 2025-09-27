using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreadSpawner : MonoBehaviour
{
    [Header("Prefab & Spawn")]
    [SerializeField] private GameObject breadPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float launchDelay = 0.5f;    // 발사 전 대기
    [SerializeField] private float launchSpeed = 5f;
    [SerializeField] private int maxBreadCount = 8;

    public List<GameObject> breads = new List<GameObject>();

    Coroutine spawnRoutine;
    bool isSpawning;

    // 발사 대기 중(아직 리스트에 안 넣은) 개수
    int pendingCount = 0;

    void Awake()
    {
        if (!spawnPoint) spawnPoint = transform;
    }

    void OnDisable()
    {
        StopAllCoroutines();
        spawnRoutine = null;
        isSpawning = false;
        pendingCount = 0;
    }

    void Update()
    {
        breads.RemoveAll(b => b == null);

        // ▶ 리스트 + 대기 중 합계를 기준으로 상한 체크
        if (!isSpawning && (breads.Count + pendingCount) < maxBreadCount)
            spawnRoutine = StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        isSpawning = true;

        while ((breads.Count + pendingCount) < maxBreadCount)
        {
            SpawnOne();
            yield return new WaitForSeconds(spawnInterval);
        }

        isSpawning = false;
        spawnRoutine = null;
    }

    void SpawnOne()
    {
        var go = Instantiate(breadPrefab, spawnPoint.position, spawnPoint.rotation);

        // 발사 방향 캡처
        Vector3 dir = spawnPoint.forward.normalized;

        if (go.TryGetComponent<Rigidbody>(out var rb))
        {
            // 스폰 직후엔 중력 OFF, 정지
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // 아직 리스트에 넣지 않음 → 대기 카운트 증가
            pendingCount++;
            StartCoroutine(LaunchAfterDelay(go, rb, dir));
        }
        else
        {
            // Rigidbody가 없으면 즉시 리스트에 추가(선택)
            breads.Add(go);
        }
    }

    IEnumerator LaunchAfterDelay(GameObject obj, Rigidbody rb, Vector3 dir)
    {
        yield return new WaitForSeconds(launchDelay);

        if (obj != null && rb != null)
        {
            // 발사 순간: 중력 ON → 힘 가하기
            rb.useGravity = true;
            rb.AddForce(dir * launchSpeed, ForceMode.VelocityChange);

            // ▶ 이제서야 리스트에 등록
            breads.Add(obj);
        }

        // 대기 카운트 감소(성공/실패 모두에서)
        pendingCount = Mathf.Max(0, pendingCount - 1);
    }
}

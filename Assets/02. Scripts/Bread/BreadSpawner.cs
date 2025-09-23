using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreadSpawner : MonoBehaviour
{
    [Header("Prefab & Spawn")]
    [SerializeField] private GameObject breadPrefab;
    [SerializeField] private Transform spawnPoint;      // 비워두면 this.transform 사용
    [SerializeField] private float spawnInterval = 1f;  // 1초마다
    [SerializeField] private float launchSpeed = 5f;    // 발사 속도
    [SerializeField] private int maxBreadCount = 8;     // 8개까지

    public List<GameObject> breads = new List<GameObject>();

    private Coroutine spawnRoutine;
    private bool isSpawning;

    private void Awake()
    {
        if (spawnPoint == null) spawnPoint = transform;
    }

    private void OnDisable()
    {
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = null;
        isSpawning = false;
    }

    private void Update()
    {
        // (선택) 유령 참조 청소
        breads.RemoveAll(b => b == null);

        // 현재 개수가 한도 미만이고, 코루틴이 돌고 있지 않으면 시작
        if (!isSpawning && breads.Count < maxBreadCount)
        {
            spawnRoutine = StartCoroutine(SpawnLoop());
        }
    }

    private IEnumerator SpawnLoop()
    {
        isSpawning = true;

        while (true)
        {
            // 매 주기마다 최신 상태 확인
            breads.RemoveAll(b => b == null);

            if (breads.Count >= maxBreadCount)
                break;

            SpawnOne();

            yield return new WaitForSeconds(spawnInterval);
        }

        // 코루틴 종료 시 상태 정리
        isSpawning = false;
        spawnRoutine = null;
    }

    private void SpawnOne()
    {
        var go = Instantiate(breadPrefab, spawnPoint.position, spawnPoint.rotation);
        breads.Add(go);

        if (go.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.AddForce(spawnPoint.forward.normalized * launchSpeed, ForceMode.VelocityChange);
        }
    }
}

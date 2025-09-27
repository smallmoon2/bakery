using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreadSpawner : MonoBehaviour
{
    [Header("Prefab & Spawn")]
    [SerializeField] private GameObject breadPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float launchDelay = 0.5f;    // �߻� �� ���
    [SerializeField] private float launchSpeed = 5f;
    [SerializeField] private int maxBreadCount = 8;

    public List<GameObject> breads = new List<GameObject>();

    Coroutine spawnRoutine;
    bool isSpawning;

    // �߻� ��� ��(���� ����Ʈ�� �� ����) ����
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

        // �� ����Ʈ + ��� �� �հ踦 �������� ���� üũ
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

        // �߻� ���� ĸó
        Vector3 dir = spawnPoint.forward.normalized;

        if (go.TryGetComponent<Rigidbody>(out var rb))
        {
            // ���� ���Ŀ� �߷� OFF, ����
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // ���� ����Ʈ�� ���� ���� �� ��� ī��Ʈ ����
            pendingCount++;
            StartCoroutine(LaunchAfterDelay(go, rb, dir));
        }
        else
        {
            // Rigidbody�� ������ ��� ����Ʈ�� �߰�(����)
            breads.Add(go);
        }
    }

    IEnumerator LaunchAfterDelay(GameObject obj, Rigidbody rb, Vector3 dir)
    {
        yield return new WaitForSeconds(launchDelay);

        if (obj != null && rb != null)
        {
            // �߻� ����: �߷� ON �� �� ���ϱ�
            rb.useGravity = true;
            rb.AddForce(dir * launchSpeed, ForceMode.VelocityChange);

            // �� �������� ����Ʈ�� ���
            breads.Add(obj);
        }

        // ��� ī��Ʈ ����(����/���� ��ο���)
        pendingCount = Mathf.Max(0, pendingCount - 1);
    }
}

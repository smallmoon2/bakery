using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreadSpawner : MonoBehaviour
{
    [Header("Prefab & Spawn")]
    [SerializeField] private GameObject breadPrefab;
    [SerializeField] private Transform spawnPoint;      // ����θ� this.transform ���
    [SerializeField] private float spawnInterval = 1f;  // 1�ʸ���
    [SerializeField] private float launchSpeed = 5f;    // �߻� �ӵ�
    [SerializeField] private int maxBreadCount = 8;     // 8������

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
        // (����) ���� ���� û��
        breads.RemoveAll(b => b == null);

        // ���� ������ �ѵ� �̸��̰�, �ڷ�ƾ�� ���� ���� ������ ����
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
            // �� �ֱ⸶�� �ֽ� ���� Ȯ��
            breads.RemoveAll(b => b == null);

            if (breads.Count >= maxBreadCount)
                break;

            SpawnOne();

            yield return new WaitForSeconds(spawnInterval);
        }

        // �ڷ�ƾ ���� �� ���� ����
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

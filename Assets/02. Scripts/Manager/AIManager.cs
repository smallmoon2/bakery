using System.Collections.Generic;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    public bool isCalculated;

    public List<GameObject> TableMoney = new List<GameObject>();
    [SerializeField] private Transform TableMoneyPoint;


    private readonly List<AIController> members = new List<AIController>();

    public List<bool> Pick = new List<bool>();
    public List<bool> hall = new List<bool>();


    public int PickStateNum = 0;
    public int hallStateNum = 0;
    public int AiNum = 0;

    private int MaxAiMum = 7;
    private int MaxPickStateNum = 3;
    private int MaxhallStateNum = 0;

    private float timer;

    [Header("Prefabs")]
    [SerializeField] private GameObject moneyPrefab;
    [SerializeField] private GameObject aiPrefab;
    [SerializeField] private Transform spawnPoints;
    // �� �״� ���� �ɼ�
    private enum GridOrder { RowMajor, ColumnMajor }
    [SerializeField] private GridOrder order = GridOrder.RowMajor; // �⺻: ���� ����

    public void Moneycreate(Transform spawnRoot, int amount)
    {
        if (moneyPrefab == null) { Debug.LogWarning("moneyPrefab ���Ҵ�"); return; }
        if (spawnRoot == null) { spawnRoot = TableMoneyPoint; }
        if (spawnRoot == null) { Debug.LogWarning("spawnRoot/TableMoneyPoint ����"); return; }

        const int GRID_COLS = 3;
        const int GRID_ROWS = 3;
        const float H_SPACING = 0.6f;   // ���� (right)
        const float V_SPACING = 0.9f;   // ���� (forward)
        const float HEIGHT_STEP = 0.3f; // 9������ �� ����

        var list = TableMoney;

        for (int i = 0; i < amount; i++)
        {
            int count = list.Count;
            int perLayer = GRID_COLS * GRID_ROWS;   // 9
            int layer = count / perLayer;           // 0,1,2...
            int idx = count % perLayer;           // 0~8

            int row, col;

            switch (order)
            {
                case GridOrder.RowMajor: // ���� ����: (0,0)->(0,1)->(0,2)->(1,0)...
                    row = idx / GRID_COLS;
                    col = idx % GRID_COLS;
                    break;

                case GridOrder.ColumnMajor: // ���� ����: (0,0)->(1,0)->(2,0)->(0,1)...
                    col = idx / GRID_ROWS;
                    row = idx % GRID_ROWS;
                    break;

                default:
                    row = idx / GRID_COLS;
                    col = idx % GRID_COLS;
                    break;
            }

            Vector3 basePos = spawnRoot.position + Vector3.up * (layer * HEIGHT_STEP);
            Vector3 offset =
                (spawnRoot.right * (col * H_SPACING)) +
                (spawnRoot.forward * (row * V_SPACING));

            // �θ� ������ ���� ���Ϸ���: �θ� ���� ���� �� SetParent(true) ���
            GameObject money = Instantiate(moneyPrefab, basePos + offset, spawnRoot.rotation);
            money.transform.SetParent(spawnRoot, true);

            list.Add(money);
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 0.5f)   // 0.25�ʸ��� ����
        {
            timer = 0f;
            SpawnAIIfFree();
        }
    }

    private void SpawnAIIfFree()
    {
        for (int i = 0; i < Pick.Count; i++)
        {
            if (!Pick[i])
            {
                var aiGO = Instantiate(aiPrefab, spawnPoints.position, spawnPoints.rotation);
                aiGO.SetActive(true);

                if (aiGO.TryGetComponent(out AIController ai))
                {
                    members.Add(ai);   // �� ���� �� ���̸� ��!
                }

                break; // �� ���� �ϳ��� ����
            }
        }
    }



}

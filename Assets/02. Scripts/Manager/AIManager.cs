using System.Collections.Generic;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    public bool isCalculated;

    public List<GameObject> TableMoney = new List<GameObject>();
    [SerializeField] private Transform TableMoneyPoint;

    public List<bool> Pick = new List<bool>();
    public List<bool> Pack = new List<bool>();
    public List<bool> Table = new List<bool>();
    public List<bool> hall = new List<bool>();

    [Header("Prefabs")]
    [SerializeField] private GameObject moneyPrefab;

    // ▼ 쌓는 순서 옵션
    private enum GridOrder { RowMajor, ColumnMajor }
    [SerializeField] private GridOrder order = GridOrder.RowMajor; // 기본: 세로 먼저

    public void Moneycreate(Transform spawnRoot, int amount)
    {
        if (moneyPrefab == null) { Debug.LogWarning("moneyPrefab 미할당"); return; }
        if (spawnRoot == null) { spawnRoot = TableMoneyPoint; }
        if (spawnRoot == null) { Debug.LogWarning("spawnRoot/TableMoneyPoint 없음"); return; }

        const int GRID_COLS = 3;
        const int GRID_ROWS = 3;
        const float H_SPACING = 0.6f;   // 가로 (right)
        const float V_SPACING = 0.9f;   // 세로 (forward)
        const float HEIGHT_STEP = 0.3f; // 9개마다 층 높이

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
                case GridOrder.RowMajor: // 가로 먼저: (0,0)->(0,1)->(0,2)->(1,0)...
                    row = idx / GRID_COLS;
                    col = idx % GRID_COLS;
                    break;

                case GridOrder.ColumnMajor: // 세로 먼저: (0,0)->(1,0)->(2,0)->(0,1)...
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

            // 부모 스케일 영향 피하려면: 부모 없이 생성 후 SetParent(true) 사용
            GameObject money = Instantiate(moneyPrefab, basePos + offset, spawnRoot.rotation);
            money.transform.SetParent(spawnRoot, true);

            list.Add(money);
        }
    }
}

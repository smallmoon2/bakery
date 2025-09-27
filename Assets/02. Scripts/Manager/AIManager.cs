using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    public bool isCalculated;

    

    public List<GameObject> TableMoney = new List<GameObject>();

    public List<GameObject> TableMoney2 = new List<GameObject>();


    [SerializeField] private Transform TableMoneyPoint;
    [SerializeField] public Transform tableMoneySpawn2;

    public enum ListState { Pack, Hall }
    [SerializeField] private List<AIController> Packmembers = new List<AIController>();
    [SerializeField] private List<AIController> Hallmembers = new List<AIController>();

    public Transform tableMoneySpawn;

    public List<bool> Pick = new List<bool>();
    public List<bool> hall = new List<bool>();

    public GameObject Trash;
    public GameObject Chair;

    public bool isHallOpen = false;
    public bool isTableempty = true;

    public int PickStateNum = 0;
    public int hallStateNum = 0;
    public int AiNum = 0;


    private float timer;

    public int BakeryAicount = 0;

    [Header("Prefabs")]
    [SerializeField] private GameObject moneyPrefab;
    [SerializeField] private GameObject aiPrefab;
    [SerializeField] private Transform spawnPoints;
    // ▼ 쌓는 순서 옵션
    private enum GridOrder { RowMajor, ColumnMajor }
    [SerializeField] private GridOrder order = GridOrder.RowMajor; // 기본: 세로 먼저

    public void Moneycreate(Transform spawnRoot, int amount, int listIndex = 1)
    {
        GameManager.Instance.sound.PlayByKey("Cost_Money");
        
        if (moneyPrefab == null) { Debug.LogWarning("moneyPrefab 미할당"); return; }
        if (spawnRoot == null) { spawnRoot = TableMoneyPoint; }
        if (spawnRoot == null) { Debug.LogWarning("spawnRoot/TableMoneyPoint 없음"); return; }

        const int GRID_COLS = 3;
        const int GRID_ROWS = 3;
        const float H_SPACING = 0.5f;   // 가로 (right)
        const float V_SPACING = 0.8f;   // 세로 (forward)
        const float HEIGHT_STEP = 0.3f; // 9개마다 층 높이

        // ★ listIndex로 어느 리스트에 담을지 결정 (기본 1)
        List<GameObject> list = (listIndex == 2) ? TableMoney2 : TableMoney;
        if (list == null)
        {
            // 혹시 null이면 안전하게 초기화
            list = new List<GameObject>();
            if (listIndex == 2) TableMoney2 = list; else TableMoney = list;
        }

        for (int i = 0; i < amount; i++)
        {
            GameManager.Instance.ui.SetGuide(UIManager.Guidestate.GetMoney);

            int count = list.Count;
            int perLayer = GRID_COLS * GRID_ROWS; // 9
            int layer = count / perLayer;         // 0,1,2...
            int idx = count % perLayer;         // 0~8

            int row, col;
            switch (order)
            {
                case GridOrder.RowMajor:   // 가로 먼저
                    row = idx / GRID_COLS;
                    col = idx % GRID_COLS;
                    break;
                case GridOrder.ColumnMajor: // 세로 먼저
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

            GameObject money = Instantiate(moneyPrefab, basePos + offset, spawnRoot.rotation);
            money.transform.SetParent(spawnRoot, true); // 부모 스케일 영향 최소화

            list.Add(money);
            GameManager.Instance.sound.PlayByKey("cash");
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 1f) // 1초마다
        {
            timer = 0f;

            // 50% 확률로 소환
            if (Random.value < 0.3f)
            {
                SpawnAIIfFree();
            }
        }
    }

    private void SpawnAIIfFree()
    {
        if (BakeryAicount >= 7)
        {
            return;
        }

        for (int i = 0; i < Pick.Count; i++)
        {
            if (!Pick[i])
            {
                var aiGO = Instantiate(aiPrefab, spawnPoints.position, spawnPoints.rotation);
                aiGO.SetActive(true);
                BakeryAicount++;


                break; // 한 번에 하나만 스폰
            }
        }
    }

    public void AddToList(AIController ai, ListState listState)
    {
        if (ai == null) return;

        switch (listState)
        {
            case ListState.Pack:
                if (!Packmembers.Contains(ai))
                    Packmembers.Add(ai);
                ReassignIndices(Packmembers, (c, i) => c.SetPackIndex(i));
                break;

            case ListState.Hall:
                if (!Hallmembers.Contains(ai))
                    Hallmembers.Add(ai);
                ReassignIndices(Hallmembers, (c, i) => c.SetHallIndex(i));
                break;
        }
    }

    public void RemoveFromList(AIController ai, ListState listState)
    {
        if (ai == null) return;

        switch (listState)
        {
            case ListState.Pack:
                if (Packmembers.Remove(ai))
                    ReassignIndices(Packmembers, (c, i) => c.SetPackIndex(i));
                break;

            case ListState.Hall:
                if (Hallmembers.Remove(ai))
                    ReassignIndices(Hallmembers, (c, i) => c.SetHallIndex(i));
                break;
        }
    }

    private void ReassignIndices(List<AIController> group, System.Action<AIController, int> setter)
    {
        for (int i = 0; i < group.Count; i++)
            setter(group[i], i);
    }

}

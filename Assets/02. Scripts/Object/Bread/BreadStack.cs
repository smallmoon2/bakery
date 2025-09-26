using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreadStack : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BreadSpawner spawner;
    [SerializeField] private BreadBasket Basket;
    [SerializeField] private Transform stackPoint;
    [SerializeField] private Transform PrestackPoint;

    [Header("Stack Settings")]
    [SerializeField] private float stepHeight = 0.25f;
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotLerp = 8f;
    [SerializeField] private bool makeKinematicOnStack = true;

    [Header("PickUp / DropOff Gate")]
    [SerializeField] private string pickUpTag = "Oven";
    [SerializeField] private string DropOffTag = "Basket";
    private bool canStack;
    private bool canDrop;

    [Header("Stack Flow")]
    [SerializeField] private float stackDelay = 0.1f;
    private Stack<GameObject> stacking = new Stack<GameObject>();

    // bread currently moving to basket: <bread, target slot>
    private readonly Dictionary<GameObject, Transform> dropping = new Dictionary<GameObject, Transform>();
    private Coroutine stackRoutine;

    // cooldown so that only one drop starts at a time
    private float nextDropTime = 0f;

    private void Awake()
    {
        if (!spawner) spawner = FindObjectOfType<BreadSpawner>();
        if (!Basket) Basket = FindObjectOfType<BreadBasket>();
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

    // pick from spawner to hand (stacking) at stackDelay interval while inside pickup area
    private IEnumerator StackLoop()
    {
        var wait = new WaitForSeconds(stackDelay);

        while (true)
        {
            if (spawner && canStack)
            {
                spawner.breads.RemoveAll(b => b == null);

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

                    // parent to stackPoint (keep world position)
                    t.SetParent(stackPoint, true);

                    // prestack pose offset by slot height
                    int slot = stacking.Count;
                    Vector3 basePos = (PrestackPoint ? PrestackPoint.position : stackPoint.position);
                    Vector3 prestackWorldPos = basePos + Vector3.up * (stepHeight * slot);

                    t.position = prestackWorldPos;

                    EnsureKinematic(picked);
                    stacking.Push(picked);
                }
            }

            yield return wait;
        }
    }

    private void Update()
    {
        if (stackPoint)
        {
            // arrange breads in hand at stackPoint
            var arr = stacking.ToArray(); // top first
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

        // drop to basket: one at a time, with delay between starts
        if (canDrop && dropping.Count == 0 && Time.time >= nextDropTime)
        {
            TryDropOneToBasket();
            nextDropTime = Time.time + stackDelay;
        }

        // process smooth movement of the bread that is dropping
        ProcessDropping();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(pickUpTag)) canStack = true;
        if (other.CompareTag(DropOffTag)) canDrop = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(pickUpTag)) canStack = false;
        if (other.CompareTag(DropOffTag)) canDrop = false;
    }

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

    // pop from hand, teleport to prestack, then smoothly move to basket slot
    private void TryDropOneToBasket()
    {
        if (!Basket) return;

        var slots = Basket.Rslots;                 // IReadOnlyList<Transform>
        if (slots == null || slots.Count == 0) return;

        // only one at a time
        if (dropping.Count > 0) return;

        int nextIndex = Basket.breads.Count;     // next slot index
        int maxCapacity = Mathf.Min(slots.Count, 8);
        if (nextIndex >= maxCapacity) return;
        if (stacking.Count == 0) return;

        var bread = stacking.Pop();
        if (!bread) return;

        var slotT = slots[nextIndex];
        if (slotT == null) return;

        var t = bread.transform;

        // teleport to prestack start position
        Vector3 startPos = PrestackPoint ? PrestackPoint.position
                                         : (stackPoint ? stackPoint.position : t.position);
        t.position = startPos;

        // parent to target slot but keep world pose so it can lerp in
        t.SetParent(slotT, worldPositionStays: true);

        EnsureKinematic(bread);

        // begin smooth move toward slot
        dropping[bread] = slotT;
    }

    private void ProcessDropping()
    {
        if (dropping.Count == 0) return;

        var finalize = new List<GameObject>();

        foreach (var kv in dropping)
        {
            var go = kv.Key;
            var slotT = kv.Value;
            if (!go || !slotT) { finalize.Add(go); continue; }

            var t = go.transform;
            t.position = Vector3.MoveTowards(t.position, slotT.position, moveSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, slotT.rotation, rotLerp * Time.deltaTime);

            if ((t.position - slotT.position).sqrMagnitude < 0.0001f)
                finalize.Add(go);
        }

        foreach (var go in finalize)
        {
            if (go && dropping.TryGetValue(go, out var slotT) && slotT)
            {
                var t = go.transform;
                t.SetParent(slotT, worldPositionStays: false);
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;

                // push into basket stack on arrival
                Basket.breads.Push(go);
            }
            dropping.Remove(go);
        }
    }
}

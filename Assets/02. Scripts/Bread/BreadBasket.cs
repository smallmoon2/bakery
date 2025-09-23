using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreadBasket : MonoBehaviour
{
    [Header("Stack Config")]
    [SerializeField] private Transform[] slots;         // 씬에 미리 배치한 자리(아래→위 or 좌→우 순서)

    public bool playerdropOff;

    // 스택 (top = 마지막으로 넣은 빵)
    public IReadOnlyList<Transform> Rslots => slots; // 읽기 전용 노출

    public Stack<GameObject> breads = new Stack<GameObject>();
    private Coroutine dropOffRoutine;

    // --- 빵 추가 메서드 ---
    public void AddBread(GameObject bread)
    {
        if (bread == null) return;
        breads.Push(bread);

        if (dropOffRoutine != null)
            StopCoroutine(dropOffRoutine);

        dropOffRoutine = StartCoroutine(SetDropOffFlag());
    }

    private IEnumerator SetDropOffFlag()
    {
        playerdropOff = true;
        yield return new WaitForSeconds(0.5f);
        playerdropOff = false;
    }


}

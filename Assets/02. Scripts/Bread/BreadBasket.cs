using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreadBasket : MonoBehaviour
{
    [Header("Stack Config")]
    [SerializeField] private Transform[] slots;         // ���� �̸� ��ġ�� �ڸ�(�Ʒ����� or �¡�� ����)

    public bool playerdropOff;

    // ���� (top = ���������� ���� ��)
    public IReadOnlyList<Transform> Rslots => slots; // �б� ���� ����

    public Stack<GameObject> breads = new Stack<GameObject>();
    private Coroutine dropOffRoutine;

    // --- �� �߰� �޼��� ---
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

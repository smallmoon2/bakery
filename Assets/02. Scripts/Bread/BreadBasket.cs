using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreadBasket : MonoBehaviour
{
    [Header("Stack Config")]
    [SerializeField] private Transform[] slots;         // ���� �̸� ��ġ�� �ڸ�(�Ʒ����� or �¡�� ����)

    // ���� (top = ���������� ���� ��)
    public IReadOnlyList<Transform> Rslots => slots; // �б� ���� ����

    public Stack<GameObject> breads = new Stack<GameObject>();


}

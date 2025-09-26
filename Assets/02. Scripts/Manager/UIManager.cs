using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI moneyUI;

    public enum Guidestate { Oven, Basket, PayTable, GetMoney, HallTable, NextContiune }

    [SerializeField] private GameObject arrowguide;
    [SerializeField] private Transform ovenguide;
    [SerializeField] private Transform basketguide;
    [SerializeField] private Transform payTableguide;
    [SerializeField] private Transform getMoneyguide;
    [SerializeField] private Transform hallTableguide;
    [SerializeField] private Transform nextContiuneguide;

    // �̹� ȣ��� ���̵� ���
    private readonly HashSet<Guidestate> used = new HashSet<Guidestate>();

    void Update()
    {
        moneyUI.text = GameManager.Instance.myMoney.ToString();
    }

    public void SetGuide(Guidestate s)
    {
        if (!arrowguide) return;

        // �̹� �ѹ� ȣ������� ����
        if (used.Contains(s)) return;

        Transform t = s switch
        {
            Guidestate.Oven => ovenguide,
            Guidestate.Basket => basketguide,
            Guidestate.PayTable => payTableguide,
            Guidestate.GetMoney => getMoneyguide,
            Guidestate.HallTable => hallTableguide,
            Guidestate.NextContiune => nextContiuneguide,
            _ => null
        };

        if (!t) { arrowguide.SetActive(false); return; }

        // ���������� ���õ� ���� ��� ó��
        used.Add(s);

        arrowguide.SetActive(true);
        arrowguide.transform.SetParent(t, worldPositionStays: false);
        arrowguide.transform.localPosition = Vector3.zero;

    }

    // �ʿ��ϸ� ������� �ʱ�ȭ
    public void ResetGuides() => used.Clear();
}

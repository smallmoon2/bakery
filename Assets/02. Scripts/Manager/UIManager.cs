using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI moneyUI;

    public enum Guidestate { Oven, Basket, PayTable, GetMoney, HallTable, trashTable, NextContiune }

    [SerializeField] public GameObject arrowguide;
    [SerializeField] private Transform ovenguide;
    [SerializeField] private Transform basketguide;
    [SerializeField] private Transform payTableguide;
    [SerializeField] private Transform getMoneyguide;
    [SerializeField] private Transform hallTableguide;
    [SerializeField] private Transform trashTableguide;   // �� �߰�
    [SerializeField] private Transform nextContiuneguide;

    private readonly HashSet<Guidestate> used = new HashSet<Guidestate>();

    void Update()
    {
        moneyUI.text = GameManager.Instance.myMoney.ToString();
    }

    public void SetGuide(Guidestate s)
    {
        if (!arrowguide) return;
        if (used.Contains(s)) return; // �̹� �ѹ� ȣ������� ����

        Transform t = s switch
        {
            Guidestate.Oven => ovenguide,
            Guidestate.Basket => basketguide,
            Guidestate.PayTable => payTableguide,
            Guidestate.GetMoney => getMoneyguide,
            Guidestate.HallTable => hallTableguide,
            Guidestate.trashTable => trashTableguide,   // �� �߰�
            Guidestate.NextContiune => nextContiuneguide,
            _ => null
        };

        if (!t) { arrowguide.SetActive(false); return; }

        used.Add(s);

        arrowguide.SetActive(true);
        arrowguide.transform.SetParent(t, worldPositionStays: false);
        arrowguide.transform.localPosition = Vector3.zero;
        arrowguide.transform.localRotation = Quaternion.identity;
    }
    public void SetGuideActive(bool on)
    {
        if (!arrowguide) return;
        arrowguide.SetActive(on);
        Debug.Log("������Ʈ Ȱ��ȭ ����");
        Debug.Log(on);
    }
    // �ʿ��ϸ� ������� �ʱ�ȭ
    public void ResetGuides() => used.Clear();
}

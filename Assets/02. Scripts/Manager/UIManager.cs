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

    // 이미 호출된 가이드 기록
    private readonly HashSet<Guidestate> used = new HashSet<Guidestate>();

    void Update()
    {
        moneyUI.text = GameManager.Instance.myMoney.ToString();
    }

    public void SetGuide(Guidestate s)
    {
        if (!arrowguide) return;

        // 이미 한번 호출됐으면 무시
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

        // 성공적으로 세팅될 때만 사용 처리
        used.Add(s);

        arrowguide.SetActive(true);
        arrowguide.transform.SetParent(t, worldPositionStays: false);
        arrowguide.transform.localPosition = Vector3.zero;

    }

    // 필요하면 진행상태 초기화
    public void ResetGuides() => used.Clear();
}

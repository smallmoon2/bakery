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
    [SerializeField] private Transform trashTableguide;   // ← 추가
    [SerializeField] private Transform nextContiuneguide;

    private readonly HashSet<Guidestate> used = new HashSet<Guidestate>();

    void Update()
    {
        moneyUI.text = GameManager.Instance.myMoney.ToString();
    }

    public void SetGuide(Guidestate s)
    {
        if (!arrowguide) return;
        if (used.Contains(s)) return; // 이미 한번 호출됐으면 무시

        Transform t = s switch
        {
            Guidestate.Oven => ovenguide,
            Guidestate.Basket => basketguide,
            Guidestate.PayTable => payTableguide,
            Guidestate.GetMoney => getMoneyguide,
            Guidestate.HallTable => hallTableguide,
            Guidestate.trashTable => trashTableguide,   // ← 추가
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
        Debug.Log("오브젝트 활설화 여부");
        Debug.Log(on);
    }
    // 필요하면 진행상태 초기화
    public void ResetGuides() => used.Clear();
}

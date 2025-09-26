using UnityEngine;

public class ArrowRotation : MonoBehaviour
{
    [SerializeField] private GameObject guideArrow; // 비워두면 자기 자신 토글

    public bool onlyYAxis = true;
    public bool smooth = true;
    public float turnSpeed = 720f;

    void Awake()
    {
        if (!guideArrow) guideArrow = gameObject;
    }

    void Update()
    {
        var ui = GameManager.Instance ? GameManager.Instance.ui : null;
        var targetGO = ui ? ui.arrowguide : null;

        bool hasTarget = targetGO && targetGO.activeInHierarchy;

        // on/off
        if (guideArrow.activeSelf != hasTarget) guideArrow.SetActive(hasTarget);
        if (!hasTarget) return;

        // 회전
        Transform target = targetGO.transform;
        Vector3 dir = target.position - transform.position;
        if (onlyYAxis) dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = smooth
            ? Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime)
            : look;
    }
}

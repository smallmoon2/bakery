using UnityEngine;

[ExecuteAlways]
public class AIUIController : MonoBehaviour
{
    [Tooltip("비우면 부모를 자동 사용")]
    public Transform target;

    [Tooltip("OnEnable 시 현재 회전을 고정한다")]
    public bool autoCaptureOnEnable = true;

    [SerializeField] private Vector3 lockedEuler; // 표시용
    private Quaternion lockedRot;

    void OnEnable()
    {
        if (!target) target = transform.parent;
        if (!target) return;

        // 회전 고정값을 현재 값으로 캡처하거나, 저장된 값 사용
        lockedRot = autoCaptureOnEnable ? transform.rotation : Quaternion.Euler(lockedEuler);
        lockedEuler = lockedRot.eulerAngles;

        // ★ 위치 오프셋을 0으로: 부모 위치 = 내 위치
        transform.position = target.position;
    }

    void LateUpdate()
    {
        if (!target) target = transform.parent;
        if (!target) return;

        // ★ 부모 '위치'만 따라감 (같은 자리)
        transform.position = target.position;

        // ★ 회전은 항상 고정
        transform.rotation = lockedRot;
    }

    [ContextMenu("Lock → Current World Rotation")]
    public void LockToCurrentRotation()
    {
        lockedRot = transform.rotation;
        lockedEuler = transform.rotation.eulerAngles;
    }
}

using UnityEngine;

[ExecuteAlways]
public class FreezeWorldRotation : MonoBehaviour
{
    [Tooltip("체크하면 부모가 회전해도 이 오브젝트의 월드 회전은 고정됩니다.")]
    public bool freeze = true;

    [Tooltip("OnEnable 시 현재 회전을 자동으로 잠급니다.")]
    public bool autoCaptureOnEnable = true;

    [SerializeField, Tooltip("잠글 월드 회전(편집용 표시)")]
    private Vector3 lockedEuler;

    private Quaternion lockedRot;

    void OnEnable()
    {
        if (autoCaptureOnEnable) LockToCurrent();
        else lockedRot = Quaternion.Euler(lockedEuler);
    }

    // 현재 회전을 잠그기(컨텍스트 메뉴로 호출 가능)
    [ContextMenu("Lock → Current World Rotation")]
    public void LockToCurrent()
    {
        lockedRot = transform.rotation;              // 월드 회전 저장
        lockedEuler = transform.rotation.eulerAngles;  // 인스펙터 표시용
    }

    void LateUpdate()
    {
        if (!freeze) return;
        // 부모가 어떻게 회전해도 월드 회전 고정
        transform.rotation = lockedRot;
    }
}

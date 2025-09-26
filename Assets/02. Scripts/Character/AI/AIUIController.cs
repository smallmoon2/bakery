using UnityEngine;

[ExecuteAlways]
public class AIUIController : MonoBehaviour
{
    [Tooltip("���� �θ� �ڵ� ���")]
    public Transform target;

    [Tooltip("OnEnable �� ���� ȸ���� �����Ѵ�")]
    public bool autoCaptureOnEnable = true;

    [SerializeField] private Vector3 lockedEuler; // ǥ�ÿ�
    private Quaternion lockedRot;

    void OnEnable()
    {
        if (!target) target = transform.parent;
        if (!target) return;

        // ȸ�� �������� ���� ������ ĸó�ϰų�, ����� �� ���
        lockedRot = autoCaptureOnEnable ? transform.rotation : Quaternion.Euler(lockedEuler);
        lockedEuler = lockedRot.eulerAngles;

        // �� ��ġ �������� 0����: �θ� ��ġ = �� ��ġ
        transform.position = target.position;
    }

    void LateUpdate()
    {
        if (!target) target = transform.parent;
        if (!target) return;

        // �� �θ� '��ġ'�� ���� (���� �ڸ�)
        transform.position = target.position;

        // �� ȸ���� �׻� ����
        transform.rotation = lockedRot;
    }

    [ContextMenu("Lock �� Current World Rotation")]
    public void LockToCurrentRotation()
    {
        lockedRot = transform.rotation;
        lockedEuler = transform.rotation.eulerAngles;
    }
}

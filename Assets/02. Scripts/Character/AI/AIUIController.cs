using UnityEngine;

[ExecuteAlways]
public class FreezeWorldRotation : MonoBehaviour
{
    [Tooltip("üũ�ϸ� �θ� ȸ���ص� �� ������Ʈ�� ���� ȸ���� �����˴ϴ�.")]
    public bool freeze = true;

    [Tooltip("OnEnable �� ���� ȸ���� �ڵ����� ��޴ϴ�.")]
    public bool autoCaptureOnEnable = true;

    [SerializeField, Tooltip("��� ���� ȸ��(������ ǥ��)")]
    private Vector3 lockedEuler;

    private Quaternion lockedRot;

    void OnEnable()
    {
        if (autoCaptureOnEnable) LockToCurrent();
        else lockedRot = Quaternion.Euler(lockedEuler);
    }

    // ���� ȸ���� ��ױ�(���ؽ�Ʈ �޴��� ȣ�� ����)
    [ContextMenu("Lock �� Current World Rotation")]
    public void LockToCurrent()
    {
        lockedRot = transform.rotation;              // ���� ȸ�� ����
        lockedEuler = transform.rotation.eulerAngles;  // �ν����� ǥ�ÿ�
    }

    void LateUpdate()
    {
        if (!freeze) return;
        // �θ� ��� ȸ���ص� ���� ȸ�� ����
        transform.rotation = lockedRot;
    }
}

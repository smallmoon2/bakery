using UnityEngine;

public class ArrowMove : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.25f; // ���Ʒ� �̵� ����
    [SerializeField] private float speed = 2f;        // �ӵ�(���ļ�)
    [SerializeField] private bool useLocal = true;    // ����/���� ��� �������� ��������

    private Vector3 basePos;
    private float phase;

    void Start()
    {
        basePos = useLocal ? transform.localPosition : transform.position;
        phase = Random.value * Mathf.PI * 2f; // ���� �� ���� �� ����ȭ �� �ǰ�
    }

    void Update()
    {
        float yOffset = Mathf.Sin(Time.time * speed + phase) * amplitude;

        if (useLocal)
            transform.localPosition = new Vector3(basePos.x, basePos.y + yOffset, basePos.z);
        else
            transform.position = new Vector3(basePos.x, basePos.y + yOffset, basePos.z);
    }
}

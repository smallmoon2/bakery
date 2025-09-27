using UnityEngine;

public class TextMove : MonoBehaviour
{
    [SerializeField] GameObject guideUi;
    [SerializeField] float minScale = 0.9f;
    [SerializeField] float maxScale = 1.05f;
    [SerializeField] float speedHz = 1f;   // �ʴ� �ݺ� Ƚ��

    Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        // 0~1�� ������ t
        float t = (Mathf.Sin(Time.time * 2f * Mathf.PI * speedHz) + 1f) * 0.5f;
        float s = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = baseScale * s;  // ��� �࿡ ���� ����

        // �� ȭ�� �� �� ��ġ/Ŭ�� �� ��Ȱ��ȭ
        if (Input.GetMouseButtonDown(0) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            guideUi.SetActive(false);  // ������Ʈ ��Ȱ��ȭ
        }
    }
}

using UnityEngine;

public class TextMove : MonoBehaviour
{
    [SerializeField] GameObject guideUi;
    [SerializeField] float minScale = 0.9f;
    [SerializeField] float maxScale = 1.05f;
    [SerializeField] float speedHz = 1f;   // 초당 반복 횟수

    Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        // 0~1로 오가는 t
        float t = (Mathf.Sin(Time.time * 2f * Mathf.PI * speedHz) + 1f) * 0.5f;
        float s = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = baseScale * s;  // 모든 축에 균일 적용

        // ▶ 화면 한 번 터치/클릭 시 비활성화
        if (Input.GetMouseButtonDown(0) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            guideUi.SetActive(false);  // 오브젝트 비활성화
        }
    }
}

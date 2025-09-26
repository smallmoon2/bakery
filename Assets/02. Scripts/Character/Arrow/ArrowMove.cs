using UnityEngine;

public class ArrowMove : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.25f; // 위아래 이동 범위
    [SerializeField] private float speed = 2f;        // 속도(주파수)
    [SerializeField] private bool useLocal = true;    // 로컬/월드 어디 기준으로 움직일지

    private Vector3 basePos;
    private float phase;

    void Start()
    {
        basePos = useLocal ? transform.localPosition : transform.position;
        phase = Random.value * Mathf.PI * 2f; // 여러 개 있을 때 동기화 안 되게
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

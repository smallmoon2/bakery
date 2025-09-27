using UnityEngine;

/// 카메라 Viewport를 조절해서 화면 비율을 고정(레터박스/필러박스)합니다.
[RequireComponent(typeof(Camera))]
public class AspectRatioLocker : MonoBehaviour
{
    [Header("Target Aspect (width : height)")]
    public int targetWidth = 1080;
    public int targetHeight = 1902;  // 세로 기준(포트레이트)

    Camera cam;
    int lastW, lastH;

    void Awake()
    {
        cam = GetComponent<Camera>();
        Apply();
    }

    void LateUpdate()
    {
        // 해상도/회전이 바뀌면 다시 적용
        if (Screen.width != lastW || Screen.height != lastH)
            Apply();
    }

    void Apply()
    {
        lastW = Screen.width;
        lastH = Screen.height;

        float target = (float)targetWidth / targetHeight;              // ~0.5673
        float window = (float)Screen.width / (float)Screen.height;

        // 화면이 더 좁으면(letterbox), 더 넓으면(pillarbox) 각각 보정
        if (window < target)
        {
            // 좌우는 꽉, 위아래에 레터박스
            float height = window / target;          // 0~1
            cam.rect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
        }
        else
        {
            // 위아래는 꽉, 좌우에 필러박스
            float width = target / window;           // 0~1
            cam.rect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
        }

        // 검은 바를 원하면 카메라 배경색/클리어플래그 확인
        cam.clearFlags = CameraClearFlags.SolidColor;
        // cam.backgroundColor = Color.black; // 필요하면 지정
    }
}

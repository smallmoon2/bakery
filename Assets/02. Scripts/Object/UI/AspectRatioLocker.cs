using UnityEngine;

/// ī�޶� Viewport�� �����ؼ� ȭ�� ������ ����(���͹ڽ�/�ʷ��ڽ�)�մϴ�.
[RequireComponent(typeof(Camera))]
public class AspectRatioLocker : MonoBehaviour
{
    [Header("Target Aspect (width : height)")]
    public int targetWidth = 1080;
    public int targetHeight = 1902;  // ���� ����(��Ʈ����Ʈ)

    Camera cam;
    int lastW, lastH;

    void Awake()
    {
        cam = GetComponent<Camera>();
        Apply();
    }

    void LateUpdate()
    {
        // �ػ�/ȸ���� �ٲ�� �ٽ� ����
        if (Screen.width != lastW || Screen.height != lastH)
            Apply();
    }

    void Apply()
    {
        lastW = Screen.width;
        lastH = Screen.height;

        float target = (float)targetWidth / targetHeight;              // ~0.5673
        float window = (float)Screen.width / (float)Screen.height;

        // ȭ���� �� ������(letterbox), �� ������(pillarbox) ���� ����
        if (window < target)
        {
            // �¿�� ��, ���Ʒ��� ���͹ڽ�
            float height = window / target;          // 0~1
            cam.rect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
        }
        else
        {
            // ���Ʒ��� ��, �¿쿡 �ʷ��ڽ�
            float width = target / window;           // 0~1
            cam.rect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
        }

        // ���� �ٸ� ���ϸ� ī�޶� ����/Ŭ�����÷��� Ȯ��
        cam.clearFlags = CameraClearFlags.SolidColor;
        // cam.backgroundColor = Color.black; // �ʿ��ϸ� ����
    }
}

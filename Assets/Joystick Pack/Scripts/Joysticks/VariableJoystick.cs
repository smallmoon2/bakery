using UnityEngine;
using UnityEngine.EventSystems;

public class VariableJoystick : Joystick
{
    public float MoveThreshold { get { return moveThreshold; } set { moveThreshold = Mathf.Abs(value); } }
    [SerializeField] private float moveThreshold = 1;
    [SerializeField] private JoystickType joystickType = JoystickType.Fixed;

    [Header("터치 입력을 받을 풀스크린 영역(없으면 Canvas 전체)")]
    [SerializeField] private RectTransform inputArea;

    private Vector2 fixedPosition = Vector2.zero;
    private Canvas _canvas;
    private RectTransform _parentRT;

    public void SetMode(JoystickType type)
    {
        joystickType = type;
        // 고정/플로팅 상관 없이, 기본은 숨김 상태 유지(터치 때만 보이게)
        background.gameObject.SetActive(false);
        if (type == JoystickType.Fixed) background.anchoredPosition = fixedPosition;
    }

    protected override void Start()
    {
        base.Start();
        _canvas = GetComponentInParent<Canvas>();
        _parentRT = background.transform.parent as RectTransform;

        if (!inputArea && _canvas) inputArea = _canvas.transform as RectTransform;

        fixedPosition = background.anchoredPosition;

        // 시작 시 숨김
        background.gameObject.SetActive(false);
        SetMode(joystickType);
    }

    // 터치한 곳에서만 보이도록
    public override void OnPointerDown(PointerEventData eventData)
    {
        var cam = eventData.pressEventCamera;
        if (cam == null && _canvas && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = _canvas.worldCamera;

        // 입력영역 기준으로 터치 지점의 월드 좌표 계산
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            inputArea ? inputArea : _parentRT, eventData.position, cam, out var worldPos);

        // 조이스틱 배경을 터치 지점으로 이동하고 보이기
        background.position = worldPos;
        background.gameObject.SetActive(true);

        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        // 터치 끝나면 항상 숨김
        background.gameObject.SetActive(false);
        base.OnPointerUp(eventData);
    }

    protected override void HandleInput(float magnitude, Vector2 normalised, Vector2 radius, Camera cam)
    {
        if (joystickType == JoystickType.Dynamic && magnitude > moveThreshold)
        {
            Vector2 diff = normalised * (magnitude - moveThreshold) * radius;
            background.anchoredPosition += diff;
        }
        base.HandleInput(magnitude, normalised, radius, cam);
    }
}

public enum JoystickType { Fixed, Floating, Dynamic }

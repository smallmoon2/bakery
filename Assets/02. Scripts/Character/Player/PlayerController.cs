using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private VariableJoystick joystick;    // 인스펙터에 연결 권장
    private PlayerObjectController playerObjectController;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float turnSpeed = 15f;

    private Animator anim;
    private CharacterController cc;
    private Vector3 moveInput;

    void Awake()
    {
        init();
    }

    void init()
    {
        playerObjectController = FindObjectOfType<PlayerObjectController>();
        if (!joystick) joystick = FindObjectOfType<VariableJoystick>(true); // 비활성 포함 탐색
        anim = GetComponentInChildren<Animator>(true);
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (!cc) cc = GetComponent<CharacterController>();
        if (!joystick) joystick = FindObjectOfType<VariableJoystick>(true);

        if (Mathf.Approximately(Time.timeScale, 0f))
        {
            // 필요하면 일시적으로 풀고 테스트: Time.timeScale = 1f;
            Debug.LogWarning("[Player] Time.timeScale==0 이라 이동이 0입니다.");
        }

        ReadInput();
        Move();
        Turn();
        UpdateAnimator();
    }

    void ReadInput()
    {
        // 조이스틱 + 키보드(백업) 합산
        float h = (joystick ? joystick.Horizontal : 0f) + Input.GetAxisRaw("Horizontal");
        float v = (joystick ? joystick.Vertical : 0f) + Input.GetAxisRaw("Vertical");

        moveInput = new Vector3(h, 0f, v);
        moveInput = Vector3.ClampMagnitude(moveInput, 1f);
    }

    void Move()
    {
        Vector3 horizontal = moveInput * moveSpeed;
        cc.Move(horizontal * Time.deltaTime);
    }

    void Turn()
    {
        if (moveInput.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveInput);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
    }

    void UpdateAnimator()
    {
        float targetValue = moveInput.sqrMagnitude > 0.001f ? 1f : 0f;

        float animValue = anim ? anim.GetFloat("Move") : 0f;
        animValue = Mathf.Lerp(animValue, targetValue, 10f * Time.deltaTime);
        if (anim) anim.SetFloat("Move", animValue);

        if (anim && playerObjectController)
        {
            bool hasStack = playerObjectController.stackPoint && playerObjectController.stackPoint.childCount > 0;
            anim.SetBool("Stack", hasStack);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerTable"))
            GameManager.Instance.ai.isCalculated = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PlayerTable"))
            GameManager.Instance.ai.isCalculated = false;
    }
}

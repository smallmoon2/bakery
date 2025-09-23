using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    private VariableJoystick joystick; // Joystick Pack
    private PlayerObjectController playerObjectController;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float turnSpeed = 15f;

    [Header("Refs")]
    [SerializeField] private Transform handStackPoint; // HandStack Point 자식 오브젝트 참조

    private Animator anim;
    private CharacterController cc;

    // 회전/이동 공용 입력 벡터 (월드 기준 XZ)
    private Vector3 moveInput;

    private void Awake()
    {
        init();
    }

    void init()
    {
        joystick = FindObjectOfType<VariableJoystick>();
        anim = GetComponentInChildren<Animator>(true);
        cc = GetComponent<CharacterController>();
    }

    private void Update()
    {
        ReadInput();
        Move();
        Turn();
        UpdateAnimator();
    }

    private void ReadInput()
    {
        float h = joystick ? joystick.Horizontal : 0f;
        float v = joystick ? joystick.Vertical : 0f;

        moveInput = new Vector3(h, 0f, v);
        moveInput = Vector3.ClampMagnitude(moveInput, 1f);
    }

    private void Move()
    {
        Vector3 horizontal = moveInput.normalized * moveSpeed;
        cc.Move(horizontal * Time.deltaTime);
    }

    private void Turn()
    {
        if (moveInput != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveInput);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimator()
    {
        // --- 이동 애니메이션 ---
        float targetValue = (moveInput != Vector3.zero) ? 1f : 0f;
        float animValue = anim.GetFloat("Move");
        animValue = Mathf.Lerp(animValue, targetValue, 10f * Time.deltaTime);
        anim.SetFloat("Move", animValue);

        // --- 스택 애니메이션 ---
        bool hasStack = handStackPoint && handStackPoint.childCount > 0;
        anim.SetBool("Stack", hasStack);
    }
}

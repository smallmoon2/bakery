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
    [SerializeField] private Transform handStackPoint; // HandStack Point �ڽ� ������Ʈ ����

    private Animator anim;
    private CharacterController cc;

    // ȸ��/�̵� ���� �Է� ���� (���� ���� XZ)
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
        // --- �̵� �ִϸ��̼� ---
        float targetValue = (moveInput != Vector3.zero) ? 1f : 0f;
        float animValue = anim.GetFloat("Move");
        animValue = Mathf.Lerp(animValue, targetValue, 10f * Time.deltaTime);
        anim.SetFloat("Move", animValue);

        // --- ���� �ִϸ��̼� ---
        bool hasStack = handStackPoint && handStackPoint.childCount > 0;
        anim.SetBool("Stack", hasStack);
    }
}

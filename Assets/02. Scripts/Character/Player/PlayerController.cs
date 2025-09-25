using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    private VariableJoystick joystick; // Joystick Pack
    private PlayerObjectController playerObjectController;
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float turnSpeed = 15f;

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
        playerObjectController = FindObjectOfType<PlayerObjectController>();
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

    // 요청하신 형태로 교체
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
        float targetValue = 0f;

        if (moveInput != Vector3.zero) // 이동 키를 누를 경우
        {
            targetValue = 1;
        }

        float animValue = anim.GetFloat("Move");
        animValue = Mathf.Lerp(animValue, targetValue, 10f * Time.deltaTime);
        anim.SetFloat("Move", animValue);

        bool hasStack = playerObjectController.stackPoint && playerObjectController.stackPoint.childCount > 0;
        anim.SetBool("Stack", hasStack);

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerTable"))
        {
            GameManager.Instance.ai.isCalculated = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PlayerTable"))
        {
            GameManager.Instance.ai.isCalculated = false;
        }
    }

}

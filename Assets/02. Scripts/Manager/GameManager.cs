using System.Collections;
using UnityEngine;
using Cinemachine;

public class GameManager : Singleton<GameManager>
{
    public AIManager ai;
    public UIManager ui;
    public SoundManager sound;

    public int myMoney;

    [Header("Cameras")]
    [SerializeField] private CinemachineVirtualCamera hallTableCam; // HallTable camera
    [SerializeField] private CinemachineVirtualCamera nextCam;      // Next camera

    private const int ActivePriority = 20;

    // �ѹ��� ����ǰ� �ϴ� ���� �÷���
    private bool hallTableCamUsed = false;
    private bool nextCamUsed = false;

    private Coroutine hallCo;
    private Coroutine nextCo;

    // ---------------- Public API ----------------
    public void ActivateHallTableCam()
    {
        if (hallTableCamUsed || hallCo != null) return; // �̹� �����/���� ���̸� ����
        hallTableCamUsed = true;
        hallCo = StartCoroutine(HallTableCamRoutine());
    }

    public void ActivateNextCam()
    {
        if (nextCamUsed || nextCo != null) return; // �̹� �����/���� ���̸� ����
        nextCamUsed = true;
        nextCo = StartCoroutine(NextCamRoutine());
    }

    // ---------------- Coroutines ----------------
    private IEnumerator HallTableCamRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        if (hallTableCam) hallTableCam.Priority = ActivePriority;

        yield return new WaitForSeconds(3f);
        if (hallTableCam) hallTableCam.Priority = 1;

        hallCo = null;
    }

    private IEnumerator NextCamRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        if (nextCam) nextCam.Priority = ActivePriority;

        yield return new WaitForSeconds(3f);
        if (nextCam) nextCam.Priority = 1;

        nextCo = null;
    }

    // (�ɼ�) �ٽ� ��� �����ϰ� �����ϰ� ������ �Ʒ� �޼��� ȣ��
    // public void ResetCameraActivations() { hallTableCamUsed = nextCamUsed = false; }
}

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

    // 한번만 실행되게 하는 가드 플래그
    private bool hallTableCamUsed = false;
    private bool nextCamUsed = false;

    private Coroutine hallCo;
    private Coroutine nextCo;

    // ---------------- Public API ----------------
    public void ActivateHallTableCam()
    {
        if (hallTableCamUsed || hallCo != null) return; // 이미 실행됨/실행 중이면 무시
        hallTableCamUsed = true;
        hallCo = StartCoroutine(HallTableCamRoutine());
    }

    public void ActivateNextCam()
    {
        if (nextCamUsed || nextCo != null) return; // 이미 실행됨/실행 중이면 무시
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

    // (옵션) 다시 사용 가능하게 리셋하고 싶으면 아래 메서드 호출
    // public void ResetCameraActivations() { hallTableCamUsed = nextCamUsed = false; }
}

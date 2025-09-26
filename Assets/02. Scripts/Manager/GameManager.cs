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

    // ---------------- Public API ----------------
    public void ActivateHallTableCam()
    {
        StartCoroutine(HallTableCamRoutine());
    }

    public void ActivateNextCam()
    {
        StartCoroutine(NextCamRoutine());
    }

    // ---------------- Coroutines ----------------
    private IEnumerator HallTableCamRoutine()
    {
        yield return new WaitForSeconds(1.5f);

        if (hallTableCam) hallTableCam.Priority = ActivePriority;

        yield return new WaitForSeconds(2f);

        if (hallTableCam) hallTableCam.Priority = 1;
        yield break; 
    }

    private IEnumerator NextCamRoutine()
    {
        yield return new WaitForSeconds(1.5f);

        if (nextCam) nextCam.Priority = ActivePriority;
        yield return new WaitForSeconds(2f);

        if (nextCam) nextCam.Priority = 1;
        yield break; 
    }
}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OpenLock : MonoBehaviour
{
    public GameObject lockPrefab;
    public GameObject unlockPrefab;
    public TextMeshPro lockCounttext;

    public void decreaseLockCount()
    {
        if (int.TryParse(lockCounttext.text, out int count))
        {
            count--;
            lockCounttext.text = count.ToString();
        }

        if(count == 0)
        {
            Open();
        }
    }
    public void Open()
    {
        GameManager.Instance.sound.PlayByKey("Success");

        GameManager.Instance.ActivateNextCam();

        GameManager.Instance.ui.SetGuideActive(false);
        GameManager.Instance.ai.isHallOpen = true;
        unlockPrefab.SetActive(true);
        lockPrefab.SetActive(false);
        
    }
}

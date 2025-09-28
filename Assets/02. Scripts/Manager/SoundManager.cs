
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SoundManager : MonoBehaviour
{
    [Header("�ּ� �պκ�/Ȯ����")]
    [SerializeField] private string baseAddress = "Assets/99. Resources/Practice/SFX/";

    AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
    }

    // ��: PlayByKey("cash") -> "Assets/99. Resources/Practice/SFX/cash.wav"
    public void PlayByKey(string key, float volume = 0.5f, string extension  = ".wav")
    {
        var address = baseAddress + key + extension;

        var op = Addressables.LoadAssetAsync<AudioClip>(address);
        op.Completed += h =>
        {
            src.PlayOneShot(h.Result, volume);
            Addressables.Release(h);
        };
    }

    public void SuccessSound()
    {
        StartCoroutine(OpenSound());
    }

    private IEnumerator OpenSound()
    {
        GameManager.Instance.sound.PlayByKey("trash", 0.5f, ".mp3");

        yield return new WaitForSeconds(0.5f);

        GameManager.Instance.sound.PlayByKey("Success");
    }

}

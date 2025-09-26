using UnityEngine;

public class TriggerImfact : MonoBehaviour
{


    private string playerTag = "Player";
    private float scaleMultiplier = 1.1f; // 얼마나 커질지
    private float scaleDuration = 0.15f;  // 부드럽게 커지는 시간
    private bool revertOnExit = true;     // 빠져나가면 원복

    Vector3 originalScale;
    Coroutine scaleCo;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        StartScale(originalScale * scaleMultiplier);
    }

    void OnTriggerExit(Collider other)
    {
        if (!revertOnExit || !other.CompareTag(playerTag)) return;
        StartScale(originalScale);
    }

    void StartScale(Vector3 target)
    {
        if (scaleCo != null) StopCoroutine(scaleCo);
        scaleCo = StartCoroutine(ScaleRoutine(target, scaleDuration));
    }

    System.Collections.IEnumerator ScaleRoutine(Vector3 target, float duration)
    {
        Vector3 start = transform.localScale;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            transform.localScale = Vector3.Lerp(start, target, u);
            yield return null;
        }
        transform.localScale = target;
        scaleCo = null;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenImfact : MonoBehaviour
{
    [Header("Durations (sec)")]
    [SerializeField] private float toOneTime = 0.20f; // 0 -> 1
    [SerializeField] private float squish1Time = 0.12f; // 1Â÷ Âî±×·¯Áü ¿Õº¹
    [SerializeField] private float squish2Time = 0.12f; // 2Â÷ Âî±×·¯Áü ¿Õº¹
    [SerializeField] private float settleTime = 0.12f; // ¸¶Áö¸· 1,1 º¹±Í

    [Header("Squish Amounts")]
    [SerializeField] private Vector2 squish1 = new Vector2(1.5f, 0.5f);   // 1Â÷: (1.5, 0.5)
    [SerializeField] private Vector2 squish2 = new Vector2(1.25f, 0.75f); // 2Â÷: (1.25, 0.75)

    private readonly List<Coroutine> running = new List<Coroutine>();

    void OnEnable()
    {
        StopAllCoroutines();
        running.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            running.Add(StartCoroutine(AnimateChild(child)));
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
        running.Clear();
    }

    private IEnumerator AnimateChild(Transform t)
    {
        float z = t.localScale.z;

        // ½ÃÀÛ: 0¿¡¼­
        t.localScale = new Vector3(0f, 0f, z);

        // 0 -> (1,1,z)
        yield return TweenScale(t, new Vector3(0f, 0f, z), new Vector3(1f, 1f, z), toOneTime);

        // 1Â÷: (1,1) -> (1.5,0.5) -> (0.5,1.5)
        yield return TweenScale(t, new Vector3(1f, 1f, z), new Vector3(squish1.x, squish1.y, z), squish1Time);
        yield return TweenScale(t, new Vector3(squish1.x, squish1.y, z), new Vector3(squish1.y, squish1.x, z), squish1Time);

        // 2Â÷: (¡¦ ) -> (1.25,0.75) -> (0.75,1.25)
        yield return TweenScale(t, t.localScale, new Vector3(squish2.x, squish2.y, z), squish2Time);
        yield return TweenScale(t, new Vector3(squish2.x, squish2.y, z), new Vector3(squish2.y, squish2.x, z), squish2Time);

        // ¸¶¹«¸®: -> (1,1)
        yield return TweenScale(t, t.localScale, new Vector3(1f, 1f, z), settleTime);
    }

    private IEnumerator TweenScale(Transform t, Vector3 from, Vector3 to, float dur)
    {
        if (dur <= 0f) { t.localScale = to; yield break; }

        float e = 0f;
        while (e < dur)
        {
            float u = e / dur;                // 0..1
            u = Smooth(u);                    // ºÎµå·¯¿î ÀÌÂ¡
            t.localScale = Vector3.LerpUnclamped(from, to, u);
            e += Time.deltaTime;
            yield return null;
        }
        t.localScale = to;
    }

    private float Smooth(float x) => x * x * (3f - 2f * x); // ease-in-out
}

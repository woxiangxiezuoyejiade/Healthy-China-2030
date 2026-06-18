using System.Collections;
using UnityEngine;

public class Game1_TutorialController : MonoBehaviour
{
    public Transform demoForearm;
    public float demoAngle = 45f;
    public float demoDuration = 2.2f;
    public Vector3 demoAxis = Vector3.up;

    Quaternion startRotation;

    void Awake()
    {
        if (demoForearm != null)
        {
            startRotation = demoForearm.localRotation;
        }
    }

    public void PlayDemo()
    {
        if (demoForearm == null) return;

        StopAllCoroutines();
        StartCoroutine(DemoRoutine());
    }

    IEnumerator DemoRoutine()
    {
        while (true)
        {
            yield return RotateTo(demoAngle);
            yield return new WaitForSeconds(0.5f);
            yield return RotateTo(0f);
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator RotateTo(float targetAngle)
    {
        Quaternion from = demoForearm.localRotation;
        Quaternion to = startRotation * Quaternion.AngleAxis(targetAngle, demoAxis.normalized);
        float elapsed = 0f;

        while (elapsed < demoDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / demoDuration);
            demoForearm.localRotation = Quaternion.Slerp(from, to, t);
            yield return null;
        }
    }
}

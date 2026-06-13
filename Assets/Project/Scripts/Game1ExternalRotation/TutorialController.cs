using System.Collections;
using UnityEngine;

public class Game1_TutorialController : MonoBehaviour
{
    public Transform demoForearm;
    public bool autoCreateDemoForearm = false;
    public Vector3 autoDemoCameraOffset = new Vector3(0.30f, -0.22f, 1.05f);
    public float demoAngle = 45f;
    public float demoDuration = 2.2f;
    public Vector3 demoAxis = Vector3.up;

    Transform demoRoot;
    Quaternion startRotation;
    Coroutine demoRoutine;
    bool ownsDemoForearm;

    void Awake()
    {
        EnsureDemoForearm();
        CacheStartRotation();
    }

    public void PlayDemo()
    {
        PlayDemoForSeconds(15f);
    }

    public void PlayDemoForSeconds(float seconds)
    {
        EnsureDemoForearm();
        if (demoForearm == null) return;

        CacheStartRotation();
        if (demoRoot != null) demoRoot.gameObject.SetActive(true);

        if (demoRoutine != null) StopCoroutine(demoRoutine);
        demoRoutine = StartCoroutine(DemoRoutine(seconds));
    }

    public void StopDemoAndReset()
    {
        if (demoRoutine != null)
        {
            StopCoroutine(demoRoutine);
            demoRoutine = null;
        }

        if (demoForearm != null)
        {
            demoForearm.localRotation = startRotation;
        }

        if (ownsDemoForearm && demoRoot != null)
        {
            demoRoot.gameObject.SetActive(false);
        }
    }

    IEnumerator DemoRoutine(float seconds)
    {
        float endTime = Time.time + Mathf.Max(0.1f, seconds);

        while (Time.time < endTime)
        {
            if (demoRoot != null && ownsDemoForearm)
            {
                FollowCamera();
            }

            yield return RotateTo(demoAngle);
            yield return new WaitForSeconds(0.5f);
            yield return RotateTo(0f);
            yield return new WaitForSeconds(0.5f);
        }

        StopDemoAndReset();
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

        demoForearm.localRotation = to;
    }

    void EnsureDemoForearm()
    {
        if (demoForearm != null || !autoCreateDemoForearm) return;

        GameObject root = new GameObject("Game1_Tutorial_DemoArm");
        demoRoot = root.transform;
        ownsDemoForearm = true;
        FollowCamera();

        GameObject pivot = new GameObject("Demo_Forearm_Pivot");
        pivot.transform.SetParent(demoRoot, false);
        pivot.transform.localPosition = Vector3.zero;
        demoForearm = pivot.transform;

        GameObject elbow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        elbow.name = "Demo_Elbow";
        elbow.transform.SetParent(pivot.transform, false);
        elbow.transform.localScale = Vector3.one * 0.075f;

        GameObject forearm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        forearm.name = "Demo_Forearm";
        forearm.transform.SetParent(pivot.transform, false);
        forearm.transform.localPosition = new Vector3(0f, 0f, 0.24f);
        forearm.transform.localScale = new Vector3(0.075f, 0.075f, 0.48f);

        GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hand.name = "Demo_Hand";
        hand.transform.SetParent(pivot.transform, false);
        hand.transform.localPosition = new Vector3(0f, 0f, 0.50f);
        hand.transform.localScale = Vector3.one * 0.10f;

        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.color = new Color(1f, 0.55f, 0.18f, 0.85f);
        SetRendererMaterial(elbow, material);
        SetRendererMaterial(forearm, material);
        SetRendererMaterial(hand, material);

        DestroyCollider(elbow);
        DestroyCollider(forearm);
        DestroyCollider(hand);

        demoRoot.gameObject.SetActive(false);
    }

    void FollowCamera()
    {
        if (demoRoot == null) return;

        if (Camera.main != null)
        {
            demoRoot.SetParent(Camera.main.transform, false);
            demoRoot.localPosition = autoDemoCameraOffset;
            demoRoot.localRotation = Quaternion.identity;
        }
        else
        {
            demoRoot.SetParent(transform, false);
            demoRoot.localPosition = new Vector3(0.35f, -0.15f, 1.0f);
            demoRoot.localRotation = Quaternion.identity;
        }
    }

    void CacheStartRotation()
    {
        if (demoForearm != null)
        {
            startRotation = demoForearm.localRotation;
        }
    }

    void SetRendererMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null) renderer.material = material;
    }

    void DestroyCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
    }
}

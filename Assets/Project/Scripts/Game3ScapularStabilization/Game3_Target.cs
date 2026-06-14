using System.Collections;
using UnityEngine;

/// <summary>
/// 射击目标组件 - 挂在目标物体上，处理命中、倒下/爆炸、重生
/// </summary>
public enum Game3_TargetType
{
    Purple,
    Red,
    White
}

public class Game3_Target : MonoBehaviour
{
    [Header("目标设置")]
    public Game3_TargetType targetType;
    public int baseScore = 10;
    public float respawnTime = 3f;

    public bool IsAlive { get; set; } = true;
    public System.Action<Game3_Target, int> OnHit;

    Renderer targetRenderer;
    Collider targetCollider;
    Vector3 originalScale;
    Vector3 originalPos;
    Quaternion originalRot;

    void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        targetCollider = GetComponent<Collider>();
        originalScale = transform.localScale;
        originalPos = transform.localPosition;
        originalRot = transform.localRotation;
    }

    /// <summary>
    /// 被弹丸命中（带命中点和方向，用于倒下方向）
    /// </summary>
    public void Hit(Vector3 hitPoint, Vector3 hitDir)
    {
        if (!IsAlive) return;
        IsAlive = false;

        // 计算得分
        float distance = 0f;
        if (Camera.main != null)
            distance = Vector3.Distance(Camera.main.transform.position, transform.position);
        int distanceBonus = Mathf.FloorToInt(distance / 5f) * 5;
        int totalScore = baseScore + distanceBonus;

        // 通知得分
        OnHit?.Invoke(this, totalScore);

        // 播放命中音效
        if (Game3_AudioSystem.Instance != null) Game3_AudioSystem.Instance.PlayHit();

        // 命中特效：倒下 + 爆炸碎片
        StartCoroutine(HitAndRespawnRoutine(hitDir, totalScore));
    }

    /// <summary>
    /// 兼容旧的无参数Hit调用
    /// </summary>
    public void Hit()
    {
        Hit(transform.position, Vector3.forward);
    }

    IEnumerator HitAndRespawnRoutine(Vector3 hitDir, int score)
    {
        // ===== 1. 命中闪光 =====
        GameObject flashObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flashObj.name = "HitFlash";
        flashObj.transform.position = transform.position;
        flashObj.transform.localScale = Vector3.one * 0.5f;
        Renderer flashR = flashObj.GetComponent<Renderer>();
        if (flashR != null)
        {
            flashR.material.color = Color.white;
            try { if (flashR.material.HasProperty("_BaseColor")) flashR.material.SetColor("_BaseColor", Color.white); } catch { }
        }
        Collider flashCol = flashObj.GetComponent<Collider>();
        if (flashCol != null) Destroy(flashCol);

        // 闪光点光源
        Light hitLight = flashObj.AddComponent<Light>();
        hitLight.type = LightType.Point;
        hitLight.color = Color.yellow;
        hitLight.intensity = 8f;
        hitLight.range = 8f;

        // 闪光缩小消失
        float flashDur = 0.2f;
        float flashElap = 0f;
        while (flashElap < flashDur)
        {
            flashElap += Time.deltaTime;
            float t = flashElap / flashDur;
            flashObj.transform.localScale = Vector3.one * 0.5f * (1f - t);
            hitLight.intensity = 8f * (1f - t);
            yield return null;
        }
        Destroy(flashObj);

        // ===== 2. 倒下动画 =====
        // 计算倒下方向（被击中的方向）
        Vector3 fallDir = hitDir.normalized;
        // 倒下绕X轴或Z轴旋转（让目标看起来像被推倒）
        Vector3 fallAxis = Vector3.Cross(Vector3.up, fallDir);
        if (fallAxis.sqrMagnitude < 0.01f) fallAxis = Vector3.right;
        fallAxis.Normalize();

        float fallDuration = 0.4f;
        float fallElapsed = 0f;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.AngleAxis(85f, fallAxis);

        while (fallElapsed < fallDuration)
        {
            fallElapsed += Time.deltaTime;
            float t = fallElapsed / fallDuration;
            // 缓出效果
            float eased = 1f - (1f - t) * (1f - t);
            transform.rotation = Quaternion.Slerp(startRot, endRot, eased);
            // 同时缩小
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.5f, eased);
            yield return null;
        }

        // ===== 2. 爆炸碎片 =====
        SpawnExplosion(transform.position);

        // ===== 3. 隐藏目标 =====
        if (targetRenderer != null) targetRenderer.enabled = false;
        if (targetCollider != null) targetCollider.enabled = false;

        // ===== 4. 等待重生 =====
        yield return new WaitForSeconds(respawnTime);

        // ===== 5. 重生 =====
        transform.localScale = originalScale;
        transform.localRotation = originalRot;
        if (targetRenderer != null) targetRenderer.enabled = true;
        if (targetCollider != null) targetCollider.enabled = true;
        IsAlive = true;

        // 重生放大弹跳动画
        float rebirthDuration = 0.4f;
        float rebirthElapsed = 0f;
        while (rebirthElapsed < rebirthDuration)
        {
            rebirthElapsed += Time.deltaTime;
            float t = rebirthElapsed / rebirthDuration;
            // 弹跳曲线：先变大再回到正常
            float bounce = 1f + 0.4f * Mathf.Sin(t * Mathf.PI);
            transform.localScale = originalScale * bounce;
            yield return null;
        }
        transform.localScale = originalScale;
    }

    /// <summary>
    /// 爆炸碎片效果：8个彩色碎片向四周飞散
    /// </summary>
    void SpawnExplosion(Vector3 center)
    {
        StartCoroutine(ExplosionRoutine(center));
    }

    IEnumerator ExplosionRoutine(Vector3 center)
    {
        int count = 12;
        GameObject[] fragments = new GameObject[count];
        Vector3[] velocities = new Vector3[count];
        Vector3[] rotSpeeds = new Vector3[count];

        // 碎片颜色：紫色目标=紫色碎片，红色目标=红色碎片，白色目标=白色碎片
        Color fragColor = targetType == Game3_TargetType.Purple
            ? new Color(0.6f, 0.2f, 1f)
            : targetType == Game3_TargetType.Red
                ? new Color(1f, 0.2f, 0.2f)
                : new Color(0.9f, 0.9f, 0.9f);

        for (int i = 0; i < count; i++)
        {
            // 随机形状：方块或球
            PrimitiveType pType = Random.value > 0.5f ? PrimitiveType.Cube : PrimitiveType.Sphere;
            GameObject frag = GameObject.CreatePrimitive(pType);
            frag.name = "ExplosionFrag";
            frag.transform.localScale = new Vector3(
                Random.Range(0.12f, 0.28f),
                Random.Range(0.12f, 0.28f),
                Random.Range(0.12f, 0.28f)
            );
            frag.transform.position = center + Random.insideUnitSphere * 0.3f;

            // 移除碰撞器
            Collider col = frag.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // 碎片颜色
            Renderer r = frag.GetComponent<Renderer>();
            if (r != null)
            {
                // 随机明暗变化
                Color c = fragColor * Random.Range(0.7f, 1.3f);
                c.a = 1f;
                r.material.color = c;
                try { if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", c); } catch { }
            }

            // 随机飞散方向（主要向上和四周）
            velocities[i] = (Vector3.up * 0.6f + Random.insideUnitSphere).normalized * Random.Range(3f, 7f);
            rotSpeeds[i] = new Vector3(
                Random.Range(200f, 600f),
                Random.Range(200f, 600f),
                Random.Range(200f, 600f)
            );
            fragments[i] = frag;
        }

        // 碎片飞行+重力下落
        float duration = 0.8f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < count; i++)
            {
                if (fragments[i] == null) continue;
                velocities[i] += Vector3.down * 12f * Time.deltaTime; // 重力
                fragments[i].transform.position += velocities[i] * Time.deltaTime;
                fragments[i].transform.Rotate(
                    rotSpeeds[i].x * Time.deltaTime,
                    rotSpeeds[i].y * Time.deltaTime,
                    rotSpeeds[i].z * Time.deltaTime
                );

                // 后半段逐渐缩小
                if (elapsed > duration * 0.5f)
                {
                    float shrinkT = (elapsed - duration * 0.5f) / (duration * 0.5f);
                    fragments[i].transform.localScale *= (1f - shrinkT * 0.05f);
                }
            }
            yield return null;
        }

        // 清理碎片
        for (int i = 0; i < count; i++)
        {
            if (fragments[i] != null) Destroy(fragments[i]);
        }
    }
}

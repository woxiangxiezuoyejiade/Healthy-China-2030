using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 弹射系统 - 从武器枪口发射弹丸，碰撞检测命中目标
/// 弹丸速度慢、体积大、持续时间长，确保肉眼可见完整飞行过程
/// </summary>
public class Game3_ProjectileSystem : MonoBehaviour
{
    [Header("弹射设置")]
    [Tooltip("弹丸速度（单位/秒）")]
    public float projectileSpeed = 12f;
    [Tooltip("弹丸存活时间（秒）")]
    public float projectileLifetime = 4f;
    [Tooltip("发射冷却时间（秒）")]
    public float fireCooldown = 0.25f;
    [Tooltip("弹丸大小")]
    public float projectileSize = 0.5f;
    [Tooltip("弹丸自动命中半径（弹丸中心到目标中心在此范围内算命中）")]
    public float autoHitRadius = 1.8f;

    [Header("引用")]
    public Game3_ShootingSceneBuilder sceneBuilder;

    float lastFireTime = -100f;
    int projectileCount;
    Game3_Target lastHitTarget;  // 记录上次击中的目标，避免连续打同一个
    List<GameObject> activeProjectiles = new List<GameObject>();  // 追踪所有活跃弹丸

    /// <summary>
    /// 停止所有飞行中的弹丸并销毁它们
    /// </summary>
    public void StopAllProjectiles()
    {
        // 停止所有此组件上的协程（FireRoutine）
        StopAllCoroutines();
        // 销毁所有追踪的弹丸GameObject
        foreach (GameObject proj in activeProjectiles)
        {
            if (proj != null) Destroy(proj);
        }
        activeProjectiles.Clear();
        Debug.Log("[ProjectileSystem] All projectiles stopped and cleared.");
    }

    /// <summary>
    /// 发射弹丸，返回是否发射成功
    /// </summary>
    public bool Fire()
    {
        if (sceneBuilder == null) return false;
        if (Time.time - lastFireTime < fireCooldown) return false;

        lastFireTime = Time.time;
        StartCoroutine(FireRoutine());
        return true;
    }

    IEnumerator FireRoutine()
    {
        Transform muzzle = sceneBuilder.WeaponMuzzle;
        Camera mainCam = Camera.main;

        // ===== 0. 先确定发射位置和方向 =====
        Vector3 startPos;
        Vector3 fireDir;

        if (muzzle != null)
        {
            startPos = muzzle.position;
        }
        else if (mainCam != null)
        {
            startPos = mainCam.transform.position + mainCam.transform.forward * 2f;
        }
        else
        {
            startPos = new Vector3(0f, 1.6f, 0f);
        }

        // 发射方向：使用大炮炮管方向（与Weapon的forward一致）
        // 不使用相机方向，避免相机俯角导致弹丸入地
        if (muzzle != null && muzzle.parent != null)
        {
            // 炮管的localRotation = (90, 0, 0)，所以炮管world的forward指向+Z方向
            fireDir = muzzle.parent.forward;
        }
        else if (mainCam != null)
        {
            fireDir = mainCam.transform.forward;
        }
        else
        {
            fireDir = Vector3.forward;
        }

        // 防御性检查：方向不能为零
        if (fireDir.sqrMagnitude < 0.01f) fireDir = Vector3.forward;

        // ===== 0.5 智能瞄准：随机选目标，直接对准，保证命中 =====
        Game3_Target aimTarget = PickRandomTarget(startPos);
        if (aimTarget != null)
        {
            // 直接朝目标方向发射，保证100%命中
            fireDir = (aimTarget.transform.position - startPos).normalized;
            Debug.Log($"[ProjectileSystem] Aim at {aimTarget.name}, fireDir={fireDir}");
        }

        // ===== 1. 创建弹丸实体（实体炮弹形状：头锥+身+尾） =====
        float s = projectileSize;
        GameObject bullet = new GameObject("Projectile_" + projectileCount++);
        bullet.transform.position = startPos;
        bullet.transform.rotation = Quaternion.LookRotation(fireDir);
        bullet.transform.localScale = Vector3.one;
        activeProjectiles.Add(bullet);  // 追踪弹丸
        // 移除碰撞器（用Raycast检测）
        // 实体炮身（不透明圆柱）
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "BulletBody";
        body.transform.SetParent(bullet.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(s, s * 1.8f, s);
        SetObjColor(body, new Color(0.15f, 0.15f, 0.18f));  // 深灰黑
        Collider bodyCol = body.GetComponent<Collider>();
        if (bodyCol != null) Destroy(bodyCol);

        // 头锥（实心锥体）
        GameObject tipCone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tipCone.name = "BulletTip";
        tipCone.transform.SetParent(bullet.transform);
        tipCone.transform.localPosition = new Vector3(0f, 0f, s * 1.0f);
        tipCone.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        tipCone.transform.localScale = new Vector3(s * 0.7f, s * 0.7f, s * 0.7f);
        SetObjColor(tipCone, new Color(0.85f, 0.85f, 0.9f));  // 银色
        Collider tipCol = tipCone.GetComponent<Collider>();
        if (tipCol != null) Destroy(tipCol);

        // 尾翼（4片小方块）
        for (int i = 0; i < 4; i++)
        {
            GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fin.name = "Fin_" + i;
            fin.transform.SetParent(bullet.transform);
            float angle = i * 90f;
            fin.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            fin.transform.localPosition = new Vector3(
                Mathf.Sin(angle * Mathf.Deg2Rad) * s * 0.6f,
                Mathf.Cos(angle * Mathf.Deg2Rad) * s * 0.6f,
                -s * 0.8f
            );
            fin.transform.localScale = new Vector3(s * 0.15f, s * 0.8f, s * 0.3f);
            SetObjColor(fin, new Color(0.3f, 0.3f, 0.35f));
            Collider finCol = fin.GetComponent<Collider>();
            if (finCol != null) Destroy(finCol);
        }

        // 尾火焰（半透明锥体，向后拉长）
        GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flame.name = "BulletFlame";
        flame.transform.SetParent(bullet.transform);
        flame.transform.localPosition = new Vector3(0f, 0f, -s * 1.5f);
        flame.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        flame.transform.localScale = new Vector3(s * 0.4f, s * 0.4f, s * 1.5f);
        SetObjColor(flame, new Color(1f, 0.5f, 0.1f, 0.75f));
        Collider flameCol = flame.GetComponent<Collider>();
        if (flameCol != null) Destroy(flameCol);

        // ===== 4. 尾焰光晕 =====
        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glow.name = "BulletGlow";
        glow.transform.SetParent(bullet.transform);
        glow.transform.localPosition = new Vector3(0f, 0f, -s * 1.5f);
        glow.transform.localScale = new Vector3(s * 1.8f, s * 1.8f, s * 1.8f);
        SetObjColor(glow, new Color(1f, 0.7f, 0.1f, 0.25f));
        Collider glowCol = glow.GetComponent<Collider>();
        if (glowCol != null) Destroy(glowCol);

        // ===== 5. 点光源 =====
        GameObject lightObj = new GameObject("BulletLight");
        lightObj.transform.SetParent(bullet.transform);
        lightObj.transform.localPosition = Vector3.zero;
        Light bulletLight = lightObj.AddComponent<Light>();
        bulletLight.type = LightType.Point;
        bulletLight.color = new Color(1f, 0.6f, 0.2f);
        bulletLight.intensity = 4f;
        bulletLight.range = 6f;

        // ===== 6. 拖尾 =====
        TrailRenderer trail = bullet.AddComponent<TrailRenderer>();
        trail.time = 0.4f;
        trail.startWidth = s * 0.6f;
        trail.endWidth = s * 0.05f;
        trail.minVertexDistance = 0.05f;

        Shader trailShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (trailShader == null) trailShader = Shader.Find("Particles/Standard Unlit");
        if (trailShader == null) trailShader = Shader.Find("Sprites/Default");
        if (trailShader == null) trailShader = Shader.Find("Unlit/Color");
        if (trailShader != null)
        {
            trail.material = new Material(trailShader);
        }
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.7f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0.1f), 1f)
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        trail.colorGradient = gradient;

        Debug.Log($"[ProjectileSystem] Bullet fired from {startPos}, dir={fireDir}, speed={projectileSpeed}");

        // 枪口闪光
        ShowMuzzleFlash();

        // 射击音效
        if (Game3_AudioSystem.Instance != null) Game3_AudioSystem.Instance.PlayShoot();

        // ===== 7. 弹丸飞行 =====
        float elapsed = 0f;
        bool hitSomething = false;
        Vector3 velocity = fireDir * projectileSpeed; // 速度向量（不含重力，直线飞行）

        while (elapsed < projectileLifetime)
        {
            elapsed += Time.deltaTime;
            float moveStep = velocity.magnitude * Time.deltaTime;
            Vector3 dir = velocity.normalized;

            // 1) Raycast检测：精确碰撞
            Ray ray = new Ray(bullet.transform.position, dir);
            RaycastHit[] hits = Physics.RaycastAll(ray, moveStep + 0.5f);

            Game3_Target closestTarget = null;
            float closestDist = float.MaxValue;
            Vector3 closestHitPoint = Vector3.zero;

            foreach (RaycastHit h in hits)
            {
                Game3_Target t = h.collider.GetComponent<Game3_Target>();
                if (t != null && t.IsAlive && h.distance < closestDist)
                {
                    closestTarget = t;
                    closestDist = h.distance;
                    closestHitPoint = h.point;
                }
            }

            // 2) 球形范围检测：兜底命中（弹丸附近autoHitRadius内任何活目标都算命中）
            if (closestTarget == null)
            {
                Collider[] nearby = Physics.OverlapSphere(bullet.transform.position, autoHitRadius);
                foreach (Collider c in nearby)
                {
                    Game3_Target t = c.GetComponent<Game3_Target>();
                    if (t != null && t.IsAlive)
                    {
                        float d = Vector3.Distance(bullet.transform.position, t.transform.position);
                        if (d < closestDist)
                        {
                            closestTarget = t;
                            closestDist = d;
                            closestHitPoint = t.transform.position;
                        }
                    }
                }
            }

            if (closestTarget != null)
            {
                // 命中！弹丸移到命中点再消失
                bullet.transform.position = closestHitPoint;
                closestTarget.Hit(closestHitPoint, dir);
                hitSomething = true;
                lastHitTarget = closestTarget;  // 记录，下次避开
                Debug.Log($"[ProjectileSystem] HIT {closestTarget.name}, score={closestTarget.baseScore}");
                break;
            }

            // 移动弹丸（直线飞行，无重力，姿态稳定）
            bullet.transform.position += velocity * Time.deltaTime;

            yield return null;
        }

        // ===== 8. 弹丸消失（带缩小动画）=====
        if (hitSomething)
        {
            // 命中时弹丸短暂停留再消失（让爆炸效果可见）
            yield return new WaitForSeconds(0.1f);
        }

        float shrinkTime = 0.3f;
        float shrinkElapsed = 0f;
        Vector3 shrinkStart = bullet.transform.localScale;
        while (shrinkElapsed < shrinkTime)
        {
            shrinkElapsed += Time.deltaTime;
            float t = shrinkElapsed / shrinkTime;
            bullet.transform.localScale = Vector3.Lerp(shrinkStart, Vector3.zero, t);
            yield return null;
        }

        Destroy(bullet);
        activeProjectiles.Remove(bullet);
    }

    /// <summary>
    /// 随机选择一个前方的活目标，避免连续击中同一个
    /// </summary>
    Game3_Target PickRandomTarget(Vector3 startPos)
    {
        List<Game3_Target> aliveTargets = new List<Game3_Target>();
        Game3_Target[] allTargets = Object.FindObjectsOfType<Game3_Target>();
        Vector3 cannonForward = sceneBuilder != null && sceneBuilder.WeaponMuzzle != null
            ? sceneBuilder.WeaponMuzzle.parent.forward
            : Vector3.forward;

        foreach (Game3_Target t in allTargets)
        {
            if (t == null || !t.IsAlive) continue;
            Vector3 toTarget = t.transform.position - startPos;
            if (Vector3.Dot(toTarget.normalized, cannonForward) < 0.2f) continue;
            aliveTargets.Add(t);
        }

        if (aliveTargets.Count == 0) return null;

        // 优先避开上次击中的目标
        List<Game3_Target> candidates = new List<Game3_Target>(aliveTargets);
        if (lastHitTarget != null && candidates.Count > 1)
        {
            candidates.Remove(lastHitTarget);
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    void ShowMuzzleFlash()
    {
        if (sceneBuilder == null || sceneBuilder.WeaponMuzzle == null) return;
        Transform flash = sceneBuilder.WeaponMuzzle.Find("MuzzleFlash");
        if (flash == null) return;

        Renderer r = flash.GetComponent<Renderer>();
        if (r != null)
        {
            StartCoroutine(FlashRoutine(r));
        }
    }

    IEnumerator FlashRoutine(Renderer r)
    {
        r.enabled = true;
        yield return new WaitForSeconds(0.15f);
        r.enabled = false;
    }

    void SetObjColor(GameObject obj, Color color)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r != null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Unlit/Color");

            if (sh != null)
            {
                r.material.shader = sh;
            }

            r.material.color = color;
            try
            {
                if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", color);
                if (r.material.HasProperty("_Color")) r.material.SetColor("_Color", color);
            }
            catch { }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 射击行动 - 场景构建器（一比一复刻参考图）
/// </summary>
public class Game3_ShootingSceneBuilder : MonoBehaviour
{
    [Header("控制器引用")]
    public Transform rightController;
    public Transform leftController;

    // ===== 配色（从参考图提取） =====
    static readonly Color COLOR_GROUND        = new Color(0.17f, 0.40f, 0.35f);     // 深墨绿（偏亮一点，确保在灯光下显绿）
    static readonly Color COLOR_WALL          = new Color(0.12f, 0.12f, 0.14f);     // 围墙深灰黑
    static readonly Color COLOR_PILLAR        = new Color(0.92f, 0.92f, 0.92f);      // 白色方柱
    static readonly Color COLOR_OBSTACLE      = new Color(0.92f, 0.92f, 0.92f);      // 金字塔白色方块
    static readonly Color COLOR_TARGET_PURPLE = new Color(0.65f, 0.15f, 0.75f);     // 紫色药丸
    static readonly Color COLOR_TARGET_RED    = new Color(0.80f, 0.10f, 0.12f);     // 红色小目标
    static readonly Color COLOR_WEAPON_BASE   = new Color(0.20f, 0.70f, 0.90f);     // 大炮蓝色底座
    static readonly Color COLOR_WEAPON_BODY   = new Color(0.35f, 0.80f, 0.30f);     // 大炮绿色炮身
    static readonly Color COLOR_MUZZLE_FLASH  = new Color(1f, 0.95f, 0.3f);
    static readonly Color COLOR_TITLE         = new Color(1f, 0.82f, 0f);            // 标题金黄
    static readonly Color COLOR_BTN_BG        = new Color(0.95f, 0.95f, 0.95f);     // 按钮白底
    static readonly Color COLOR_BTN_TEXT      = new Color(0.08f, 0.08f, 0.08f);     // 按钮黑字
    static readonly Color COLOR_SKY           = new Color(0.55f, 0.65f, 0.75f);     // 天空浅蓝灰（与地面区分明显）

    public Transform WeaponMuzzle { get; private set; }
    public List<Game3_Target> AllTargets { get; private set; } = new List<Game3_Target>();

    bool built = false;
    static Game3_ShootingSceneBuilder instance;

    public void AutoBuild()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
        if (built) return;
        built = true;

        FullSceneCleanup();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
            else DestroyImmediate(transform.GetChild(i).gameObject);
        }

        AllTargets.Clear();

        BuildGround();
        BuildWalls();
        BuildPillars();
        BuildPurpleTargets();
        BuildRedTargets();
        BuildPyramids();
        BuildWeapon();
        BuildTitle();
        BuildButtons();
        ConfigureLighting();

        Debug.Log("[Game3] Scene build complete!");
    }

    #region 清理
    void FullSceneCleanup()
    {
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (GameObject obj in Object.FindObjectsOfType<GameObject>(true))
        {
            if (obj == gameObject || obj.transform.IsChildOf(transform)) continue;
            if (IsXRRigOrChild(obj)) continue;
            if (obj.name.Contains("DirectionalLight") && obj.GetComponent<Light>() != null) continue;
            if (obj.GetComponent<MeshRenderer>() != null || obj.GetComponent<SkinnedMeshRenderer>() != null ||
                obj.GetComponent<ParticleSystem>() != null || obj.GetComponent<LineRenderer>() != null)
                toDestroy.Add(obj);
        }
        foreach (GameObject obj in toDestroy)
        {
            if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj);
        }
    }

    bool IsXRRigOrChild(GameObject obj)
    {
        Transform t = obj.transform;
        while (t != null)
        {
            if (t.name != null && (t.name.Contains("XR Origin") || t.name.Contains("XRRig") ||
                t.name.Contains("XR Rig") || t.name.Contains("OVR") || t.name.Contains("Camera Offset") ||
                t.name.Contains("CameraRig") || t.name.StartsWith("[") || t.name.Contains("EventSystem") ||
                t.name.Contains("Controller"))) return true;
            if (t.parent == null) break;
            t = t.parent;
        }
        return false;
    }
    #endregion

    #region 地面
    void BuildGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(transform);
        ground.transform.localPosition = Vector3.zero;
        ground.transform.localScale = new Vector3(5f, 1f, 4f);
        SetColor(ground, COLOR_GROUND);
    }
    #endregion

    #region 围墙（三面）
    void BuildWalls()
    {
        // 后墙
        MakeWall("BackWall", new Vector3(0f, 3f, 24f), new Vector3(52f, 6f, 0.5f));
        // 左墙
        MakeWall("LeftWall", new Vector3(-26f, 3f, 8f), new Vector3(0.5f, 6f, 32f));
        // 右墙
        MakeWall("RightWall", new Vector3(26f, 3f, 8f), new Vector3(0.5f, 6f, 32f));
    }

    void MakeWall(string name, Vector3 pos, Vector3 scale)
    {
        GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = name;
        w.transform.SetParent(transform);
        w.transform.localPosition = pos;
        w.transform.localScale = scale;
        SetColor(w, COLOR_WALL);
        RemoveCollider(w);
    }
    #endregion

    #region 白色方柱（四角）
    void BuildPillars()
    {
        MakePillar(-24f, 3.5f, 0f, 1.6f, 7f);
        MakePillar(24f, 3.5f, 0f, 1.6f, 7f);
        MakePillar(-24f, 3.5f, 20f, 1.6f, 7f);
        MakePillar(24f, 3.5f, 20f, 1.6f, 7f);
    }

    void MakePillar(float x, float y, float z, float w, float h)
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
        p.name = "Pillar";
        p.transform.SetParent(transform);
        p.transform.localPosition = new Vector3(x, y, z);
        p.transform.localScale = new Vector3(w, h, w);
        SetColor(p, COLOR_PILLAR);
        RemoveCollider(p);
    }
    #endregion

    #region 紫色目标（Capsule药丸形，竖立贴地）
    void BuildPurpleTargets()
    {
        // {x, z} 位置列表
        float[][] positions = {
            new float[] { 10f,  5f },
            new float[] {  6f,  9f },
            new float[] { 13f, 10f },
            new float[] { -6f,  8f },
            new float[] {-10f, 12f },
            new float[] {  8f, 14f },
            new float[] { -3f, 16f },
            new float[] { 12f, 17f },
            new float[] {-13f,  5f },
            new float[] {  3f,  6f },
        };

        foreach (float[] pos in positions)
        {
            CreatePillTarget(pos[0], pos[1], 0.35f, 1.2f, COLOR_TARGET_PURPLE,
                Game3_TargetType.Purple, 10, 3f);
        }
    }
    #endregion

    #region 红色目标（小型药丸，远处散布）
    void BuildRedTargets()
    {
        float[][] positions = {
            new float[] {-19f,  8f},
            new float[] {-16f, 13f},
            new float[] { 16f, 13f},
            new float[] { 19f,  8f},
            new float[] {-21f,  5f},
            new float[] { 21f,  5f},
            new float[] { -2f, 10f},
            new float[] {  0f, 18f},
            new float[] { -9f, 18f},
            new float[] {  9f, 18f},
        };

        foreach (float[] pos in positions)
        {
            CreatePillTarget(pos[0], pos[1], 0.22f, 0.75f, COLOR_TARGET_RED,
                Game3_TargetType.Red, 5, 2f);
        }
    }
    #endregion

    #region 金字塔（3座白色方块金字塔，无外框）
    void BuildPyramids()
    {
        float[] xOffsets = { -5f, 0f, 5f };
        float zPos = 14f;

        for (int p = 0; p < xOffsets.Length; p++)
        {
            GameObject pyramid = new GameObject("Pyramid_" + p);
            pyramid.transform.SetParent(transform);
            pyramid.transform.localPosition = new Vector3(xOffsets[p], 0f, zPos);

            int layers = 3;
            float bs = 0.55f;
            for (int layer = 0; layer < layers; layer++)
            {
                int count = layers - layer;
                float startX = -(count - 1) * bs * 0.5f;
                for (int xx = 0; xx < count; xx++)
                {
                    for (int zz = 0; zz < count; zz++)
                    {
                        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        block.transform.SetParent(pyramid.transform);
                        block.transform.localPosition = new Vector3(
                            startX + xx * bs,
                            layer * bs + bs * 0.5f,
                            zz * bs - (count - 1) * bs * 0.5f);
                        block.transform.localScale = Vector3.one * bs * 0.95f;
                        SetColor(block, COLOR_OBSTACLE);

                        Game3_Target tc = block.AddComponent<Game3_Target>();
                        tc.targetType = Game3_TargetType.White;
                        tc.baseScore = 5;
                        tc.respawnTime = 4f;
                        AllTargets.Add(tc);
                    }
                }
            }
        }
    }
    #endregion

    #region 大炮（蓝色竖立胶囊底座 + 绿色方块炮身）
    void BuildWeapon()
    {
        GameObject weapon = new GameObject("Weapon");
        weapon.transform.SetParent(transform);
        weapon.transform.localPosition = new Vector3(-2.5f, 0f, 0.5f);
        // 强制贴地
        Vector3 wp = weapon.transform.position;
        wp.y = 0f;
        weapon.transform.position = wp;
        weapon.transform.localRotation = Quaternion.identity;

        // 蓝色胶囊底座（竖立，像图片中的圆形大底座）
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        baseObj.name = "CannonBase";
        baseObj.transform.SetParent(weapon.transform);
        baseObj.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        baseObj.transform.localRotation = Quaternion.identity; // 竖立
        baseObj.transform.localScale = new Vector3(0.65f, 1.0f, 0.65f);
        SetColor(baseObj, COLOR_WEAPON_BASE);
        RemoveCollider(baseObj);

        // 绿色方块炮身（从底座伸出的绿色长方体）
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "CannonBody";
        body.transform.SetParent(weapon.transform);
        body.transform.localPosition = new Vector3(0f, 0.75f, 0.45f);
        body.transform.localScale = new Vector3(0.40f, 0.40f, 0.70f);
        SetColor(body, COLOR_WEAPON_BODY);
        RemoveCollider(body);

        // 枪口位置
        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(weapon.transform);
        muzzle.transform.localPosition = new Vector3(0f, 0.75f, 1.05f);
        WeaponMuzzle = muzzle.transform;

        // 枪口闪光
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "MuzzleFlash";
        flash.transform.SetParent(muzzle.transform);
        flash.transform.localPosition = Vector3.zero;
        flash.transform.localScale = Vector3.one * 0.25f;
        SetColor(flash, COLOR_MUZZLE_FLASH);
        flash.GetComponent<Renderer>().enabled = false;
    }
    #endregion

    #region 标题（"射击行动"金黄大字）
    void BuildTitle()
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(transform);
        titleObj.transform.localPosition = new Vector3(0f, 4.0f, 10f);

        GameObject textObj = new GameObject("TitleText");
        textObj.transform.SetParent(titleObj.transform);
        textObj.transform.localPosition = Vector3.zero;

        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = "射击行动";
        tm.fontSize = 90;
        tm.color = COLOR_TITLE;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.characterSize = 0.14f;
        tm.fontStyle = FontStyle.Bold;
    }
    #endregion

    #region 按钮（白底黑字）
    void BuildButtons()
    {
        CreateButton("StartButton", "开始游戏", new Vector3(0f, 2.5f, 10f));
        CreateButton("ExitButton", "退出游戏", new Vector3(0f, 1.6f, 10f));
    }

    void CreateButton(string name, string text, Vector3 pos)
    {
        GameObject btn = new GameObject(name);
        btn.transform.SetParent(transform);
        btn.transform.localPosition = pos;

        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bg.name = name + "_BG";
        bg.transform.SetParent(btn.transform);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale = new Vector3(2.4f, 0.55f, 0.25f);
        SetColor(bg, COLOR_BTN_BG);

        BoxCollider coll = bg.GetComponent<BoxCollider>();
        if (coll == null) coll = bg.AddComponent<BoxCollider>();

        GameObject textObj = new GameObject(name + "_Text");
        textObj.transform.SetParent(btn.transform);
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.18f);

        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 50;
        tm.color = COLOR_BTN_TEXT;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.characterSize = 0.072f;
        tm.fontStyle = FontStyle.Bold;
    }
    #endregion

    #region 灯光与天空
    void ConfigureLighting()
    {
        // 主方向光（暖白，强度足够照亮整个场景）
        Light dirLight = FindObjectOfType<Light>();
        if (dirLight == null)
        {
            GameObject lightObj = new GameObject("DirectionalLight");
            lightObj.transform.SetParent(transform);
            dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
        }
        dirLight.color = new Color(1f, 0.97f, 0.92f);  // 暖白光
        dirLight.intensity = 1.8f;                       // 明亮
        dirLight.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

        // 环境光提亮（偏暖绿，让地面显绿）
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.55f);  // 偏绿的环境光

        // 补光点光源（场景中央上方，确保不暗角）
        GameObject fillLightObj = new GameObject("FillLight");
        fillLightObj.transform.SetParent(transform);
        fillLightObj.transform.localPosition = new Vector3(0f, 8f, 10f);
        Light fillLight = fillLightObj.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.color = new Color(0.9f, 0.95f, 1f);
        fillLight.intensity = 1.5f;
        fillLight.range = 40f;

        // 设置所有相机背景色（包括VR相机）
        Camera[] allCams = Object.FindObjectsOfType<Camera>(true);
        foreach (Camera cam in allCams)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = COLOR_SKY;
        }
    }
    #endregion

    #region 目标创建（Capsule药丸形）

    /// <summary>
    /// 创建一个胶囊体目标（天然药丸形状，竖立贴地）
    /// </summary>
    void CreatePillTarget(float x, float z, float radius, float height, Color color,
        Game3_TargetType type, int score, float respawn)
    {
        // 直接用 Capsule 原始类型 —— 天然就是竖立药丸形状
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        target.name = type.ToString() + "_Tgt_" + AllTargets.Count;
        target.transform.SetParent(transform);
        // Capsule 默认高2、沿Y轴竖立，scale.y=height/2 得到实际高度
        target.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        // 贴地：Capsule中心在 height/2 处
        target.transform.localPosition = new Vector3(x, height * 0.5f, z);
        // 强制世界坐标贴地
        Vector3 worldPos = target.transform.position;
        worldPos.y = height * 0.5f;
        target.transform.position = worldPos;
        // 确保不旋转（保持竖立）
        target.transform.localRotation = Quaternion.identity;
        SetColor(target, color);

        Game3_Target tc = target.AddComponent<Game3_Target>();
        tc.targetType = type;
        tc.baseScore = score;
        tc.respawnTime = respawn;
        AllTargets.Add(tc);
    }
    #endregion

    #region 工具方法

    static void SetColor(GameObject go, Color c)
    {
        if (go == null) return;
        Renderer r = go.GetComponent<Renderer>();
        if (r == null || r.material == null) return;
        r.material.color = c;
        try { if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", c); } catch { }
        try { if (r.material.HasProperty("_Color")) r.material.SetColor("_Color", c); } catch { }
    }

    static void RemoveCollider(GameObject go)
    {
        if (go == null) return;
        Collider c = go.GetComponent<Collider>();
        if (c != null) Object.Destroy(c);
    }
    #endregion

    public void SetControllers(Transform right, Transform left)
    {
        rightController = right;
        leftController = left;
    }

    public void EnterGameMode()
    {
        HideUI("Title");
        HideUI("StartButton");
        HideUI("ExitButton");
        ResetWeaponPos();
    }

    public void EnterMenuMode()
    {
        ResetWeaponPos();
        ShowUI("Title");
        ShowUI("StartButton");
        ShowUI("ExitButton");
    }

    void ResetWeaponPos()
    {
        Transform weapon = transform.Find("Weapon");
        if (weapon == null) return;
        if (weapon.parent != transform) weapon.SetParent(transform);
        weapon.transform.localPosition = new Vector3(-2.5f, 0f, 0.5f);
        Vector3 wp = weapon.transform.position;
        wp.y = 0f;
        weapon.transform.position = wp;
        weapon.transform.localRotation = Quaternion.identity;
        weapon.gameObject.SetActive(true);
    }

    void HideUI(string name)
    {
        Transform t = transform.Find(name);
        if (t != null) t.gameObject.SetActive(false);
    }

    void ShowUI(string name)
    {
        Transform t = transform.Find(name);
        if (t != null) t.gameObject.SetActive(true);
    }
}

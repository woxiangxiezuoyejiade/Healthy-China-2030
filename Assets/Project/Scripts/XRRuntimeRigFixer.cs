using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class XRRuntimeRigFixer : MonoBehaviour
{
    [Header("Tracking Origin")]
    public bool fixCameraOffsetHeight = true;
    public string requestedTrackingOriginMode = "Device";
    public float cameraOffsetHeight = 0.85f;
    public bool forceRigGroundY = true;
    public bool forceRigGroundYOnlyOnAndroid = true;
    public float rigGroundY = 0f;
    public bool forceRigCenterOnAndroid = true;
    public Vector2 rigGroundXZ = Vector2.zero;
    public float forceHeightForSeconds = 20f;

    [Header("Controller References")]
    public Transform leftController;
    public Transform rightController;
    public Transform head;

    [Header("Controller Visuals")]
    public bool createControllerVisuals = true;
    public float controllerSphereSize = 0.08f;
    public float rayLength = 4f;
    public float rayWidth = 0.012f;
    public Color leftColor = new Color(0.18f, 0.55f, 1f, 1f);
    public Color rightColor = new Color(1f, 0.48f, 0.18f, 1f);
    public Color rayColor = new Color(0.70f, 0.95f, 1f, 0.82f);

    [Header("UI Click")]
    public bool enableTriggerClick = true;

    [Header("Start Alignment")]
    public bool enableStartAlignment = true;
    public float startTargetForward = 1.08f;
    public float startTargetBelowHead = 0.28f;
    public float startTargetHalfWidth = 0.42f;
    public float startTargetTolerance = 0.36f;
    public float startAlignmentHoldSeconds = 0.35f;
    public float startTargetSize = 0.16f;
    public bool requireExactStartTarget = false;
    public Color startTargetWaitingColor = new Color(1f, 1f, 1f, 0.42f);
    public Color startTargetReadyColor = new Color(0.42f, 1f, 0.70f, 0.72f);

    InputDevice leftDevice;
    InputDevice rightDevice;
    LineRenderer leftRay;
    LineRenderer rightRay;
    bool leftPressedLastFrame;
    bool rightPressedLastFrame;
    float startTime;
    float alignedHoldTime;
    Transform leftStartTarget;
    Transform rightStartTarget;
    Material leftStartTargetMaterial;
    Material rightStartTargetMaterial;
    readonly List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();

    void Awake()
    {
        startTime = Time.time;
        ResolveReferences();
        ApplyTrackingOriginFix();
        EnsureVisuals();
    }

    void Start()
    {
        ApplyTrackingOriginFix();
    }

    void LateUpdate()
    {
        ResolveReferences();
        EnsureVisuals();

        if (fixCameraOffsetHeight && Time.time - startTime <= forceHeightForSeconds)
        {
            ApplyTrackingOriginFix();
        }

        UpdateController(XRNode.LeftHand, ref leftDevice, leftController, leftRay, ref leftPressedLastFrame);
        UpdateController(XRNode.RightHand, ref rightDevice, rightController, rightRay, ref rightPressedLastFrame);
    }

    public void Configure(Game1_ActionDetector detector, Game1_HandGuideVisual guideVisual)
    {
        ResolveReferences();

        if (detector != null)
        {
            if (detector.leftController == null) detector.leftController = leftController;
            if (detector.rightController == null) detector.rightController = rightController;
            if (detector.head == null) detector.head = head;
        }

        if (guideVisual != null)
        {
            if (guideVisual.leftController == null) guideVisual.leftController = leftController;
            if (guideVisual.rightController == null) guideVisual.rightController = rightController;
            if (guideVisual.head == null) guideVisual.head = head;
        }
    }

    public void Configure(Game2_ActionDetector detector, Game2_HandGuideVisual guideVisual)
    {
        ResolveReferences();

        if (detector != null)
        {
            if (detector.leftController == null) detector.leftController = leftController;
            if (detector.rightController == null) detector.rightController = rightController;
            if (detector.head == null) detector.head = head;
        }

        if (guideVisual != null)
        {
            if (guideVisual.leftController == null) guideVisual.leftController = leftController;
            if (guideVisual.rightController == null) guideVisual.rightController = rightController;
        }
    }

    void ResolveReferences()
    {
        if (head == null && Camera.main != null) head = Camera.main.transform;
        if (leftController == null) leftController = FindTransformByName("Left Controller");
        if (rightController == null) rightController = FindTransformByName("Right Controller");
        if (leftController != null && !leftController.gameObject.activeSelf) leftController.gameObject.SetActive(true);
        if (rightController != null && !rightController.gameObject.activeSelf) rightController.gameObject.SetActive(true);
    }

    void ApplyTrackingOriginFix()
    {
        Transform cameraOffset = FindTransformByName("Camera Offset");
        if (cameraOffset != null)
        {
            Vector3 localPosition = cameraOffset.localPosition;
            localPosition.y = cameraOffsetHeight;
            cameraOffset.localPosition = localPosition;
        }

        GameObject xrRig = GameObject.Find("XRRig");
        if (xrRig == null) return;

        bool shouldForceRigY = forceRigGroundY &&
                               (!forceRigGroundYOnlyOnAndroid || Application.platform == RuntimePlatform.Android);
        bool shouldForceRigXZ = forceRigCenterOnAndroid && Application.platform == RuntimePlatform.Android;
        if (shouldForceRigY || shouldForceRigXZ)
        {
            Vector3 rigPosition = xrRig.transform.position;
            if (shouldForceRigY) rigPosition.y = rigGroundY;
            if (shouldForceRigXZ)
            {
                rigPosition.x = rigGroundXZ.x;
                rigPosition.z = rigGroundXZ.y;
            }
            xrRig.transform.position = rigPosition;
        }

        Component[] components = xrRig.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null || component.GetType().Name != "XROrigin") continue;

            TrySetProperty(component, "CameraYOffset", cameraOffsetHeight);
            TrySetTrackingOriginMode(component, requestedTrackingOriginMode);
        }

        SubsystemManager.GetSubsystems(inputSubsystems);
        foreach (XRInputSubsystem subsystem in inputSubsystems)
        {
            if (subsystem != null && subsystem.running)
            {
                subsystem.TrySetTrackingOriginMode(GetTrackingOriginFlags());
            }
        }
    }

    public bool IsAnyConfirmPressed()
    {
        EnsureDevices();
        return IsControllerPressed(leftDevice) || IsControllerPressed(rightDevice);
    }

    public bool IsConfirmPressed(XRNode node)
    {
        EnsureDevices();
        return node == XRNode.LeftHand ? IsControllerPressed(leftDevice) : IsControllerPressed(rightDevice);
    }

    public void SetStartAlignmentVisible(bool visible)
    {
        EnsureStartTargets();
        if (leftStartTarget != null) leftStartTarget.gameObject.SetActive(visible && enableStartAlignment);
        if (rightStartTarget != null) rightStartTarget.gameObject.SetActive(visible && enableStartAlignment);
        if (!visible) alignedHoldTime = 0f;
    }

    public bool TickStartAlignment(float deltaTime, out float progress)
    {
        progress = 1f;
        if (!enableStartAlignment || leftController == null || rightController == null || head == null)
        {
            return true;
        }

        EnsureStartTargets();
        UpdateStartTargetPositions();

        bool leftReady = Vector3.Distance(leftController.position, leftStartTarget.position) <= startTargetTolerance;
        bool rightReady = Vector3.Distance(rightController.position, rightStartTarget.position) <= startTargetTolerance;
        bool confirmReady = IsAnyConfirmPressed();
        bool ready = confirmReady && (!requireExactStartTarget || (leftReady && rightReady));

        alignedHoldTime = ready
            ? alignedHoldTime + deltaTime
            : 0f;

        progress = Mathf.Clamp01(alignedHoldTime / Mathf.Max(0.05f, startAlignmentHoldSeconds));
        UpdateStartTargetMaterial(leftStartTargetMaterial, leftReady);
        UpdateStartTargetMaterial(rightStartTargetMaterial, rightReady);
        return progress >= 1f;
    }

    void EnsureDevices()
    {
        if (!leftDevice.isValid) leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (!rightDevice.isValid) rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    void UpdateController(XRNode node, ref InputDevice device, Transform target, LineRenderer ray, ref bool pressedLastFrame)
    {
        if (target == null) return;

        if (!device.isValid)
        {
            device = InputDevices.GetDeviceAtXRNode(node);
        }

        if (device.isValid)
        {
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
            {
                target.localPosition = position;
            }

            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                target.localRotation = rotation;
            }
        }

        float visibleRayLength = rayLength;
        bool pressed = IsControllerPressed(device);
        Button hoveredButton = enableTriggerClick ? FindButtonOnRay(new Ray(target.position, target.forward), out visibleRayLength) : null;

        if (enableTriggerClick && pressed && !pressedLastFrame && hoveredButton != null)
        {
            hoveredButton.onClick.Invoke();
        }

        pressedLastFrame = pressed;
        UpdateRay(ray, visibleRayLength, hoveredButton != null);
    }

    void EnsureStartTargets()
    {
        if (!enableStartAlignment) return;

        if (leftStartTarget == null)
        {
            leftStartTarget = CreateStartTarget("Runtime_Left_Start_Target", out leftStartTargetMaterial);
        }

        if (rightStartTarget == null)
        {
            rightStartTarget = CreateStartTarget("Runtime_Right_Start_Target", out rightStartTargetMaterial);
        }
    }

    Transform CreateStartTarget(string objectName, out Material material)
    {
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        target.name = objectName;
        target.transform.localScale = Vector3.one * startTargetSize;

        Collider collider = target.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        material = CreateTransparentMaterial(objectName + "_Mat", startTargetWaitingColor);
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null) renderer.material = material;

        target.SetActive(false);
        return target.transform;
    }

    void UpdateStartTargetPositions()
    {
        if (head == null || leftStartTarget == null || rightStartTarget == null) return;

        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 center = head.position + forward * startTargetForward - Vector3.up * startTargetBelowHead;

        leftStartTarget.position = center - right * startTargetHalfWidth;
        rightStartTarget.position = center + right * startTargetHalfWidth;
    }

    void UpdateStartTargetMaterial(Material material, bool ready)
    {
        if (material == null) return;

        Color color = ready ? startTargetReadyColor : startTargetWaitingColor;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.color = color;
    }

    bool IsControllerPressed(InputDevice device)
    {
        if (!device.isValid) return false;

        if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed) return true;
        if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryPressed) && primaryPressed) return true;
        if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryPressed) && secondaryPressed) return true;
        if (device.TryGetFeatureValue(CommonUsages.menuButton, out bool menuPressed) && menuPressed) return true;
        if (device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool stickClick) && stickClick) return true;
        if (device.TryGetFeatureValue(CommonUsages.secondary2DAxisClick, out bool stickClick2) && stickClick2) return true;
        if (device.TryGetFeatureValue(CommonUsages.gripButton, out bool gripPressed) && gripPressed) return true;
        return false;
    }

    Button FindButtonOnRay(Ray ray, out float hitDistance)
    {
        hitDistance = rayLength;
        Button closestButton = null;
        float closestDistance = rayLength;

        Button[] buttons = FindObjectsOfType<Button>(false);
        foreach (Button button in buttons)
        {
            if (button == null || !button.isActiveAndEnabled || !button.interactable) continue;

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null) continue;

            if (TryRaycastRect(ray, rect, out float distance) && distance < closestDistance)
            {
                closestDistance = distance;
                closestButton = button;
            }
        }

        if (closestButton != null)
        {
            hitDistance = Mathf.Clamp(closestDistance, 0.05f, rayLength);
        }

        return closestButton;
    }

    bool TryRaycastRect(Ray ray, RectTransform rectTransform, out float distance)
    {
        distance = 0f;

        Plane plane = new Plane(rectTransform.forward, rectTransform.position);
        if (!plane.Raycast(ray, out distance)) return false;
        if (distance < 0f || distance > rayLength) return false;

        Vector3 worldPoint = ray.GetPoint(distance);
        Vector3 localPoint = rectTransform.InverseTransformPoint(worldPoint);
        Rect rect = rectTransform.rect;
        return rect.Contains(new Vector2(localPoint.x, localPoint.y));
    }

    void EnsureVisuals()
    {
        if (!createControllerVisuals) return;

        if (leftController != null)
        {
            EnsureControllerSphere(leftController, "Runtime_Left_Controller_Visual", leftColor);
            leftRay = EnsureControllerRay(leftController, "Runtime_Left_Controller_Ray");
        }

        if (rightController != null)
        {
            EnsureControllerSphere(rightController, "Runtime_Right_Controller_Visual", rightColor);
            rightRay = EnsureControllerRay(rightController, "Runtime_Right_Controller_Ray");
        }
    }

    void EnsureControllerSphere(Transform parent, string objectName, Color color)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null) return;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = objectName;
        sphere.transform.SetParent(parent, false);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localRotation = Quaternion.identity;
        sphere.transform.localScale = Vector3.one * controllerSphereSize;

        Collider collider = sphere.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        Renderer renderer = sphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = CreateMaterial(objectName + "_Mat", color);
        }
    }

    LineRenderer EnsureControllerRay(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        LineRenderer line = existing != null ? existing.GetComponent<LineRenderer>() : null;
        if (line != null) return line;

        GameObject rayObject = new GameObject(objectName);
        rayObject.transform.SetParent(parent, false);
        rayObject.transform.localPosition = Vector3.zero;
        rayObject.transform.localRotation = Quaternion.identity;

        line = rayObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.startWidth = rayWidth;
        line.endWidth = rayWidth * 0.45f;
        line.numCapVertices = 4;
        line.material = CreateMaterial(objectName + "_Mat", rayColor);
        UpdateRay(line, rayLength, false);
        return line;
    }

    void UpdateRay(LineRenderer line, float length, bool hoveringButton)
    {
        if (line == null) return;

        line.enabled = true;
        line.startColor = hoveringButton ? Color.white : rayColor;
        line.endColor = hoveringButton ? new Color(1f, 1f, 1f, 0.25f) : new Color(rayColor.r, rayColor.g, rayColor.b, 0.15f);
        line.SetPosition(0, Vector3.zero);
        line.SetPosition(1, Vector3.forward * Mathf.Max(0.05f, length));
    }

    Material CreateMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.name = materialName;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.color = color;
        return material;
    }

    Material CreateTransparentMaterial(string materialName, Color color)
    {
        Material material = CreateMaterial(materialName, color);
        material.renderQueue = 3000;
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_ALPHABLEND_ON");
        return material;
    }

    Transform FindTransformByName(string objectName)
    {
        GameObject[] objects = FindObjectsOfType<GameObject>(true);
        foreach (GameObject obj in objects)
        {
            if (obj.name == objectName) return obj.transform;
        }

        return null;
    }

    void TrySetProperty(Component component, string propertyName, object value)
    {
        try
        {
            var property = component.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(component, value);
            }
        }
        catch (Exception)
        {
        }
    }

    void TrySetTrackingOriginMode(Component component, string modeName)
    {
        try
        {
            var property = component.GetType().GetProperty("RequestedTrackingOriginMode");
            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum) return;

            object mode = Enum.Parse(property.PropertyType, modeName);
            property.SetValue(component, mode);
        }
        catch (Exception)
        {
        }
    }

    TrackingOriginModeFlags GetTrackingOriginFlags()
    {
        return string.Equals(requestedTrackingOriginMode, "Floor", StringComparison.OrdinalIgnoreCase)
            ? TrackingOriginModeFlags.Floor
            : TrackingOriginModeFlags.Device;
    }
}


using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Fun1AvatarInstaller
{
    [MenuItem("Tools/Healthy China/Fun1/Install Block Avatar")]
    public static void InstallBlockAvatar()
    {
        Game1_ExternalRotationController controller = Object.FindObjectOfType<Game1_ExternalRotationController>(true);
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Fun1 Block Avatar", "没有找到 Game1_ExternalRotationController。请先打开 Func1 场景。", "OK");
            return;
        }

        Undo.RecordObject(controller, "Install Fun1 Block Avatar");

        Game1_AvatarVisual avatar = controller.GetComponent<Game1_AvatarVisual>();
        if (avatar == null)
        {
            avatar = Undo.AddComponent<Game1_AvatarVisual>(controller.gameObject);
        }

        Undo.RecordObject(avatar, "Configure Fun1 Block Avatar");
        avatar.avatarOffset = new Vector3(0.52f, -0.26f, 1.45f);
        avatar.avatarScale = 0.22f;
        avatar.followCamera = true;
        avatar.tutorialCycleTime = 3.0f;
        avatar.alternateHandsInTutorial = true;
        avatar.tutorialVisualAngle = 74f;
        avatar.smoothSpeed = 12f;
        avatar.bodyAlpha = 0.88f;
        avatar.activeArmAlpha = 1f;
        avatar.inactiveArmAlpha = 0.72f;
        avatar.showLegs = false;
        avatar.deleteLegacyCapsuleObject = true;

        controller.avatarVisual = avatar;

        GameObject capsule = GameObject.Find("Capsule");
        if (capsule != null && capsule != controller.gameObject)
        {
            Undo.DestroyObjectImmediate(capsule);
        }

        DeleteGeneratedAvatarRoot("Game1_BlockAvatarRoot");
        DeleteGeneratedAvatarRoot("HologramRoot");

        EditorUtility.SetDirty(avatar);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Selection.activeGameObject = controller.gameObject;

        EditorUtility.DisplayDialog(
            "Fun1 Block Avatar",
            "已经安装新版小体积积木小人。运行 Func1 时，小人的当前训练手前臂会跟随动作旋转，旧的 Capsule 已删除。",
            "OK");
    }

    static void DeleteGeneratedAvatarRoot(string objectName)
    {
        GameObject generated = GameObject.Find(objectName);
        if (generated != null)
        {
            Undo.DestroyObjectImmediate(generated);
        }
    }
}

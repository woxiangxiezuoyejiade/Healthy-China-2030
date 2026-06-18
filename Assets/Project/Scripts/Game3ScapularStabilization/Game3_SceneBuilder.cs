using UnityEngine;

/// <summary>
/// 射击行动 - 一键场景构建器
/// 挂到空GameObject上，运行时自动生成整个Game3场景
/// </summary>
public class Game3_SceneBuilder : MonoBehaviour
{
    [Header("游戏设置")]
    public string mainMenuSceneName = "MainMenu";

    void Awake()
    {
        // 确保Controller存在
        Game3_Controller controller = gameObject.GetComponent<Game3_Controller>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<Game3_Controller>();
        }

        controller.mainMenuSceneName = mainMenuSceneName;

        EnsureSingleAudioListener();

        Debug.Log("[Game3_SceneBuilder] 场景自动搭建完成！");
    }

    void EnsureSingleAudioListener()
    {
        AudioListener[] listeners = Object.FindObjectsOfType<AudioListener>(true);
        if (listeners.Length <= 1) return;

        AudioListener primary = null;
        foreach (var listener in listeners)
        {
            if (listener.GetComponent<Camera>() != null) { primary = listener; break; }
        }
        if (primary == null && listeners.Length > 0) primary = listeners[0];

        foreach (var listener in listeners)
        {
            if (listener != primary) Destroy(listener);
        }
    }
}

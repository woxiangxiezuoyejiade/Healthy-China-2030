using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI����")]
    public TextMeshProUGUI healthScoreText;
    public Slider healthBar;
    public TextMeshProUGUI feedbackText;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // ����GameManager�Ļ��ֱ仯�¼�
        GameManager.Instance.OnPointsChanged += UpdateHealthScore;
        GameManager.Instance.OnTargetReached += ShowSuccessMessage;
    }

    // ���»�����ʾ
    public void UpdateHealthScore(int points)
    {
        GameManager.Instance.OnPointsChanged += UpdateHealthScore;
        GameManager.Instance.OnTargetReached += ShowSuccessMessage;
        healthScoreText.text = $"��������: {points}";
        healthBar.value = GameManager.Instance.GetProgress();
    }

    // ������ʾ����
    public void UpdateFeedback(string message)
    {
        feedbackText.text = message;
    }

    void ShowSuccessMessage()
    {
        feedbackText.text = "Ŀ���ɣ������й�����+1";
        feedbackText.color = Color.green;
    }
}
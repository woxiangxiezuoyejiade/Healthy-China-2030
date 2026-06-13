using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Global Score")]
    public int totalScore = 0;

    [Header("Legacy Session Score")]
    public int healthPoints = 0;
    public int sessionTarget = 100;
    public bool isGameActive = false;

    public event System.Action<int> OnTotalScoreChanged;
    public event System.Action<int> OnPointsChanged;
    public event System.Action OnTargetReached;
    public event System.Action OnGameStarted;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        healthPoints = 0;
        isGameActive = true;
        OnGameStarted?.Invoke();
        OnPointsChanged?.Invoke(healthPoints);
    }

    public void AddPoints(int points)
    {
        if (!isGameActive) return;

        healthPoints += points;
        OnPointsChanged?.Invoke(healthPoints);

        if (healthPoints >= sessionTarget)
        {
            isGameActive = false;
            OnTargetReached?.Invoke();
        }
    }

    public float GetProgress()
    {
        return Mathf.Clamp01((float)healthPoints / sessionTarget);
    }

    public void addScore(int score)
    {
        if (score <= 0) return;

        totalScore += score;
        OnTotalScoreChanged?.Invoke(totalScore);
    }

    public void AddScore(int score)
    {
        addScore(score);
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.Rendering.PostProcessing;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private String state;
    public int CurrentPlayer { get { return currentPlayer; } }
    public String State { get { return state; } }

    [Header("Start Scene UI Elemente")]
    public GameObject startScene;
    public Button startButton;

    [Header("Game Scene UI Elemente")]
    public GameObject gameScene;
    public Text roundTimerText;
    public Button finishedButton;
    private float roundTime = 120f;
    private bool isRoundActive = true;
    private bool isGameStarted = false;
    private bool isGameFinished = false;

    [Header("Transformation")]
    public Button transformButton;

    [Header("Break Scene UI Elemente")]
    public GameObject breakScene;
    public Button resumeButton;
    public Button quitButton;
    private bool isPaused = false;

    [Header("Win Scene UI")]
    public GameObject winScene;
    public Button ReturnToMain;
    public TMP_Text winnerText;

    [Header("Spieler Kameras")]
    public Camera player1Camera;
    public Camera player2Camera;
    private int currentPlayer = 1;

    void Start()
    {
        state = "StartMenu";
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        startScene.SetActive(true);
        gameScene.SetActive(false);
        breakScene.SetActive(false);
        winScene.SetActive(false);
        Time.timeScale = 0f;

        player1Camera.enabled = true;
        player2Camera.enabled = false;

        roundTimerText.gameObject.SetActive(false);
        transformButton.gameObject.SetActive(false);

        startButton.onClick.AddListener(StartGame);
        resumeButton.onClick.AddListener(ResumeGame);
        quitButton.onClick.AddListener(QuitGame);
        finishedButton.onClick.AddListener(MoveFinished);
        ReturnToMain.onClick.AddListener(QuitGame);
    }

    void Update()
    {
        if (isGameStarted && roundTime > 0)
        {
            roundTime -= Time.deltaTime;
            UpdateRoundTimerText();

            if (roundTime <= 0)
            {
                roundTime = 0;
                Debug.Log("Rundenzeit abgelaufen.");
                MoveFinished();
            }
        }

        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void StartGame()
    {
        state = "GameRun";
        startScene.SetActive(false);
        gameScene.SetActive(true);
        Time.timeScale = 1f;
        isGameStarted = true;
        isPaused = true;

        roundTimerText.gameObject.SetActive(true);
        transformButton.gameObject.SetActive(true);

        Debug.Log("Runden-Timer und Transformations-Button aktiviert, state: " + state);

        transformButton.onClick.AddListener(GenerateBoard.Instance.TransformPiece);
    }

    public void UpdateRoundTimerText()
    {
        int minutes = Mathf.FloorToInt(roundTime / 60);
        int seconds = Mathf.FloorToInt(roundTime % 60);
        roundTimerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void MoveFinished()
    {
        Debug.Log($"Der Spieler {currentPlayer} hat vor der Zeit sein Spielzug beendet!");
        roundTime = 120f;
        isRoundActive = true;

        // Setze die angehobene Figur zurück
        GenerateBoard.Instance.ResetDraggingPiece();

        // Setze die Flags zurück
        GenerateBoard.Instance.hasMoved = false;
        GenerateBoard.Instance.hasTransformed = false; // Setze das Transformations-Flag zurück

        // Wechsle den Spieler
        SwitchPlayer();
    }

    public void PauseGame()
    {
        state = "PauseMenu";
        breakScene.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        Debug.Log("Spiel pausiert. State: " + state);
    }

    public void WinGame(String winTeam)
    {
        state = "Win";
        winnerText.SetText(winTeam + " team won!");
        gameScene.SetActive(false);
        breakScene.SetActive(false);

        Time.timeScale = 0f;
        winScene.SetActive(true);
    }

    public void ResumeGame()
    {
        state = "GameRun";
        breakScene.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        Debug.Log("Spiel fortgesetzt.");
    }

    public void QuitGame()
    {
        state = "StartMenu";
        Debug.Log("Zurück zum Startbildschirm, State :" + state);

        gameScene.SetActive(false);
        breakScene.SetActive(false);
        winScene.SetActive(false);

        startScene.SetActive(true);

        isGameStarted = false;
        isPaused = false;
        roundTime = 120f;
        Time.timeScale = 0f;

        roundTimerText.gameObject.SetActive(false);
        transformButton.gameObject.SetActive(false);
    }

    public void SwitchPlayer()
    {
        currentPlayer = (currentPlayer == 1) ? 2 : 1;
        UpdateCamera();
    }

    private void UpdateCamera()
    {
        if (currentPlayer == 1)
        {
            player1Camera.enabled = true;
            player2Camera.enabled = false;
        }
        else
        {
            player1Camera.enabled = false;
            player2Camera.enabled = true;
        }
    }
}
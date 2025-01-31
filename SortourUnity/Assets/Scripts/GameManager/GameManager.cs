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
    //private bool isGameFinished = false;    --> AUSKOMMENTIERT: keine Verwendung

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
    private GenerateBoard board;

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

        ResetGame(); // Reset the game state when the StartScene is loaded

        startScene.SetActive(true);
        gameScene.SetActive(false);
        breakScene.SetActive(false);
        winScene.SetActive(false);
        Time.timeScale = 0f;

        player1Camera.enabled = true;
        player2Camera.enabled = false;

        roundTimerText.gameObject.SetActive(false);
        transformButton.gameObject.SetActive(false);
        // startButton.gameObject.SetActive(true);         //  --> Neu Hinzugefügt

        SetInitialCamera();

        if (finishedButton != null)
        {
            finishedButton.onClick.RemoveAllListeners();
            finishedButton.onClick.AddListener(MoveFinished);
        }

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

        if (isRoundActive)
        {
            if (roundTime <= 0)
            {
                Debug.Log($"Zeit abgelaufen! Spieler {currentPlayer} wird automatisch gewechselt.");
                MoveFinished();             //  Spielzug beendet und Spieler wechsel wird aufgerufen
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
        isPaused = false;

        roundTimerText.gameObject.SetActive(true);
        transformButton.gameObject.SetActive(true);
        finishedButton.gameObject.SetActive(true);

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

        finishedButton.gameObject.SetActive(false);
        transformButton.gameObject.SetActive(false);
    }

    public void WinGame(String winTeam)
    {
        state = "Win";

        //  Formatierung der Farbe hinzugefügt, für das Gewinner Team
        if (winTeam == "Black")
        {
            winnerText.text = $"<color=black>{winTeam}</color> Team won!";
        }
        else if (winTeam == "White")
        {
            winnerText.text = $"{winTeam} Team won!";
        }

        gameScene.SetActive(false);
        breakScene.SetActive(false);

        Time.timeScale = 0f;
        winScene.SetActive(true);
    }

    public void ResumeGame()
    {
        state = "GameRun";
        breakScene.SetActive(false);
        Time.timeScale = 1f;                // Zeit fortsetzen
        isPaused = false;

        finishedButton.gameObject.SetActive(true);
        transformButton.gameObject.SetActive(true);

        Debug.Log("Spiel fortgesetzt.");    //  Debug Information 
    }

    public void QuitGame()
    {
        state = "StartMenu";
        Debug.Log("Zurück zum Startbildschirm, State :" + state);

        // Deaktiviere die Game Scene und das Pausenmenü
        gameScene.SetActive(false);
        breakScene.SetActive(false);
        winScene.SetActive(false);

        // Aktiviere das Startmenü
        startScene.SetActive(true);

        // Spielzustände zurücksetzen
        ResetGame();
    }

    private void ResetGame()
    {
        isGameStarted = false;
        isPaused = false;
        roundTime = 120f; // Timer zurücksetzen
        Time.timeScale = 0f; // Zeit anhalten

        // Blende das Timer-Textfeld und den Transformations-Button aus
        roundTimerText.gameObject.SetActive(false);
        transformButton.gameObject.SetActive(false);

        // Setze den currentPlayer auf 1 zurück, damit das Spiel mit der Player1Camera beginnt
        currentPlayer = 1;
        SetInitialCamera();     //  Stellt sicher das die Kamera zurückgesetzt wird
    }

    private void SetInitialCamera()
    {
        if (player1Camera != null) player1Camera.enabled = true;
        if (player2Camera != null) player2Camera.enabled = false;

        if (board != null)
        {
            board.SetCamera(currentPlayer);
        }
    }

    public void SwitchPlayer()
    {
        currentPlayer = (currentPlayer == 1) ? 2 : 1;

        //board.SetCamera(currentPlayer);  --> AUSKOMMENTIERT: Damit der Finish Button funktioniert
        if (currentPlayer == 1)
        {
            if (player1Camera != null) player1Camera.enabled = true;
            if (player2Camera != null) player2Camera.enabled = false;
        }
        if (currentPlayer == 2)
        {
            if (player1Camera != null) player1Camera.enabled = false;
            if (player2Camera != null) player2Camera.enabled = true;
        }
        Debug.Log("current player after switch Player: " + currentPlayer);
    }
}
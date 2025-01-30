using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.Rendering.PostProcessing;
using TMPro;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Zustandsverwaltung mit Enum (Punkt 2)
    public enum GameState
    {
        StartMenu,
        GameRun,
        PauseMenu,
        Win
    }

    private GameState state;
    public int CurrentPlayer { get { return currentPlayer; } private set { } }
    public GameState State { get { return state; } }

    //  ####    Start Scene UI_Elemente  ###
    [Header("Start Scene UI Elemente")]
    public GameObject startScene;           // Referenz zum Startmenü UI
    public Button startButton;              // Button, um das Spiel zu starten

    //  ####    Game Scene UI_Elemente  ####
    [Header("Game Scene UI Elemente")]
    public GameObject gameScene;
    public Text roundTimerText;             // UI-Textfeld für den Runden-Timer
    public Button finishedButton;           // Button um vor der Zeit seinen Spielzug zu beenden 
    private float roundTime = 120f;         // 2 Minuten in Sekunden
    private bool isRoundActive = true;
    private bool isGameStarted = false;     // Bool, um zu überprüfen, ob das Spiel gestartet ist
    private bool isGameFinished = false;    // Bool, um zu überprüfen, ob der Spielzug früher beendet wurde

    //  ####    Transformation  ####
    public Button transformButton;          // Button für die Figuren-Transformation

    //  ####    Break Scene UI_Elemente     ####
    [Header("Break Scene UI Elemente")]
    public GameObject breakScene;
    public Button resumeButton;
    public Button quitButton;
    private bool isPaused = false;          // Bool, um zu überprüfen, ob das Spiel sich in Pause befindet

    // Win Scene UI
    [Header("Win Scene UI")]
    public GameObject winScene;
    public Button ReturnToMain;
    public TMP_Text winnerText;

    //  ####    Spieler 1 & 2 hinzufügen    ####
    [Header("Spieler Kameras")]
    public Camera player1Camera;
    public Camera player2Camera;
    private int currentPlayer = 1;          // 1 für Spieler 1, 2 für Spieler 2  --> INFO: es muss bei 1 angefangen werden
    private GenerateBoard board;

    // Event-System für Button-Listener (Punkt 5)
    [SerializeField] private UnityEvent onGameStart;
    [SerializeField] private UnityEvent onGamePause;
    [SerializeField] private UnityEvent onGameResume;
    [SerializeField] private UnityEvent onGameQuit;

    void Start()
    {
        Initialize(); // Initialisierung (Punkt 7)
    }

    private void Initialize()
    {
        state = GameState.StartMenu;
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        // Startmenü anzeigen & Game/Break Scene zu beginn deaktivieren
        SetUIState(true, false, false, false); // UI-Verwaltung (Punkt 4)
        Time.timeScale = 0f;

        // Spieler Camera auf true oder false setzen
        SetCameraState(player1Camera, player2Camera); // Redundanz vermeiden (Punkt 1)

        // Transformations-Button und Timer ausblenden, bis das Spiel gestartet ist
        roundTimerText.gameObject.SetActive(false);
        transformButton.gameObject.SetActive(false);

        board = FindFirstObjectByType<GenerateBoard>();

        SetInitialCamera();

        // Event-Listener hinzufügen (Punkt 5)
        AddButtonListeners();
    }

    private void AddButtonListeners()
    {
        if (finishedButton != null)
        {
            finishedButton.onClick.RemoveAllListeners();
            finishedButton.onClick.AddListener(MoveFinished);
        }

        startButton.onClick.AddListener(StartGame);
        resumeButton.onClick.AddListener(ResumeGame);
        quitButton.onClick.AddListener(QuitGame);
        ReturnToMain.onClick.AddListener(QuitGame);

        // Event-System verwenden
        onGameStart.AddListener(StartGame);
        onGamePause.AddListener(PauseGame);
        onGameResume.AddListener(ResumeGame);
        onGameQuit.AddListener(QuitGame);
    }

    void Update()
    {
        // Timer-Logik optimiert (Punkt 3)
        if (isGameStarted && !isPaused && roundTime > 0)
        {
            roundTime -= Time.deltaTime;
            UpdateRoundTimerText();

            if (roundTime <= 0)
            {
                roundTime = 0;
                Log("Rundenzeit abgelaufen."); // Debug-Ausgaben optimiert (Punkt 6)
                MoveFinished();
            }
        }

        // Prüfen, ob die ESC gedrückt wurde
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
        state = GameState.GameRun;
        SetUIState(false, true, false, false); // UI-Verwaltung (Punkt 4)
        Time.timeScale = 1f;
        isGameStarted = true;
        isPaused = false;

        // Runden-Timer und Transformations-Button anzeigen
        roundTimerText.gameObject.SetActive(true);
        transformButton.gameObject.SetActive(true);

        Log("Runden-Timer und Transformations-Button aktiviert, state: " + state); // Debug-Ausgaben optimiert (Punkt 6)

        // Den Listener für den Transformations-Button hinzufügen
        transformButton.onClick.AddListener(TransformPiece);
    }

    public void UpdateRoundTimerText()
    {
        // Konvertiere die Zeit in Minuten und Sekunden und aktualisiere die Anzeige
        int minutes = Mathf.FloorToInt(roundTime / 60);
        int seconds = Mathf.FloorToInt(roundTime % 60);
        roundTimerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void TransformPiece()
    {
        Log("Transformation der Schachfigur durchgeführt!"); // Debug-Ausgaben optimiert (Punkt 6)
    }

    public void MoveFinished()
    {
        Log($"Der Spieler {currentPlayer} hat vor der Zeit sein Spielzug beendet!"); // Debug-Ausgaben optimiert (Punkt 6)

        roundTime = 120f;       //  Runden zeit zurücksetzen
        isRoundActive = true;   //  Runde erneut aktivieren

        SwitchPlayer();
    }

    public void PauseGame()
    {
        state = GameState.PauseMenu;
        SetUIState(false, false, true, false); // UI-Verwaltung (Punkt 4)
        Time.timeScale = 0f;
        isPaused = true;

        Log("Spiel pausiert. State: " + state); // Debug-Ausgaben optimiert (Punkt 6)
    }

    public void WinGame(String winTeam)
    {
        state = GameState.Win;
        winnerText.SetText(winTeam + " team won!");
        SetUIState(false, false, false, true); // UI-Verwaltung (Punkt 4)
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        state = GameState.GameRun;
        SetUIState(false, true, false, false); // UI-Verwaltung (Punkt 4)
        Time.timeScale = 1f;
        isPaused = false;

        Log("Spiel fortgesetzt."); // Debug-Ausgaben optimiert (Punkt 6)
    }

    public void QuitGame()
    {
        state = GameState.StartMenu;
        Log("Zurück zum Startbildschirm, State :" + state); // Debug-Ausgaben optimiert (Punkt 6)

        SetUIState(true, false, false, false); // UI-Verwaltung (Punkt 4)
        Time.timeScale = 0f;

        // Spielzustände zurücksetzen
        isGameStarted = false;
        isPaused = false;
        roundTime = 120f; // Timer zurücksetzen

        // Blende das Timer-Textfeld und den Transformations-Button aus
        roundTimerText.gameObject.SetActive(false);
        transformButton.gameObject.SetActive(false);
    }

    private void SetInitialCamera()
    {
        SetCameraState(player1Camera, player2Camera); // Redundanz vermeiden (Punkt 1)

        if (board != null)
        {
            board.SetCamera(currentPlayer);
        }
    }

    public void SwitchPlayer()
    {
        currentPlayer = (currentPlayer == 1) ? 2 : 1;
        SetCameraState(currentPlayer == 1 ? player1Camera : player2Camera,
                       currentPlayer == 1 ? player2Camera : player1Camera); // Redundanz vermeiden (Punkt 1)

        Log("current player after switch Player: " + currentPlayer); // Debug-Ausgaben optimiert (Punkt 6)
    }

    // Hilfsmethoden (Punkt 1 und 4)
    private void SetCameraState(Camera activeCamera, Camera inactiveCamera)
    {
        if (activeCamera != null) activeCamera.enabled = true;
        if (inactiveCamera != null) inactiveCamera.enabled = false;
    }

    private void SetUIState(bool startActive, bool gameActive, bool breakActive, bool winActive)
    {
        startScene.SetActive(startActive);
        gameScene.SetActive(gameActive);
        breakScene.SetActive(breakActive);
        winScene.SetActive(winActive);
    }

    // Debug-Ausgaben optimiert (Punkt 6)
    private void Log(string message)
    {
        Debug.Log($"[GameManager] {message}");
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.Rendering.PostProcessing;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Kamera Shake Komponenten")]
    public CameraShake player1CameraShake;
    public CameraShake player2CameraShake;

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

    [Header("Mana System")]
    public int maxMana = 10;
    private int[] currentMana = new int[2];

    public int GetCurrentMana(int player)
    {
        if (player < 1 || player > 2)
        {
            Debug.LogError("Ungültiger Spielerindex!");
            return 0;
        }
        return currentMana[player - 1];
    }

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

        SetCurrentMana(1, maxMana);
        SetCurrentMana(2, maxMana);

        UpdateManaUI();

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
        

        if (player1Camera != null)
        {
            player1CameraShake = player1Camera.GetComponent<CameraShake>();
            if (player1CameraShake == null) Debug.LogError("Player1Camera hat keine CameraShake Komponente!");
        }
        if (player2Camera != null)
        {
            player2CameraShake = player2Camera.GetComponent<CameraShake>();
            if (player2CameraShake == null) Debug.LogError("Player2Camera hat keine CameraShake Komponente!");
        }


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

    // Neue Methode, um den Shake auf der aktuellen Kamera auszulösen:
    public void TriggerActiveCameraShake(float duration, float amount)
    {
        if (currentPlayer == 1 && player1CameraShake != null && player1Camera.enabled)
        {
            player1CameraShake.TriggerShake(duration, amount);
        }
        else if (currentPlayer == 2 && player2CameraShake != null && player2Camera.enabled)
        {
            player2CameraShake.TriggerShake(duration, amount);
        }
        else
        {
            Debug.LogWarning("Konnte keinen aktiven CameraShake für den aktuellen Spieler finden.");
        }
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

    public void SetCurrentMana(int player, int amount)
    {
        if (player < 1 || player > 2)
        {
            Debug.LogError("Ungültiger Spielerindex!");
            return;
        }
        currentMana[player - 1] = Mathf.Clamp(amount, 0, maxMana);
        UpdateManaUI();
    }

    public void UseMana(int player, int amount)
    {
        if (player < 1 || player > 2)
        {
            Debug.LogError("Ungültiger Spielerindex!");
            return;
        }

        if (currentMana[player - 1] >= amount)
        {
            SetCurrentMana(player, currentMana[player - 1] - amount);
        }
        else
        {
            Debug.Log($"Spieler {player} hat nicht genug Mana!");
        }
    }

    private void UpdateManaUI()
    {
        // Aktualisiert das Mana-UI für beide Spieler
        Hud.Instance.UpdateManaUI(currentPlayer, currentMana[0], currentMana[1], maxMana);
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

        transformButton.onClick.RemoveAllListeners(); // Remove previous listeners to avoid duplicates
        transformButton.onClick.AddListener(GenerateBoard.Instance.TransformPiece);

        // Reset the board state when starting a new game
        GenerateBoard.Instance.ResetBoardState();
    }

    public void UpdateRoundTimerText()
    {
        int minutes = Mathf.FloorToInt(roundTime / 60);
        int seconds = Mathf.FloorToInt(roundTime % 60);
        roundTimerText.text = $"{minutes:00}:{seconds:00}";
    }

    // In GameManager/GameManager.cs

    public void MoveFinished()
    {
        // Verarbeite blockierte Tiles über den TileManager
        TileManager.Instance.ProcessDisabledTurns();

        // Rest der Logik...
        roundTime = 120f;
        isRoundActive = true;
        GenerateBoard.Instance.ResetDraggingPiece();
        GenerateBoard.Instance.hasMoved = false;
        GenerateBoard.Instance.hasTransformed = false;
        GenerateBoard.Instance.ResetSelectedPieceForTransformation();
        GenerateBoard.Instance.ResetLastMovedPieceAndTrapChoice();
        GenerateBoard.Instance.RemoveHighlightTilesPublic();

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
        GenerateBoard.Instance.ResetBoardState();
    }

    private void ResetGame()
    {
        isGameStarted = false;
        isPaused = false;
        isRoundActive = true;
        roundTime = 120f; // Timer zurücksetzen
        Time.timeScale = 0f; // Zeit anhalten

        // Blende das Timer-Textfeld und den Transformations-Button aus
        roundTimerText.gameObject.SetActive(false);
        transformButton.gameObject.SetActive(false);

        // Setze den currentPlayer auf 1 zurück, damit das Spiel mit der Player1Camera beginnt
        currentPlayer = 1;
        SetInitialCamera();

        // Mana für beide Spieler zurücksetzen
        SetCurrentMana(1, maxMana);
        SetCurrentMana(2, maxMana);

        // Aktualisiere das Mana-UI
        UpdateManaUI();
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


        // Camera switching logic
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

        Debug.Log("Switched to player " + currentPlayer);
        UpdateManaUI();
    }


}
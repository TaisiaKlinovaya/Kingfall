using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    private float roundTime = 20f;
    private bool isRoundActive = true;
    private bool isGameStarted = false;

    [Header("Transformation")]
    public Button transformButton;
    public Button transformButton2;
    public Button transformButton3;

    [Header("Weiße Karten (Spieler 1)")]
    public List<Card> whiteTeamCards = new List<Card>();
    private List<bool> whiteTeamUnlockedCards;

    [Header("Schwarze Karten (Spieler 2)")]
    public List<Card> blackTeamCards = new List<Card>();
    private List<bool> blackTeamUnlockedCards;

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

    [Header("Audio Quellen")]
    public AudioClip menuClip;
    public AudioClip gameClip;

    private AudioSource audioSource;

    public int GetCurrentMana(int player)
    {
        if (player < 1 || player > 2)
        {
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
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        ReturnToMain.onClick.AddListener(QuitGame);
        UpdateMusic();

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
        transformButton2.gameObject.SetActive(false);
        transformButton3.gameObject.SetActive(false);
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

    private void UpdateMusic()
    {
        if (state == "StartMenu" || state == "PauseMenu")
        {
            if (audioSource.clip != menuClip)
            {
                audioSource.clip = menuClip;
                audioSource.Play();
            }
        }
        else if (state == "GameRun")
        {
            if (audioSource.clip != gameClip)
            {
                audioSource.clip = gameClip;
                audioSource.Play();
            }
        }
        else if (state == "Win")
        {
            audioSource.Stop();
        }
    }
    void Update()
    {
        if (!isGameStarted) return;

        // ESC zum Pausieren
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPaused)
            {
                PauseGame();
            }
            else
            {
                ResumeGame();
            }
        }

        if (isPaused) return;

        if (isRoundActive && roundTime > 0)
        {
            roundTime -= Time.deltaTime;
            UpdateRoundTimerText();

            if (roundTime <= 0)
            {
                HandleTimeExpired();
            }
        }
    }

    private void HandleTimeExpired()
    {
        Debug.Log($"Zeit abgelaufen für Spieler {currentPlayer}!");

        // Falls der Spieler nichts gemacht hat, trotzdem den Zug beenden
        if (!GenerateBoard.Instance.HasPlayerPerformedActionThisTurn())
        {
            Debug.Log("Keine Aktion ausgeführt – Zug wird trotzdem beendet wegen Zeitablauf.");
        }

        roundTime = 60f; // Timer zurücksetzen
        isRoundActive = true; // Runde aktiv halten

        GenerateBoard.Instance.ResetDraggingPiece();
        GenerateBoard.Instance.ResetMantisTrapMode();
        GenerateBoard.Instance.hasMoved = false;
        GenerateBoard.Instance.hasTransformed = false;
        GenerateBoard.Instance.ResetSelectedPieceForTransformation();
        GenerateBoard.Instance.ResetLastMovedPiece();
        GenerateBoard.Instance.RemoveHighlightTilesPublic();

        SwitchPlayer();
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
        isRoundActive = true; 
        roundTime = 60f;
        UpdateMusic();

        roundTimerText.gameObject.SetActive(true);
        transformButton.gameObject.SetActive(true);
        transformButton2.gameObject.SetActive(true);
        transformButton3.gameObject.SetActive(true);
        finishedButton.gameObject.SetActive(true);

        transformButton.onClick.RemoveAllListeners();
        transformButton2.onClick.RemoveAllListeners();
        transformButton3.onClick.RemoveAllListeners();
        transformButton.onClick.AddListener(GenerateBoard.Instance.TransformPiece);
        transformButton2.onClick.AddListener(GenerateBoard.Instance.TransformPiece);
        transformButton3.onClick.AddListener(GenerateBoard.Instance.TransformPiece);

        GenerateBoard.Instance.ResetBoardState();
        UpdateRoundTimerText(); // Timer-Text aktualisieren
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
        if (!GenerateBoard.Instance.HasPlayerPerformedActionThisTurn())
        {
            // Überprüfen, ob wir im Mantis-Fallenmodus sind. Wenn ja, und keine Falle gestellt wurde,
            // könnte dies als "keine Aktion" zählen, es sei denn, das Fallenstellen selbst ist die Aktion.
            // Fürs Erste: Wenn keine Bewegung UND keine Transformation, dann keine Aktion.
            // Das Stellen der Mantis-Falle selbst (nach der Bewegung) beendet den Zug NICHT automatisch.
            // Der Spieler MUSS sich bewegt haben, um den Fallenstellmodus überhaupt zu erreichen.

            // Wenn die Mantis bewegt wurde (hasMoved = true), aber die Falle noch nicht gestellt ist
            // (currentMantisTrapState == AwaitingDirectionInput), dann darf der Zug noch nicht beendet werden,
            // ODER das Beenden des Zuges ohne Falleneingabe bricht das Fallenstellen ab.
            // Deine aktuelle ResetMantisTrapMode() in GenerateBoard macht letzteres, was gut ist.

            // Wenn also hasMoved false ist, hat der Spieler definitiv nichts Gültiges getan.
            if (!GenerateBoard.Instance.hasMoved)
            {
                Debug.Log("Keine Aktion ausgeführt! Bitte bewege oder transformiere zuerst eine Figur.");
                return;
            }
        }

        Debug.Log($"Spieler {currentPlayer} beendet den Zug.");
        roundTime = 60f; // Timer auf 1 Minuten zurücksetzen
        isRoundActive = true; // Runde wieder aktivieren

        GenerateBoard.Instance.ResetDraggingPiece();
        GenerateBoard.Instance.ResetMantisTrapMode();
        GenerateBoard.Instance.hasMoved = false;
        GenerateBoard.Instance.hasTransformed = false;
        GenerateBoard.Instance.ResetSelectedPieceForTransformation();
        GenerateBoard.Instance.ResetLastMovedPiece();
        GenerateBoard.Instance.RemoveHighlightTilesPublic();

        SwitchPlayer();
    }
    public void PauseGame()
    {
        state = "PauseMenu";
        breakScene.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        UpdateMusic();


        finishedButton.gameObject.SetActive(false);
        transformButton.gameObject.SetActive(false);
        transformButton2.gameObject.SetActive(false);
        transformButton3.gameObject.SetActive(false);
    }

    public void WinGame(String winTeam)
    {
        state = "Win";
        UpdateMusic();

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
        UpdateMusic();

        finishedButton.gameObject.SetActive(true);
        transformButton.gameObject.SetActive(true);
        transformButton2.gameObject.SetActive(true);
        transformButton3.gameObject.SetActive(true);
    }

    public void QuitGame()
    {
        state = "StartMenu";
        UpdateMusic();

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
        TileManager.Instance.ResetDisabledTiles();
        whiteTeamUnlockedCards = new List<bool>(new bool[whiteTeamCards.Count]);
        blackTeamUnlockedCards = new List<bool>(new bool[blackTeamCards.Count]);

        // Stelle sicher, dass alle Karten visuell als gelockt erscheinen
        foreach (var card in whiteTeamCards) card.Lock();
        foreach (var card in blackTeamCards) card.Lock();

        // Blende das Timer-Textfeld und den Transformations-Button aus
        roundTimerText.gameObject.SetActive(false);
        transformButton.gameObject.SetActive(false);
        transformButton2.gameObject.SetActive(false);
        transformButton3.gameObject.SetActive(false);

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
        UpdateManaUI();
        // Kartenanzeige aktualisieren
        UpdateCardVisibility();
    }

    public void UnlockCardForTeam(int index)
    {
        List<Card> teamCards = (currentPlayer == 1) ? whiteTeamCards : blackTeamCards;
        List<bool> unlockedList = (currentPlayer == 1) ? whiteTeamUnlockedCards : blackTeamUnlockedCards;

        if (index < 0 || index >= teamCards.Count)
        {
            Debug.LogWarning("Ungültiger Kartenindex!");
            return;
        }

        if (unlockedList[index])
        {
            Debug.Log("Diese Karte wurde bereits freigeschaltet.");
            return;
        }

        if (GetCurrentMana(currentPlayer) < 3)
        {
            Debug.Log($"Spieler {currentPlayer} hat nicht genug Mana!");
            return;
        }

        unlockedList[index] = true;
        teamCards[index].Unlock();
        UseMana(currentPlayer, 3);
        Debug.Log($"Spieler {currentPlayer} hat Karte {index} freigeschaltet.");
    }

    private void UpdateCardVisibility()
    {
        for (int i = 0; i < whiteTeamCards.Count; i++)
        {
            if (currentPlayer == 1)
            {
                // Nur weiße Karten aktualisieren
                whiteTeamCards[i].SetVisibility(whiteTeamUnlockedCards[i]);
                // Schwarze Karten nicht ändern
            }
            else
            {
                // Nur schwarze Karten aktualisieren
                blackTeamCards[i].SetVisibility(blackTeamUnlockedCards[i]);
                // Weiße Karten nicht ändern
            }
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.Rendering.PostProcessing;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private String state;
    public int CurrentPlayer { get { return currentPlayer; } }
    public String State { get { return state; } }

    //  ####    Start Scene UI_Elemente  ###
    [Header("Start Scene UI Elemente")]
    public GameObject startScene;           // Referenz zum Startmenü UI
    public Button startButton;              // Button, um das Spiel zu starten

    //
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

    //
    //  ####    Break Scene UI_Elemente     ####
    [Header("Break Scene UI Elemente")]
    public GameObject breakScene;
    public Button resumeButton;
    public Button quitButton;
    private bool isPaused = false;                  // Bool, um zu überprüfen, ob das Spiel sich in Pause befindet

    //
    //  ####    Spieler 1 & 2 hinzufügen    ####
    [Header("Spieler Kameras")]
    public Camera player1Camera;
    public Camera player2Camera;
    private int currentPlayer = 1;          // 1 für Spieler 1, 2 für Spieler 2

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

        // Startmenü anzeigen & Game/Break Scene zu beginn deaktivieren
        startScene.SetActive(true);
        gameScene.SetActive(false);
        breakScene.SetActive(false);
        Time.timeScale = 0f;

        // Spieler Camera auf true oder false setzen
        player1Camera.enabled = true;
        player2Camera.enabled = false;

        // Transformations-Button und Timer ausblenden, bis das Spiel gestartet ist
        roundTimerText.gameObject.SetActive(false);
        transformButton.gameObject.SetActive(false);

        // Listener für den Start-Button, Resume-Button, Quit-Button und Finished-Button
        startButton.onClick.AddListener(StartGame);
        resumeButton.onClick.AddListener(ResumeGame);
        quitButton.onClick.AddListener(QuitGame);
        finishedButton.onClick.AddListener(MoveFinished);  // << Hinzugefügt
    }

    void Update()
    {
        // Wenn das Spiel gestartet ist, den Timer laufen lassen
        if (isGameStarted && roundTime > 0)
        {
            roundTime -= Time.deltaTime; // Timer herunterzählen
            UpdateRoundTimerText();      // Timer-Anzeige aktualisieren

            // Wenn die Zeit abgelaufen ist, kann man zusätzliche Logik hinzufügen
            if (roundTime <= 0)
            {
                roundTime = 0;

                Debug.Log("Rundenzeit abgelaufen.");    //  Debug Information

                // Hier kann man eine Funktion aufrufen, die das Ende der Runde anzeigt
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

        if (isRoundActive)
        {
            if(roundTime <= 0)
            {
                Debug.Log($"Zeit abgelaufen! Spieler {currentPlayer} wird automatisch gewechselt.");
                MoveFinished();             //  Spielzug beendet und Spieler wechsel wird aufgerufen
            }
        }
    }

    public void StartGame()
    {
        state = "GameRun";
        // Startmenü deaktivieren und das Spiel fortsetzen
        startScene.SetActive(false);
        gameScene.SetActive(true);             // << Aktiviert die Game Scene
        Time.timeScale = 1f;
        isGameStarted = true;
        isPaused = true;

        // Runden-Timer und Transformations-Button anzeigen
        roundTimerText.gameObject.SetActive(true);
        transformButton.gameObject.SetActive(true);

        Debug.Log("Runden-Timer und Transformations-Button aktiviert, state: " + state);  // Debug Information


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

    //
    //  ###     Bearbeitung - Transformation bestimmter Schachfiguren (später)     ###
    public void TransformPiece()
    {
        Debug.Log("Transformation der Schachfigur durchgeführt!");  // Debug Information
        // Hier wird eine Beispielausgabe gezeigt, man kann jedoch beliebige Transformationslogik hinzufügen.
    }

    //
    //  ###     Spielzug früher beenden     ###     NEU Funktioniert 
    public void MoveFinished()
    {
        Debug.Log($"Der Spieler {currentPlayer} hat vor der Zeit sein Spielzug beendet!");  // Debug: Überprüft, ob der Finished Button funktioniert

        roundTime = 120f;       //  Runden zeit zurücksetzen
        isRoundActive = true;   //  Runde erneut aktivieren
        SwitchPlayer();         //  Spieler wird gewechselt
    }

    //
    //  ###     Break Menü     ###
    public void PauseGame()
    {
        state = "PauseMenu";
        breakScene.SetActive(true);
        Time.timeScale = 0f;            // Zeit anhalten
        isPaused = true;

        Debug.Log("Spiel pausiert. State: " + state);   //  Debug Information 
    }

    public void ResumeGame()
    {
        state = "GameRun";
        breakScene.SetActive(false);
        Time.timeScale = 1f;                // Zeit fortsetzen
        isPaused = false;

        Debug.Log("Spiel fortgesetzt.");    //  Debug Information 
    }

    public void QuitGame()
    {
        state = "StartMenu";
        Debug.Log("Zurück zum Startbildschirm, State :" + state);

        // Deaktiviere die Game Scene und das Pausenmenü
        gameScene.SetActive(false);
        breakScene.SetActive(false);

        // Aktiviere das Startmenü
        startScene.SetActive(true);

        // Spielzustände zurücksetzen
        isGameStarted = false;
        isPaused = false;
        roundTime = 120f; // Timer zurücksetzen
        Time.timeScale = 0f; // Zeit anhalten

        // Blende das Timer-Textfeld und den Transformations-Button aus
        roundTimerText.gameObject.SetActive(false);
        transformButton.gameObject.SetActive(false);
    }

    // 
    //  ####    Funktion zum wechseln der Spieler   ####
    // Update: Spiellogik noch hinzufügen, welche Spieler gerade am zug ist
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
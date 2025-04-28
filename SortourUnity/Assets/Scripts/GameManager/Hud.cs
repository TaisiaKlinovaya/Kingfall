using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Hud : MonoBehaviour
{
    public TMP_Text player1ManaText;
    public TMP_Text player2ManaText;

    public static Hud Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public void UpdateManaUI(int currentPlayer, int player1Mana, int player2Mana, int maxMana)
    {
        if (currentPlayer == 1)
        {
            // Zeige nur das Mana von Spieler 1 an
            player1ManaText.text = $"Spieler 1 Mana: {player1Mana} / {maxMana}";
            player1ManaText.gameObject.SetActive(true); 
            player2ManaText.gameObject.SetActive(false); 
        }
        else if (currentPlayer == 2)
        {
            // Zeige nur das Mana von Spieler 2 an
            player2ManaText.text = $"Spieler 2 Mana: {player2Mana} / {maxMana}";
            player2ManaText.gameObject.SetActive(true);
            player1ManaText.gameObject.SetActive(false); 
        }
    }
}

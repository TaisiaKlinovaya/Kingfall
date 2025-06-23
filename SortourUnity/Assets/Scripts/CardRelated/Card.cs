using UnityEngine;
using UnityEngine.UI;

public enum Team
{
    White,
    Black
}

[System.Serializable]
public class Card
{
    public string cardName;
    public Team team;
    public Image coverImage;

    public bool isUnlocked => !coverImage.gameObject.activeSelf;

    public void Unlock()
    {
        if (coverImage != null)
        {
            coverImage.gameObject.SetActive(false);
        }
    }

    public void Lock()
    {
        if (coverImage != null)
        {
            coverImage.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Setzt die Sichtbarkeit der Karte: true = sichtbar (freigeschaltet), false = verdeckt (gesperrt).
    /// </summary>
    public void SetVisibility(bool visible)
    {
        if (coverImage != null)
        {
            coverImage.gameObject.SetActive(!visible);
        }
    }
}


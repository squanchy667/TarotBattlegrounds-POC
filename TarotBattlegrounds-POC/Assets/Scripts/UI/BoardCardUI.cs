using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class BoardCardUI : MonoBehaviour
{
    [Header("Card Display")]
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private TMP_Text tribeText;
    [SerializeField] private Image cardArtwork;

    [Header("Visual")]
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image selectionBorder;
    [SerializeField] private Button cardButton;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.4f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.7f, 0.3f, 0.3f, 1f);

    private Card card;
    private int index;
    private Action<int> onClickCallback;

    public void Setup(Card cardData, int cardIndex, Action<int> onClick)
    {
        card = cardData;
        index = cardIndex;
        onClickCallback = onClick;

        if (cardNameText != null) cardNameText.text = card.cardName;
        if (attackText != null) attackText.text = card.attack.ToString();
        if (healthText != null) healthText.text = card.health.ToString();
        if (tierText != null) tierText.text = $"T{card.tier}";
        if (tribeText != null) tribeText.text = card.tribe;
        if (cardArtwork != null && card.cardImage != null) cardArtwork.sprite = card.cardImage;

        if (cardButton != null)
            cardButton.onClick.AddListener(OnCardClicked);

        SetSelected(false);
    }

    private void OnCardClicked()
    {
        onClickCallback?.Invoke(index);
    }

    public void SetSelected(bool selected)
    {
        if (selectionBorder != null)
            selectionBorder.gameObject.SetActive(selected);

        if (cardBackground != null)
            cardBackground.color = selected ? selectedColor : normalColor;
    }

    public void UpdateStats(int attack, int health)
    {
        if (attackText != null) attackText.text = attack.ToString();
        if (healthText != null) healthText.text = health.ToString();
    }

    private void OnDestroy()
    {
        if (cardButton != null)
            cardButton.onClick.RemoveAllListeners();
    }
}
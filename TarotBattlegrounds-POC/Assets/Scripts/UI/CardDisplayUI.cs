using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CardDisplayUI : MonoBehaviour
{
    [Header("Card Info")]
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private TMP_Text tribeText;
    [SerializeField] private TMP_Text costText;

    [Header("Visuals")]
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image cardArtwork;
    [SerializeField] private Image selectionBorder;

    [Header("Selection Colors")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.5f, 0.3f, 1f);

    private Card card;
    private int index;
    private Action<int> onClickCallback;
    private Button cardButton;

    private void Awake()
    {
        cardButton = GetComponent<Button>();
        if (cardButton != null)
            cardButton.onClick.AddListener(OnCardClicked);
    }

    public void Setup(Card cardData, int cardIndex, Action<int> onClick)
    {
        card = cardData;
        index = cardIndex;
        onClickCallback = onClick;

        // Display card data
        if (cardNameText != null) cardNameText.text = card.cardName;
        if (attackText != null) attackText.text = card.attack.ToString();
        if (healthText != null) healthText.text = card.health.ToString();
        if (tierText != null) tierText.text = $"T{card.tier}";
        if (tribeText != null) tribeText.text = card.tribe;
        
        // Display artwork
        if (cardArtwork != null)
        {
            if (card.cardImage != null)
            {
                cardArtwork.sprite = card.cardImage;
                cardArtwork.color = Color.white;
            }
            else
            {
                cardArtwork.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Gray placeholder
            }
        }

        SetSelected(false);
    }

    public void SetCostVisible(bool visible, int cost = 3)
    {
        if (costText != null)
        {
            costText.gameObject.SetActive(visible);
            if (visible) costText.text = $"{cost}g";
        }
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

    private void OnCardClicked()
    {
        onClickCallback?.Invoke(index);
    }

    public Card GetCard() => card;
    public int GetIndex() => index;

    private void OnDestroy()
    {
        if (cardButton != null)
            cardButton.onClick.RemoveAllListeners();
    }
}
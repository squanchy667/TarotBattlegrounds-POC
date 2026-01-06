using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ShopCardUI : MonoBehaviour
{
    [Header("Card Display")]
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private TMP_Text tribeText;
    [SerializeField] private TMP_Text costText;

    [Header("Visual")]
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image selectionBorder;
    [SerializeField] private Button cardButton;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.4f, 0.6f, 0.2f, 1f);

    private Card card;
    private int index;
    private Action<int> onClickCallback;

    public void Setup(Card cardData, int cardIndex, Action<int> onClick)
    {
        card = cardData;
        index = cardIndex;
        onClickCallback = onClick;

        // Display card info
        if (cardNameText != null) cardNameText.text = card.cardName;
        if (attackText != null) attackText.text = card.attack.ToString();
        if (healthText != null) healthText.text = card.health.ToString();
        if (tierText != null) tierText.text = $"T{card.tier}";
        if (tribeText != null) tribeText.text = card.tribe;
        if (costText != null) costText.text = "3g";

        // Setup button
        if (cardButton != null)
        {
            cardButton.onClick.AddListener(OnCardClicked);
        }

        SetSelected(false);
    }

    private void OnCardClicked()
    {
        onClickCallback?.Invoke(index);
    }

    public void SetSelected(bool selected)
    {
        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(selected);
        }

        if (cardBackground != null)
        {
            cardBackground.color = selected ? selectedColor : normalColor;
        }
    }

    private void OnDestroy()
    {
        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
        }
    }
}
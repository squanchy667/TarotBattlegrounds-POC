using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using TarotBattlegrounds.UI;

public class HandCardUI : MonoBehaviour
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
    [SerializeField] private Color normalColor = Tokens.CharredWood;
    [SerializeField] private Color selectedColor = Tokens.Ember;

    private Card card;
    private int index;
    private Action<int> onClickCallback;

    // UX08: Card interaction feedback component (hover, selection glow, frozen shimmer)
    private CardInteractionFeedback interactionFeedback;

    private void Awake()
    {
        // UX08: Cache CardInteractionFeedback for hover/selection effects
        interactionFeedback = GetComponent<CardInteractionFeedback>();
    }

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
        // UX08: Delegate to CardInteractionFeedback if present
        if (interactionFeedback != null)
        {
            interactionFeedback.SetSelected(selected);
        }

        // Backward compat: still update border and background
        if (selectionBorder != null)
            selectionBorder.gameObject.SetActive(selected);

        if (cardBackground != null)
            cardBackground.color = selected ? selectedColor : normalColor;
    }

    public Card GetCard() => card;

    private void OnDestroy()
    {
        if (cardButton != null)
            cardButton.onClick.RemoveAllListeners();
    }
}
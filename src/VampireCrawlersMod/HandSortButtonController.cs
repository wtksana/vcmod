using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Il2CppInterop.Runtime.Attributes;
using Nosebleed.Pancake.GameLogic;
using Nosebleed.Pancake.Modal;
using Nosebleed.Pancake.Models;
using Nosebleed.Pancake.View;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace VampireCrawlersMod;

public sealed class HandSortButtonController : MonoBehaviour
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;
    private const float DefaultButtonReferenceX = 236f;
    private const float DefaultButtonReferenceY = 835f;
    private const float ButtonReferenceWidth = 128f;
    private const float ButtonReferenceHeight = 56f;
    private const int ButtonCanvasSortingOrder = 10;

    private static PlayerModel _player;
    private static ConfigEntry<float> _buttonReferenceX;
    private static ConfigEntry<float> _buttonReferenceY;
    private static Vector2 _runtimeButtonReferencePosition;
    private static bool _hasRuntimeButtonReferencePosition;
    private static bool _isDraggingButton;
    private static bool _isLeftButtonDownOnSortButton;
    private static Vector2 _dragOffset;
    private static bool _isMouseOverButton;
    private static CardSlotHolder _pendingLayoutRefreshCardGroup;
    private static int _pendingLayoutRefreshFrames;

    private GameObject _buttonRoot;
    private RectTransform _buttonRectTransform;
    private Image[] _buttonInnerFrameImages;
    private Image[] _buttonFaceImages;
    private Image _buttonBottomShade;
    private Text _buttonText;

    public HandSortButtonController(IntPtr ptr) : base(ptr)
    {
    }

    [HideFromIl2Cpp]
    public static void Configure(ConfigFile config)
    {
        _buttonReferenceX = config.Bind(
            "HandSortButton",
            "ReferenceX",
            DefaultButtonReferenceX,
            "整理按钮在 1920x1080 参考坐标中的左上角 X。按住右键拖动按钮后会自动更新。");

        _buttonReferenceY = config.Bind(
            "HandSortButton",
            "ReferenceY",
            DefaultButtonReferenceY,
            "整理按钮在 1920x1080 参考坐标中的左上角 Y。按住右键拖动按钮后会自动更新。");
    }

    [HideFromIl2Cpp]
    public static void SetPlayer(PlayerModel player)
    {
        _player = player;
    }

    [HideFromIl2Cpp]
    public static void ClearPlayer(PlayerModel player)
    {
        if (_player == player)
        {
            _player = null;
        }
    }

    private void Update()
    {
        PlayerModel player = _player;
        if (!CanShowButton(player))
        {
            _isDraggingButton = false;
            _isLeftButtonDownOnSortButton = false;
            SetButtonVisible(false);
            return;
        }

        float scale = GetUiScale();
        EnsureButtonUi();
        SetButtonVisible(true);
        UpdateButtonTransform(scale);

        RefreshButtonVisual();
    }

    private void OnGUI()
    {
        PlayerModel player = _player;
        if (!CanShowButton(player))
        {
            return;
        }

        float scale = GetUiScale();
        Rect buttonRect = GetButtonGuiRect(scale);
        if (HandleButtonInput(buttonRect, scale))
        {
            SortHandByManaCost(player);
        }
    }

    private void LateUpdate()
    {
        if (_pendingLayoutRefreshFrames <= 0)
        {
            return;
        }

        _pendingLayoutRefreshFrames--;
        RefreshHandLayout(_pendingLayoutRefreshCardGroup);
        if (_pendingLayoutRefreshFrames == 0)
        {
            _pendingLayoutRefreshCardGroup = null;
        }
    }

    [HideFromIl2Cpp]
    private static bool CanShowButton(PlayerModel player)
    {
        if (player == null || !player.IsInEncounter)
        {
            return false;
        }

        if (HasActiveModal())
        {
            return false;
        }

        CardPileModel cardPile = player.HandPile?.CardPile;
        return cardPile != null && cardPile.Count > 1;
    }

    [HideFromIl2Cpp]
    private static bool HasActiveModal()
    {
        try
        {
            return ModalManager.Exists && ModalManager.Instance != null && ModalManager.Instance.HasModalActive;
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogWarning($"Unable to read modal state: {ex.Message}");
            return false;
        }
    }

    [HideFromIl2Cpp]
    private void EnsureButtonUi()
    {
        if (_buttonRoot != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("VampireCrawlersMod.HandSortButtonCanvas");
        canvasObject.transform.SetParent(transform, false);
        Canvas buttonCanvas = canvasObject.AddComponent<Canvas>();
        buttonCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        buttonCanvas.sortingOrder = ButtonCanvasSortingOrder;
        canvasObject.AddComponent<CanvasScaler>();

        _buttonRoot = new GameObject("HandSortButton");
        _buttonRoot.transform.SetParent(canvasObject.transform, false);
        _buttonRectTransform = _buttonRoot.AddComponent<RectTransform>();
        _buttonRectTransform.anchorMin = Vector2.zero;
        _buttonRectTransform.anchorMax = Vector2.zero;
        _buttonRectTransform.pivot = Vector2.zero;
        AddPixelPanel("Shadow", 8f, -7f, -2f, 7f, 8f, new Color(0f, 0f, 0f, 0.5f));
        AddPixelPanel("OuterFrame", 0f, 0f, 0f, 0f, 9f, new Color(0.09f, 0.08f, 0.11f, 1f));
        _buttonInnerFrameImages = AddPixelPanel("InnerFrame", 5f, 5f, 5f, 5f, 6f, new Color(0.42f, 0.31f, 0.14f, 1f));
        _buttonFaceImages = AddPixelPanel("Face", 9f, 8f, 9f, 8f, 5f, GetButtonFaceColor(false, false));
        AddButtonLayer("TopHighlight", new Vector2(22f, 35f), new Vector2(-42f, -12f), new Color(0.95f, 0.83f, 0.53f, 0.34f));
        _buttonBottomShade = AddButtonLayer("BottomShade", new Vector2(24f, 8f), new Vector2(-42f, -43f), new Color(0.42f, 0.29f, 0.13f, 0.4f));

        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(_buttonRoot.transform, false);
        RectTransform textTransform = textObject.AddComponent<RectTransform>();
        textTransform.anchorMin = Vector2.zero;
        textTransform.anchorMax = Vector2.one;
        textTransform.offsetMin = new Vector2(10f, 8f);
        textTransform.offsetMax = new Vector2(-10f, -5f);

        _buttonText = textObject.AddComponent<Text>();
        _buttonText.text = "整理手牌";
        _buttonText.alignment = TextAnchor.MiddleCenter;
        _buttonText.fontStyle = FontStyle.Bold;
        _buttonText.color = new Color(0.33f, 0.27f, 0.18f, 1f);
        _buttonText.raycastTarget = false;
        _buttonText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _buttonText.verticalOverflow = VerticalWrapMode.Overflow;
        _buttonText.font = CreateButtonFont();
    }

    [HideFromIl2Cpp]
    private Image[] AddPixelPanel(string name, float left, float bottom, float right, float top, float cutSize, Color color)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(_buttonRoot.transform, false);
        RectTransform panelTransform = panelObject.AddComponent<RectTransform>();
        panelTransform.anchorMin = Vector2.zero;
        panelTransform.anchorMax = Vector2.one;
        panelTransform.offsetMin = new Vector2(left, bottom);
        panelTransform.offsetMax = new Vector2(-right, -top);

        Image topImage = AddPanelSegment(panelObject.transform, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(cutSize, -cutSize), new Vector2(-cutSize, 0f), color);
        Image middleImage = AddPanelSegment(panelObject.transform, "Middle", Vector2.zero, Vector2.one, new Vector2(0f, cutSize), new Vector2(0f, -cutSize), color);
        Image bottomImage = AddPanelSegment(panelObject.transform, "Bottom", Vector2.zero, new Vector2(1f, 0f), new Vector2(cutSize, 0f), new Vector2(-cutSize, cutSize), color);
        return new[] { topImage, middleImage, bottomImage };
    }

    [HideFromIl2Cpp]
    private static Image AddPanelSegment(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        GameObject segmentObject = new GameObject(name);
        segmentObject.transform.SetParent(parent, false);
        RectTransform rectTransform = segmentObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;

        Image image = segmentObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    [HideFromIl2Cpp]
    private Image AddButtonLayer(string name, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        GameObject layerObject = new GameObject(name);
        layerObject.transform.SetParent(_buttonRoot.transform, false);
        RectTransform rectTransform = layerObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;

        Image image = layerObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    [HideFromIl2Cpp]
    private static Font CreateButtonFont()
    {
        Font font = Font.CreateDynamicFontFromOSFont("Arial", 22);
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    [HideFromIl2Cpp]
    private void SetButtonVisible(bool visible)
    {
        if (_buttonRoot != null && _buttonRoot.activeSelf != visible)
        {
            _buttonRoot.SetActive(visible);
        }
    }

    [HideFromIl2Cpp]
    private void UpdateButtonTransform(float scale)
    {
        Vector2 referencePosition = GetButtonReferencePosition(scale);
        float width = ButtonReferenceWidth * scale;
        float height = ButtonReferenceHeight * scale;
        float bottom = (ReferenceHeight - referencePosition.y - ButtonReferenceHeight) * scale;

        _buttonRectTransform.sizeDelta = new Vector2(width, height);
        _buttonRectTransform.anchoredPosition = new Vector2(referencePosition.x * scale, bottom);
        _buttonText.fontSize = Mathf.Max(12, Mathf.RoundToInt(22f * scale));
    }

    [HideFromIl2Cpp]
    private static float GetUiScale()
    {
        return Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
    }

    [HideFromIl2Cpp]
    private static Rect GetButtonGuiRect(float scale)
    {
        Vector2 referencePosition = GetButtonReferencePosition(scale);
        return new Rect(
            referencePosition.x * scale,
            Screen.height - (ReferenceHeight - referencePosition.y) * scale,
            ButtonReferenceWidth * scale,
            ButtonReferenceHeight * scale);
    }

    [HideFromIl2Cpp]
    private static Vector2 GetButtonReferencePosition(float scale)
    {
        if (!_hasRuntimeButtonReferencePosition)
        {
            float x = _buttonReferenceX?.Value ?? DefaultButtonReferenceX;
            float y = _buttonReferenceY?.Value ?? DefaultButtonReferenceY;
            _runtimeButtonReferencePosition = ClampReferencePosition(new Vector2(x, y), scale);
            _hasRuntimeButtonReferencePosition = true;
        }

        return _runtimeButtonReferencePosition;
    }

    [HideFromIl2Cpp]
    private static bool HandleButtonInput(Rect buttonRect, float scale)
    {
        Event currentEvent = Event.current;
        if (currentEvent == null)
        {
            return false;
        }

        Vector2 mousePosition = currentEvent.mousePosition;
        _isMouseOverButton = buttonRect.Contains(mousePosition);

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1 && _isMouseOverButton)
        {
            _isDraggingButton = true;
            _dragOffset = mousePosition - buttonRect.position;
            currentEvent.Use();
            return false;
        }

        if (_isDraggingButton && currentEvent.type == EventType.MouseDrag && currentEvent.button == 1)
        {
            Vector2 newScreenPosition = mousePosition - _dragOffset;
            Vector2 newReferencePosition = new Vector2(
                newScreenPosition.x / scale,
                ReferenceHeight - (Screen.height - newScreenPosition.y) / scale);

            _runtimeButtonReferencePosition = ClampReferencePosition(newReferencePosition, scale);
            currentEvent.Use();
            return false;
        }

        if (_isDraggingButton && currentEvent.type == EventType.MouseUp && currentEvent.button == 1)
        {
            _isDraggingButton = false;
            SaveButtonReferencePosition();
            currentEvent.Use();
            return false;
        }

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            _isLeftButtonDownOnSortButton = _isMouseOverButton;
            if (_isMouseOverButton)
            {
                currentEvent.Use();
            }

            return false;
        }

        if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
        {
            bool clicked = _isLeftButtonDownOnSortButton && _isMouseOverButton;
            _isLeftButtonDownOnSortButton = false;
            if (clicked)
            {
                currentEvent.Use();
            }

            return clicked;
        }

        return false;
    }

    [HideFromIl2Cpp]
    private static Vector2 ClampReferencePosition(Vector2 referencePosition, float scale)
    {
        float referenceScreenWidth = Screen.width / scale;
        float referenceScreenHeight = Screen.height / scale;
        float minY = ReferenceHeight - referenceScreenHeight;
        float maxX = Mathf.Max(0f, referenceScreenWidth - ButtonReferenceWidth);
        float maxY = Mathf.Max(minY, ReferenceHeight - ButtonReferenceHeight);

        return new Vector2(
            Mathf.Clamp(referencePosition.x, 0f, maxX),
            Mathf.Clamp(referencePosition.y, minY, maxY));
    }

    [HideFromIl2Cpp]
    private static void SaveButtonReferencePosition()
    {
        if (_buttonReferenceX != null)
        {
            _buttonReferenceX.Value = _runtimeButtonReferencePosition.x;
        }

        if (_buttonReferenceY != null)
        {
            _buttonReferenceY.Value = _runtimeButtonReferencePosition.y;
        }
    }

    [HideFromIl2Cpp]
    private void RefreshButtonVisual()
    {
        if (_buttonFaceImages == null)
        {
            return;
        }

        bool pressed = _isDraggingButton || (_isLeftButtonDownOnSortButton && _isMouseOverButton);
        SetImagesColor(_buttonFaceImages, GetButtonFaceColor(_isMouseOverButton, pressed));
        SetImagesColor(_buttonInnerFrameImages, pressed ? new Color(0.31f, 0.22f, 0.11f, 1f) : new Color(0.42f, 0.31f, 0.14f, 1f));
        _buttonBottomShade.color = pressed ? new Color(0.28f, 0.2f, 0.11f, 0.55f) : new Color(0.42f, 0.29f, 0.13f, 0.45f);
    }

    [HideFromIl2Cpp]
    private static void SetImagesColor(Image[] images, Color color)
    {
        if (images == null)
        {
            return;
        }

        foreach (Image image in images)
        {
            if (image != null)
            {
                image.color = color;
            }
        }
    }

    [HideFromIl2Cpp]
    private static Color GetButtonFaceColor(bool hovered, bool pressed)
    {
        if (pressed)
        {
            return new Color(0.63f, 0.48f, 0.23f, 1f);
        }

        return hovered ? new Color(0.88f, 0.72f, 0.4f, 1f) : new Color(0.79f, 0.63f, 0.34f, 1f);
    }

    [HideFromIl2Cpp]
    private static void SortHandByManaCost(PlayerModel player)
    {
        CardPileModel cardPile = player?.HandPile?.CardPile;
        if (cardPile == null || cardPile.Count <= 1)
        {
            return;
        }

        List<CardSortEntry> sortedCards = GetSortedCards(cardPile);
        for (int targetIndex = 0; targetIndex < sortedCards.Count; targetIndex++)
        {
            CardModel desiredCard = sortedCards[targetIndex].Card;
            CardModel currentCard = GetCardAt(cardPile, targetIndex);
            if (currentCard == null || desiredCard == null || currentCard == desiredCard)
            {
                continue;
            }

            if (!cardPile.Contains(desiredCard))
            {
                continue;
            }

            cardPile.TrySwapCards(currentCard, desiredCard);
        }

        player.HandPile.View?.RefreshCardsUI(player);
        SortHandSlotsByManaCost(player);
        Plugin.Logger?.LogInfo("Sorted hand by mana cost.");
    }

    [HideFromIl2Cpp]
    private static void SortHandSlotsByManaCost(PlayerModel player)
    {
        CardSlotHolder cardGroup = player?.HandPile?.View?.CardGroup;
        if (cardGroup == null)
        {
            return;
        }

        List<CardSlot> slots = GetVisibleCardSlots(cardGroup);
        if (slots == null || slots.Count <= 1)
        {
            Plugin.Logger?.LogWarning($"Unable to sort hand slots. Slot count: {slots?.Count ?? 0}");
            return;
        }

        List<CardSlotSortEntry> sortedSlots = new();
        for (int i = 0; i < slots.Count; i++)
        {
            CardSlot slot = slots[i];
            CardModel card = GetSlotCard(slot);
            if (slot != null && card != null)
            {
                sortedSlots.Add(new CardSlotSortEntry(slot, GetSortCost(card), IsWildCard(card), i));
            }
        }

        sortedSlots.Sort(CompareSlots);
        for (int i = 0; i < sortedSlots.Count; i++)
        {
            sortedSlots[i].Slot.transform.SetSiblingIndex(i);
            SetCardRenderOrder(sortedSlots[i].Slot, i);
        }

        ScheduleHandLayoutRefresh(cardGroup);

        Plugin.Logger?.LogInfo($"Sorted {sortedSlots.Count} hand slots by mana cost.");
    }

    [HideFromIl2Cpp]
    private static List<CardSortEntry> GetSortedCards(CardPileModel cardPile)
    {
        List<CardSortEntry> entries = new();
        for (int i = 0; i < cardPile.Count; i++)
        {
            CardModel card = GetCardAt(cardPile, i);
            if (card != null)
            {
                entries.Add(new CardSortEntry(card, GetSortCost(card), IsWildCard(card), i));
            }
        }

        entries.Sort(CompareCards);
        return entries;
    }

    [HideFromIl2Cpp]
    private static CardModel GetCardAt(CardPileModel cardPile, int index)
    {
        return cardPile.TryPeekIndex(index, out CardModel card) ? card : null;
    }

    [HideFromIl2Cpp]
    private static int CompareCards(CardSortEntry x, CardSortEntry y)
    {
        return CompareSortValues(x.IsWild, x.Cost, x.OriginalIndex, y.IsWild, y.Cost, y.OriginalIndex);
    }

    [HideFromIl2Cpp]
    private static bool IsWildCard(CardModel card)
    {
        try
        {
            CardCostType costType = card?.CardCostType;
            if (costType == null)
            {
                return false;
            }

            return costType is WildCostType || costType.TryCast<WildCostType>() != null;
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogWarning($"Unable to read card cost type: {ex.Message}");
            return false;
        }
    }

    [HideFromIl2Cpp]
    private static int GetSortCost(CardModel card)
    {
        try
        {
            return card.GetCardCostTypeManaCost();
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogWarning($"Unable to read card mana cost: {ex.Message}");
            return int.MaxValue;
        }
    }

    [HideFromIl2Cpp]
    private static List<CardSlot> GetVisibleCardSlots(CardSlotHolder cardGroup)
    {
        if (cardGroup == null)
        {
            return null;
        }

        CardSlot[] allSlots = cardGroup.GetComponentsInChildren<CardSlot>(true);
        List<CardSlot> slots = new();
        foreach (CardSlot slot in allSlots)
        {
            if (slot != null && slot.gameObject.activeInHierarchy && GetSlotCard(slot) != null)
            {
                slots.Add(slot);
            }
        }

        slots.Sort((x, y) => x.transform.GetSiblingIndex().CompareTo(y.transform.GetSiblingIndex()));
        return slots;
    }

    [HideFromIl2Cpp]
    private static CardModel GetSlotCard(CardSlot slot)
    {
        InteractableCard interactableCard = slot?.SlottedInteractableCard;
        return interactableCard?.CardView?.CardModel;
    }

    [HideFromIl2Cpp]
    private static int CompareSlots(CardSlotSortEntry x, CardSlotSortEntry y)
    {
        return CompareSortValues(x.IsWild, x.Cost, x.OriginalIndex, y.IsWild, y.Cost, y.OriginalIndex);
    }

    [HideFromIl2Cpp]
    private static int CompareSortValues(bool xIsWild, int xCost, int xOriginalIndex, bool yIsWild, int yCost, int yOriginalIndex)
    {
        int wildCompare = xIsWild.CompareTo(yIsWild);
        if (wildCompare != 0)
        {
            return wildCompare;
        }

        int costCompare = xCost.CompareTo(yCost);
        return costCompare != 0 ? costCompare : xOriginalIndex.CompareTo(yOriginalIndex);
    }

    [HideFromIl2Cpp]
    private static void SetCardRenderOrder(CardSlot slot, int index)
    {
        CardView cardView = slot?.SlottedInteractableCard?.CardView;
        if (cardView == null)
        {
            return;
        }

        SortingGroup sortingGroup = cardView.GetComponentInChildren<SortingGroup>(true);
        if (sortingGroup != null)
        {
            sortingGroup.sortingOrder = index;
        }

        cardView.transform.SetSiblingIndex(index);
        cardView.TweenContainer?.SetSiblingIndex(index);
    }

    [HideFromIl2Cpp]
    private static void ScheduleHandLayoutRefresh(CardSlotHolder cardGroup)
    {
        _pendingLayoutRefreshCardGroup = cardGroup;
        _pendingLayoutRefreshFrames = 3;
        RefreshHandLayout(cardGroup);
    }

    [HideFromIl2Cpp]
    private static void RefreshHandLayout(CardSlotHolder cardGroup)
    {
        if (cardGroup == null)
        {
            return;
        }

        CardLayoutGroup layoutGroup = cardGroup.GetComponentInChildren<CardLayoutGroup>(true);
        if (layoutGroup == null)
        {
            return;
        }

        layoutGroup.SelectedIndex = -1;
        layoutGroup.ForceLayoutRefresh();
        if (layoutGroup.transform is RectTransform rectTransform)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        Canvas.ForceUpdateCanvases();
    }

    private readonly struct CardSortEntry
    {
        public CardSortEntry(CardModel card, int cost, bool isWild, int originalIndex)
        {
            Card = card;
            Cost = cost;
            IsWild = isWild;
            OriginalIndex = originalIndex;
        }

        public CardModel Card { get; }
        public int Cost { get; }
        public bool IsWild { get; }
        public int OriginalIndex { get; }
    }

    private readonly struct CardSlotSortEntry
    {
        public CardSlotSortEntry(CardSlot slot, int cost, bool isWild, int originalIndex)
        {
            Slot = slot;
            Cost = cost;
            IsWild = isWild;
            OriginalIndex = originalIndex;
        }

        public CardSlot Slot { get; }
        public int Cost { get; }
        public bool IsWild { get; }
        public int OriginalIndex { get; }
    }
}

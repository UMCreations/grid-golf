using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class StoreItemView : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private Image packIconImage;
    [SerializeField] private Sprite packImage;
    [SerializeField] private TMP_Text valueLabel;
    [SerializeField] private TMP_Text priceLabel;
    [SerializeField] private GameObject offerRoot;
    [SerializeField] private TMP_Text offerLabel;
    [SerializeField] private Button buyButton;
    private Action purchaseAction;
    private Sprite defaultPackIcon;

    private void Awake()
    {
        if (itemImage == null)
            itemImage = transform.Find("coins/Image/Image")?.GetComponent<Image>();
        if (packIconImage == null)
            packIconImage = transform.Find("packIcon")?.GetComponent<Image>();
        if (packIconImage != null)
            defaultPackIcon = packIconImage.sprite;
        if (valueLabel == null)
            valueLabel = transform.Find("coins/Image/coinvalue")?.GetComponent<TMP_Text>();
        if (priceLabel == null)
            priceLabel = transform.Find("Button/Text (TMP)")?.GetComponent<TMP_Text>();
        if (offerRoot == null)
            offerRoot = transform.Find("coins/offertag")?.gameObject;
        if (offerLabel == null && offerRoot != null)
            offerLabel = offerRoot.GetComponentInChildren<TMP_Text>(true);
        if (buyButton == null)
            buyButton = transform.Find("Button")?.GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(HandleBuyClicked);
            buyButton.onClick.AddListener(HandleBuyClicked);
        }
    }

    private void OnDisable()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(HandleBuyClicked);
    }

    public void Configure(StorePackDefinition pack, string localizedPrice, bool available, Action onBuy)
    {
        if (pack == null) return;
        purchaseAction = onBuy;

        if (itemImage != null && pack.Image != null)
            itemImage.sprite = pack.Image;
        else if (itemImage != null && packImage != null)
            itemImage.sprite = packImage;
        if (packIconImage != null)
            packIconImage.sprite = pack.PackIcon != null ? pack.PackIcon : defaultPackIcon;
        if (valueLabel != null)
            valueLabel.text = pack.CoinAmount.ToString("N0");
        if (priceLabel != null)
            priceLabel.text = localizedPrice;
        if (buyButton != null)
            buyButton.interactable = available;

        bool showOffer = pack.OfferEnabled;
        if (offerRoot != null)
            offerRoot.SetActive(showOffer);
        if (offerLabel != null)
            offerLabel.text = $"+{pack.OfferPercent}%";
    }

    public void Configure(int coinAmount, string localizedPrice, bool available, Action onBuy)
    {
        purchaseAction = onBuy;
        if (valueLabel != null) valueLabel.text = coinAmount.ToString("N0");
        if (priceLabel != null) priceLabel.text = localizedPrice;
        if (buyButton != null) buyButton.interactable = available;
        if (offerRoot != null) offerRoot.SetActive(false);
    }

    private void HandleBuyClicked()
    {
        purchaseAction?.Invoke();
    }
}

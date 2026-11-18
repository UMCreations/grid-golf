using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR || UNITY_ANDROID
using UnityEngine.Purchasing;
using IapStoreController = UnityEngine.Purchasing.StoreController;
#endif

public sealed class StoreController : MonoBehaviour
{
    private const string SmallPackId = "gridgolf_coins_100";
    private const string MediumPackId = "gridgolf_coins_500";
    private const string LargePackId = "gridgolf_coins_1200";

    [SerializeField] private Button closeButton;
    [Header("Coin Pack Buttons")]
    [SerializeField] private Button smallPackButton;
    [SerializeField] private TMP_Text smallPackLabel;
    [SerializeField] private Button mediumPackButton;
    [SerializeField] private TMP_Text mediumPackLabel;
    [SerializeField] private Button largePackButton;
    [SerializeField] private TMP_Text largePackLabel;

#if UNITY_EDITOR || UNITY_ANDROID
    private IapStoreController iapStore;
    private bool productsFetched;
#endif

    private static readonly CoinPack[] CoinPacks =
    {
        new CoinPack(SmallPackId, 100),
        new CoinPack(MediumPackId, 500),
        new CoinPack(LargePackId, 1200)
    };

    private void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        InitializePurchasing();
#else
        RefreshPackViews("Google Play only", false);
#endif
    }

    private void OnEnable()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseStore);
        if (smallPackButton != null)
            smallPackButton.onClick.AddListener(BuySmallPack);
        if (mediumPackButton != null)
            mediumPackButton.onClick.AddListener(BuyMediumPack);
        if (largePackButton != null)
            largePackButton.onClick.AddListener(BuyLargePack);
#if UNITY_EDITOR || UNITY_ANDROID
        if (productsFetched) RefreshPackViews();
#endif
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseStore);
        if (smallPackButton != null)
            smallPackButton.onClick.RemoveListener(BuySmallPack);
        if (mediumPackButton != null)
            mediumPackButton.onClick.RemoveListener(BuyMediumPack);
        if (largePackButton != null)
            largePackButton.onClick.RemoveListener(BuyLargePack);
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        if (iapStore == null) return;
        iapStore.OnStoreConnected -= OnStoreConnected;
        iapStore.OnProductsFetched -= OnProductsFetched;
        iapStore.OnProductsFetchFailed -= OnProductsFetchFailed;
        iapStore.OnPurchasePending -= OnPurchasePending;
        iapStore.OnPurchaseFailed -= OnPurchaseFailed;
#endif
    }

    public void OpenStore()
    {
        gameObject.SetActive(true);
    }

    private void CloseStore()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        gameObject.SetActive(false);
    }

    private void BuySmallPack() => PurchasePack(0);
    private void BuyMediumPack() => PurchasePack(1);
    private void BuyLargePack() => PurchasePack(2);

    private void PurchasePack(int packIndex)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

#if UNITY_ANDROID && !UNITY_EDITOR
        if (iapStore == null || !productsFetched)
        {
            Debug.LogWarning("Google Play products are not ready yet.");
            return;
        }

        iapStore.PurchaseProduct(CoinPacks[packIndex].ProductId);
#else
        Debug.Log("Google Play purchases are available in an Android build installed through Google Play.");
#endif
    }

#if UNITY_EDITOR || UNITY_ANDROID
    private async void InitializePurchasing()
    {
        iapStore = UnityIAPServices.StoreController();
        iapStore.OnStoreConnected += OnStoreConnected;
        iapStore.OnProductsFetched += OnProductsFetched;
        iapStore.OnProductsFetchFailed += OnProductsFetchFailed;
        iapStore.OnPurchasePending += OnPurchasePending;
        iapStore.OnPurchaseFailed += OnPurchaseFailed;

        try
        {
            await iapStore.Connect();
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"Google Play Billing connection failed: {exception.Message}");
        }
    }

    private void OnStoreConnected()
    {
        var definitions = CoinPacks
            .Select(pack => new ProductDefinition(pack.ProductId, ProductType.Consumable))
            .ToList();
        iapStore.FetchProducts(definitions);
    }

    private void OnProductsFetched(List<Product> _)
    {
        productsFetched = true;
        RefreshPackViews();
    }

    private void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        Debug.LogWarning($"Google Play product fetch failed: {failure.FailureReason}");
        RefreshPackViews("Unavailable", false);
    }

    private void RefreshPackViews(string unavailableText = null, bool useStorePrices = true)
    {
        for (int i = 0; i < CoinPacks.Length; i++)
        {
            CoinPack pack = CoinPacks[i];
            Product product = useStorePrices && iapStore != null
                ? iapStore.GetProductById(pack.ProductId)
                : null;
            bool available = product != null && product.availableToPurchase;
            Button button = GetPackButton(i);
            TMP_Text label = GetPackLabel(i);

            if (button != null)
                button.interactable = available;
            if (label != null)
            {
                string price = available && product.metadata != null
                    ? product.metadata.localizedPriceString
                    : unavailableText ?? "Price unavailable";
                label.text = $"{pack.Coins:N0} GolfCoins\n{price}";
            }
        }
    }

    private void OnPurchasePending(PendingOrder order)
    {
        List<CartItem> items = order.CartOrdered.Items().ToList();
        if (items.Count != 1 || items[0]?.Product?.definition == null || items[0].Quantity < 1)
        {
            Debug.LogError("Cannot grant GolfCoins for an invalid or multi-item purchase order.");
            return;
        }

        string productId = items[0].Product.definition.id;
        CoinPack pack = CoinPacks.FirstOrDefault(candidate => candidate.ProductId == productId);
        string purchaseToken = order.Info.Google?.PurchaseToken ?? order.Info.TransactionID;
        if (string.IsNullOrEmpty(pack.ProductId) || string.IsNullOrWhiteSpace(purchaseToken))
        {
            Debug.LogError($"Unrecognized or unverifiable GolfCoins purchase: {productId}");
            return;
        }

        int grantAmount;
        try
        {
            grantAmount = checked(pack.Coins * items[0].Quantity);
        }
        catch (System.OverflowException)
        {
            Debug.LogError("GolfCoins purchase amount overflowed.");
            return;
        }

        WalletService wallet = LevelManager.Instance != null ? LevelManager.Instance.Wallet : null;
        if (wallet == null || !wallet.TryGrantPurchasedGolfCoins(grantAmount, purchaseToken))
        {
            Debug.LogError("GolfCoins purchase could not be granted. Purchase remains pending for retry.");
            return;
        }

        iapStore.ConfirmPurchase(order);
    }

    private void OnPurchaseFailed(FailedOrder order)
    {
        Debug.LogWarning($"GolfCoins purchase failed: {order.FailureReason}");
    }
#endif

    private Button GetPackButton(int index)
    {
        switch (index)
        {
            case 0: return smallPackButton;
            case 1: return mediumPackButton;
            default: return largePackButton;
        }
    }

    private TMP_Text GetPackLabel(int index)
    {
        switch (index)
        {
            case 0: return smallPackLabel;
            case 1: return mediumPackLabel;
            default: return largePackLabel;
        }
    }

    private readonly struct CoinPack
    {
        public string ProductId { get; }
        public int Coins { get; }

        public CoinPack(string productId, int coins)
        {
            ProductId = productId;
            Coins = coins;
        }
    }
}

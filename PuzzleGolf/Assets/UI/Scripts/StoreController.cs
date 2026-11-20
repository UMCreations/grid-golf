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
    [Header("Catalog-driven store (optional; uses legacy fields when empty)")]
    [SerializeField] private StorePackCatalog catalog;
    [SerializeField] private RectTransform itemsContainer;
    [SerializeField] private StoreItemView itemPrefab;
    [SerializeField] private StoreItemView[] storeItemViews;
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

    private readonly List<StoreItemView> activeItemViews = new List<StoreItemView>();
    private readonly List<StorePackDefinition> activePacks = new List<StorePackDefinition>();

    private static readonly CoinPack[] CoinPacks =
    {
        new CoinPack(SmallPackId, 100),
        new CoinPack(MediumPackId, 500),
        new CoinPack(LargePackId, 1200)
    };

    private void Awake()
    {
        BuildItemViews();

#if UNITY_EDITOR || UNITY_ANDROID
        InitializePurchasing();
#else
        RefreshPackViews("Android only", false);
#endif
    }

    private void OnEnable()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseStore);
        if (!HasCatalog)
        {
            if (smallPackButton != null)
                smallPackButton.onClick.AddListener(BuySmallPack);
            if (mediumPackButton != null)
                mediumPackButton.onClick.AddListener(BuyMediumPack);
            if (largePackButton != null)
                largePackButton.onClick.AddListener(BuyLargePack);
        }
#if UNITY_EDITOR || UNITY_ANDROID
        if (productsFetched) RefreshPackViews();
#endif
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseStore);
        if (!HasCatalog)
        {
            if (smallPackButton != null)
                smallPackButton.onClick.RemoveListener(BuySmallPack);
            if (mediumPackButton != null)
                mediumPackButton.onClick.RemoveListener(BuyMediumPack);
            if (largePackButton != null)
                largePackButton.onClick.RemoveListener(BuyLargePack);
        }
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

    private void BuySmallPack() => PurchasePack(CoinPacks[0].ProductId);
    private void BuyMediumPack() => PurchasePack(CoinPacks[1].ProductId);
    private void BuyLargePack() => PurchasePack(CoinPacks[2].ProductId);

    private void PurchasePack(string productId)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

#if UNITY_EDITOR || UNITY_ANDROID
        if (iapStore == null || !productsFetched)
        {
            Debug.LogWarning("IAP products are not ready yet.");
            return;
        }

        iapStore.PurchaseProduct(productId);
#else
        Debug.Log("Google Play purchases are available in an Android build installed through Google Play.");
#endif
    }

    private bool HasCatalog => catalog != null;

    private void BuildItemViews()
    {
        activeItemViews.Clear();
        activePacks.Clear();

        if (HasCatalog)
        {
            activePacks.AddRange(catalog.Packs
                .Where(pack => pack != null && !string.IsNullOrWhiteSpace(pack.ProductId))
                .GroupBy(pack => pack.ProductId)
                .Select(group => group.First()));
            if (activePacks.Count == 0)
                Debug.LogWarning("StorePackCatalog has no valid packs. Add entries with unique Google Play product IDs.", this);

            if (itemsContainer != null && itemPrefab != null)
            {
                EnsureItemLayout();
                foreach (StorePackDefinition _ in activePacks)
                    activeItemViews.Add(Instantiate(itemPrefab, itemsContainer));
            }
            else
            {
                activeItemViews.AddRange(GetComponentsInChildren<StoreItemView>(true));
                Debug.LogWarning("Store catalog is assigned, but Items Container or Item Prefab is missing.", this);
            }
            return;
        }

        if (storeItemViews != null)
            activeItemViews.AddRange(storeItemViews.Where(view => view != null));
        if (activeItemViews.Count == 0)
            activeItemViews.AddRange(GetComponentsInChildren<StoreItemView>(true));
    }

    private void EnsureItemLayout()
    {
        if (itemsContainer.GetComponent<LayoutGroup>() != null) return;

        RectTransform prefabRect = itemPrefab.transform as RectTransform;
        var grid = itemsContainer.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = prefabRect != null && prefabRect.sizeDelta.x > 0 && prefabRect.sizeDelta.y > 0
            ? prefabRect.sizeDelta
            : new Vector2(230, 250);
        grid.spacing = new Vector2(12, 12);
        grid.padding = new RectOffset(8, 8, 8, 8);
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;
    }

    private int PackCount => HasCatalog ? activePacks.Count : CoinPacks.Length;

    private string GetProductId(int index)
    {
        return HasCatalog ? activePacks[index].ProductId : CoinPacks[index].ProductId;
    }

    private StorePackDefinition GetCatalogPack(string productId)
    {
        return HasCatalog ? activePacks.FirstOrDefault(pack => pack.ProductId == productId) : null;
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
            Debug.LogError($"IAP store connection failed: {exception.Message}");
        }
    }

    private void OnStoreConnected()
    {
        var definitions = Enumerable.Range(0, PackCount)
            .Select(GetProductId)
            .Where(productId => !string.IsNullOrWhiteSpace(productId))
            .Distinct()
            .Select(productId => new ProductDefinition(productId, ProductType.Consumable))
            .ToList();
        if (definitions.Count == 0)
        {
            Debug.LogError("Store catalog contains no valid IAP product IDs.", this);
            return;
        }
        iapStore.FetchProducts(definitions);
    }

    private void OnProductsFetched(List<Product> _)
    {
        productsFetched = true;
        RefreshPackViews();
    }

    private void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        Debug.LogWarning($"IAP product fetch failed: {failure.FailureReason}");
        RefreshPackViews("Unavailable", false);
    }

    private void RefreshPackViews(string unavailableText = null, bool useStorePrices = true)
    {
        for (int i = 0; i < PackCount; i++)
        {
            string productId = GetProductId(i);
            Product product = useStorePrices && iapStore != null
                ? iapStore.GetProductById(productId)
                : null;
            bool available = product != null && product.availableToPurchase;
            string price = available && product.metadata != null
                ? product.metadata.localizedPriceString
                : unavailableText ?? "Price unavailable";
            StorePackDefinition definition = GetCatalogPack(productId);
#if UNITY_EDITOR
            if (available && definition != null && !string.IsNullOrWhiteSpace(definition.EditorPreviewPrice))
                price = definition.EditorPreviewPrice;
#endif

            if (i < activeItemViews.Count && activeItemViews[i] != null)
            {
                if (definition != null)
                    activeItemViews[i].Configure(definition, price, available, () => PurchasePack(productId));
                else
                    activeItemViews[i].Configure(CoinPacks[i].Coins, price, available, () => PurchasePack(productId));
                continue;
            }

            if (HasCatalog) continue;

            Button button = GetPackButton(i);
            TMP_Text label = GetPackLabel(i);

            if (button != null)
                button.interactable = available;
            if (label != null)
                label.text = $"{CoinPacks[i].Coins:N0} GolfCoins\n{price}";
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
        StorePackDefinition catalogPack = GetCatalogPack(productId);
        CoinPack pack = catalogPack == null
            ? CoinPacks.FirstOrDefault(candidate => candidate.ProductId == productId)
            : default;
        string purchaseToken = order.Info.Google?.PurchaseToken ?? order.Info.TransactionID;
        if ((catalogPack == null && string.IsNullOrEmpty(pack.ProductId)) || string.IsNullOrWhiteSpace(purchaseToken))
        {
            Debug.LogError($"Unrecognized or unverifiable GolfCoins purchase: {productId}");
            return;
        }

        int grantAmount;
        try
        {
            grantAmount = catalogPack != null
                ? catalogPack.GetGrantAmount(items[0].Quantity)
                : checked(pack.Coins * items[0].Quantity);
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

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Grid Golf/Store Pack Catalog", fileName = "StorePackCatalog")]
public sealed class StorePackCatalog : ScriptableObject
{
    [SerializeField] private List<StorePackDefinition> packs = new List<StorePackDefinition>();

    public IReadOnlyList<StorePackDefinition> Packs => packs;

    private void OnValidate()
    {
        if (packs == null) packs = new List<StorePackDefinition>();
        var productIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (StorePackDefinition pack in packs)
        {
            if (pack == null) continue;
            pack.Validate(productIds);
        }
    }
}

[Serializable]
public sealed class StorePackDefinition
{
    [SerializeField] private string productId;
    [SerializeField, Min(1)] private int coinAmount = 100;
    [SerializeField, Tooltip("Displayed only in the Unity Editor; runtime prices come from the store.")] private string editorPreviewPrice;
    [SerializeField] private Sprite image;
    [SerializeField] private Sprite packIcon;
    [SerializeField] private bool offerEnabled;
    [SerializeField, Min(0)] private int offerPercent;

    public string ProductId => productId;
    public int CoinAmount => coinAmount;
    public string EditorPreviewPrice => editorPreviewPrice;
    public Sprite Image => image;
    public Sprite PackIcon => packIcon;
    public bool OfferEnabled => offerEnabled && offerPercent > 0;
    public int OfferPercent => offerPercent;

    public int GetGrantAmount(int quantity)
    {
        if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));
        int bonus = OfferEnabled ? checked(coinAmount * offerPercent / 100) : 0;
        return checked(checked(coinAmount + bonus) * quantity);
    }

    internal void Validate(HashSet<string> productIds)
    {
        productId = productId?.Trim();
        coinAmount = Math.Max(1, coinAmount);
        offerPercent = Math.Max(0, offerPercent);

        if (string.IsNullOrWhiteSpace(productId))
            Debug.LogWarning("Store pack is missing its Google Play product ID.");
        else if (!productIds.Add(productId))
            Debug.LogWarning($"Duplicate store product ID '{productId}' in StorePackCatalog.");
    }
}

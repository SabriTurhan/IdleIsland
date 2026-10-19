using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace IslandEscape
{
    /// <summary>
    /// Yandex Games purchases. Product ids in the Developer Console: starter_pack, noads, gold_small,
    /// gold_medium, gold_big, pirate_outfit. Only products present in the console catalog are offered (the store hides
    /// the others). Consumables are consumed right after the reward is granted; unprocessed purchases are
    /// handled at start. Non-consumables (no ads, starter pack, pirate outfit) stay unconsumed, so they restore on any device.
    /// </summary>
    public class YandexIAPWrapper : IAPWrapper
    {
        public static string YandexId(ProductKeyType key)
        {
            switch (key)
            {
                case ProductKeyType.NoAds: return "noads";
                case ProductKeyType.StarterPack: return "starter_pack";
                case ProductKeyType.GoldSmall: return "gold_small";
                case ProductKeyType.GoldMedium: return "gold_medium";
                case ProductKeyType.GoldBig: return "gold_big";
                case ProductKeyType.PlayerSkin: return "pirate_outfit";
                default: return key.ToString().ToLowerInvariant();
            }
        }

        private readonly Dictionary<ProductKeyType, YandexGames.Product> catalog = new Dictionary<ProductKeyType, YandexGames.Product>();
        private readonly HashSet<ProductKeyType> owned = new HashSet<ProductKeyType>();
        private static readonly List<YandexGames.Purchase> pending = new List<YandexGames.Purchase>();

        /// <summary>
        /// Takes one unprocessed purchase of this product (made in an earlier session) and consumes it.
        /// The caller grants the reward first.
        /// </summary>
        public static bool ClaimPending(ProductKeyType key, System.Action grant)
        {
            for (int i = 0; i < pending.Count; i++)
            {
                if (YandexId(key) != pending[i].productID) continue;

                YandexGames.Purchase purchase = pending[i];
                pending.RemoveAt(i);
                grant();
                SaveController.Save(true);
                YandexGames.Consume(purchase.purchaseToken);
                return true;
            }

            return false;
        }

        public static bool HasPending => pending.Count > 0;

        /// <summary>True when the console has active products (otherwise no purchase UI may be shown).</summary>
        public static bool HasProducts { get; private set; }

        public override Task Init(IAPSettings settings)
        {
            catalog.Clear();
            foreach (YandexGames.Product product in YandexGames.GetCatalog())
                if (TryGetKey(product.id, out ProductKeyType key)) catalog[key] = product;

            HasProducts = catalog.Count > 0;

            IAPManager.OnModuleInitialized();

            // unprocessed purchases: consumables wait until their store offer can grant the reward (then
            // they are consumed), non-consumables are owned for good
            pending.Clear();
            foreach (YandexGames.Purchase purchase in YandexGames.GetUnprocessedPurchases())
            {
                if (!TryGetKey(purchase.productID, out ProductKeyType key)) continue;

                if (IsConsumable(key))
                {
                    pending.Add(purchase);
                }
                else
                {
                    owned.Add(key);
                    // both the no-ads item and the starter pack (it includes no ads) turn the interstitials off
                    if (key == ProductKeyType.NoAds || key == ProductKeyType.StarterPack) AdsManager.DisableForcedAd();
                }
            }

            return Task.CompletedTask;
        }

        public override void BuyProduct(ProductKeyType productKeyType)
        {
            if (!catalog.ContainsKey(productKeyType)) return;

            SystemMessage.ShowLoadingPanel();
            YandexGames.BuyProduct(YandexId(productKeyType), purchase =>
            {
                SystemMessage.HideLoadingPanel();

                if (IsConsumable(productKeyType))
                {
                    IAPManager.OnPurchaseCompleted(productKeyType);
                    SaveController.Save(true);
                    YandexGames.Consume(purchase.purchaseToken);
                }
                else
                {
                    owned.Add(productKeyType);
                    IAPManager.OnPurchaseCompleted(productKeyType);
                    SaveController.Save(true);
                }
            }, _ =>
            {
                SystemMessage.HideLoadingPanel();
                IAPManager.OnPurchaseFailed(productKeyType, PurchaseFailureReason.UserCancelled);
            });
        }

        public override ProductData GetProductData(ProductKeyType productKeyType)
        {
            if (!catalog.TryGetValue(productKeyType, out YandexGames.Product product)) return null;

            IAPItem item = IAPManager.GetIAPItem(productKeyType);
            ProductType type = item != null ? item.ProductType : ProductType.Consumable;
            decimal.TryParse(product.priceValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal price);

            return new ProductData(type, price, product.priceCurrencyCode, owned.Contains(productKeyType), product.priceValue + " " + product.priceCurrencyCode, product.currencyImage);
        }

        public override bool IsSubscribed(ProductKeyType productKeyType) => false;

        public override void RestorePurchases() { }

        private static bool IsConsumable(ProductKeyType key)
        {
            IAPItem item = IAPManager.GetIAPItem(key);
            return item == null || item.ProductType == ProductType.Consumable;
        }

        private static bool TryGetKey(string yandexId, out ProductKeyType key)
        {
            foreach (ProductKeyType candidate in System.Enum.GetValues(typeof(ProductKeyType)))
            {
                if (YandexId(candidate) == yandexId)
                {
                    key = candidate;
                    return true;
                }
            }

            key = default;
            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace TubityWAI
{
    /// <summary>
    /// Singleton manager that handles Unity In-App Purchasing (IAP 5).
    /// Products: Remove Ads (non-consumable) and a 100-coin pack (consumable).
    /// </summary>
    public class IAPManager : MonoBehaviour
    {
        public static IAPManager Instance { get; private set; }

        public const string ProductRemoveAds = "com.dubchuck.tubityx.removeads";
        public const string ProductCoins100 = "com.dubchuck.tubityx.coins100";

        public const int Coins100Amount = 100;
        public const string Coins100FallbackPrice = "$0.99";
        public const string RemoveAdsFallbackPrice = "$0.99";

        // Consumable grants are keyed by transaction so a re-delivered pending
        // order (app killed before ConfirmPurchase) never pays out twice.
        private const string PrefGrantedTxPrefix = "IAP_Granted_";

        private StoreController _storeController;
        private bool _isInitialized;
        private bool _productsFetched;

        public event Action OnPurchaseComplete;
        public event Action<string> OnPurchaseFailedEvent;
        public event Action OnRestoreComplete;
        /// <summary>Fired once store prices are available for display.</summary>
        public event Action OnProductsReady;
        /// <summary>Fired after coins are credited; carries the amount granted.</summary>
        public event Action<int> OnCoinsPurchased;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePurchasing();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async void InitializePurchasing()
        {
            if (_isInitialized) return;

            Debug.Log("[IAPManager] Initializing Unity Purchasing (IAP 5)...");
            _storeController = UnityIAPServices.StoreController();

            // Register every handler before connecting: pending purchases from a
            // previous session can fire the moment the store connects.
            // (IAP 5.0.0 has no OnStoreConnected event; Connect() completing is the signal.)
            _storeController.OnStoreDisconnected += OnStoreDisconnected;
            _storeController.OnProductsFetched += OnProductsFetched;
            _storeController.OnProductsFetchFailed += OnProductsFetchFailed;
            _storeController.OnPurchasePending += OnPurchasePending;
            _storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _storeController.OnPurchaseFailed += OnPurchaseFailed;
            _storeController.OnPurchaseDeferred += OnPurchaseDeferred;
            _storeController.OnCheckEntitlement += OnCheckEntitlement;

            try
            {
                await _storeController.Connect();
                OnStoreConnected();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IAPManager] Store Initialization failed: {ex.Message}");
            }
        }

        private static List<ProductDefinition> BuildCatalog()
        {
            return new List<ProductDefinition>
            {
                new ProductDefinition(ProductRemoveAds, ProductType.NonConsumable),
                new ProductDefinition(ProductCoins100, ProductType.Consumable)
            };
        }

        public bool IsInitialized()
        {
            return _isInitialized && _storeController != null;
        }

        /// <summary>
        /// Store-localized price for a product, or the fallback when the
        /// catalog has not been fetched yet (or in the editor).
        /// </summary>
        public string GetLocalizedPrice(string productId, string fallback)
        {
            if (!_productsFetched || _storeController == null) return fallback;

            Product product = _storeController.GetProductById(productId);
            string price = product?.metadata?.localizedPriceString;
            return string.IsNullOrEmpty(price) ? fallback : price;
        }

        // ------------------------------------------------------------------
        // Purchase entry points
        // ------------------------------------------------------------------

        public void BuyRemoveAds()
        {
            Debug.Log("[IAPManager] BuyRemoveAds requested.");
            if (Application.isEditor || !IsInitialized())
            {
                Debug.Log("[IAPManager] (EDITOR TEST SIMULATION) Purchase successful! Granting 'Remove Ads' entitlement.");
                SetAdsRemoved(true);
                OnPurchaseComplete?.Invoke();
                return;
            }

            StartPurchase(ProductRemoveAds);
        }

        public void BuyCoins100()
        {
            Debug.Log("[IAPManager] BuyCoins100 requested.");
            if (Application.isEditor || !IsInitialized())
            {
                Debug.Log($"[IAPManager] (EDITOR TEST SIMULATION) Purchase successful! Granting {Coins100Amount} coins.");
                GrantCoins(Coins100Amount);
                OnPurchaseComplete?.Invoke();
                return;
            }

            StartPurchase(ProductCoins100);
        }

        private void StartPurchase(string productId)
        {
            if (!IsInitialized())
            {
                Debug.LogError($"[IAPManager] Purchase of '{productId}' failed: Store is not initialized.");
                OnPurchaseFailedEvent?.Invoke("Store not initialized");
                return;
            }

            Product product = _storeController.GetProductById(productId);
            if (product != null && product.availableToPurchase)
            {
                Debug.Log($"[IAPManager] Initiating purchase for: '{product.definition.id}'");
                _storeController.PurchaseProduct(product);
            }
            else
            {
                Debug.LogError($"[IAPManager] Purchase of '{productId}' failed: Product not found or not available for purchase.");
                OnPurchaseFailedEvent?.Invoke("Product not available");
            }
        }

        public void RestorePurchases()
        {
            Debug.Log("[IAPManager] RestorePurchases requested.");
            if (Application.isEditor || !IsInitialized())
            {
                Debug.Log("[IAPManager] (EDITOR TEST SIMULATION) RestorePurchases transaction sync successful.");
                SetAdsRemoved(true);
                OnRestoreComplete?.Invoke();
                return;
            }

            Debug.Log("[IAPManager] RestorePurchases starting...");
            _storeController.RestoreTransactions((success, error) => {
                if (success)
                {
                    Debug.Log("[IAPManager] RestorePurchases transaction sync successful.");
                    CheckActivePurchases();
                    OnRestoreComplete?.Invoke();
                }
                else
                {
                    Debug.LogError($"[IAPManager] RestorePurchases failed: {error}");
                    OnPurchaseFailedEvent?.Invoke($"Restore failed: {error}");
                }
            });
        }

        // Only non-consumables carry an entitlement; coin packs are consumed on grant.
        private void CheckActivePurchases()
        {
            if (_storeController == null) return;

            Product removeAdsProduct = _storeController.GetProductById(ProductRemoveAds);
            if (removeAdsProduct != null)
            {
                _storeController.CheckEntitlement(removeAdsProduct);
            }
        }

        // ------------------------------------------------------------------
        // Persistence
        // ------------------------------------------------------------------

        public bool IsAdsRemoved()
        {
            return PlayerPrefs.GetInt("AdsRemoved", 0) == 1;
        }

        private void SetAdsRemoved(bool removed)
        {
            PlayerPrefs.SetInt("AdsRemoved", removed ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[IAPManager] AdsRemoved PlayerPref set to: {removed}");
        }

        private void GrantCoins(int amount)
        {
            GameManager.GrantCoins(amount);
            Debug.Log($"[IAPManager] Granted {amount} coins. New total: {PlayerPrefs.GetInt("TotalCoins", 0)}");
            OnCoinsPurchased?.Invoke(amount);
        }

        private static bool WasTransactionGranted(string transactionId)
        {
            return !string.IsNullOrEmpty(transactionId)
                && PlayerPrefs.GetInt(PrefGrantedTxPrefix + transactionId, 0) == 1;
        }

        private static void MarkTransactionGranted(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return;
            PlayerPrefs.SetInt(PrefGrantedTxPrefix + transactionId, 1);
            PlayerPrefs.Save();
        }

        // ------------------------------------------------------------------
        // Unity IAP 5 event handlers
        // ------------------------------------------------------------------

        private void OnStoreConnected()
        {
            Debug.Log("[IAPManager] Unity IAP 5 Connected successfully. Fetching products...");
            _isInitialized = true;
            _storeController.FetchProducts(BuildCatalog());
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            Debug.LogError($"[IAPManager] Store disconnected: {failure.Message}");
        }

        private void OnProductsFetched(List<Product> products)
        {
            _productsFetched = true;
            foreach (var p in products)
            {
                Debug.Log($"[IAPManager] Product ready: {p.definition.id} @ {p.metadata?.localizedPriceString}");
            }
            CheckActivePurchases();
            OnProductsReady?.Invoke();
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            Debug.LogError($"[IAPManager] Product fetch failed: {failure.FailureReason}");
        }

        private void OnCheckEntitlement(Entitlement entitlement)
        {
            if (entitlement?.Product == null) return;
            if (entitlement.Product.definition.id != ProductRemoveAds) return;

            if (entitlement.Status == EntitlementStatus.FullyEntitled)
            {
                Debug.Log("[IAPManager] Active entitlement found for Remove Ads. Granting product.");
                SetAdsRemoved(true);
            }
        }

        private void OnPurchasePending(PendingOrder order)
        {
            if (order == null || order.CartOrdered == null) return;

            string txId = order.Info?.TransactionID;
            bool grantedSomething = false;

            foreach (var cartItem in order.CartOrdered.Items())
            {
                var product = cartItem?.Product;
                if (product == null) continue;
                string id = product.definition.id;

                if (string.Equals(id, ProductRemoveAds, StringComparison.Ordinal))
                {
                    Debug.Log($"[IAPManager] Purchase successful for: '{id}'");
                    SetAdsRemoved(true);
                    grantedSomething = true;
                }
                else if (string.Equals(id, ProductCoins100, StringComparison.Ordinal))
                {
                    if (WasTransactionGranted(txId))
                    {
                        Debug.Log($"[IAPManager] Coins for transaction '{txId}' already granted; confirming only.");
                    }
                    else
                    {
                        int qty = Mathf.Max(1, cartItem.Quantity);
                        Debug.Log($"[IAPManager] Purchase successful for: '{id}' x{qty}");
                        GrantCoins(Coins100Amount * qty);
                        MarkTransactionGranted(txId);
                    }
                    grantedSomething = true;
                }
                else
                {
                    Debug.LogWarning($"[IAPManager] Pending order for unknown product '{id}'. Confirming to clear it.");
                }
            }

            _storeController.ConfirmPurchase(order);
            if (grantedSomething) OnPurchaseComplete?.Invoke();
        }

        private void OnPurchaseConfirmed(Order order)
        {
            switch (order)
            {
                case ConfirmedOrder confirmed:
                    Debug.Log($"[IAPManager] Purchase confirmed (tx {confirmed.Info?.TransactionID}).");
                    break;
                case FailedOrder failed:
                    Debug.LogError($"[IAPManager] Purchase confirmation failed: {failed.FailureReason} - {failed.Details}");
                    break;
            }
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            Debug.LogError($"[IAPManager] Purchase failed. Reason: {order.FailureReason}, Details: {order.Details}");
            OnPurchaseFailedEvent?.Invoke(order.FailureReason.ToString());
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            // Ask-to-Buy / parental approval. Nothing is granted until OnPurchasePending fires.
            Debug.Log("[IAPManager] Purchase deferred, awaiting approval.");
        }

        private void OnDestroy()
        {
            if (_storeController != null)
            {
                _storeController.OnStoreDisconnected -= OnStoreDisconnected;
                _storeController.OnProductsFetched -= OnProductsFetched;
                _storeController.OnProductsFetchFailed -= OnProductsFetchFailed;
                _storeController.OnPurchasePending -= OnPurchasePending;
                _storeController.OnPurchaseConfirmed -= OnPurchaseConfirmed;
                _storeController.OnPurchaseFailed -= OnPurchaseFailed;
                _storeController.OnPurchaseDeferred -= OnPurchaseDeferred;
                _storeController.OnCheckEntitlement -= OnCheckEntitlement;
            }
        }
    }
}

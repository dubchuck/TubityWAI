using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace TubityWAI
{
    /// <summary>
    /// Singleton manager that handles Unity In-App Purchasing (IAP 5) to unlock features (Remove Ads).
    /// </summary>
    public class IAPManager : MonoBehaviour
    {
        public static IAPManager Instance { get; private set; }

        public const string ProductRemoveAds = "com.dubchuck.tubityx.removeads";

        private StoreController _storeController;
        private bool _isInitialized;

        public event Action OnPurchaseComplete;
        public event Action<string> OnPurchaseFailedEvent;
        public event Action OnRestoreComplete;

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

            var products = new List<ProductDefinition>
            {
                new ProductDefinition(ProductRemoveAds, ProductType.NonConsumable)
            };

            // Register event handlers before connecting
            _storeController.OnPurchasePending += OnPurchasePending;
            _storeController.OnPurchaseFailed += OnPurchaseFailed;

            try
            {
                await _storeController.Connect();
                _storeController.FetchProducts(products);
                _isInitialized = true;
                Debug.Log("[IAPManager] Unity IAP 5 Connected successfully.");
                CheckActivePurchases();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IAPManager] Store Initialization failed: {ex.Message}");
            }
        }

        public bool IsInitialized()
        {
            return _isInitialized && _storeController != null;
        }

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

            if (IsInitialized())
            {
                Product product = _storeController.GetProductById(ProductRemoveAds);
                if (product != null && product.availableToPurchase)
                {
                    Debug.Log($"[IAPManager] Initiating purchase for: '{product.definition.id}'");
                    _storeController.PurchaseProduct(product.definition.id);
                }
                else
                {
                    Debug.LogError("[IAPManager] BuyRemoveAds failed: Product not found or not available for purchase.");
                    OnPurchaseFailedEvent?.Invoke("Product not available");
                }
            }
            else
            {
                Debug.LogError("[IAPManager] BuyRemoveAds failed: Store is not initialized.");
                OnPurchaseFailedEvent?.Invoke("Store not initialized");
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

            if (!IsInitialized())
            {
                Debug.LogError("[IAPManager] RestorePurchases failed: Store is not initialized.");
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

        private void CheckActivePurchases()
        {
            if (_storeController != null)
            {
                Product removeAdsProduct = _storeController.GetProductById(ProductRemoveAds);
                if (removeAdsProduct != null)
                {
                    _storeController.OnCheckEntitlement += entitlement => {
                        if (entitlement != null && entitlement.Status == EntitlementStatus.FullyEntitled)
                        {
                            Debug.Log("[IAPManager] Active entitlement found for Remove Ads. Granting product.");
                            SetAdsRemoved(true);
                        }
                    };
                    _storeController.CheckEntitlement(removeAdsProduct);
                }
            }
        }

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

        // --- Unity IAP 5 Event Handlers ---

        private void OnPurchasePending(PendingOrder order)
        {
            if (order != null && order.CartOrdered != null)
            {
                foreach (var cartItem in order.CartOrdered.Items())
                {
                    var product = cartItem?.Product;
                    if (product != null && string.Equals(product.definition.id, ProductRemoveAds, StringComparison.Ordinal))
                    {
                        Debug.Log($"[IAPManager] Purchase successful for: '{product.definition.id}'");
                        SetAdsRemoved(true);
                        _storeController.ConfirmPurchase(order);
                        OnPurchaseComplete?.Invoke();
                        return;
                    }
                }
            }
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            Debug.LogError($"[IAPManager] Purchase failed. Reason: {order.FailureReason}, Details: {order.Details}");
            OnPurchaseFailedEvent?.Invoke(order.FailureReason.ToString());
        }

        private void OnDestroy()
        {
            if (_storeController != null)
            {
                _storeController.OnPurchasePending -= OnPurchasePending;
                _storeController.OnPurchaseFailed -= OnPurchaseFailed;
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

public sealed class StoreController : MonoBehaviour
{
    private const int TemporaryCoinGrant = 50;

    [SerializeField] private Button closeButton;
    [SerializeField] private Button temporaryCoinButton;

    private void OnEnable()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseStore);
        if (temporaryCoinButton != null)
            temporaryCoinButton.onClick.AddListener(GrantTemporaryCoins);
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseStore);
        if (temporaryCoinButton != null)
            temporaryCoinButton.onClick.RemoveListener(GrantTemporaryCoins);
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

    private void GrantTemporaryCoins()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

        WalletService wallet = LevelManager.Instance != null ? LevelManager.Instance.Wallet : null;
        if (wallet == null || !wallet.TryGrantGolfCoins(TemporaryCoinGrant))
            Debug.LogWarning("Could not grant temporary GolfCoins: wallet is unavailable.");
    }
}

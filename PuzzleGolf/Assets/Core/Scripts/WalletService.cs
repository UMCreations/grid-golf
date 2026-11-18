using System;

public sealed class WalletService
{
    private readonly Func<PlayerProfile> getProfile;
    private readonly Action saveProfile;

    public event Action<int> GolfCoinsChanged;

    public int GolfCoins => Math.Max(0, getProfile()?.golfCoins ?? 0);

    public WalletService(Func<PlayerProfile> getProfile, Action saveProfile)
    {
        this.getProfile = getProfile ?? throw new ArgumentNullException(nameof(getProfile));
        this.saveProfile = saveProfile ?? throw new ArgumentNullException(nameof(saveProfile));
    }

    public bool TryGrantGolfCoins(int amount)
    {
        PlayerProfile profile = getProfile();
        if (profile == null || amount <= 0 || profile.golfCoins < 0 ||
            profile.golfCoins > int.MaxValue - amount)
            return false;

        profile.golfCoins += amount;
        saveProfile();
        GolfCoinsChanged?.Invoke(profile.golfCoins);
        return true;
    }

    public bool TrySpendGolfCoins(int amount)
    {
        PlayerProfile profile = getProfile();
        if (profile == null || amount <= 0 || profile.golfCoins < amount)
            return false;

        profile.golfCoins -= amount;
        saveProfile();
        GolfCoinsChanged?.Invoke(profile.golfCoins);
        return true;
    }
}

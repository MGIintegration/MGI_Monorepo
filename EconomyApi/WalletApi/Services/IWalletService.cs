using WalletApi.Models;

namespace WalletApi.Services;

public interface IWalletService
{
    bool TryDisplay(string playerId, out WalletDisplayResponse response);
    WalletActionResponse Update(WalletUpdateRequest request);
}
using LotteryApp.Entities;
using LotteryApp.Entities.Lottery;

namespace LotteryApp.Services;

public interface IPlayerService
{
    void SetupCpuLotteryPlayers(string lotteryId);
    List<LotteryPlayer> GetPlayersByLottery(string lotteryId);
    void AddPlayerToLottery(LotteryPlayer lotteryPlayer);
}
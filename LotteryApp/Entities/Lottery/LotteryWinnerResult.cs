using LotteryApp.Enums;

namespace LotteryApp.Entities.Lottery;

public class LotteryWinnerResult
{
    public AwardType AwardType { get; set; }
    public decimal PrizePerWinner { get; set; }
    public List<LotteryPlayer> Winners { get; set; }
}
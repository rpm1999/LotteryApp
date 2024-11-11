namespace LotteryApp.Entities.Config;

public class LotteryConfig
{
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public decimal StartingBalance { get; set; }
    public decimal TicketPrice { get; set; }
    public decimal GrandPrizePercentage { get; set; }
    public decimal SecondTierPercentage { get; set; }
    public decimal ThirdTierPercentage { get; set; }
    public decimal SecondTierUserSharePercentage { get; set; }
    public decimal ThirdTierUserSharePercentage { get; set; }
}
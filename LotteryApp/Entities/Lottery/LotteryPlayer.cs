namespace LotteryApp.Entities.Lottery;

public class LotteryPlayer(int playerId)
{
    public string LotteryId { get; set; }
    public int PlayerId { get; set; } = playerId;
    public decimal Balance { get; set; }
    public int TicketsPurchased { get; set; }
    public List<string> TicketNumbers { get; set; } = new();
}
using LotteryApp.Entities.Config;
using LotteryApp.Entities.Exceptions;
using LotteryApp.Entities.Lottery;

namespace LotteryApp.Services;

public class PlayerService(LotteryConfig config, ITicketService ticketService) : IPlayerService
{
    private static readonly Random Random = new Random();
    private readonly List<LotteryPlayer> _players = new();

    // Sets up random amount of CPU players for the specified lottery
    public void SetupCpuLotteryPlayers(string lotteryId)
    {
        try
        {
            var cpuPlayerCount = Random.Next(config.MinPlayers - 1, config.MaxPlayers); // Set the amount of CPUs in the game
            var cpuPlayers = new List<LotteryPlayer>();
            
            // Initialize CPUs, adding them to the lottery game with a starting balance
            for (var i = 2; i <= cpuPlayerCount + 1; i++)
            {
                var cpuPlayer = new LotteryPlayer(Random.Next())
                {
                    LotteryId = lotteryId,
                    Balance = config.StartingBalance,
                    TicketNumbers = new()
                }; 
                AddPlayerToLottery(cpuPlayer);
                cpuPlayers.Add(cpuPlayer);
            }

            // Simulate CPUs buying tickets
            foreach (var player in cpuPlayers)
            {
                var ticketAmount = Random.Next(0, 10); // Randomize ticket quantity for each CPU player
                ticketService.PurchaseTickets(player, ticketAmount);
            }
        }
        catch (Exception ex)
        {
            throw new LotteryException($"Error setting up CPU players: {ex.Message}", ex);
        }
    }

    // Retrieves players for a specific lottery
    public List<LotteryPlayer> GetPlayersByLottery(string lotteryId)
    {
        return _players.Where(p => p.LotteryId == lotteryId).ToList();
    }

    // Adds a player to the lottery
    public void AddPlayerToLottery(LotteryPlayer lotteryPlayer)
    {
        _players.Add(lotteryPlayer);
    }
}
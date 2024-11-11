using LotteryApp.Entities.Config;
using LotteryApp.Entities.Exceptions;
using LotteryApp.Entities.Lottery;
using LotteryApp.Enums;
using LotteryApp.Helpers;
using Microsoft.Extensions.Logging;

namespace LotteryApp.Services;

public class LotteryGameService(LotteryConfig config, IPlayerService playerService, 
    ITicketService ticketService, ILogger<LotteryGameService> logger) : ILotteryGameService
{
    // The user (us) playing the lottery, initialized with starting balance
    private readonly LotteryPlayer _user = new(1)
    {
        Balance = config.StartingBalance
    };

    public List<string> ActiveTicketNumbers = new(); // Stores all active ticket numbers for the draw
    private decimal _houseRevenue;
    private static readonly Random Random = new();

    // Method to begin a lottery game, displays UI aswell as award prizes
    public void StartLottery()
    {
        try
        {
            // I've left logging out for the rest of the code, this is just to showcase the use of it
            // Typically I would log the start of the method, along with a message and any relevant params to it, aswell as error logs inside catch blocks
            // I'd typically also log after any requests/responses or complex calculations had been made
            
            var lotteryId = LotteryHelper.GenerateGuid();
            logger.LogInformation("{MethodName}: Starting Lottery {LotteryId} ", nameof(StartLottery), lotteryId);

            DisplayWelcomeMessage();
            SetupGame(lotteryId);

            var players = playerService.GetPlayersByLottery(lotteryId);
            DisplaySummary(players);
            ActiveTicketNumbers = players.SelectMany(p => p.TicketNumbers).ToList();

            var results = DrawAndAwardWinners(lotteryId); // Draw and award winners for each prize tier
            DisplayWinners(results);

            var totalDistributed = results.Sum(result => result.Winners.Count * result.PrizePerWinner);
            DisplayAndCalculateHouseProfit(totalDistributed, players); // Calculate and show house profit
        }
        catch (LotteryException e)
        {
            Console.WriteLine($"Lottery Error: {e.Message}");
            logger.LogError("{MethodName}: Error Message = {LotteryId} ", nameof(StartLottery), e.Message);

        }
        catch (Exception e)
        {
            Console.WriteLine($"An unexpected error occurred: {e.Message}");
            logger.LogError("{MethodName}: Error Message = {LotteryId} ", nameof(StartLottery), e.Message);
        }
    }

    // Setup simulation
    private void SetupGame(string lotteryId)
    {
        playerService.SetupCpuLotteryPlayers(lotteryId); // Create CPU players for this lottery game
        _user.LotteryId = lotteryId; // Setup our user to play the current game
        playerService.AddPlayerToLottery(_user);
        
        // User purchase tickets
        var ticketAmount = int.Parse(Console.ReadLine() ?? "0");
        ticketService.PurchaseTickets(_user, ticketAmount);
    }

    #region AwardPrizes
    
    // Grand Prize award method
    public LotteryWinnerResult AwardGrandPrize(decimal totalRevenue, List<LotteryPlayer> players)
    {
        try
        {
            var grandPrizeAmount = totalRevenue * config.GrandPrizePercentage;

            if (ActiveTicketNumbers.Count == 0)
            {
                throw new LotteryException("There are no active tickets currently");
            }

            // Select a random winning ticket
            var winningTicketIndex = Random.Next(ActiveTicketNumbers.Count);
            var winningTicket = ActiveTicketNumbers[winningTicketIndex];

            // Find the player who owns the winning ticket
            var grandPrizeWinner = players.Where(p => p.TicketNumbers.Contains(winningTicket)).ToList();
            grandPrizeWinner.FirstOrDefault()!.Balance += grandPrizeAmount;
            ActiveTicketNumbers.Remove(winningTicket); // Remove the ticket from the active ticket pool
            
            return new LotteryWinnerResult
            {
                AwardType = AwardType.GrandPrize,
                Winners = grandPrizeWinner,
                PrizePerWinner = grandPrizeAmount
            };
        }
        catch (Exception ex)
        {
            throw new LotteryException("An error occurred while awarding the grand prize.", ex);
        }
    }

    private List<LotteryWinnerResult> DrawAndAwardWinners(string lotteryId)
    {
        try
        {
            var players = playerService.GetPlayersByLottery(lotteryId);
            var totalRevenue = LotteryHelper.GetTotalRevenue(players, config.TicketPrice); // Used to calculate prize amounts

            // Draw for each prize tier and collect results
            var grandPrizeResult = AwardGrandPrize(totalRevenue, players);
            var secondTierResult = AwardTierTickets(AwardType.SecondTier, totalRevenue, players);
            var thirdTierResult = AwardTierTickets(AwardType.ThirdTier, totalRevenue, players);

            var result = new List<LotteryWinnerResult> { grandPrizeResult, secondTierResult, thirdTierResult };

            return result;
        }
        catch (Exception ex)
        {
            throw new LotteryException("An error occurred during drawing winners.", ex);
        }
    }

    public LotteryWinnerResult AwardTierTickets(AwardType awardType, decimal totalRevenue, List<LotteryPlayer> players)
    {
        try
        {
            decimal prizePercentage;
            decimal winnerPercentage;

            // Set prize and winner percentages based on the award tier
            switch (awardType)
            {
                case AwardType.SecondTier:
                    prizePercentage = config.SecondTierPercentage;
                    winnerPercentage = config.SecondTierUserSharePercentage;
                    break;
                case AwardType.ThirdTier:
                    prizePercentage = config.ThirdTierPercentage;
                    winnerPercentage = config.ThirdTierUserSharePercentage;
                    break;
                default:
                    throw new LotteryException($"Award Type - {awardType} is invalid.");
            }

            var totalTickets = players.Sum(p => p.TicketsPurchased); // Calculate total tickets purchased

            var winnersCount = (int)Math.Round(totalTickets * winnerPercentage); // Determine number of winners
            var winningTickets = DrawWinningTickets(winnersCount); // Draw winning tickets for specified award type

            var prizeAmount = totalRevenue * prizePercentage; // Calculate prize amount for this tier
            var prizePerWinner = Math.Round(prizeAmount / winnersCount, 2);

            // Calculate and store any remaining amount due to rounding as house profit
            var distributedAmount = prizePerWinner * winnersCount;
            var remainder = prizeAmount - distributedAmount;
            _houseRevenue += remainder;

            return AwardWinningTickets(winningTickets, prizePerWinner, awardType, players);
        }
        catch (Exception ex)
        {
            throw new LotteryException("An error occurred while awarding tier tickets.", ex);
        }
    }

    public LotteryWinnerResult AwardWinningTickets(List<string> winningTickets, decimal prizeAmount, AwardType awardType, List<LotteryPlayer> players)
    {
        try
        {
            var winners = new List<LotteryPlayer>();

            // Award prizes to winners of the lottery game
            foreach (var ticket in winningTickets)
            {
                var winner = players.First(p => p.TicketNumbers.Contains(ticket));
                winner.Balance += prizeAmount;
                ActiveTicketNumbers.Remove(ticket); // Remove the ticket from the active ticket pool
                winners.Add(winner);
            }

            return new LotteryWinnerResult
            {
                AwardType = awardType,
                Winners = winners,
                PrizePerWinner = prizeAmount
            };
        }
        catch (Exception ex)
        {
            throw new LotteryException("An error occurred while awarding the winning tickets.", ex);
        }
    }

    private List<string> DrawWinningTickets(int count)
    {
        try
        {
            var winningTickets = new List<string>();
            
            // Randomly select the winning tickets
            for (var i = 0; i < count; i++)
            {
                var winningTicketIndex = Random.Next(ActiveTicketNumbers.Count);
                var winningTicket = ActiveTicketNumbers[winningTicketIndex];
                winningTickets.Add(winningTicket);
            }

            return winningTickets;
        }
        catch (Exception ex)
        {
            throw new LotteryException("An error occurred while drawing winning tickets.", ex);
        }
    }
    #endregion

    #region UI

    private void DisplayWinners(List<LotteryWinnerResult> results)
    {
        // Display lottery results
        // If a player has won more than once on a tier through a different ticket, it will display how many time and total prize amount
        // Used AI here with the clear goal in mind
        foreach (var result in results)
        {
            switch (result.AwardType)
            {
                case AwardType.GrandPrize:
                    Console.WriteLine(
                        $"* Grand Prize: Player {result.Winners.First().PlayerId} wins ${result.PrizePerWinner}!");
                    break;

                case AwardType.SecondTier:
                    var singleOccurrencesSecondTier = result.Winners
                        .GroupBy(w => w.PlayerId)
                        .Where(g => g.Count() == 1)
                        .Select(g => g.First())
                        .ToList();

                    Console.WriteLine(
                        $"* Second Tier: Players {string.Join(", ", singleOccurrencesSecondTier.Select(p => p.PlayerId))} win ${result.PrizePerWinner} each!");

                    var duplicateGroupsSecondTier = result.Winners
                        .GroupBy(w => w.PlayerId)
                        .Where(g => g.Count() > 1)
                        .GroupBy(g => g.Count())
                        .OrderByDescending(g => g.Key)
                        .ToList();

                    foreach (var group in duplicateGroupsSecondTier)
                    {
                        var playerIds = string.Join(", ", group.Select(g => g.Key));
                        Console.WriteLine($"    * Players {playerIds} have won {group.Key} times, winnings = ${group.Key * result.PrizePerWinner}");
                    }

                    break;

                case AwardType.ThirdTier:
                    var singleOccurrencesThirdTier = result.Winners
                        .GroupBy(w => w.PlayerId)
                        .Where(g => g.Count() == 1)
                        .Select(g => g.First())
                        .ToList();

                    Console.WriteLine(
                        $"* Third Tier: Players {string.Join(", ", singleOccurrencesThirdTier.Select(p => p.PlayerId))} win ${result.PrizePerWinner} each!");

                    var duplicateGroupsThirdTier = result.Winners
                        .GroupBy(w => w.PlayerId)
                        .Where(g => g.Count() > 1)
                        .GroupBy(g => g.Count())
                        .OrderByDescending(g => g.Key)
                        .ToList();

                    foreach (var group in duplicateGroupsThirdTier)
                    {
                        var playerIds = string.Join(", ", group.Select(g => g.Key));
                        Console.WriteLine($"    * Players {playerIds} have won {group.Key} times, winnings = ${group.Key * result.PrizePerWinner}");
                    }

                    break;
            }
        }
    }

    private void DisplayWelcomeMessage()
    {
        Console.WriteLine($"Welcome to the Bede Lottery, Player {_user.PlayerId}!\n");
        Console.WriteLine($"* Your digital balance: ${_user.Balance}");
        Console.WriteLine($"* Ticket Price: ${config.TicketPrice} each");
        Console.WriteLine($"\nHow many tickets do you want to buy, Player {_user.PlayerId}?");
    }
    
    private void DisplayAndCalculateHouseProfit(decimal totalDistributed, List<LotteryPlayer> players)
    {
        var totalRevenue = LotteryHelper.GetTotalRevenue(players, config.TicketPrice);
        _houseRevenue = totalRevenue - totalDistributed;
        Console.WriteLine($"\nHouse Revenue: ${_houseRevenue}");
    }

    public void DisplaySummary(List<LotteryPlayer> players)
    {
        if (players == null || players.Count == 0)
        {
            throw new LotteryException("No players available to display in the summary.");
        }

        Console.WriteLine($"\n{players.Count} players have purchased tickets.");
        Console.WriteLine($"\nTicket Draw Results:\n");
    }
    #endregion
}

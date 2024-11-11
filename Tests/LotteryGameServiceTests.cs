using LotteryApp.Entities.Config;
using LotteryApp.Entities.Exceptions;
using LotteryApp.Entities.Lottery;
using LotteryApp.Enums;
using LotteryApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;

public class LotteryGameServiceTests
{
    private readonly Mock<IPlayerService> _mockPlayerService;
    private readonly Mock<ITicketService> _mockTicketService;
    private readonly Mock<ILogger<LotteryGameService>> _mockLogger;
    private readonly LotteryGameService _lotteryGameService;
    private readonly LotteryConfig _config;
    private readonly TicketService _ticketService;

    public LotteryGameServiceTests()
    {
        _config = new LotteryConfig
        {
            StartingBalance = 10m,
            TicketPrice = 1m,
            SecondTierPercentage = 0.3m,
            SecondTierUserSharePercentage = 0.1m,
            GrandPrizePercentage = 0.5m, 
        };
        
        _mockPlayerService = new Mock<IPlayerService>();
        _mockTicketService = new Mock<ITicketService>();
        _mockLogger = new Mock<ILogger<LotteryGameService>>();
        
        _lotteryGameService = new LotteryGameService(
            _config, 
            _mockPlayerService.Object, 
            _mockTicketService.Object, 
            _mockLogger.Object);

        _ticketService = new TicketService(_config);
    }
    
    [Fact]
    public void AwardGrandPrize_ShouldRemoveWinningTicketFromActiveTickets()
    {
        // Arrange
        var player = new LotteryPlayer(1) { TicketNumbers = new List<string> { "ticket1" }, Balance = 0m };
        var players = new List<LotteryPlayer> { player };

        // Add active tickets
        var activeTickets = new List<string> { "ticket1" };
        _lotteryGameService.ActiveTicketNumbers.AddRange(activeTickets);
        
        // Act
        _lotteryGameService.AwardGrandPrize(It.IsAny<decimal>(), players);

        // Assert
        Assert.DoesNotContain("ticket1", _lotteryGameService.ActiveTicketNumbers);
    }
    
    [Fact]
    public void AwardTierTickets_ShouldRemoveWinningTicketFromActiveTickets()
    {
        // Arrange
        var player1 = new LotteryPlayer(1)
            { TicketNumbers = new List<string> { "ticket1" }, Balance = 0m, TicketsPurchased = 10 };
        var player2 = new LotteryPlayer(2) { TicketNumbers = new List<string> { "ticket2" }, Balance = 0m, TicketsPurchased = 8};
        var players = new List<LotteryPlayer> { player1, player2 };

        // Add active tickets
        var activeTickets = new List<string> { "ticket1" };
        _lotteryGameService.ActiveTicketNumbers.AddRange(activeTickets);
        
        // Act
        _lotteryGameService.AwardTierTickets(AwardType.SecondTier, It.IsAny<decimal>(), players);

        // Assert
        Assert.DoesNotContain("ticket1", _lotteryGameService.ActiveTicketNumbers);
    }
    
    [Fact]
    public void PurchaseTickets_WhenTicketAmountExceedsAffordableTickets_ShouldOnlyPurchaseAffordableTickets()
    {
        // Arrange
        var player = new LotteryPlayer(1) { Balance = 3m, TicketNumbers = new () }; // Can afford 3 tickets
        var ticketAmount = 5; // Requesting 5 tickets

        // Act
        _ticketService.PurchaseTickets(player, ticketAmount);

        // Assert
        Assert.Equal(3, player.TicketsPurchased); // Only 3 tickets should be purchased
        Assert.Equal(3, player.TicketNumbers.Count); // Only 3 ticket numbers should be added
        Assert.Equal(0m, player.Balance); // Balance should be reduced to 0
    }
    
    [Fact]
    public void AwardGrandPrize_ShouldDistributeCorrectAmountToWinner()
    {
        // Arrange
        var player = new LotteryPlayer(1) { TicketNumbers = new List<string> { "ticket1" }, Balance = 0m };
        var players = new List<LotteryPlayer> { player };
        
        // Add active tickets
        _lotteryGameService.ActiveTicketNumbers = new List<string> { "ticket1" };
        
        // Act
        var result = _lotteryGameService.AwardGrandPrize(10m, players);

        // Assert
        Assert.Equal(AwardType.GrandPrize, result.AwardType);
        Assert.Single(result.Winners);
        Assert.Equal(player, result.Winners.First());
        Assert.Equal(5m, result.PrizePerWinner);
        Assert.Equal(5m, player.Balance);
        
        Assert.DoesNotContain("ticket1", _lotteryGameService.ActiveTicketNumbers);
    }
    
    [Fact]
    public void AwardWinningTickets_ShouldDistributeCorrectAmountToWinners()
    {
        // Arrange
        var player1 = new LotteryPlayer(1) { TicketNumbers = new List<string> { "ticket1", "ticket3" }, Balance = 0m };
        var player2 = new LotteryPlayer(2) { TicketNumbers = new List<string> { "ticket2" }, Balance = 0m };
        var players = new List<LotteryPlayer> { player1, player2 };
        
        // Set active tickets in the lottery service
        _lotteryGameService.ActiveTicketNumbers = new List<string> { "ticket1", "ticket2", "ticket3" };
    
        var winningTickets = new List<string> { "ticket1", "ticket2" };
        var prizeAmount = 10m;
        var awardType = AwardType.SecondTier;

        // Act
        var result = _lotteryGameService.AwardWinningTickets(winningTickets, prizeAmount, awardType, players);

        // Assert
        Assert.Equal(awardType, result.AwardType);
        Assert.Equal(2, result.Winners.Count);
        Assert.Contains(player1, result.Winners);
        Assert.Contains(player2, result.Winners);
        Assert.Equal(10m, player1.Balance); // Player 1 wins for ticket1
        Assert.Equal(10m, player2.Balance); // Player 2 wins for ticket2

        // Ensure that the awarded tickets are removed from active tickets
        Assert.DoesNotContain("ticket1", _lotteryGameService.ActiveTicketNumbers);
        Assert.DoesNotContain("ticket2", _lotteryGameService.ActiveTicketNumbers);
        Assert.Contains("ticket3", _lotteryGameService.ActiveTicketNumbers);
    }

    
    [Fact]
    public void DisplayPlayerSummary_WhenPlayersIsNull_ShouldThrowLotteryException()
    {
        // Arrange
        List<LotteryPlayer> players = null;

        // Act & Assert
        Assert.Throws<LotteryException>(() => _lotteryGameService.DisplaySummary(players));
    }
}
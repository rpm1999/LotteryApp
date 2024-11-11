using LotteryApp.Entities.Config;
using LotteryApp.Entities.Exceptions;
using LotteryApp.Entities.Lottery;
using LotteryApp.Helpers;

namespace LotteryApp.Services;

public class TicketService(LotteryConfig config) : ITicketService
{
    private static readonly Random Random = new Random();

    // Handles ticket purchases
    public void PurchaseTickets(LotteryPlayer lotteryPlayer, int ticketAmount)
    {
        if (ticketAmount < 0)
        {
            throw new LotteryException("Ticket amount must be greater than 0");
        }

        // Calculate if player can purchase requested amount of tickets
        var balance = lotteryPlayer.Balance;
        var affordableTickets = Math.Min((int)(balance / config.TicketPrice), ticketAmount); // Limit tickets to what player can afford
        var totalPrice = affordableTickets * config.TicketPrice;

        // Disclaimer in case of the event where they request to much
        // Could ask the user if they wanted to re-enter an amount
        if (ticketAmount > affordableTickets)
        {
            Console.WriteLine($"\nNote:\nTicket amount requested exceeds player balance, player instead will purchase {affordableTickets} tickets");
        }
        
        lotteryPlayer.Balance -= totalPrice;

        // Generate unique ticket numbers for each purchased ticket
        for (var i = 1; i <= affordableTickets; i++)
        {
            var ticketId = LotteryHelper.GenerateGuid();
            lotteryPlayer.TicketNumbers.Add(ticketId);
        }
        
        lotteryPlayer.TicketsPurchased = affordableTickets;
    }
}

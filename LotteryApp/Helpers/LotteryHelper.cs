using LotteryApp.Entities;
using LotteryApp.Entities.Config;
using LotteryApp.Entities.Exceptions;
using LotteryApp.Entities.Lottery;
using LotteryApp.Services;

namespace LotteryApp.Helpers;

public static class LotteryHelper
{
    // Calculates total revenue based on players' tickets purchased
    public static decimal GetTotalRevenue(List<LotteryPlayer> players, decimal ticketPrice)
    {
        var totalTickets = players.Sum(p => p.TicketsPurchased);
        return totalTickets * ticketPrice;
    }

    // Generates a unique GUID string
    public static string GenerateGuid()
    {
        return Guid.NewGuid().ToString();
    }
}
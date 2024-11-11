using LotteryApp.Entities;
using LotteryApp.Entities.Lottery;

namespace LotteryApp.Services;

public interface ITicketService
{
    void PurchaseTickets(LotteryPlayer lotteryPlayer, int ticketAmount);
}
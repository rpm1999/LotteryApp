namespace LotteryApp.Entities.Exceptions;

public class LotteryException : Exception
{
    public LotteryException(string message) : base(message) { }
        
    public LotteryException(string message, Exception innerException) : base(message, innerException) { }
}
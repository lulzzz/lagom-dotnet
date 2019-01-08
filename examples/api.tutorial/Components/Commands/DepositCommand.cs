using wyvern.api;
using wyvern.utils;
using Akka;
using wyvern.entity.command;

public abstract partial class BankAccountCommand : AbstractCommand
{
    public sealed class DepositCommand : BankAccountCommand, IReplyType<Done>
    {
        public string Id { get; }
        public decimal Amount { get; }
        public string From { get; }

        public DepositCommand(string id, string from, decimal amount)
        {
            id.IsNotNull();
            from.IsNotNull();
            Preconditions.IsPositive(amount);

            Id = id;
            From = from;
            Amount = amount;
        }
    }
}

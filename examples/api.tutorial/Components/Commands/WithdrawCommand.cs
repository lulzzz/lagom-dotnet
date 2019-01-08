using wyvern.api;
using wyvern.utils;
using Akka;
using wyvern.entity.command;

public abstract partial class BankAccountCommand : AbstractCommand
{
    public sealed class WithdrawCommand : BankAccountCommand, IReplyType<Done>
    {
        public string Id { get; }
        public decimal Amount { get; }
        public string To { get; }

        public WithdrawCommand(string id, string to, decimal amount)
        {
            id.IsNotNull();
            to.IsNotNull();
            Preconditions.IsPositive(amount);

            Id = id;
            To = to;
            Amount = amount;
        }
    }
}

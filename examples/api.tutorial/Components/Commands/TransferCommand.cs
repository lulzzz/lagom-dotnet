using wyvern.api;
using wyvern.utils;
using Akka;
using wyvern.entity.command;

public abstract partial class BankAccountCommand : AbstractCommand
{
    public sealed class TransferCommand : BankAccountCommand, IReplyType<Done>
    {
        public string AccountId { get; }
        public string ToAccountId { get; }
        public decimal Amount { get; }
        public string Comment { get; }

        public TransferCommand(string accountId, string toAccountId, decimal amount, string comment)
        {
            accountId.IsNotNull();
            toAccountId.IsNotNull();
            Preconditions.IsPositive(amount);
            comment.IsNotNull();

            AccountId = accountId;
            ToAccountId = toAccountId;
            Amount = amount;
            Comment = comment;
        }
    }
}

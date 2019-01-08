using wyvern.api;
using wyvern.utils;

public abstract partial class BankAccountCommand
{
    public sealed class AccountBalanceCommand : BankAccountCommand, IReplyType<AccountBalance>
    {
        public string AccountId { get; }

        public AccountBalanceCommand(string accountId)
        {
            accountId.IsNotNull(nameof(accountId));

            AccountId = accountId;
        }
    }

    public sealed class AccountBalance
    {
        public decimal Amount { get; }

        public AccountBalance(decimal amount)
            => Amount = amount;
    }
}

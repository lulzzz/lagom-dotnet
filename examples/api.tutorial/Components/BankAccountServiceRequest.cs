using Newtonsoft.Json;

public abstract class BankAccountServiceRequest
{
    public sealed class CreateAccountRequest
    {
        // NOTE: In this scenario, an external system is responsible for generating
        // the customer's unique identifier, and the account unique identifier
        public string AccountId { get; }
        public string Name { get; }
        public decimal InitialDeposit { get; } = .0m;

        [JsonConstructor]
        public CreateAccountRequest
            (string accountId, string name, decimal initialDeposit)
            => (AccountId, Name, InitialDeposit)
            = (accountId, name, initialDeposit);
    }

    public sealed class DepositRequest
    {
        public decimal Amount { get; }
        public string From { get; }

        [JsonConstructor]
        public DepositRequest
            (decimal amount, string from)
            => (Amount, From)
            = (amount, from);
    }

    public sealed class WithdrawRequest
    {
        public decimal Amount { get; }
        public string To { get; }

        [JsonConstructor]
        public WithdrawRequest
            (decimal amount, string to)
            => (Amount, To)
            = (amount, to);
    }

    public sealed class TransferRequest
    {
        public string ToAccountId { get; }
        public decimal Amount { get; }
        public string Comment { get; }

        [JsonConstructor]
        public TransferRequest
            (string toAccountId, decimal amount, string comment)
            => (ToAccountId, Amount, Comment)
            = (toAccountId, amount, comment);
    }
}

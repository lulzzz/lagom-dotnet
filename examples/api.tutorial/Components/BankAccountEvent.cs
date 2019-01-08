using wyvern.entity.@event;

public abstract class BankAccountEvent : AbstractEvent
{
    public sealed class BankAccountCreatedEvent : BankAccountEvent
    {
        public string TransactionId { get; }
        public string AccountId { get; }
        public string Name { get; }
        public decimal InitialBalance { get; }

        public BankAccountCreatedEvent
            (string transactionId, string accountId, string name, decimal initialBalance)
            => (TransactionId, AccountId, Name, InitialBalance)
            = (transactionId, accountId, name, initialBalance);
    }

    public sealed class AccountBalanceChangedEvent : BankAccountEvent
    {
        public string TransactionId { get; }
        public string AccountId { get; }
        public string EventType { get; }
        public string Description { get; }
        public decimal Amount { get; }

        public AccountBalanceChangedEvent
            (string transactionId, string accountId, string eventType, string description, decimal amount)
            => (TransactionId, AccountId, EventType, Description, Amount)
            = (transactionId, accountId, eventType, description, amount);
    }

    public sealed class TransferFundsEvent : BankAccountEvent
    {
        public string TransactionId { get; }
        public string AccountId { get; }
        public string Description { get; }
        public decimal Amount { get; }

        public TransferFundsEvent
            (string transactionId, string accountId, string description, decimal amount)
            => (TransactionId, AccountId, Description, Amount)
            = (transactionId, accountId, description, amount);
    }

    public sealed class FundsTransferredEvent : BankAccountEvent
    {
        public string TransactionId { get; }
        public string AccountId { get; }
        public decimal Amount { get; }

        public FundsTransferredEvent
            (string transactionId, string accountId, decimal amount)
            => (TransactionId, AccountId, Amount)
            = (transactionId, accountId, amount);
    }
}

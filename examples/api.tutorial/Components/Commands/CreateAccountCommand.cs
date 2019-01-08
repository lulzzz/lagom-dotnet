using wyvern.api;
using wyvern.utils;
using Akka;
using wyvern.entity.command;

public abstract partial class BankAccountCommand : AbstractCommand
{
    public sealed class CreateAccountCommand : BankAccountCommand, IReplyType<Done>
    {
        public string Id { get; }
        public string Name { get; }
        public decimal InitialDeposit { get; }

        public CreateAccountCommand
            (string id, string name, decimal initialDeposit)
        {
            id.IsNotNull(nameof(id));
            name.IsNotNull(nameof(name));
            Preconditions.IsPositive(initialDeposit, nameof(initialDeposit));

            Id = id;
            Name = name;
            InitialDeposit = initialDeposit;
        }
    }
}

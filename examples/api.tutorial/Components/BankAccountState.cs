using System.Collections.Generic;
using System.Linq;
using wyvern.entity.state;

public class BankAccountState : AbstractState
{
    public bool Created { get; } = false;

    public string Id { get; }
    public string Name { get; }
    public decimal Balance { get; } = .0m;
    public (string, decimal)[] FrozenFunds { get; } = new (string, decimal)[] { };

    public BankAccountState()
    {

    }

    BankAccountState(string id, string name, decimal balance, (string, decimal)[] frozenFunds)
    {
        Created = true;
        Id = id;
        Name = name;
        Balance = balance;
        FrozenFunds = frozenFunds;
    }


    public BankAccountState WithBalance(decimal balance)
        => new BankAccountState(Id, Name, balance, FrozenFunds);

    public BankAccountState WithCreated()
        => new BankAccountState(Id, Name, Balance, FrozenFunds);

    public BankAccountState WithId(string id)
        => new BankAccountState(id, Name, Balance, FrozenFunds);

    public BankAccountState WithName(string name)
        => new BankAccountState(Id, name, Balance, FrozenFunds);

    public BankAccountState FreezeFunds(string transactionId, decimal amount)
    {
        var frozen = new List<(string, decimal)>(FrozenFunds);
        frozen.Add((transactionId, amount));
        return new BankAccountState(Id, Name, Balance, frozen.ToArray());
    }

    public BankAccountState UnfreezeFunds(string transactionId)
    {
        var frozen = FrozenFunds.Where(x => x.Item1 != transactionId);
        return new BankAccountState(Id, Name, Balance, frozen.ToArray());
    }
}

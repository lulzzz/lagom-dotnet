using wyvern.api;
using System;
using Akka;
using System.Threading.Tasks;
using static BankAccountServiceRequest;
using static BankAccountCommand;

public sealed class BankAccountServiceImpl : BankAccountService
{
    IShardedEntityRegistry Registry { get; }

    public BankAccountServiceImpl
        (IShardedEntityRegistry registry)
        => Registry = registry;

    public override Func<string, Func<NotUsed, Task<Object>>> Balance
        => accountId
        => async _
        => await Registry
            .RefFor<BankAccountEntity>(accountId)
            .Ask(new AccountBalanceCommand(accountId));

    public override Func<Func<CreateAccountRequest, Task<Object>>> NewAccount
        => ()
        => async req
        => await Registry
                .RefFor<BankAccountEntity>(req.AccountId)
                .Ask(new CreateAccountCommand(req.AccountId, req.Name, req.InitialDeposit));

    public override Func<string, Func<DepositRequest, Task<Object>>> Deposit
        => accountId
        => async req
        => await Registry
            .RefFor<BankAccountEntity>(accountId)
            .Ask(new DepositCommand(accountId, req.From, req.Amount));

    public override Func<string, Func<WithdrawRequest, Task<Object>>> Withdraw
        => accountId
        => async req
        => await Registry
            .RefFor<BankAccountEntity>(accountId)
            .Ask(new WithdrawCommand(accountId, req.To, req.Amount));

    public override Func<string, Func<TransferRequest, Task<Object>>> Transfer
        => accountId
        => async req
        => await Registry
                .RefFor<BankAccountEntity>(accountId)
                .Ask(new TransferCommand(accountId, req.ToAccountId, req.Amount, req.Comment));

}

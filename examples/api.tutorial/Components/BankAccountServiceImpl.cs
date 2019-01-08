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

    protected override Func<string, Func<NotUsed, Task<AccountBalance>>> Balance
        => accountId
        => async _
        => await Registry
            .RefFor<BankAccountEntity>(accountId)
            .Ask(new AccountBalanceCommand(accountId));

    protected override Func<Func<CreateAccountRequest, Task<Done>>> NewAccount
        => ()
        => async req
        => await Registry
                .RefFor<BankAccountEntity>(req.AccountId)
                .Ask(new CreateAccountCommand(req.AccountId, req.Name, req.InitialDeposit));

    protected override Func<string, Func<DepositRequest, Task<Done>>> Deposit
        => accountId
        => async req
        => await Registry
            .RefFor<BankAccountEntity>(accountId)
            .Ask(new DepositCommand(accountId, req.From, req.Amount));

    protected override Func<string, Func<WithdrawRequest, Task<Done>>> Withdraw
        => accountId
        => async req
        => await Registry
            .RefFor<BankAccountEntity>(accountId)
            .Ask(new WithdrawCommand(accountId, req.To, req.Amount));

    protected override Func<string, Func<TransferRequest, Task<Done>>> Transfer
        => accountId
        => async req
        => await Registry
                .RefFor<BankAccountEntity>(accountId)
                .Ask(new TransferCommand(accountId, req.ToAccountId, req.Amount, req.Comment));

}

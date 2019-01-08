using wyvern.api;
using System;
using Akka;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using static BankAccountServiceRequest;
using static BankAccountCommand;

public abstract class BankAccountService : Service
{
    protected abstract Func<Func<CreateAccountRequest, Task<Done>>> NewAccount { get; }

    protected abstract Func<string, Func<NotUsed, Task<AccountBalance>>> Balance { get; }

    protected abstract Func<string, Func<DepositRequest, Task<Done>>> Deposit { get; }
    protected abstract Func<string, Func<WithdrawRequest, Task<Done>>> Withdraw { get; }
    protected abstract Func<string, Func<TransferRequest, Task<Done>>> Transfer { get; }

    public override IDescriptor Descriptor => Named("BankAccount")
        .WithCalls(
            RestCall(Method.GET,  "/api/accounts/{accountId}/balance",  Balance),
            RestCall(Method.POST, "/api/accounts",                      NewAccount),
            RestCall(Method.POST, "/api/account/{accountId}/deposits",  Deposit),
            RestCall(Method.POST, "/api/account/{accountId}/withdraws", Withdraw),
            RestCall(Method.POST, "/api/account/{accountId}/transfers", Transfer)
        );
}

using wyvern.api;
using System;
using Akka;
using System.Threading.Tasks;
using static BankAccountServiceRequest;
using static BankAccountCommand;

public abstract class BankAccountService : Service
{
    public abstract Func<Func<CreateAccountRequest, Task<Object>>> NewAccount { get; }

    public abstract Func<string, Func<NotUsed, Task<Object>>> Balance { get; }

    public abstract Func<string, Func<DepositRequest, Task<Object>>> Deposit { get; }
    public abstract Func<string, Func<WithdrawRequest, Task<Object>>> Withdraw { get; }
    public abstract Func<string, Func<TransferRequest, Task<Object>>> Transfer { get; }

    public override IDescriptor Descriptor => Named("BankAccount")
        .WithCalls(
            RestCall(Method.GET,  "/api/accounts/{accountId}/balance",  Balance),
            RestCall(Method.POST, "/api/accounts",                      NewAccount),
            RestCall(Method.POST, "/api/account/{accountId}/deposits",  Deposit),
            RestCall(Method.POST, "/api/account/{accountId}/withdraws", Withdraw),
            RestCall(Method.POST, "/api/account/{accountId}/transfers", Transfer)
        );
}

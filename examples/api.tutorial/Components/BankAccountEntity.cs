using wyvern.entity;
using wyvern.api.ioc;
using wyvern.utils;
using Akka.Streams.Util;
using System;
using Akka;
using static BankAccountCommand;
using static BankAccountEvent;
using static api.tutorial.HttpStatusCodeExceptionMiddleware;
using api.tutorial;
using System.Linq;

public class BankAccountEntity : ShardedEntity<BankAccountCommand, BankAccountEvent, BankAccountState>
{
    public override Behavior InitialBehavior(Option<BankAccountState> state)
        => NewBehaviorBuilder(state.OrElse(new BankAccountState()))
            .SetCommandHandler<CreateAccountCommand, Done>(
                (cmd, ctx) =>
                {
                    // TODO: Need a base command handler for all commands for global validation
                    if (State.Created)
                    {
                        ctx.CommandFailed(new ConflictException("Account already exists"));
                        return ctx.Done();
                    }

                    return ctx.ThenPersist(
                        new BankAccountCreatedEvent(
                            Guid.NewGuid().ToString(), // NOTE: Ideally we would employ an id generator
                            cmd.Id,
                            cmd.Name,
                            cmd.InitialDeposit
                        ),
                        (e) => ctx.Reply(Done.Instance)
                    );
                }
            )
            .SetReadOnlyCommandHandler<AccountBalanceCommand, AccountBalance>(
                (cmd, ctx) =>
                {
                    if (!State.Created)
                    {
                        ctx.CommandFailed(new NotFoundException("Account not found"));
                        return;
                    }

                    ctx.Reply(new AccountBalance(State.Balance));
                }
            )
            .SetCommandHandler<DepositCommand, Done>(
                (cmd, ctx) =>
                {
                    if (!State.Created)
                    {
                        ctx.CommandFailed(new NotFoundException("Account not found"));
                        return ctx.Done();
                    }

                    return ctx.ThenPersist(
                        new AccountBalanceChangedEvent(
                            Guid.NewGuid().ToString(),
                            cmd.Id,
                            "Deposit",
                            cmd.From,
                            cmd.Amount
                        )
                    );
                }
            )
            .SetCommandHandler<WithdrawCommand, Done>(
                (cmd, ctx) =>
                {
                    if (!State.Created)
                    {
                        ctx.CommandFailed(new NotFoundException("Account not found"));
                        return ctx.Done();
                    }

                    return ctx.ThenPersist(
                        new AccountBalanceChangedEvent(
                            Guid.NewGuid().ToString(),
                            cmd.Id,
                            "Withdraw",
                            cmd.To,
                            cmd.Amount
                        )
                    );
                }
            )
            .SetCommandHandler<TransferCommand, Done>(
                (cmd, ctx) =>
                {
                    if (!State.Created)
                    {
                        ctx.CommandFailed(new NotFoundException("Account not found"));
                        return ctx.Done();
                    }

                    return ctx.ThenPersist(
                        new TransferFundsEvent(
                            Guid.NewGuid().ToString(),
                            cmd.AccountId,
                            cmd.ToAccountId,
                            cmd.Amount
                        )
                    );
                }
            )
            .SetEventHandler<TransferFundsEvent, BankAccountState>(e =>
                State
                    .FreezeFunds(e.TransactionId, e.Amount)
                    .WithBalance(State.Balance - e.Amount)
            )
            .SetEventHandler<FundsTransferredEvent, BankAccountState>(e =>
                State
                    .UnfreezeFunds(e.TransactionId)
                    .WithBalance(State.Balance + e.Amount)
            )
            .SetEventHandler<BankAccountCreatedEvent, BankAccountState>(e =>
                State
                    .WithId(e.AccountId)
                    .WithName(e.Name)
                    .WithBalance(e.InitialBalance)
                    .WithCreated()
            )
            .SetEventHandler<AccountBalanceChangedEvent, BankAccountState>(e =>
                State
                    .WithBalance(State.Balance + e.Amount)
            )
            .Build();
}

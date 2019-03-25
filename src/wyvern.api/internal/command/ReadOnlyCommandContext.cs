using System;
using Akka.Actor;
using wyvern.api.abstractions;
using wyvern.api.exceptions;

namespace wyvern.api.@internal.command
{
    /// <summary>
    /// Readonly command context
    /// </summary>
    internal abstract class ReadOnlyCommandContext : IReadOnlyCommandContext
    {
        /// <summary>
        /// Actor to reply to (generally the sender of the command)
        /// </summary>
        /// <value></value>
        private IActorRef ReplyTo { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sender"></param>
        internal ReadOnlyCommandContext(IActorRef sender) => ReplyTo = sender;

        /// <summary>
        /// Reply to the sender with a message of type R
        /// </summary>
        /// <param name="msg"></param>
        /// <typeparam name="TR"></typeparam>
        public void Reply<TR>(TR msg) => ReplyTo.Tell(msg);

        /// <summary>
        /// Reply to the sender with the given exception
        /// </summary>
        /// <param name="ex"></param>
        public void CommandFailed(Exception ex) => ReplyTo.Tell(ex);

        /// <summary>
        /// Reply to the sender with an 'invalid command exception'
        /// </summary>
        /// <param name="message"></param>
        internal void InvalidCommand(string message) => CommandFailed(new BadRequestException(message));
    }
}

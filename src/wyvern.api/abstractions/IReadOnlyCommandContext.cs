using System;

namespace wyvern.api
{
    /// <summary>
    ///     Readonly command context interface
    /// </summary>
    public interface IReadOnlyCommandContext
    {
        /// <summary>
        ///     Command failed event emitter
        /// </summary>
        /// <param name="ex"></param>
        void CommandFailed(Exception ex);

        /// <summary>
        ///     Reply response emitter
        /// </summary>
        /// <param name="msg"></param>
        /// <typeparam name="R"></typeparam>
        void Reply<R>(R msg);
    }
}
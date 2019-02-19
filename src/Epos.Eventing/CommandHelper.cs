using System;
using System.Threading.Tasks;

namespace Epos.Eventing
{
    /// <summary> Messaging helper functionalities. </summary>
    public class CommandHelper : ICommandHelper
    {
        private readonly Func<Task> myAck;

        /// <summary> Creates an instance of the <b>CommandHelper</b> class.
        /// </summary>
        /// <param name="ack">Ack delegate</param>
        public CommandHelper(Func<Task> ack) {
            myAck = ack ?? throw new ArgumentNullException(nameof(ack));
        }

        /// <summary> Acknowledges the message. </summary>
        public Task Ack() => myAck();
    }
}

using System;
using System.Threading.Tasks;

namespace Epos.Messaging
{
    /// <summary> Messaging helper functionalities. </summary>
    public class IntegrationCommandHelper : IIntegrationCommandHelper
    {
        private readonly Func<Task> myAck;

        /// <summary> Creates an instance of the <b>CommandHelper</b> class.
        /// </summary>
        /// <param name="ack">Ack delegate</param>
        public IntegrationCommandHelper(Func<Task> ack) {
            myAck = ack ?? throw new ArgumentNullException(nameof(ack));
        }

        /// <summary> Acknowledges the message. </summary>
        public Task Ack() => myAck();
    }
}

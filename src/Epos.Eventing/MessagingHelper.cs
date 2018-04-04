using System;

namespace Epos.Eventing
{
    /// <summary> Messaging helper functionalities. </summary>
    public class MessagingHelper
    {
        private readonly Action myAck;

        /// <summary> Creates an instance of the <b>MessagingHelper</b> class.
        /// </summary>
        /// <param name="ack">Ack delegate</param>
        public MessagingHelper(Action ack) {
            myAck = ack ?? throw new ArgumentNullException(nameof(ack));
        }

        /// <summary> Acknowledges the message. </summary>
        public void Ack() => myAck();
    }
}

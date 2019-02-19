using System.Threading.Tasks;

namespace Epos.Eventing
{
    /// <summary> Messaging helper functionalities. </summary>
    public interface ICommandHelper
    {
        /// <summary> Acknowledges the message. </summary>
        Task Ack();
    }
}

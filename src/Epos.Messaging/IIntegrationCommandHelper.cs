using System.Threading.Tasks;

namespace Epos.Messaging
{
    /// <summary> Integration command helper functionalities. </summary>
    public interface IIntegrationCommandHelper
    {
        /// <summary> Acknowledges the command. </summary>
        Task Ack();
    }
}

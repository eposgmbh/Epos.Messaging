using System;

namespace Epos.Eventing
{
    /// <summary> Provides a topic for an integration command to differentiate the integration command further.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class TopicAttribute : Attribute
    {
        /// <summary> Creates an instance of the <b>TopicAttribute</b> class. </summary>
        /// <param name="topic">Topic</param>
        public TopicAttribute(string topic) {
            Topic = topic;
        }

        /// <summary> Gets the topic. </summary>
        public string Topic { get; }
    }
}

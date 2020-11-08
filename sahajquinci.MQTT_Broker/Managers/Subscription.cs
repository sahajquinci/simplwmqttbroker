using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace sahajquinci.MQTT_Broker.Managers
{
    /// <summary>
    /// MQTT subscription
    /// </summary>
    public class Subscription
    {
        public string ClientId { get; set; }
        public string Topic { get; set; }
        /// <summary>
        /// QoS level granted for the subscription
        /// </summary>
        public byte QosLevel { get; set; }
        
        public Subscription()
        {
            this.ClientId = null;
            this.Topic = null;
            this.QosLevel = 0x00;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="clientId">Client Id of the subscription</param>
        /// <param name="topic">Topic of subscription</param>
        /// <param name="qosLevel">QoS level of subscription</param>
        public Subscription(string clientId, string topic, byte qosLevel)
        {
            this.ClientId = clientId;
            this.Topic = topic;
            this.QosLevel = qosLevel;
        }

        /// <summary>
        /// Dispose subscription
        /// </summary>
        public void Dispose()
        {
            this.ClientId = null;
            this.Topic = null;
            this.QosLevel = 0;
        }
    }

}
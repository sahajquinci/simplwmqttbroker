using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;

namespace sahajquinci.MQTT_Broker.Managers
{
    /// <summary>
    /// MQTT subscription comparer
    /// </summary>
    public class MqttSubscriptionComparer : IEqualityComparer<Subscription>
    {
        /// <summary>
        /// MQTT subscription comparer type
        /// </summary>
        public MqttSubscriptionComparerType Type { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">MQTT subscription comparer type</param>
        public MqttSubscriptionComparer(MqttSubscriptionComparerType type)
        {
            this.Type = type;
        }

        public bool Equals(Subscription x, Subscription y)
        {
            if (this.Type == MqttSubscriptionComparerType.OnClientId)
                return x.ClientId.Equals(y.ClientId);
            else if (this.Type == MqttSubscriptionComparerType.OnTopic)
                return (new Regex(x.Topic)).IsMatch(y.Topic);
            else
                return false;
        }
        public int GetHashCode(Subscription obj)
        {
            if (this.Type == MqttSubscriptionComparerType.OnClientId)
                return obj.ClientId.GetHashCode();
            else if (this.Type == MqttSubscriptionComparerType.OnTopic)
                return obj.Topic.GetHashCode();
            else
                return 0;
        }

        /// <summary>
        /// MQTT subscription comparer type
        /// </summary>
        public enum MqttSubscriptionComparerType
        {
            OnClientId,
            OnTopic
        }
    }

}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using sahajquinci.MQTT_Broker.Messages;

namespace sahajquinci.MQTT_Broker
{
    public class MqttClient : IEquatable<MqttClient>
    {
        public string ClientId { get; set; }
        public bool IsConnected { get; set; }
        public uint ClientIndex { get; set; }
        public bool CleanSession { get; private set; }
        public bool WillFlag { get; set; }
        public byte WillQosLevel { get; private set; }
        public string WillTopic { get; private set; }
        public string WillMessage { get; private set; }
        public bool WillRetain { get; private set; }

        public bool IsWebSocketClient { get; private set; }

        public MqttClient(uint clientIndex, MqttMsgConnect mqttMsgConnect, bool isWebSocketClient) : this(clientIndex,mqttMsgConnect)
        {
             this.IsWebSocketClient = isWebSocketClient;
        }

        public MqttClient(uint clientIndex, MqttMsgConnect mqttMsgConnect)
        {
            ClientId = mqttMsgConnect.ClientId;
            CleanSession = mqttMsgConnect.CleanSession;
            ClientIndex = clientIndex;
            WillFlag = mqttMsgConnect.WillFlag;
            WillQosLevel = mqttMsgConnect.WillQosLevel;
            WillTopic = mqttMsgConnect.WillTopic;
            WillRetain = mqttMsgConnect.WillRetain;
            WillMessage = mqttMsgConnect.WillMessage;
            IsConnected = true;
        }

        #region IEquatable<MqttClient> Members

        public bool Equals(MqttClient other)
        {
            if (this.ClientId == other.ClientId &&
                this.CleanSession == other.CleanSession &&
                this.ClientIndex == other.ClientIndex &&
                this.IsConnected == other.IsConnected &&
                this.WillFlag == other.WillFlag &&
                this.WillMessage == other.WillMessage &&
                this.WillQosLevel == other.WillQosLevel &&
                this.WillRetain == other.WillRetain &&
                this.WillTopic == other.WillTopic)
            {
                return true;
            }
            else
                return false;
        }

        public override bool Equals(object other)
        {
            // other could be a reference type, thus we check for null
            if (other == null) return base.Equals(other);

            if (other is MqttClient)
            {
                return this.Equals((MqttClient)other);
            }
            else if (other is double)
            {
                return this.Equals((double)other);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = (int)2166136261;
                // Suitable nullity checks etc, of course :)
                hash = (hash * 16777619) ^ ClientId.GetHashCode();
                hash = (hash * 16777619) ^ CleanSession.GetHashCode();
                hash = (hash * 16777619) ^ ClientIndex.GetHashCode();
                hash = (hash * 16777619) ^ IsConnected.GetHashCode();
                if (WillFlag)
                {
                    hash = (hash * 16777619) ^ WillFlag.GetHashCode();
                    hash = (hash * 16777619) ^ WillMessage.GetHashCode();
                    hash = (hash * 16777619) ^ WillQosLevel.GetHashCode();
                    hash = (hash * 16777619) ^ WillRetain.GetHashCode();
                    hash = (hash * 16777619) ^ WillTopic.GetHashCode();
                }

                return hash;
            }
        }

        #endregion
    }
}
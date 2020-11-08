using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using sahajquinci.MQTT_Broker.Messages;

namespace sahajquinci.MQTT_Broker.Events
{
    public class PacketReceivedEventHandler : EventArgs
    {
        public bool IsWebSocketClient { get; private set; }
        public uint ClientIndex { get; private set; }
        public MqttMsgBase Packet { get; private set; }
        public PacketReceivedEventHandler(uint clientIndex, MqttMsgBase packet, bool isWebSocketClient)
        {
            this.ClientIndex = clientIndex;
            this.Packet = packet;
            this.IsWebSocketClient = isWebSocketClient;
        }
    }
}
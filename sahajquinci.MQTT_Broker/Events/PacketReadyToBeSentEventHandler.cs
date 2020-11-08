using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace sahajquinci.MQTT_Broker.Events
{
    public class PacketReadyToBeSentEventHandler : EventArgs
    {
        public byte[] Packet{ get; private set; }
        public string ClientId { get; private set; }
        public PacketReadyToBeSentEventHandler(string clientId , byte[] packet)
        {
            this.ClientId = clientId;
            this.Packet = packet;
        }
    }
}
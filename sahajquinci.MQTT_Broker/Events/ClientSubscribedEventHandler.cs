using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace sahajquinci.MQTT_Broker
{
    public class ClientSubscribedEventHandler : EventArgs
    {
        public string[] Topics { get; private set; }
        public string ClientId { get; private set; }
        public ClientSubscribedEventHandler(string[] topics , string clientId)
        {
            Topics = topics;
            ClientId = clientId;
        }
    }
}
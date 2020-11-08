using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using sahajquinci.MQTT_Broker.Managers;

namespace sahajquinci.MQTT_Broker.Managers
{
    public class Session
    {
        public string ClientId { get; private set; }
        public List<Subscription> Subscriptions { get; set; }

        public Session(string clientId)
        {
            ClientId = clientId;
            Subscriptions = new List<Subscription>();
        }
    }
}
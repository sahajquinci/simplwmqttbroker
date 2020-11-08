using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using sahajquinci.MQTT_Broker.Messages;

namespace sahajquinci.MQTT_Broker.Managers
{
    public class SessionManager
    {
        private SubscriptionManager subscriptionManager;
        public static List<Session> sessions = new List<Session>();

        public SessionManager(SubscriptionManager subscriptionManager)
        {
            this.subscriptionManager = subscriptionManager;
        }
        public static List<Subscription> GetAllTheSubscriptions()
        {
            List<Subscription> subs = new List<Subscription>();
            sessions.ForEach(s => subs.AddRange(s.Subscriptions));
            return subs;
        }

        internal void Destroy(string clientId)
        {
            Session session = sessions.FirstOrDefault(s => s.ClientId == clientId);
            if (session != null)
            {
                sessions.Remove(session);
                subscriptionManager.Unsubscribe(clientId);
            }
        }

        internal void Create(string clientId, MqttClient client)
        {
            Session s = new Session(clientId);
            sessions.Add(s);
        }

        internal void AddSubscription(string clientId, MqttMsgSubscribe packet)
        {
            subscriptionManager.Subscribe(clientId, packet);
        }

        internal void Unsubscribe(string clientId, MqttMsgUnsubscribe packet)
        {
            subscriptionManager.Unsubscribe(clientId,packet);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using sahajquinci.MQTT_Broker.Messages;

namespace sahajquinci.MQTT_Broker.Managers
{
    /// <summary>
    /// Manager for topics and subscribers.
    /// Handles the clients subscriptions to the various topics.
    /// </summary>
    public class SubscriptionManager
    {
        #region Constants ...

        // topic wildcards '+' and '#'
        private const string PLUS_WILDCARD = "+";
        private const string SHARP_WILDCARD = "#";

        // replace for wildcards '+' and '#' for using regular expression on topic match
        private const string PLUS_WILDCARD_REPLACE = @"[^/]+";
        private const string SHARP_WILDCARD_REPLACE = @".*";

        #endregion

        private MqttSubscriptionComparer comparer = new MqttSubscriptionComparer(MqttSubscriptionComparer.MqttSubscriptionComparerType.OnClientId);

        public event EventHandler<ClientSubscribedEventHandler> ClientSubscribed;

        private void OnClientSubscribed(string[] topics, string clientId)
        {
            if (ClientSubscribed != null)
            {
                ClientSubscribed(this, new ClientSubscribedEventHandler(topics, clientId));
            }
        }

        public void Subscribe(string clientId, MqttMsgSubscribe packet)
        {
            try
            {
                Session session = SessionManager.sessions.First(ss => ss.ClientId == clientId);
                List<Subscription> subs = session.Subscriptions;
                lock (subs)
                {
                    for (int i = 0; i < packet.Topics.Length; i++)
                    {
                        string topicReplaced = packet.Topics[i].Replace(PLUS_WILDCARD, PLUS_WILDCARD_REPLACE).Replace(SHARP_WILDCARD, SHARP_WILDCARD_REPLACE);
                        topicReplaced = "^" + topicReplaced + "$";

                        Subscription existingSubscription = subs.FirstOrDefault(sub => sub.Topic == packet.Topics[i]);
                        if (existingSubscription == null)
                        {
                            Subscription s = new Subscription(clientId, topicReplaced, packet.QoSLevels[i]);
                            subs.Add(s);
                        }
                    }
                    OnClientSubscribed(packet.Topics, clientId);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Unsubscribe(string clientId, MqttMsgUnsubscribe packet)
        {
            try
            {
                Session session = SessionManager.sessions.First(ss => ss.ClientId == clientId);
                List<Subscription> subs = session.Subscriptions;
                lock (subs)
                {
                    for (int i = 0; i < packet.Topics.Length; i++)
                    {
                        string topicReplaced = packet.Topics[i].Replace(PLUS_WILDCARD, PLUS_WILDCARD_REPLACE).Replace(SHARP_WILDCARD, SHARP_WILDCARD_REPLACE);
                        topicReplaced = "^" + topicReplaced + "$";

                        if (subs != null)
                            subs.Remove(subs.First(s => s.Topic == topicReplaced));
                    }
                }
            }
            catch (Exception)
            {
                throw new ArgumentNullException("Couldn't find the client in subscriptions");
            }
        }

        public void Unsubscribe(string clientId)
        {
            Session s = SessionManager.sessions.FirstOrDefault(ss => ss.ClientId == clientId);
            if (s != null)
                s.Subscriptions = null;
        }

        #region RETREIVE_SUBS_REGION

        /// <summary>
        /// Get a subscription for a specified topic and client
        /// </summary>
        /// <param name="topic">Topic to get subscription</param>
        /// <param name="clientId">Client Id to get subscription</param>
        /// <returns>Subscription list</returns>
        public Subscription GetSubscription(string topic, string clientId)
        {
            List<Subscription> subs = SessionManager.GetAllTheSubscriptions();
            var query = from ss in subs
                        where new Regex(ss.Topic).IsMatch(topic)    // check for topics based also on wildcard with regex
                        && ss.ClientId == clientId
                        select ss;

            // use comparer for multiple subscriptions that overlap (e.g. /test/# and  /test/+/foo)
            // If a client is subscribed to multiple subscriptions with topics that overlap
            // it has more entries into subscriptions list but broker sends only one message
            this.comparer.Type = MqttSubscriptionComparer.MqttSubscriptionComparerType.OnClientId;
            return query.Distinct(comparer).FirstOrDefault();
        }

        /// <summary>
        /// Get subscription list for a specified topic
        /// </summary>
        /// <param name="topic">Topic to get subscription list</param>
        /// <returns>Subscription list</returns>
        public List<Subscription> GetSubscriptionsByTopic(string topic)
        {
            lock (SessionManager.sessions)
            {
                List<Subscription> subs = SessionManager.GetAllTheSubscriptions();
                var query = from ss in subs
                            where new Regex(ss.Topic).IsMatch(topic)    // check for topics based also on wildcard with regex
                            select ss;
                if (query.Count() > 0)
                {
                    // use comparer for multiple subscriptions that overlap (e.g. /test/# and  /test/+/foo)
                    // If a client is subscribed to multiple subscriptions with topics that overlap
                    // it has more entries into subscriptions list but broker sends only one message
                    this.comparer.Type = MqttSubscriptionComparer.MqttSubscriptionComparerType.OnClientId;
                    return query.Distinct(comparer).ToList();
                }
                else
                    return null;
            }
        }

        /// <summary>
        /// Get subscription list for a specified client
        /// </summary>
        /// <param name="clientId">Client Id to get subscription list</param>
        /// <returns>Subscription lis</returns>
        public List<Subscription> GetSubscriptionsByClient(string clientId)
        {
            lock (SessionManager.sessions)
            {
                List<Subscription> subs = SessionManager.GetAllTheSubscriptions();
                var query = from s in subs
                            where s.ClientId == clientId
                            select s;

                // use comparer for multiple subscriptions that overlap (e.g. /test/# and  /test/+/foo)
                // If a client is subscribed to multiple subscriptions with topics that overlap
                // it has more entries into subscriptions list but broker sends only one message
                //this.comparer.Type = MqttSubscriptionComparer.MqttSubscriptionComparerType.OnTopic;
                //return query.Distinct(comparer).ToList();
                this.comparer.Type = MqttSubscriptionComparer.MqttSubscriptionComparerType.OnTopic;
                return query.Distinct(comparer).ToList();
                // I need all subscriptions, also overlapped (used to save session)
                // return query.ToList();
            }
        }
    }

        #endregion
}


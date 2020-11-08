using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using sahajquinci.MQTT_Broker.Managers;
using sahajquinci.MQTT_Broker;
using sahajquinci.MQTT_Broker.Messages;
using System.Text.RegularExpressions;
using sahajquinci.MQTT_Broker.Events;

namespace sahajquinci.MQTT_Broker.Managers
{
    public class PublishManager
    {
        private Dictionary<string, Queue<MqttMsgPublish>> publishQueue = new Dictionary<string, Queue<MqttMsgPublish>>();
        private Dictionary<string, MqttMsgPublish> retainedMessages = new Dictionary<string, MqttMsgPublish>();

        private SubscriptionManager subscriptionManager;

        public event EventHandler<PacketReadyToBeSentEventHandler> PacketReadyToBeSent;

        public PublishManager(SubscriptionManager subscriptionManager)
        {
            this.subscriptionManager = subscriptionManager;
            subscriptionManager.ClientSubscribed += this.DeliverRetained;
        }

        internal void ManagePubRec(uint clientIndex, MqttMsgPubrec pubRec)
        {
            throw new NotImplementedException();
        }

        internal void ManagePubRel(uint clientIndex, MqttMsgPubrel pubRel)
        {
            throw new NotImplementedException();
        }

        internal void ManagePubAck(uint clientIndex, MqttMsgPuback pubAck)
        {
            throw new NotImplementedException();
        }

        internal void Publish(MqttMsgPublish packet)
        {
            CheckRetain(packet);
            packet.Retain = false; //MQTT-3.3.1-9
            if (!publishQueue.ContainsKey(packet.Topic))
            {
                publishQueue.Add(packet.Topic, new Queue<MqttMsgPublish>());
            }
            publishQueue[packet.Topic].Enqueue(packet);
            RouteOnTheQoSLevelOfTheSubscriber(packet.Topic);
            lock (publishQueue)
            {
                if (publishQueue.ContainsKey(packet.Topic))
                {
                    if (publishQueue[packet.Topic].Count == 0)
                        publishQueue.Remove(packet.Topic);
                }
            }
        }

        private void RouteOnTheQoSLevelOfTheSubscriber(string topic)
        {
            MqttMsgPublish publish = publishQueue[topic].Dequeue();
            //var connectedClients = MqttServer.GetConnectedClientIds();
            var connectedClients = PacketManager.GetConnectedClientIds();
            var subscriptionsByTopic = subscriptionManager.GetSubscriptionsByTopic(publish.Topic);
            var subscriptions = subscriptionsByTopic != null ? subscriptionsByTopic.Where(s => connectedClients.Contains(s.ClientId)) : null;
            if (subscriptions != null)
            {
                foreach (var sub in subscriptions)
                {
                    if (sub.QosLevel == 0x00 && publish.QosLevel > MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE)
                    {
                        MqttMsgPublish pub = MqttMsgPublish.Parse(publish.GetBytes());
                        pub.QosLevel = MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE;
                        RouteOnQoS(pub, sub);
                    }
                    else if (sub.QosLevel == 0x01 && publish.QosLevel > MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE)
                    {
                        MqttMsgPublish pub = MqttMsgPublish.Parse(publish.GetBytes());
                        pub.QosLevel = MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE;
                        RouteOnQoS(pub, sub);
                    }
                    else
                    {
                        RouteOnQoS(publish, sub);
                    }
                }
            }
        }

        private void RouteOnQoS(MqttMsgPublish publish, Subscription sub)
        {
            switch (publish.QosLevel)
            {
                case MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE:
                    {
                        ManageQoS0(publish, sub);
                        break;
                    }
                case MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE:
                    {
                        throw new NotImplementedException();
                    }
                case MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE:
                    {
                        throw new NotImplementedException();
                    }
                default:
                    break;
            }
        }

        #region QOS0_REGION
        /// <summary>
        /// Delivers to the subscribers with qos 0 of the publish topic the publish message 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="publish"></param>
        private void ManageQoS0(MqttMsgPublish publish, Subscription subscription)
        {
            try
            {
                //MqttServer.Instance.Send(subscription.ClientId, publish.GetBytes());
                OnPacketReadyToBeSent(subscription.ClientId, publish.GetBytes());
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        private void CheckRetain(MqttMsgPublish publish)
        {
            if (publish.Retain)
            {
                // retained message already exists for the topic
                if (retainedMessages.ContainsKey(publish.Topic))
                {
                    // if empty message, remove current retained message
                    if (publish.Message.Length == 0)
                        retainedMessages.Remove(publish.Topic);
                    // set new retained message for the topic
                    else
                        retainedMessages[publish.Topic] = publish;
                }
                else
                {
                    // add new topic with related retained message
                    retainedMessages.Add(publish.Topic, publish);
                }
            }
        }

        private void DeliverRetained(object source, ClientSubscribedEventHandler e)
        {
            string clientId = e.ClientId;
            string[] topics = e.Topics;
            foreach (string topic in topics)
            {
                var subs = subscriptionManager.GetSubscriptionsByTopic(topic).Where(sub => sub.ClientId == clientId);

                var query = from r in retainedMessages
                            from s in subs
                            where (new Regex(s.Topic)).IsMatch(r.Key)
                            select r.Value.GetBytes();

                if (query.Count() > 0)
                {
                    byte[] messages = query.SelectMany(b => b).ToArray();
                    //MqttServer.Instance.Send(clientId, messages);
                    OnPacketReadyToBeSent(clientId, messages);
                }
            }
        }

        public void OnPacketReadyToBeSent(string clientId , byte[] packet)
        {
            if (PacketReadyToBeSent != null)
            {
                PacketReadyToBeSent(this, new PacketReadyToBeSentEventHandler(clientId,packet));
            }
        }

    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using sahajquinci.MQTT_Broker.Managers;
using sahajquinci.MQTT_Broker.Events;
using sahajquinci.MQTT_Broker.Messages;
using sahajquinci.MQTT_Broker.Utility;
using Crestron.SimplSharp.CrestronLogger;

namespace sahajquinci.MQTT_Broker
{
    public abstract class ServerBase
    {
        protected SecureTCPServer Server { get; set; }
        protected SessionManager SessionManager { get; set; }
        protected List<MqttClient> Clients { get; set; }
        protected List<ushort> PacketIdentifiers { get; set; }
        protected Random Rand { get; set; }
        public event EventHandler<PacketReceivedEventHandler> PacketReceived;

        protected int Port { get; private set; }
        protected int NumberOfConnections { get; private set; }

        public ServerBase(List<MqttClient> clients, SessionManager sessionManager, List<ushort> packetIdentifiers, Random rand, int port, int numberOfConnections)
        {
            Clients = clients;
            SessionManager = sessionManager;
            PacketIdentifiers = packetIdentifiers;
            Rand = rand;
            this.Port = port;
            this.NumberOfConnections = numberOfConnections;
            Server = new SecureTCPServer(port, 4096, EthernetAdapterType.EthernetLANAdapter, numberOfConnections);
            Server.SocketStatusChange += OnSocketStatusChange;
            Server.WaitForConnectionAsync(IPAddress.Parse("0.0.0.0"), this.ConnectionCallback);
        }

        protected abstract void ConnectionCallback(SecureTCPServer server, uint clientIndex);
        public abstract void Send(uint clientIndex, byte[] buffer);
        public abstract void Receive(uint clientIndex);
        protected abstract void ReceiveCallback(SecureTCPServer myTCPServer, uint clientIndex, int numberOfBytesReceived);
        protected abstract void DecodeMultiplePacketsByteArray(uint clientIndex, byte[] data);
        public abstract void DisconnectClient(uint clientIndex, bool withDisconnectPacket);
        public abstract void RejectConnection(uint clientIndex);

        protected void SendCallback(SecureTCPServer myTCPServer, uint clientIndex, int numberOfBytesSent)
        {
           // CrestronLogger.WriteToLog("MQTTServer - SEND CALLBACK - Data Sent to client  " + GetClientByIndex(clientIndex).ClientId + " Number of bytes : " + numberOfBytesSent, 2);
        }

        protected void OnClientDisconnected(MqttClient client, bool withDisconnectPacket)
        {
            try
            {
                if (!withDisconnectPacket && client.WillFlag)
                {
                    MqttMsgPublish publish = MsgBuilder.BuildPublish(client.WillTopic, false, client.WillQosLevel, client.WillRetain, Encoding.ASCII.GetBytes(client.WillMessage), GetNewPacketIdentifier());
                    //Send(client.ClientIndex, publish.GetBytes());                    
                    OnPacketReceived(client.ClientIndex, publish, client.IsWebSocketClient);
                }
                else if (withDisconnectPacket)
                {
                    SessionManager.Destroy(client.ClientId);
                    client.IsConnected = false;
                }

            }
            catch (Exception e)
            {
                CrestronLogger.WriteToLog("MQTTSERVER - OnClientDisconnected , error message : " + e.Message, 8);
                CrestronLogger.WriteToLog("MQTTSERVER - OnClientDisconnected , error STACK TRACE : " + e.StackTrace, 8);
            }
        }

        public void OnPacketReceived(uint clientIndex, MqttMsgBase packet, bool isWebSocketClient)
        {
            if (PacketReceived != null)
            {
                PacketReceived(this, new PacketReceivedEventHandler(clientIndex, packet, isWebSocketClient));
            }
        }

        protected void OnSocketStatusChange(SecureTCPServer myTCPServer, uint clientIndex, SocketStatus serverSocketStatus)
        {
            if (serverSocketStatus != SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                DisconnectClient(clientIndex, false);
            }
        }

        protected MqttClient GetClientByIndex(uint clientIndex, bool isWebSocketClient)
        {
            //Get the client corresponding to the clientIndex and type of server
            var query = from client in Clients
                        where client.ClientIndex.Equals(clientIndex) && (client.IsWebSocketClient == isWebSocketClient)
                        select client;

            return query.First();
        }

        internal ushort GetNewPacketIdentifier()
        {
            lock (PacketIdentifiers)
            {
                ushort identifier = (ushort)Rand.Next(0, 65535);
                while (PacketIdentifiers.Contains(identifier))
                {
                    identifier = (ushort)Rand.Next(0, 65535);
                }
                PacketIdentifiers.Add(identifier);
                return identifier;
            }
        }
    }
}
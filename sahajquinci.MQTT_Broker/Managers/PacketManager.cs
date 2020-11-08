using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using sahajquinci.MQTT_Broker.Messages;
using sahajquinci.MQTT_Broker.Utility;
using Crestron.SimplSharp.CrestronLogger;
using sahajquinci.MQTT_Broker.Exceptions;
using Crestron.SimplSharp.CrestronSockets;
using sahajquinci.MQTT_Broker.Events;
using Crestron.SimplSharp.CrestronAuthentication;

namespace sahajquinci.MQTT_Broker.Managers
{
    public class PacketManager
    {
        private Random rand;
        private SessionManager sessionManager;
        private PublishManager publishManager;
        private SubscriptionManager subscriptionManager;
        private static List<MqttClient> clients;
        private List<ushort> packetIdentifiers;

        private TCPServer tcpServer;
        private WSServer wsServer;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="port"></param>
        /// <param name="numberOfConnections"></param>
        public PacketManager(Random rand, SessionManager sessionManager, PublishManager publishManager, SubscriptionManager subscriptionManager,
            List<MqttClient> clients, List<ushort> packetIdentifiers, TCPServer tcpServer, WSServer wsServer)
        {
            this.rand = rand;
            this.sessionManager = sessionManager;
            this.publishManager = publishManager;
            this.publishManager.PacketReadyToBeSent += Send;
            this.subscriptionManager = subscriptionManager;
            PacketManager.clients = clients;
            this.packetIdentifiers = packetIdentifiers;

            this.tcpServer = tcpServer;
            this.wsServer = wsServer;
            this.tcpServer.PacketReceived += RouteControlPacketToMethodHandler;
            this.wsServer.PacketReceived += RouteControlPacketToMethodHandler;
        }


        /// <summary>
        /// Sends asynchronously the packet to a specific client.
        /// </summary>
        /// <param name="clientIndex">Index of the client.</param>
        /// <param name="pBufferToSend"></param>
        /// <param name="numBytesToSend"></param>
        public void Send(uint clientIndex, byte[] pBufferToSend, int numBytesToSend, bool isWebSocketClient)
        {
            try
            {
                MqttClient c = GetClientByIndex(clientIndex, isWebSocketClient);
                if (!c.IsWebSocketClient)
                {
                    tcpServer.Send(clientIndex, pBufferToSend);
                }
                else
                {
                    wsServer.Send(clientIndex, pBufferToSend);
                }
            }
            catch (Exception e)
            {
                CrestronLogger.WriteToLog("MQTTSERVER - Send - Exception occured trying to send data message :  " + e.Message, 8);
                CrestronLogger.WriteToLog("MQTTSERVER - Send - Exception occured trying to send data stack trace :  " + e.StackTrace, 8);
            }
        }

        public void Send(object sourcer, PacketReadyToBeSentEventHandler args)
        {
            MqttClient client = clients.First(c => c.ClientId == args.ClientId);
            uint cIndx = client.ClientIndex;
            Send(cIndx, args.Packet, args.Packet.Length, client.IsWebSocketClient);
        }

        /// Disconnects a specific client.
        /// If the client wasn't connected already it will return false.
        /// </summary>
        /// <param name="clientIndex"></param>
        ///<param name="withDisconnectPacket"> True = Disconnect packet received from the client. \n False = protocol violation or error has occured</param>
        /// <returns>True if the client has been successfully disconnected , or was disconnected already</returns>
        public void DisconnectClient(uint clientIndex, bool withDisconnectPacket, bool isWebSocketClient)
        {
            MqttClient c = GetClientByIndex(clientIndex, isWebSocketClient);
            if (!c.IsWebSocketClient)
            {
                tcpServer.DisconnectClient(clientIndex, withDisconnectPacket);
            }
            else
            {
                wsServer.DisconnectClient(clientIndex, withDisconnectPacket);
            }


        }



        #region PACKET_HANDLER_REGION

        private void RouteControlPacketToMethodHandler(object source, PacketReceivedEventHandler args)
        {
            MqttMsgBase packet = args.Packet;
            uint clientIndex = args.ClientIndex;
            bool isWebSocketClient = args.IsWebSocketClient;
            try
            {
                switch (packet.Type)
                {
                    case MqttMsgBase.MQTT_MSG_CONNECT_TYPE:
                        {
                            HandleCONNECTType(clientIndex, (MqttMsgConnect)packet, args.IsWebSocketClient);
                            break;
                        }
                    case MqttMsgBase.MQTT_MSG_CONNACK_TYPE:
                        {
                            CrestronLogger.WriteToLog("MQTTSERVER - ROUTE PACKET -  conack packet to broker is not allowed , Disconnecting client", 8);
                            DisconnectClient(clientIndex, false, isWebSocketClient);
                            break;
                        }
                    case MqttMsgBase.MQTT_MSG_PUBLISH_TYPE:
                        {
                            HandlePUBLISHType(clientIndex, (MqttMsgPublish)packet);
                            break;
                        }
                    case MqttMsgBase.MQTT_MSG_PUBACK_TYPE:
                        {
                            HandlePUBACKType(clientIndex, (MqttMsgPuback)packet);
                            break;
                        }
                    case MqttMsgBase.MQTT_MSG_PUBREC_TYPE:
                        {
                            HandlePUBRECType(clientIndex, (MqttMsgPubrec)packet);
                            break;
                        }
                    case MqttMsgBase.MQTT_MSG_PUBREL_TYPE:
                        {
                            HandlePUBRELType(clientIndex, (MqttMsgPubrel)packet);
                            break;
                        }
                    case MqttMsgBase.MQTT_MSG_PUBCOMP_TYPE:
                        {
                            HandlePUBCOMPType(clientIndex, (MqttMsgPubcomp)packet, isWebSocketClient);
                            break;
                        }
                    case MqttMsgBase.MQTT_MSG_SUBSCRIBE_TYPE:
                        {
                            HandleSUBSCRIBEType(clientIndex, (MqttMsgSubscribe)packet, isWebSocketClient);
                            break;
                        }
                    case MqttMsgBase.MQTT_MSG_SUBACK_TYPE:
                        {
                            DisconnectClient(clientIndex, false, isWebSocketClient);
                            break;
                        }
                    case MqttMsgBase.MQTT_MSG_UNSUBSCRIBE_TYPE:
                        {
                            HandleUNSUBSCRIBEType(clientIndex, (MqttMsgUnsubscribe)packet, isWebSocketClient);
                            break;
                        }
                    case MqttMsgBase.MQTT_MSG_UNSUBACK_TYPE:
                        {
                            DisconnectClient(clientIndex, false, isWebSocketClient);
                            break;
                        }
                    case MqttMsgBase.MQTT_MSG_PINGREQ_TYPE:
                        {
                            HandlePINGREQType(clientIndex, (MqttMsgPingReq)packet, isWebSocketClient);
                            break;
                        }
                    case MqttMsgBase.MQTT_MSG_PINGRESP_TYPE:
                        {
                            HandlePINGRESPType(clientIndex, (MqttMsgPingResp)packet, isWebSocketClient);
                            break;
                        }
                    case MqttMsgBase.MQTT_MSG_DISCONNECT_TYPE:
                        {
                            HandleDISCONNECTType(clientIndex, (MqttMsgDisconnect)packet, isWebSocketClient);
                            break;
                        }
                    default:
                        {
                            throw new MqttCommunicationException(new FormatException("MQTTSERVER -Pacchetto non valido" + packet));
                        }
                }
            }
            catch (Exception e)
            {
                CrestronLogger.WriteToLog("MQTTSERVER - RouteControlPacketToMethodHandler - Exception occured trying route packet :  " + e.Message, 8);
                CrestronLogger.WriteToLog("MQTTSERVER - RouteControlPacketToMethodHandler - Stack trace :  " + e.StackTrace, 8);
            }
        }


        #region CONNECTION_REGION
        private void HandleCONNECTType(uint clientIndex, MqttMsgConnect packet, bool isWebSocketClient)
        {
            try
            {                
                byte returnCode = ConnectVerify(packet);
                SendConnack(clientIndex, packet.CleanSession, returnCode, isWebSocketClient);
                if (returnCode.Equals(MqttMsgConnack.CONN_ACCEPTED))
                {                   
                    if (packet.CleanSession)
                    {
                        MqttClient c = new MqttClient(clientIndex, packet, isWebSocketClient);
                        sessionManager.Destroy(packet.ClientId);
                        sessionManager.Create(packet.ClientId, c);
                        clients.Add(c);
                        if (!c.IsWebSocketClient)
                        {
                            tcpServer.Receive(clientIndex);
                        }
                        else
                        {
                            wsServer.Receive(clientIndex);
                        }

                    }
                    else
                    {
                        throw new NotImplementedException("Clean session 0 is not implemented");
                    }
                }
                else
                {                   
                    DisconnectClient(clientIndex, false, isWebSocketClient);
                    throw new MqttConnectionException("Connection rejected" + returnCode, new ArgumentException());
                }
            }
            catch (Exception e)
            {
                CrestronLogger.WriteToLog("\n MQTTSERVER - CONNECT Error Message : " + e.Message, 7);
                
                if (!isWebSocketClient)
                {
                    tcpServer.RejectConnection(clientIndex);
                }
                else
                {
                    wsServer.RejectConnection(clientIndex);
                }

            }
        }

        private void SendConnack(uint clientIndex, bool cleanSession, byte returnCode, bool isWebSocketClient)
        {
            MqttMsgConnack connack = MsgBuilder.BuildConnack(cleanSession, returnCode);

            if (!isWebSocketClient)
            {
                tcpServer.Send(clientIndex, connack.GetBytes());
            }
            else
            {
                wsServer.Send(clientIndex, connack.GetBytes());
            }
            //Send(clientIndex, connack.GetBytes(), connack.GetBytes().Length);
        }

        /// <summary>
        /// Check CONNECT message to accept or not the connection request 
        /// </summary>
        /// <param name="connect">CONNECT message received from client</param>
        /// <returns>Return code for CONNACK message</returns>
        private byte ConnectVerify(MqttMsgConnect connect)
        {
            byte returnCode = MqttMsgConnack.CONN_ACCEPTED;

            // unacceptable protocol version
            if (connect.ProtocolVersion != MqttMsgConnect.PROTOCOL_VERSION_V3_1_1)
            {
                returnCode = MqttMsgConnack.CONN_REFUSED_PROT_VERS;
            }
            else
            {
                // [v.3.1.1] client id zero length is allowed but clean session must be true
                if ((connect.ClientId.Length == 0) && (!connect.CleanSession))
                    returnCode = MqttMsgConnack.CONN_REFUSED_IDENT_REJECTED;
                else
                    if (!MqttSettings.Instance.ControlSytemAuthentication)
                    {
                        if (MqttSettings.Instance.Username != null && MqttSettings.Instance.Password != null && (connect.Username != MqttSettings.Instance.Username || connect.Password != MqttSettings.Instance.Password))
                        {
                            return returnCode = MqttMsgConnack.CONN_REFUSED_USERNAME_PASSWORD;
                        }
                    }
                    else
                    {
                        Authentication.UserInformation userInformation = Authentication.ValidateUserInformation(connect.Username, connect.Password);
                        if (!userInformation.Authenticated || (userInformation.Authenticated && (userInformation.Access != "Administrator" && !userInformation.Groups.Contains("MQTT"))))
                            return returnCode = MqttMsgConnack.CONN_REFUSED_USERNAME_PASSWORD;
                    }
            }
            return returnCode;
        }


        #endregion
        #region PING_REGION

        private void HandlePINGRESPType(uint clientIndex, MqttMsgPingResp mqttMsgSubscribe, bool isWebSocketClient)
        {
            throw new NotImplementedException();
        }

        private void HandlePINGREQType(uint clientIndex, MqttMsgPingReq request, bool isWebSocketClient)
        {
            MqttMsgPingResp response = MsgBuilder.BuildPingResp();
            byte[] resp = response.GetBytes();
            Send(clientIndex, resp, resp.Length, isWebSocketClient);
        }

        #endregion

        #region PUBLISH/SUBSCRIBE_REGION

        private void HandleUNSUBSCRIBEType(uint clientIndex, MqttMsgUnsubscribe packet, bool isWebSocketClient)
        {
            CrestronLogger.WriteToLog("MQTTSERVER  - HandleUNSUBSCRIBEType - Unsubscribe Received" + packet.ToString(), 1);
            sessionManager.Unsubscribe(GetClientByIndex(clientIndex, isWebSocketClient).ClientId, packet);

            byte[] unSubAckBytes = MsgBuilder.BuildUnSubAck(packet.MessageId).GetBytes();
            Send(clientIndex, unSubAckBytes, unSubAckBytes.Length, isWebSocketClient);
        }

        private void HandleSUBSCRIBEType(uint clientIndex, MqttMsgSubscribe packet, bool isWebSocketClient)
        {
            CrestronLogger.WriteToLog("MQTTSERVER  - HandleSUBSCRIBEType - Subscription Received" + packet.ToString(), 6);
            sessionManager.AddSubscription(GetClientByIndex(clientIndex, isWebSocketClient).ClientId, packet);

            byte[] subAckBytes = MsgBuilder.BuildSubAck(packet.MessageId, packet.QoSLevels).GetBytes();
            Send(clientIndex, subAckBytes, subAckBytes.Length, isWebSocketClient);
        }

        private void HandlePUBLISHType(uint clientIndex, MqttMsgPublish packet)
        {
            try
            {
                publishManager.Publish(packet);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void HandlePUBACKType(uint clientIndex, MqttMsgPuback pubAck)
        {
            try
            {
                publishManager.ManagePubAck(clientIndex, pubAck);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void HandlePUBCOMPType(uint clientIndex, MqttMsgPubcomp pubComp, bool isWebSocketClient)
        {
            CrestronLogger.WriteToLog("MQTTSERVER - " + GetClientByIndex(clientIndex, isWebSocketClient).ClientId + " HandlePUBCOMPType - ricevuto PUBCOMP , packetIdentifier: " + pubComp.MessageId, 5);
        }

        private void HandlePUBRELType(uint clientIndex, MqttMsgPubrel pubRel)
        {
            try
            {
                publishManager.ManagePubRel(clientIndex, pubRel);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void HandlePUBRECType(uint clientIndex, MqttMsgPubrec pubRec)
        {
            try
            {
                publishManager.ManagePubRec(clientIndex, pubRec);
            }
            catch (Exception)
            {
                throw;
            }
        }


        #endregion


        private void HandleDISCONNECTType(uint clientIndex, MqttMsgDisconnect mqttMsgDisconnect, bool isWebSocketClient)
        {
            DisconnectClient(clientIndex, true, isWebSocketClient);
        }

        #endregion
        private MqttClient GetClientByIndex(uint clientIndex, bool isWebSocketClient)
        {
            //Get the client corresponding to the clientIndex and type of server
            var query = from client in clients
                        where client.ClientIndex.Equals(clientIndex) && (client.IsWebSocketClient == isWebSocketClient)
                        select client;

            return query.First();
        }

        internal ushort GetNewPacketIdentifier()
        {
            lock (packetIdentifiers)
            {
                ushort identifier = (ushort)rand.Next(0, 65535);
                while (packetIdentifiers.Contains(identifier))
                {
                    identifier = identifier = (ushort)rand.Next(0, 65535);
                }
                packetIdentifiers.Add(identifier);
                return identifier;
            }
        }

        public static List<string> GetConnectedClientIds()
        {
            return clients.Select(c => c.ClientId).ToList();
        }

        internal void FreePacketIdentifier(ushort identifier)
        {
            if (packetIdentifiers.Contains(identifier))
                packetIdentifiers.Remove(identifier);
        }

    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using sahajquinci.MQTT_Broker.Managers;
using Crestron.SimplSharp.CrestronSockets;
using sahajquinci.MQTT_Broker.Messages;
using sahajquinci.MQTT_Broker.Events;
using sahajquinci.MQTT_Broker.Utility;
using Crestron.SimplSharp.CrestronLogger;

namespace sahajquinci.MQTT_Broker
{
    public class TCPServer : ServerBase
    {

        public TCPServer(List<MqttClient> clients, SessionManager sessionManager, List<ushort> packetIdentifiers,Random rand, int port, int numberOfConnections) 
            : base ( clients,  sessionManager ,  packetIdentifiers, rand, port,  numberOfConnections)
        {
            ;
        }

        override protected void ConnectionCallback(SecureTCPServer server, uint clientIndex)
        {
            try
            {
                Server.WaitForConnectionAsync(IPAddress.Parse("0.0.0.0"), this.ConnectionCallback);
                if (Server.ClientConnected(clientIndex))
                {
                    int lenghtOfData = Server.ReceiveData(clientIndex);
                    byte[] data = Server.GetIncomingDataBufferForSpecificClient(clientIndex);
                    MqttMsgBase packet = PacketDecoder.DecodeControlPacket(data);
                    if (packet.Type == MqttMsgBase.MQTT_MSG_CONNECT_TYPE)
                        OnPacketReceived(clientIndex, packet,false);
                    else
                        throw new ArgumentException("Attempted connection with a non CONNECT packet");
                }
            }
            catch (Exception e)
            {
                DisconnectClient(clientIndex, false);
            }
        }

        override  public void Send(uint clientIndex, byte[] buffer)
        {
            Server.SendDataAsync(clientIndex, buffer, buffer.Length, SendCallback);
        }

        override public void Receive(uint clientIndex)
        {
            Server.ReceiveDataAsync(clientIndex, ReceiveCallback);
        }
        override protected void ReceiveCallback(SecureTCPServer myTCPServer, uint clientIndex, int numberOfBytesReceived)
        {
            try
            {
                if (numberOfBytesReceived != 0)
                {
                    byte[] data = new byte[numberOfBytesReceived];
                    Array.Copy(myTCPServer.GetIncomingDataBufferForSpecificClient(clientIndex), data, numberOfBytesReceived);
                    Server.ReceiveDataAsync(clientIndex, ReceiveCallback);
                    DecodeMultiplePacketsByteArray(clientIndex, data);
                }
            }
            catch (Exception e)
            {
                CrestronLogger.WriteToLog("MQTTSERVER - RECEIVE CALLBACK - " + " inner exception" + e.InnerException + " Error Message : " + e.Message, 8);
                CrestronLogger.WriteToLog("MQTTSERVER - RECEIVE CALLBACK - StackTrace : " + e.StackTrace, 8);
                CrestronLogger.WriteToLog("MQTTSERVER - RECEIVE CALLBACK - Exception occured , Disconnecting client", 8);
                DisconnectClient(clientIndex, false);
            }
        }

        override protected void DecodeMultiplePacketsByteArray(uint clientIndex, byte[] data)
        {
            List<MqttMsgBase> packetsInTheByteArray = new List<MqttMsgBase>();
            int numberOfBytesProcessed = 0;
            int numberOfBytesToProcess = 0;
            int numberOfBytesReceived = data.Length;
            byte[] packetByteArray;
            MqttMsgBase tmpPacket = new MqttMsgSubscribe();
            while (numberOfBytesProcessed != numberOfBytesReceived)
            {
                int remainingLength = MqttMsgBase.decodeRemainingLength(data);
                int remainingLenghtIndex = tmpPacket.encodeRemainingLength(remainingLength, data, 1);
                numberOfBytesToProcess = remainingLength + remainingLenghtIndex;
                packetByteArray = new byte[numberOfBytesToProcess];
                Array.Copy(data, 0, packetByteArray, 0, numberOfBytesToProcess);
                {
                    byte[] tmp = new byte[data.Length - numberOfBytesToProcess];
                    Array.Copy(data, numberOfBytesToProcess, tmp, 0, tmp.Length);
                    data = tmp;
                }
                numberOfBytesProcessed += numberOfBytesToProcess;
                OnPacketReceived(clientIndex, PacketDecoder.DecodeControlPacket(packetByteArray) , false);
            }
        }

        override public void DisconnectClient(uint clientIndex, bool withDisconnectPacket)
        {
            MqttClient client = GetClientByIndex(clientIndex,false);
            try
            {
                if (Server.ClientConnected(clientIndex))
                {
                    var res = Server.Disconnect(clientIndex);
                }
                OnClientDisconnected(client, withDisconnectPacket);
            }
            catch (Exception e)
            {
                CrestronLogger.WriteToLog("TCPSERVER - DISCONNECT_CLIENT - Client number : " + clientIndex + " errors occured during disconnection. ", 8);
                Server.Disconnect(clientIndex);
            }
            finally
            {
               Clients.Remove(client);
            }
        }

        public override void RejectConnection(uint clientIndex)
        {
            try
            {
                if (Server.ClientConnected(clientIndex))
                {
                    var res = Server.Disconnect(clientIndex);
                }
            }
            catch (Exception e)
            {
                CrestronLogger.WriteToLog("TCPSERVER - DISCONNECT_CLIENT - Client number : " + clientIndex + " errors rejecting client connection ", 8);
                Server.Disconnect(clientIndex);
            }
        }
    }
}
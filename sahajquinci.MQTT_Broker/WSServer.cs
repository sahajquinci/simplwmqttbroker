using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using Crestron.SimplSharp.CrestronLogger;
using System.Text.RegularExpressions;
using sahajquinci.MQTT_Broker.Messages;
using sahajquinci.MQTT_Broker.Utility;
using Crestron.SimplSharp.Cryptography;
using sahajquinci.MQTT_Broker.Managers;
using sahajquinci.MQTT_Broker.Events;

namespace sahajquinci.MQTT_Broker
{
    public class WSServer : ServerBase
    {

        public WSServer(List<MqttClient> clients, SessionManager sessionManager, List<ushort> packetIdentifiers, Random rand, int port, int numberOfConnections)
            : base(clients, sessionManager, packetIdentifiers ,rand , port, numberOfConnections)
        {
            ;
        }

        override protected void ConnectionCallback(SecureTCPServer server, uint clientIndex)
        {
            server.ReceiveDataAsync(clientIndex, ConnectionDataCallback);
            server.WaitForConnectionAsync("0.0.0.0", ConnectionCallback);
        }

        private void ConnectionDataCallback(SecureTCPServer server, uint clientIndex, int numberOfBytesReceived)
        {
            try
            {
                byte[] data = server.GetIncomingDataBufferForSpecificClient(clientIndex);
                string dataASCIIEncoded = Encoding.ASCII.GetString(data, 0, data.Length);
                //TODO: Cambiare la regex e renderla sicura
                if (new Regex("^GET").IsMatch(dataASCIIEncoded))
                {
                    CrestronLogger.WriteToLog("WS_SERVER - PERFORMING HANDSHAKE", 1);
                    PerformHandShake(clientIndex, dataASCIIEncoded);
                    if (server.ClientConnected(clientIndex))
                    {
                        oldDecodedFrame.Add(clientIndex, new List<byte>());
                        server.ReceiveDataAsync(clientIndex, ReceiveCallback);
                    }
                }
                else
                {
                    RejectConnection(clientIndex);
                }
            }
            catch (Exception)
            {
                CrestronLogger.WriteToLog("WS_SERVER - ConnectionCallback - connection error ", 8);
                RejectConnection(clientIndex);
            }
        }

        override public void Receive(uint clientIndex)
        {
            ;
        }

        override protected void ReceiveCallback(SecureTCPServer server, uint clientIndex, int numberOfBytesReceived)
        {
            try
            {
                if (numberOfBytesReceived > 0 && server.GetIncomingDataBufferForSpecificClient(clientIndex) != null)
                {
                    byte[] data = new byte[numberOfBytesReceived];
                    Array.Copy(server.GetIncomingDataBufferForSpecificClient(clientIndex), data, numberOfBytesReceived);
                    server.ReceiveDataAsync(clientIndex, ReceiveCallback);
                    //PrintFrameStatus(data);
                    if (((data[0] & 0x0F) == 0x09))
                    {
                        CrestronLogger.WriteToLog("WS_SERVER Receive callback PING DETECTED", 9);
                        SendPongToWebSocketClient(data, clientIndex);
                    }
                    else if (((data[0] & 0x0F) == 0x08))
                    {
                        CrestronLogger.WriteToLog("WS_SERVER - Receive callback - CONNECTION CLOSE PACKET RECEIVED, CLOSING...", 9);
                        Send(clientIndex, data);
                        DisconnectClient(clientIndex, true);
                    }
                    else
                    {
                        ParseFrame(clientIndex, data);
                    }

                }

            }
            catch (Exception e)
            {
                CrestronLogger.WriteToLog("WS_SERVER - Exception occured : " + e.Message + "   " + e.InnerException + "   " + e.StackTrace, 9);
                DisconnectClient(clientIndex, false);
            }
        }

        private void ParseFrame(uint clientIndex, byte[] data)
        {
            try
            {
                byte[] decodedData = DecodeWebsocketFrame(data);
                byte[] allData = decodedData;
                if (oldDecodedFrame[clientIndex].Count > 0)
                {
                    allData = new byte[decodedData.Length + oldDecodedFrame[clientIndex].Count];
                    oldDecodedFrame[clientIndex].CopyTo(allData, 0);
                    Array.Copy(decodedData, 0, allData, oldDecodedFrame[clientIndex].Count, decodedData.Length);
                    oldDecodedFrame[clientIndex].Clear();
                }
                DecodeMultiplePacketsByteArray(clientIndex, allData,true);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private ulong GetPayloadLenght(byte[] buffer)
        {
            byte b = buffer[1];
            ulong dataLength = 0;
            if (b - 128 <= 125)
            {
                dataLength = (ulong)(b & 0x7F);
            }
            else
                if (b - 128 == 126)
                {
                    var tmp = new byte[] { buffer[3], buffer[2] };
                    dataLength = Convert.ToUInt16(tmp);

                }
                else
                    if (b - 128 == 127)
                    {
                        var tmp = new byte[] { buffer[9], buffer[8], buffer[7], buffer[6], buffer[5], buffer[4], buffer[3], buffer[2] };
                        dataLength = Convert.ToUInt64(tmp);
                    }
            return dataLength;
        }

        private void PrintFrameStatus(byte[] data)
        {
            byte firstByte = data[0];
            CrestronLogger.WriteToLog("WS_SERVER FIRST BYTE " + firstByte, 9);
            CrestronLogger.WriteToLog("WS_SERVER FIN BIT : " + (firstByte & 0x80), 9);
            CrestronLogger.WriteToLog("WS_SERVER OPCODE : " + (firstByte & 0x0F), 9);
            CrestronLogger.WriteToLog("PAYLOAD LENGTH : " + GetPayloadLenght(data), 9);

        }

        private void SendPongToWebSocketClient(byte[] ping, uint clientIndex)
        {
            CrestronLogger.WriteToLog("WS_SERVER SENDING A PONG", 9);
            byte[] pong = EncodeMessageToSend(ping);
            pong[0] = 145;
            Send(clientIndex, pong);
        }

        override public void Send(uint clientIndex, byte[] data)
        {
            try
            {
                byte[] dataEncoded = EncodeMessageToSend(data);
                if (Server.GetServerSocketStatusForSpecificClient(clientIndex) == SocketStatus.SOCKET_STATUS_CONNECTED)
                    Server.SendDataAsync(clientIndex, dataEncoded, dataEncoded.Length, SendCallback);
                else
                {
                    throw new Exception("The client is not connected");
                }
            }
            catch (Exception e)
            {
                CrestronLogger.WriteToLog("WS_SERVER - Exception occured : " + e.Message + "   " + e.InnerException + "   " + e.StackTrace, 9);
                DisconnectClient(clientIndex, false);
            }

        }

        private void PerformHandShake(uint clientIndex, string dataASCIIEncoded)
        {
            const string eol = "\r\n"; // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
            string secWebProtocol = dataASCIIEncoded.Contains("Sec-WebSocket-Protocol") ?
                ("Sec-WebSocket-Protocol: " + new Regex("Sec-WebSocket-Protocol: (.*)").Match(dataASCIIEncoded).Groups[1].Value.Trim() + eol + eol) : null;
            string s;
            if (secWebProtocol == null)
                s = eol;
            else
                s = secWebProtocol;
            //Console.WriteLine(s);
            Byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + eol
                + "Connection: Upgrade" + eol
                + "Upgrade: websocket" + eol
                + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                    SHA1.Create().ComputeHash(
                        Encoding.UTF8.GetBytes(
                            new Regex("Sec-WebSocket-Key: (.*)").Match(dataASCIIEncoded).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                        )
                    )
                ) + eol
                + s
                );
            Server.SendData(clientIndex, response, response.Length);
        }

        public static byte[] DecodeWebsocketFrame(Byte[] bytes)
        {
            List<Byte[]> ret = new List<Byte[]>();
            int offset = 0;
            while (offset + 6 < bytes.Length)
            {
                // format: 0==ascii/binary 1=length-0x80, byte 2,3,4,5=key, 6+len=message, repeat with offset for next...
                int len = bytes[offset + 1] - 0x80;

                if (len <= 125)
                {

                    Byte[] key = new Byte[] { bytes[offset + 2], bytes[offset + 3], bytes[offset + 4], bytes[offset + 5] };
                    Byte[] decoded = new Byte[len];
                    for (int i = 0; i < len; i++)
                    {
                        int realPos = offset + 6 + i;
                        decoded[i] = (Byte)(bytes[realPos] ^ key[i % 4]);
                    }
                    offset += 6 + len;
                    ret.Add(decoded);
                }
                else
                {
                    int a = bytes[offset + 2];
                    int b = bytes[offset + 3];
                    len = (a << 8) + b;
                    //Debug.Log("Length of ws: " + len);

                    Byte[] key = new Byte[] { bytes[offset + 4], bytes[offset + 5], bytes[offset + 6], bytes[offset + 7] };
                    Byte[] decoded = new Byte[len];
                    for (int i = 0; i < len; i++)
                    {
                        int realPos = offset + 8 + i;
                        decoded[i] = (Byte)(bytes[realPos] ^ key[i % 4]);
                    }

                    offset += 8 + len;
                    ret.Add(decoded);
                }
            }
            return ret.SelectMany(b => b).ToArray();
        }

        private Byte[] EncodeMessageToSend(byte[] bytesRaw)
        {
            Byte[] response;
            Byte[] frame = new Byte[10];

            Int32 indexStartRawData = -1;
            Int32 length = bytesRaw.Length;

            frame[0] = 130;
            if (length <= 125)
            {
                frame[1] = (Byte)length;
                indexStartRawData = 2;
            }
            else if (length >= 126 && length <= 65535)
            {
                frame[1] = (Byte)126;
                frame[2] = (Byte)((length >> 8) & 255);
                frame[3] = (Byte)(length & 255);
                indexStartRawData = 4;
            }
            else
            {
                frame[1] = (Byte)127;
                frame[2] = (Byte)((length >> 56) & 255);
                frame[3] = (Byte)((length >> 48) & 255);
                frame[4] = (Byte)((length >> 40) & 255);
                frame[5] = (Byte)((length >> 32) & 255);
                frame[6] = (Byte)((length >> 24) & 255);
                frame[7] = (Byte)((length >> 16) & 255);
                frame[8] = (Byte)((length >> 8) & 255);
                frame[9] = (Byte)(length & 255);

                indexStartRawData = 10;
            }

            response = new Byte[indexStartRawData + length];

            Int32 i, reponseIdx = 0;

            //Add the frame bytes to the reponse
            for (i = 0; i < indexStartRawData; i++)
            {
                response[reponseIdx] = frame[i];
                reponseIdx++;
            }

            //Add the data bytes to the response
            for (i = 0; i < length; i++)
            {
                response[reponseIdx] = bytesRaw[i];
                reponseIdx++;
            }

            return response;
        }

        private void SendCloseFrame(uint clientIndex)
        {
            byte[] disconnectPacket = new byte[1];
            disconnectPacket[0] = 0x08;
            Send(clientIndex, disconnectPacket);
        }

        override public void DisconnectClient(uint clientIndex, bool withDisconnectPacket)
        {
            MqttClient client = GetClientByIndex(clientIndex, true);
            try
            {
                oldDecodedFrame.Remove(clientIndex);
                if (Server.ClientConnected(clientIndex))
                {
                    SendCloseFrame(clientIndex);
                    Server.Disconnect(clientIndex);
                }
                OnClientDisconnected(client, withDisconnectPacket);
            }
            catch (Exception e)
            {
                CrestronLogger.WriteToLog("MQTTSERVER - DISCONNECT_CLIENT - Client number : " + clientIndex + " errors occured during disconnection. " + e.Message + "\n" + e.StackTrace, 8);
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
                oldDecodedFrame.Remove(clientIndex);
                if (Server.ClientConnected(clientIndex))
                {
                    SendCloseFrame(clientIndex);
                    Server.Disconnect(clientIndex);
                }
            }
            catch (Exception e)
            {
                CrestronLogger.WriteToLog("WSSERVER - DISCONNECT_CLIENT - Client number : " + clientIndex + " errors rejecting client connection. " + e.Message + "\n" + e.StackTrace, 8);
                Server.Disconnect(clientIndex);
            }
            //finally
            //{
            //    if (Server.NumberOfClientsConnected == 0 && clientIndex >= this.NumberOfConnections)
            //    {                    
            //        Server.DisconnectAll();
            //        Server.HandleLinkLoss();
            //    }
            //}
        }

    }
}



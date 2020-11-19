using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronLogger;
using sahajquinci.MQTT_Broker;
using sahajquinci.MQTT_Broker.Managers;

namespace sahajquinci.MqttBroker
{
    public class MqttBrokerStart
    {
        private Random rand;
        private SessionManager sessionManager;
        private PublishManager publishManager;
        private SubscriptionManager subscriptionManager;
        private static List<MqttClient> clients;
        private List<ushort> packetIdentifiers;
        
        public MqttBrokerStart()
        {
            CrestronLogger.Mode = LoggerModeEnum.DEFAULT;
            CrestronLogger.PrintTheLog(true);
            CrestronLogger.Initialize(10);
            CrestronLogger.LogOnlyCurrentDebugLevel = false;
        }

       
        public void InitializeBis(string username, string password, uint controlSytemAuthentication, uint port, uint ssl, string certificateFileName,
            string privateKeyFileName, uint enableWebSocketServer, uint webSocketServerPort)
        {
            if (username != "//" || password != "//")
            {
                MqttSettings.Instance.Username = username;
                MqttSettings.Instance.Password = password;
            }
            MqttSettings.Instance.ControlSytemAuthentication = controlSytemAuthentication == 1 ? true : false;
            MqttSettings.Instance.Port = Convert.ToInt32(port);
            MqttSettings.Instance.SSLCertificateProvided = ssl == 0 ? false : true;
            MqttSettings.Instance.CertificateFileName = certificateFileName;
            MqttSettings.Instance.PrivateKeyFileName = privateKeyFileName;
            CrestronLogger.WriteToLog("INITIALIZE DEL BROKER BIS: " + port + " " + ssl + " " + certificateFileName + " " + privateKeyFileName +
                "\n Web Socket Server " + enableWebSocketServer + " Web Socket Port " + webSocketServerPort, 1);

            subscriptionManager = new SubscriptionManager();
            sessionManager = new SessionManager(subscriptionManager);
            publishManager = new PublishManager(subscriptionManager);
            clients = new List<MqttClient>();
            rand = new Random();          
            packetIdentifiers = new List<ushort>();

            TCPServer tcpServer = new TCPServer(clients, sessionManager, packetIdentifiers, rand, Convert.ToInt32(port), 60);
            WSServer webSocketServer = new WSServer(clients, sessionManager, packetIdentifiers, rand, Convert.ToInt32(webSocketServerPort), 60);
            PacketManager pm = new PacketManager(rand, sessionManager, publishManager, subscriptionManager, clients, packetIdentifiers, tcpServer, webSocketServer);

        }

        public void SetLogLevel(uint logLevel)
        {
            if (logLevel == 0)
            {
                CrestronLogger.DebugLevel = 10;
                CrestronLogger.LogOnlyCurrentDebugLevel = false;
            }
            else
            {
                logLevel = (logLevel > 10) ? 10 : logLevel;
                if (logLevel < 0)
                {
                    SetLogLevel(0);
                }
                else
                {
                    CrestronLogger.LogOnlyCurrentDebugLevel = true;
                    CrestronLogger.DebugLevel = logLevel;
                }
            }
        }

    }
}
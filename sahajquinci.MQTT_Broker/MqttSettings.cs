
namespace sahajquinci.MQTT_Broker
{
    /// <summary>
    /// Settings class for the MQTT broker
    /// </summary>
    public class MqttSettings
    {
        // default port for MQTT protocol
        public const int MQTT_BROKER_DEFAULT_PORT = 1883;
        public const int MQTT_BROKER_DEFAULT_SSL_PORT = 8883;
        // default timeout on receiving from client
        public const int MQTT_DEFAULT_TIMEOUT = 30000;
        // max publish, subscribe and unsubscribe retry for QoS Level 1 or 2
        public const int MQTT_ATTEMPTS_RETRY = 3;
        // delay for retry publish, subscribe and unsubscribe for QoS Level 1 or 2
        public const int MQTT_DELAY_RETRY = 10000;
        // broker need to receive the first message (CONNECT)
        // within a reasonable amount of time after TCP/IP connection 
        public const int MQTT_CONNECT_TIMEOUT = 30000;
        // default inflight queue size
        public const int MQTT_MAX_INFLIGHT_QUEUE_SIZE = int.MaxValue;
        //Number of simultaneous connections at any given time
        public const int NUMBER_OF_CONNECTIONS = 20;
        //Number of simultaneous connections at any given time
        public const byte PROTOCOL_VERSION_3_1_1 = 0x04;

        public const int WEB_CLIENT_BUFFER_SIZE = 4096;
        /// <summary>
        /// Listening connection port
        /// </summary>
        public int Port { get; internal set; }

        /// <summary>
        /// Port of the WebSocketServer
        /// </summary>
        public int WsPort { get; internal set; }
        /// <summary>
        /// Listening connection SSL port
        /// </summary>
        public int SslPort { get; internal set; }

        /// <summary>
        /// Timeout on client connection (before receiving CONNECT message)
        /// </summary>
        public int TimeoutOnConnection { get; internal set; }

        /// <summary>
        /// Timeout on receiving
        /// </summary>
        public int TimeoutOnReceiving { get; internal set; }

        /// <summary>
        /// Attempts on retry
        /// </summary>
        public int AttemptsOnRetry { get; internal set; }

        /// <summary>
        /// Delay on retry
        /// </summary>
        public int DelayOnRetry { get; internal set; }

        /// <summary>
        /// Inflight queue size
        /// </summary>
        public int InflightQueueSize { get; set; }

        /// <summary>
        /// Number of clients connected to the server
        /// </summary>
        public int NumberOfConnections { get; internal set; }

        //Username for authenticating with the broker
        public string Username { get; internal set; }

        /// <summary>
        /// Password for authenticating with the broker
        /// </summary>
        public string Password { get; internal set; }

        public bool ControlSytemAuthentication { get; internal set; }

        public string CertificateFileName { get; internal set; }

        public string PrivateKeyFileName { get; internal set; }

        public bool SSLCertificateProvided { get; internal set; }

        /// <summary>
        /// Singleton instance of settings
        /// </summary>
        public static MqttSettings Instance
        {
            get
            {
                if (instance == null)
                    instance = new MqttSettings();
                return instance;
            }
        }

        // singleton instance
        private static MqttSettings instance;

        /// <summary>
        /// Constructor
        /// </summary>
        private MqttSettings()
        {
            this.Port = MQTT_BROKER_DEFAULT_PORT;
            this.SslPort = MQTT_BROKER_DEFAULT_SSL_PORT;
            this.TimeoutOnReceiving = MQTT_DEFAULT_TIMEOUT;
            this.AttemptsOnRetry = MQTT_ATTEMPTS_RETRY;
            this.DelayOnRetry = MQTT_DELAY_RETRY;
            this.TimeoutOnConnection = MQTT_CONNECT_TIMEOUT;
            this.InflightQueueSize = MQTT_MAX_INFLIGHT_QUEUE_SIZE;
            this.NumberOfConnections = NUMBER_OF_CONNECTIONS;
        }
    }
}

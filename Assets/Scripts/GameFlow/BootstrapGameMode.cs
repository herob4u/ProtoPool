using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;

namespace GameFlow
{
    public enum ESessionPrivacy
    {
        Public,
        FriendsOnly,
        Private
    };

    public struct SessionJoinParams
    {
        // IP info
        public string IPAddress;
        public int ConnectPort;

        public string GameMode;

        public SessionJoinParams(string gamemode, string ip, int port = 7777)
        {
            GameMode = gamemode;
            IPAddress = ip;
            ConnectPort = port;
        }
    };

    public struct SessionCreateParams
    {
        public string GameMode;
        public int ListeningPort;
        public int MaxPlayers;
        public ESessionPrivacy SessionPrivacy;

        public SessionCreateParams(string gamemode, int listenPort, int maxPlayers = 2, ESessionPrivacy privacy = ESessionPrivacy.Public)
        {
            GameMode = gamemode;
            ListeningPort = listenPort;
            MaxPlayers = maxPlayers;
            SessionPrivacy = privacy;
        }
    };

    public class ConnectionTicket
    {
        public int GameModeId;

        public ConnectionTicket()
        {
            GameModeId = -1;
        }

        public ConnectionTicket(string gameMode)
        {
            GameModeId = gameMode.GetHashCode();
        }

        public byte[] ToByteArray()
        {
            int byteSize = sizeof(int); // GameModeId

            FastBufferWriter writer = new FastBufferWriter(byteSize, Unity.Collections.Allocator.Temp);

            if(!writer.TryBeginWrite(byteSize))
            {
                Debug.LogError("Not enough space in byte buffer");
                return null;
            }

            writer.WriteValueSafe(GameModeId);
            byte[] outData = writer.ToArray();

            writer.Dispose();

            return outData;
        }

        public static ConnectionTicket FromByteArray(byte[] inData)
        {
            ConnectionTicket ticket = new ConnectionTicket();

            FastBufferReader reader = new FastBufferReader(inData, Unity.Collections.Allocator.None);
            if(!reader.TryBeginRead(inData.Length))
            {
                Debug.LogError("Byte buffer read overrun.");
                return ticket;
            }

            reader.ReadValueSafe(out ticket.GameModeId);
            reader.Dispose();

            return ticket;
        }
    };

    [RequireComponent(typeof(GameSession))]
    public class BootstrapGameMode : MonoBehaviour
    {
        public List<GameModeConfig> GameModeConfigs;
        
        private string CurrentGameMode;
        private AsyncOperation LoadAsyncOp;

        private UnityEngine.Events.UnityAction<Scene, LoadSceneMode> OnSceneLoadedCallback;

        private float JoinTimeoutTimer = 0.0f;
        private static float JOIN_TIMEOUT_DURATION = 5.0f;

        public static BootstrapGameMode Instance { get; private set; }

        private void Awake()
        {
            if(Instance)
            {
                Debug.LogError("BootstrapGameMode already initialized.");
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        // Start is called before the first frame update
        void Start()
        {
            DontDestroyOnLoad(this.gameObject);
        }

        // Update is called once per frame
        void Update()
        {
            if(JoinTimeoutTimer > 0.0f)
            {
                if(NetworkManager.Singleton.IsConnectedClient)
                {
                    // Join successful
                    OnJoinSuccessful();
                    JoinTimeoutTimer = 0.0f;
                    return;
                }

                JoinTimeoutTimer -= Time.deltaTime;
                if(JoinTimeoutTimer <= 0.0f)
                {
                    OnJoinFailed();
                }
            }
        }

        public void SwitchGameMode(string gamemode)
        {
            if(IsBootstrapping())
            {
                Debug.LogWarning("Cannot switch gamemode. Already bootstrapping a gamemode");
                return;
            }

            if (gamemode == CurrentGameMode)
            {
                return;
            }

            GameModeConfig config = GameModeConfigs.Find(config =>
           {
               return config.GameModeName == gamemode;
           });

            if(!config)
            {
                Debug.LogWarningFormat("Cannot switch gamemode. No GameModeConfig for gamemode {0}", gamemode);
                return;
            }

            if (config.SceneName == null || config.SceneName.Length == 0)
            {
                Debug.LogWarningFormat("Cannot join session. GameMode {0} has no valid scene", gamemode);
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoadedCallback;
            LoadAsyncOp = SceneManager.LoadSceneAsync(config.SceneName, LoadSceneMode.Single);
        }

        public bool IsBootstrapping()
        {
            return LoadAsyncOp != null;
        }

        public void CreateGameSession(SessionCreateParams createParams)
        {
            UNetTransport transport = NetworkManager.Singleton.GetComponent<UNetTransport>();
            transport.ConnectAddress = "127.0.0.1";
            transport.ConnectPort = createParams.ListeningPort;

            #region
            // Scene load callback as a result of gamemode switch
            OnSceneLoadedCallback = (scene, loadSceneMode) =>
            {
                // Reset callbacks
                SceneManager.sceneLoaded -= OnSceneLoadedCallback;
                OnSceneLoadedCallback = null;
                LoadAsyncOp = null;

                // Start client
                bool success = NetworkManager.Singleton.StartHost();
                if (!success)
                {
                    Debug.LogErrorFormat("Failed to create session. [GameMode={0}]", createParams.GameMode);
                    NetworkManager.Singleton.Shutdown();
                }

                Debug.LogFormat("IsHost: {0}, IsServer: {1}", NetworkManager.Singleton.IsHost, NetworkManager.Singleton.IsServer);

                InitGameSession(createParams);
                InitGameMode(createParams.GameMode);
            };

            SwitchGameMode(createParams.GameMode);
            #endregion
        }

        public void JoinGameSession(SessionJoinParams joinParams)
        {
            GameModeConfig config = GameModeConfigs.Find(config =>
            {
                return config.GameModeName == joinParams.GameMode;
            });

            if (!config)
            {
                Debug.LogWarningFormat("Cannot join session. GameMode {0} does not exist.", joinParams.GameMode);
                return;
            }
            
            if(config.IsOfflineMode)
            {
                Debug.LogWarningFormat("Cannot join session. GameMode {0} is an offline only gamemode.", joinParams.GameMode);
                return;
            }

            if(config.SceneName == null || config.SceneName.Length == 0)
            {
                Debug.LogWarningFormat("Cannot join session. GameMode {0} has no valid scene", joinParams.GameMode);
                return;
            }

            UNetTransport transport = NetworkManager.Singleton.GetComponent<UNetTransport>();
            transport.ConnectAddress = joinParams.IPAddress;
            transport.ConnectPort = joinParams.ConnectPort;

            // Create the ticket by which the server/host verifies our join
            ConnectionTicket ticket = new ConnectionTicket(joinParams.GameMode);
            NetworkManager.Singleton.NetworkConfig.ConnectionData = ticket.ToByteArray();

            if (!NetworkManager.Singleton.StartClient())
            {
                Debug.LogErrorFormat("Failed to join session. [IP={0}][Port={1}]", joinParams.IPAddress, joinParams.ConnectPort);
                NetworkManager.Singleton.Shutdown();
            }

            #region
            /*
            // Switch scene
            OnSceneLoadedCallback = (scene, loadSceneMode) =>
            {
                // Reset callbacks
                SceneManager.sceneLoaded -= OnSceneLoadedCallback;
                OnSceneLoadedCallback = null;
                LoadAsyncOp = null;

                // Start client
                bool success = NetworkManager.Singleton.StartClient();
                if (!success)
                {
                    Debug.LogErrorFormat("Failed to join session. [IP={0}][Port={1}]", joinParams.IPAddress, joinParams.ConnectPort);
                }

                JoinTimeoutTimer = JOIN_TIMEOUT_DURATION;

                InitGameMode(joinParams.GameMode);
            };

            SceneManager.sceneLoaded += OnSceneLoadedCallback;
            LoadAsyncOp = SceneManager.LoadSceneAsync(config.SceneName, LoadSceneMode.Single);
            */
            #endregion
        }

        public void LeaveSession()
        {
            if(NetworkManager.Singleton.IsServer)
            {
                foreach(ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    NetworkManager.Singleton.DisconnectClient(clientId);
                }
            }
            
            NetworkManager.Singleton.Shutdown();
        }

        void InitGameSession(SessionCreateParams createParams)
        {
            GameSession.Properties sessionProperties = new GameSession.Properties();
            sessionProperties.PlayerCapacity = createParams.MaxPlayers;
            sessionProperties.SpectatorCapacity = 0;
            sessionProperties.JoinRestriction = createParams.SessionPrivacy == ESessionPrivacy.Public ? ESessionRestriction.Public : ESessionRestriction.Private;

            GameSession session = GetComponent<GameSession>();
            if(!session)
            {
                session = gameObject.AddComponent<GameSession>();
            }

            session.InitSession(sessionProperties);
        }

        void InitGameMode(string gamemode)
        {
            CurrentGameMode = gamemode;

            // Spawn from gamemode configs...
            GameModeConfig config = GameModeConfigs.Find(config => { return config.GameModeName == gamemode; });
            if(config != null)
            {
                if(NetworkManager.Singleton.IsServer)
                {
                    GameObject hierarchyRoot = new GameObject("ServerOnlyObjects");
                    foreach(GameObject serverObj in config.ServerOnlyObjects)
                    {
                        GameObject obj = Instantiate(serverObj, hierarchyRoot.transform);
                    }
                }
                else if(NetworkManager.Singleton.IsClient)
                {
                    GameObject hierarchyRoot = new GameObject("ClientOnlyObjects");
                    foreach (GameObject clientObj in config.ClientOnlyObjects)
                    {
                        GameObject obj = Instantiate(clientObj, hierarchyRoot.transform);
                    }
                }
            }
        }

        void OnJoinSuccessful()
        {
            Debug.Log("Join successful");
        }

        void OnJoinFailed()
        {
            Debug.LogWarning("Join failed");
            NetworkManager.Singleton.Shutdown();
        }
    }
}

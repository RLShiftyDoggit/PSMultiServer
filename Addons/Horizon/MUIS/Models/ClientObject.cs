﻿using DotNetty.Transport.Channels;
using MultiServer.Addons.Horizon.RT.Common;
using MultiServer.Addons.Horizon.RT.Models;
using MultiServer.Addons.Horizon.LIBRARY.Common;
using MultiServer.Addons.Horizon.LIBRARY.Database.Models;
using MultiServer.Addons.Horizon.MUIS.PluginArgs;
using MultiServer.Addons.Horizon.LIBRARY.Pipeline.Udp;
using System.Collections.Concurrent;
using System.Net;
using static MultiServer.Addons.Horizon.MUIS.Models.Game;
using MultiServer.PluginManager;

namespace MultiServer.Addons.Horizon.MUIS.Models
{
    public class ClientObject
    {
        protected static Random RNG = new Random();
        public IPAddress IP { get; protected set; } = IPAddress.Any;

        public List<GameClient> Clients = new List<GameClient>();

        /// <summary>
        /// 
        /// </summary>
        public int UdpPort = 0;

        /// <summary>
        /// 
        /// </summary>
        public IChannel Tcp { get; protected set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public IPEndPoint RemoteUdpEndpoint { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public int DmeId { get; protected set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        public uint ScertId { get; set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        public RT_RECV_FLAG RecvFlag { get; set; } = RT_RECV_FLAG.RECV_BROADCAST;

        /// <summary>
        /// 
        /// </summary>
        public ConcurrentQueue<BaseScertMessage> TcpSendMessageQueue { get; } = new ConcurrentQueue<BaseScertMessage>();

        /// <summary>
        /// 
        /// </summary>
        public ConcurrentQueue<ScertDatagramPacket> UdpSendMessageQueue { get; } = new ConcurrentQueue<ScertDatagramPacket>();

        /// <summary>
        /// 
        /// </summary>
        public MediusPlayerStatus PlayerStatus => GetStatus();

        /// <summary>
        /// 
        /// </summary>
        public MGCL_GAME_HOST_TYPE ServerType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public MediusConnectionType MediusConnectionType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public NetConnectionType NetConnectionType { get; set; }

        /// <summary>
        /// Client's language type
        /// </summary>
        public MediusLanguageType LanguageType { get; set; }

        /// <summary>
        /// Client's Timezone if set
        /// </summary>
        public MediusTimeZone TimeZone { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int AccountId { get; protected set; } = -1;

        /// <summary>
        /// 
        /// </summary>
        public string AccountName { get; protected set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public byte[] AccountStats { get; protected set; } = null;

        /// <summary>
        /// Anonymous Login Name for the duration of that session
        /// </summary>
        public string AccountDisplayName { get; set; } = null;

        /// <summary>
        /// Current access token required to access the account.
        /// </summary>
        public string Token { get; protected set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public string SessionKey { get; protected set; } = null;

        /// <summary>
        /// MGCL Session Key
        /// </summary>
        public string MGCLSessionKey { get; protected set; } = null;

        /// <summary>
        /// Unique MGCL hardcoded game identifer per Medius title
        /// </summary>
        public int ApplicationId { get; set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        public int LocationId { get; set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        public int MediusVersion { get; set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        public int? ClanId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int WorldId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int SignalId { get; set; }

        #region Lobby World Filters

        public uint FilterMask1;
        public uint FilterMask2;
        public uint FilterMask3;
        public uint FilterMask4;
        public MediusLobbyFilterType LobbyFilterType;
        public MediusLobbyFilterMaskLevelType FilterMaskLevel;

        #endregion

        /// <summary>
        /// 
        /// </summary>
        public Channel CurrentChannel { get; protected set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public Game CurrentGame { get; protected set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public int? DmeClientId { get; protected set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public ConcurrentQueue<BaseScertMessage> SendMessageQueue { get; } = new ConcurrentQueue<BaseScertMessage>();

        /// <summary>
        /// 
        /// </summary>
        public DateTime UtcLastServerEchoSent { get; protected set; } = Utils.GetHighPrecisionUtcTime();

        /// <summary>
        /// 
        /// </summary>
        public DateTime UtcLastServerEchoReply { get; protected set; } = Utils.GetHighPrecisionUtcTime();

        /// <summary>
        /// 
        /// </summary>
        public DateTime TimeCreated { get; protected set; } = Utils.GetHighPrecisionUtcTime();

        /// <summary>
        /// 
        /// </summary>
        public DateTime? TimeAuthenticated { get; protected set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public bool Disconnected { get; protected set; } = false;

        /// <summary>
        /// 
        /// </summary>
        public string Metadata { get; set; } = null;

        /// <summary>
        /// RTT (ms)
        /// </summary>
        public uint LatencyMs { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public List<string> FriendsListPS3 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<int, string> FriendsList { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int[] Stats { get; set; } = new int[15];

        /// <summary>
        /// 
        /// </summary>
        public int[] WideStats { get; set; } = new int[100];

        /// <summary>
        /// 
        /// </summary>
        public int[] CustomWideStats { get; set; } = new int[0];

        /// <summary>
        /// 
        /// </summary>
        public UploadState Upload { get; set; }

        /// <summary>
        /// File being Uploaded
        /// </summary>
        public MediusFile mediusFileToUpload;

        /// <summary>
        /// 
        /// </summary>
        public int AggTimeMs { get; set; } = 20;

        public virtual bool IsLoggedIn => !_logoutTime.HasValue && _loginTime.HasValue && IsConnected;
        public bool IsInGame => CurrentGame != null && CurrentChannel != null && CurrentChannel.Type == ChannelType.Game;
        //public bool 
        public virtual bool IsConnectingGracePeriod => !TimeAuthenticated.HasValue && (Utils.GetHighPrecisionUtcTime() - TimeCreated).TotalSeconds < MuisClass.GetAppSettingsOrDefault(ApplicationId).ClientTimeoutSeconds;
        public virtual bool Timedout => UtcLastServerEchoReply < UtcLastServerEchoSent && (Utils.GetHighPrecisionUtcTime() - UtcLastServerEchoReply).TotalSeconds > MuisClass.GetAppSettingsOrDefault(ApplicationId).ClientTimeoutSeconds;
        public virtual bool IsConnected => KeepAlive || (_hasSocket && _hasActiveSession && !Timedout);  //(KeepAlive || _hasActiveSession) && !Timedout;
        public virtual bool IsAuthenticated => TimeAuthenticated.HasValue;
        public virtual bool Destroy => Disconnected || (!IsConnected && !IsConnectingGracePeriod);
        public virtual bool IsDestroyed { get; protected set; } = false;
        public virtual bool IsAggTime => !LastAggTime.HasValue || (Utils.GetMillisecondsSinceStartup() - LastAggTime.Value) >= AggTimeMs;
        public bool KeepAlive => _keepAliveTime.HasValue && (Utils.GetHighPrecisionUtcTime() - _keepAliveTime).Value.TotalSeconds < MuisClass.GetAppSettingsOrDefault(ApplicationId).KeepAliveGracePeriodSeconds;

        public Action<ClientObject> OnDestroyed;

        /// <summary>
        /// 
        /// </summary>
        protected DateTime? _loginTime = null;

        /// <summary>
        /// 
        /// </summary>
        protected DateTime? _logoutTime = null;

        /// <summary>
        /// 
        /// </summary>
        protected bool _hasActiveSession = true;

        /// <summary>
        /// 
        /// </summary>
        private uint _gameListFilterIdCounter = 0;

        /// <summary>
        /// 
        /// </summary>
        private bool _hasSocket = false;

        /// <summary>
        /// 
        /// </summary>
        long? LastAggTime { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        protected DateTime? _keepAliveTime = null;

        /// <summary>
        /// 
        /// </summary>
        private DateTime _lastServerEchoValue = DateTime.UnixEpoch;



        public ClientObject()
        {
            // Generate new session key
            SessionKey = MuisClass.GenerateSessionKey();

            // Generate new token
            byte[] tokenBuf = new byte[12];
            RNG.NextBytes(tokenBuf);
            Token = Convert.ToBase64String(tokenBuf);
        }

        public void QueueServerEcho()
        {
            SendMessageQueue.Enqueue(new RT_MSG_SERVER_ECHO());
            UtcLastServerEchoSent = Utils.GetHighPrecisionUtcTime();
        }

        public void OnRecvServerEcho(RT_MSG_SERVER_ECHO echo)
        {
            var echoTime = echo.UnixTimestamp.ToUtcDateTime();
            if (echoTime > _lastServerEchoValue)
            {
                _lastServerEchoValue = echoTime;
                UtcLastServerEchoReply = Utils.GetHighPrecisionUtcTime();
                LatencyMs = (uint)(UtcLastServerEchoReply - echoTime).TotalMilliseconds;
            }
        }

        public void OnRecvClientEcho(RT_MSG_CLIENT_ECHO echo)
        {
            // older medius doesn't use server echo
            // so instead we'll increment our timeout dates by the client echo
            var echoTime = DateTime.UtcNow;
            if (echoTime > _lastServerEchoValue)
            {
                if (MediusVersion <= 108)
                {
                    _lastServerEchoValue = echoTime;
                    UtcLastServerEchoSent = Utils.GetHighPrecisionUtcTime();
                    UtcLastServerEchoReply = Utils.GetHighPrecisionUtcTime();
                    LatencyMs = (uint)(UtcLastServerEchoReply - echoTime).TotalMilliseconds;
                }
            }

        }

        public virtual void OnPlayerReport(MediusPlayerReport report)
        {
            // Ensure report is for correct game world
            if (report.MediusWorldID != WorldId)
                return;

            AccountStats = report.Stats;
        }


        #region Send Queue

        public void EnqueueTcp(BaseScertMessage message)
        {
            TcpSendMessageQueue.Enqueue(message);
        }

        public void EnqueueTcp(IEnumerable<BaseScertMessage> messages)
        {
            foreach (var message in messages)
                EnqueueTcp(message);
        }

        #endregion

        #region Connection / Disconnection

        public void KeepAliveUntilNextConnection()
        {
            _keepAliveTime = Utils.GetHighPrecisionUtcTime();
        }

        public void OnConnected()
        {
            _keepAliveTime = null;
            _hasSocket = true;
        }

        public void OnDisconnected()
        {
            _hasSocket = false;
        }

        #endregion

        #region Status

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private MediusPlayerStatus GetStatus()
        {
            if (!IsConnected || !IsLoggedIn)
                return MediusPlayerStatus.MediusPlayerDisconnected;

            if (IsInGame)
                return MediusPlayerStatus.MediusPlayerInGameWorld;


            return MediusPlayerStatus.MediusPlayerInChatWorld;

            /* // Needs proper handling between Universes for MUIS
            if (IsInOtherUniverse)
                return MediusPlayerStatus.MediusPlayerInOtherUniverse;
            */

        }

        /// <summary>
        /// Posts current account status to database.
        /// </summary>
        protected virtual void PostStatus()
        {
            _ = ServerConfiguration.Database.PostAccountStatus(new AccountStatusDTO()
            {
                AppId = ApplicationId,
                AccountId = AccountId,
                LoggedIn = IsLoggedIn,
                ChannelId = CurrentChannel?.Id,
                GameId = CurrentGame?.Id,
                GameName = CurrentGame?.GameName,
                WorldId = CurrentGame?.Id ?? CurrentChannel?.Id
            });
        }

        #endregion

        #region Login / Logout

        /// <summary>
        /// 
        /// </summary>
        public async Task Logout()
        {
            // Prevent logout twice
            if (_logoutTime.HasValue || !_loginTime.HasValue)
                return;

            // Raise plugin event
            await MuisClass.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_LOGGED_OUT, new OnPlayerArgs() { Player = this });

            // Leave game
            await LeaveCurrentGame();

            // Leave channel
            LeaveCurrentChannel();

            // Logout
            _logoutTime = Utils.GetHighPrecisionUtcTime();

            // Tell database
            PostStatus();
        }



        public async Task RefreshAccount()
        {
            var accountDto = await ServerConfiguration.Database.GetAccountById(AccountId);
            if (accountDto != null)
            {
                FriendsList = accountDto.Friends.ToDictionary(x => x.AccountId, x => x.AccountName);
                ClanId = accountDto.ClanId;
            }
        }

        #endregion


        #region Game

        public async Task JoinGame(Game game, int dmeClientIndex)
        {
            // Leave current game
            await LeaveCurrentGame();

            CurrentGame = game;
            DmeClientId = dmeClientIndex;
            CurrentGame.AddPlayer(this);

            // Tell database
            PostStatus();
        }

        public async Task JoinGameP2P(Game game)
        {
            // Leave current game
            await LeaveCurrentGame();

            CurrentGame = game;
            CurrentGame.AddPlayer(this);

            // Tell database
            PostStatus();
        }

        public async Task LeaveGame(Game game)
        {
            if (CurrentGame != null && CurrentGame == game)
            {
                await LeaveCurrentGame();

                // Tell database
                PostStatus();
            }
        }

        private async Task LeaveCurrentGame()
        {
            if (CurrentGame != null)
            {
                await CurrentGame.RemovePlayer(this);
                CurrentGame = null;
            }
            DmeClientId = null;
        }

        #endregion

        #region Channel

        public async Task JoinChannel(Channel channel)
        {
            // Leave current channel
            await LeaveCurrentChannel();

            CurrentChannel = channel;
            await CurrentChannel.OnPlayerJoined(this);

            // Tell database
            PostStatus();
        }

        public async Task LeaveChannel(Channel channel)
        {
            if (CurrentChannel != null && CurrentChannel == channel)
            {
                await LeaveCurrentChannel();

                // Tell database
                PostStatus();
            }
        }

        private Task LeaveCurrentChannel()
        {
            if (CurrentChannel != null)
            {
                CurrentChannel.OnPlayerLeft(this);
                CurrentChannel = null;
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Session

        /// <summary>
        /// Begin DME Session
        /// </summary>
        public void BeginSession()
        {
            _hasActiveSession = true;
        }

        /// <summary>
        /// End DME Session
        /// </summary>
        public void EndSession()
        {
            _hasActiveSession = false;
        }

        #endregion

        #region Send Queue

        public void Queue(BaseScertMessage message)
        {
            SendMessageQueue.Enqueue(message);
        }

        public void Queue(IEnumerable<BaseScertMessage> messages)
        {
            foreach (var message in messages)
                Queue(message);
        }

        public void Queue(BaseMediusMessage message)
        {
            if (NetConnectionType == NetConnectionType.NetConnectionTypeClientListenerTCP ||
               NetConnectionType == NetConnectionType.NetConnectionTypeClientListenerTCPAuxUDP ||
               NetConnectionType == NetConnectionType.NetConnectionTypeClientListenerUDP)
            {
                ServerConfiguration.LogWarn("\"Can't send on a client listener connection type\"");
            }
            else
            {
                Queue(new RT_MSG_SERVER_APP() { Message = message });
            }
        }

        public void Queue(BaseMediusPluginMessage message)
        {
            Queue(new RT_MSG_SERVER_PLUGIN_TO_APP() { Message = message });
        }

        public void Queue(IEnumerable<BaseMediusMessage> messages)
        {
            Queue(messages.Select(x => new RT_MSG_SERVER_APP() { Message = x }));
        }

        #endregion

        #region SetIP
        public void SetIp(string ip)
        {
            switch (Uri.CheckHostName(ip))
            {
                case UriHostNameType.IPv4:
                    {
                        IP = IPAddress.Parse(ip).MapToIPv4() ?? IPAddress.Any;
                        break;
                    }
                case UriHostNameType.Dns:
                    {
                        IP = Dns.GetHostAddresses(ip).FirstOrDefault()?.MapToIPv4() ?? IPAddress.Any;
                        break;
                    }
                default:
                    {
                        ServerConfiguration.LogError($"Unhandled UriHostNameType {Uri.CheckHostName(ip)} from {ip} in DMEObject.SetIp()");
                        break;
                    }
            }
        }
        #endregion

        public override string ToString()
        {
            return $"({AccountId}:{AccountName})";
            //return $"(worldId: {DmeWorld.WorldId},clientId: {DmeId})";
        }
    }

    public class UploadState
    {
        public FileStream Stream { get; set; }
        public int FileId { get; set; }
        public int PacketNumber { get; set; }
        public int TotalSize { get; set; }
        public int BytesReceived { get; set; }
        public DateTime TimeBegan { get; set; } = DateTime.UtcNow;
    }
}

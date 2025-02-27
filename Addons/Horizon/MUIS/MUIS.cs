﻿using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using MultiServer.Addons.Horizon.RT.Common;
using MultiServer.Addons.Horizon.RT.Cryptography;
using MultiServer.Addons.Horizon.RT.Models;
using MultiServer.Addons.Horizon.LIBRARY.Pipeline.Tcp;
using System.Collections.Concurrent;
using System.Net;
using MultiServer.Addons.Horizon.MUIS.Config;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MultiServer.Addons.Horizon.MUIS
{
    /// <summary>
    /// Introduced in Medius 1.43
    /// Modified in Medius 1.50 deprecating INFO_UNIVERSES Standard Flow
    /// </summary>
    public class MUIS
    {
        public static double homeretailver = 0;

        public static double homebetaver = 0;

        public static Random RNG = new();

        public static ServerSettings Settings = new();

        private int _port = 0;
        public int Port => _port;

        protected IEventLoopGroup _bossGroup = null;
        protected IEventLoopGroup _workerGroup = null;
        protected IChannel _boundChannel = null;
        protected ScertServerHandler _scertHandler = null;
        private uint _clientCounter = 0;

        protected internal class ChannelData
        {
            public int ApplicationId { get; set; } = 0;
            public ConcurrentQueue<BaseScertMessage> RecvQueue { get; } = new ConcurrentQueue<BaseScertMessage>();
            public ConcurrentQueue<BaseScertMessage> SendQueue { get; } = new ConcurrentQueue<BaseScertMessage>();
        }

        protected ConcurrentDictionary<string, ChannelData> _channelDatas = new ConcurrentDictionary<string, ChannelData>();

        public MUIS(int port)
        {
            _port = port;
        }

        /// <summary>
        /// Start the MUIS TCP Server.
        /// </summary>
        public virtual async void Start()
        {
            _bossGroup = new MultithreadEventLoopGroup(1);
            _workerGroup = new MultithreadEventLoopGroup();
            _scertHandler = new ScertServerHandler();

            // Add client on connect
            _scertHandler.OnChannelActive += (channel) =>
            {
                string key = channel.Id.AsLongText();
                _channelDatas.TryAdd(key, new ChannelData());
            };

            // Remove client on disconnect
            _scertHandler.OnChannelInactive += (channel) =>
            {
                string key = channel.Id.AsLongText();
                _channelDatas.TryRemove(key, out var data);
            };

            // Queue all incoming messages
            _scertHandler.OnChannelMessage += (channel, message) =>
            {
                string key = channel.Id.AsLongText();
                if (_channelDatas.TryGetValue(key, out var data))
                {
                    data.RecvQueue.Enqueue(message);
                }

                // Log if id is set
                if (message.CanLog())
                    ServerConfiguration.LogInfo($"TCP RECV {channel}: {message}");
            };

            var bootstrap = new ServerBootstrap();
            bootstrap
                .Group(_bossGroup, _workerGroup)
                .Channel<TcpServerSocketChannel>()
                .Option(ChannelOption.SoBacklog, 100)
                .Handler(new LoggingHandler(LogLevel.INFO))
                .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;

                    pipeline.AddLast(new WriteTimeoutHandler(15));
                    pipeline.AddLast(new ScertEncoder());
                    pipeline.AddLast(new ScertIEnumerableEncoder());
                    pipeline.AddLast(new ScertTcpFrameDecoder(DotNetty.Buffers.ByteOrder.LittleEndian, 1024, 1, 2, 0, 0, false));
                    pipeline.AddLast(new ScertDecoder());
                    pipeline.AddLast(new ScertMultiAppDecoder());
                    pipeline.AddLast(_scertHandler);
                }));

            _boundChannel = await bootstrap.BindAsync(Port);
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public virtual async Task Stop()
        {
            try
            {
                await _boundChannel.CloseAsync();
            }
            finally
            {
                await Task.WhenAll(
                        _bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
                        _workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
            }
        }

        /// <summary>
        /// Process messages.
        /// </summary>
        public async Task Tick()
        {
            if (_scertHandler == null || _scertHandler.Group == null)
                return;

            await Task.WhenAll(_scertHandler.Group.Select(c => Tick(c)));
        }

        private async Task Tick(IChannel clientChannel)
        {
            if (clientChannel == null)
                return;

            List<BaseScertMessage> responses = new List<BaseScertMessage>();
            string key = clientChannel.Id.AsLongText();

            try
            {
                if (_channelDatas.TryGetValue(key, out var data))
                {
                    // Process all messages in queue
                    while (data.RecvQueue.TryDequeue(out var message))
                    {
                        try
                        {
                            ProcessMessage(message, clientChannel, data);
                        }
                        catch (Exception e)
                        {
                            ServerConfiguration.LogError(e);
                        }
                    }

                    // Send if writeable
                    if (clientChannel.IsWritable)
                    {
                        // Add send queue to responses
                        while (data.SendQueue.TryDequeue(out var message))
                            responses.Add(message);

                        if (responses.Count > 0)
                            await clientChannel.WriteAndFlushAsync(responses);
                    }
                }
            }
            catch (Exception e)
            {
                ServerConfiguration.LogError(e);
            }
        }

        #region Message Processing

        protected void ProcessMessage(BaseScertMessage message, IChannel clientChannel, ChannelData data)
        {
            // Get ScertClient data
            var scertClient = clientChannel.GetAttribute(LIBRARY.Pipeline.Constants.SCERT_CLIENT).Get();
            scertClient.CipherService.EnableEncryption = MuisClass.Settings.EncryptMessages;

            switch (message)
            {
                case RT_MSG_CLIENT_HELLO clientHello:
                    {
                        // send hello
                        Queue(new RT_MSG_SERVER_HELLO() { RsaPublicKey = MuisClass.Settings.EncryptMessages ? MuisClass.Settings.DefaultKey.N : Org.BouncyCastle.Math.BigInteger.Zero }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CRYPTKEY_PUBLIC clientCryptKeyPublic:
                    {
                        // generate new client session key
                        scertClient.CipherService.GenerateCipher(CipherContext.RSA_AUTH, clientCryptKeyPublic.PublicKey.Reverse().ToArray());
                        scertClient.CipherService.GenerateCipher(CipherContext.RC_CLIENT_SESSION);

                        Queue(new RT_MSG_SERVER_CRYPTKEY_PEER() { SessionKey = scertClient.CipherService.GetPublicKey(CipherContext.RC_CLIENT_SESSION) }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_TCP clientConnectTcp:
                    {
                        data.ApplicationId = clientConnectTcp.AppId;
                        scertClient.ApplicationID = clientConnectTcp.AppId;

                        List<int> pre108ServerComplete = new List<int>() { 10334, 10540 };

                        if (scertClient.CipherService.HasKey(CipherContext.RC_CLIENT_SESSION))
                        {
                            Queue(new RT_MSG_SERVER_CRYPTKEY_GAME() { GameKey = scertClient.CipherService.GetPublicKey(CipherContext.RC_CLIENT_SESSION) }, clientChannel);
                        }

                        Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                        {
                            IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address,
                        }, clientChannel);

                        if (pre108ServerComplete.Contains(data.ApplicationId))
                        {
                            Queue(new RT_MSG_SERVER_CONNECT_COMPLETE() { ClientCountAtConnect = 0x0001 }, clientChannel);
                        }
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_REQUIRE clientConnectReadyRequire:
                    {
                        if (scertClient.MediusVersion >= 109 && scertClient.CipherService.EnableEncryption == true)
                        {
                            Queue(new RT_MSG_SERVER_CRYPTKEY_GAME() { GameKey = scertClient.CipherService.GetPublicKey(CipherContext.RC_CLIENT_SESSION) }, clientChannel);
                        }
                        Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                        {
                            PlayerId = 0,
                            ScertId = GenerateNewScertClientId(),
                            PlayerCount = 0x0001,
                            IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address
                        }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_TCP clientConnectReadyTcp:
                    {
                        Queue(new RT_MSG_SERVER_CONNECT_COMPLETE() { ClientCountAtConnect = 0x0001 }, clientChannel);
                        break;
                    }
                case RT_MSG_SERVER_ECHO serverEchoReply:
                    {

                        break;
                    }
                case RT_MSG_CLIENT_ECHO clientEcho:
                    {
                        Queue(new RT_MSG_CLIENT_ECHO() { Value = clientEcho.Value }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_APP_TOSERVER clientAppToServer:
                    {
                        ProcessMediusMessage(clientAppToServer.Message, clientChannel, data);
                        break;
                    }
                case RT_MSG_CLIENT_APP_LIST clientAppList:
                    {

                        break;
                    }
                case RT_MSG_CLIENT_DISCONNECT _:
                case RT_MSG_CLIENT_DISCONNECT_WITH_REASON clientDisconnectWithReason:
                    {
                        _ = clientChannel.CloseAsync();
                        break;
                    }
                default:
                    {
                        ServerConfiguration.LogWarn($"UNHANDLED RT MESSAGE: {message}");
                        break;
                    }
            }

            return;
        }

        protected virtual void ProcessMediusMessage(BaseMediusMessage message, IChannel clientChannel, ChannelData data)
        {
            if (message == null)
                return;

            switch (message)
            {
                #region Version Server
                // KZ1
                case MediusVersionServerRequest versionServerRequest:
                    {
                        // ERROR - Need a session
                        if (data == null)
                        {
                            ServerConfiguration.LogError($"INVALID OPERATION: {clientChannel} sent {versionServerRequest} without channeldata.");
                            break;
                        }

                        if (Settings.MediusServerVersionOverride == true)
                        {
                            #region Killzone TCES/Pubeta Version Override
                            // Killzoze TCES/Pubeta
                            if (data.ApplicationId == 10442)
                            {
                                Queue(new RT_MSG_SERVER_APP()
                                {
                                    Message = new MediusVersionServerResponse()
                                    {
                                        MessageID = versionServerRequest.MessageID,
                                        VersionServer = "Medius Universe Information Server Version 1.50.0009",
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                    }


                                }, clientChannel);
                            }
                            #endregion
                        }
                        else
                        {
                            // If MediusServerVersionOverride is false, we send our own Version String
                            Queue(new RT_MSG_SERVER_APP()
                            {
                                Message = new MediusVersionServerResponse()
                                {
                                    MessageID = versionServerRequest.MessageID,
                                    VersionServer = Settings.MUISVersion,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                }
                            }, clientChannel);

                        }


                        break;
                    }

                #endregion

                #region MediusGetUniverse_ExtraInfo
                case MediusGetUniverse_ExtraInfoRequest getUniverse_ExtraInfoRequest:
                    {
                        int compAppId = MuisClass.Settings.CompatibleApplicationIds.Find(appId => appId == data.ApplicationId);

                        if (data.ApplicationId == compAppId)
                        {
                            if (MuisClass.Settings.Universes.TryGetValue(data.ApplicationId, out var infos))
                            {
                                if (getUniverse_ExtraInfoRequest.InfoType == 0)
                                {
                                    ServerConfiguration.LogWarn("InfoType not specified to return anything.");

                                    Queue(new RT_MSG_SERVER_APP()
                                    {
                                        Message = new MediusUniverseStatusList_ExtraInfoResponse()
                                        {
                                            MessageID = new MessageId(),
                                            StatusCode = MediusCallbackStatus.MediusInvalidRequestMsg,
                                            EndOfList = true,
                                        }
                                    }, clientChannel);
                                }

                                #region INFO_UNIVERSES

                                foreach (var info in infos)
                                {
                                    var isLast = infos.LastOrDefault() == info;

                                    #region SVOUrl
                                    if (getUniverse_ExtraInfoRequest.InfoType.HasFlag(MediusUniverseVariableInformationInfoFilter.INFO_SVO_URL))
                                    {
                                        Queue(new RT_MSG_SERVER_APP()
                                        {
                                            Message = new MediusUniverseSvoURLResponse()
                                            {
                                                MessageID = new MessageId(),
                                                URL = info.SvoURL
                                            }
                                        }, clientChannel);
                                    }
                                    #endregion

                                    // MUIS Standard Flow - Deprecated after Medius Client/Server Library 1.50
                                    if (getUniverse_ExtraInfoRequest.InfoType.HasFlag(MediusUniverseVariableInformationInfoFilter.INFO_UNIVERSES))
                                    {

                                        Queue(new RT_MSG_SERVER_APP()
                                        {
                                            Message = new MediusUniverseStatusList_ExtraInfoResponse()
                                            {
                                                MessageID = new MessageId(),
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                UniverseName = info.Name,
                                                DNS = info.Endpoint,
                                                Port = info.Port,
                                                UniverseDescription = info.Description,
                                                Status = info.Status,
                                                UserCount = info.UserCount,
                                                MaxUsers = info.MaxUsers,
                                                BillingSystemName = info.BillingSystemName,
                                                UniverseBilling = info.UniverseBilling,
                                                EndOfList = true,
                                                ExtendedInfo = info.ExtendedInfo,
                                            }
                                        }, clientChannel);

                                        #region News
                                        if (getUniverse_ExtraInfoRequest.InfoType.HasFlag(MediusUniverseVariableInformationInfoFilter.INFO_NEWS))
                                        {
                                            ServerConfiguration.LogInfo("MUIS: News bit set in request");

                                            Queue(new RT_MSG_SERVER_APP()
                                            {
                                                Message = new MediusUniverseNewsResponse()
                                                {
                                                    MessageID = getUniverse_ExtraInfoRequest.MessageID,
                                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                                    News = "Simulated News!",
                                                    EndOfList = true
                                                }
                                            }, clientChannel);
                                        }
                                        #endregion
                                    }

                                    ServerConfiguration.LogInfo($"MUIS: send univ info (ctr=):  [{MuisClass.Settings.Universes.ToArray().Length}]");
                                }
                                #endregion
                            }
                            else
                            {
                                ServerConfiguration.LogWarn($"MUIS: No universes out there.");

                                Queue(new RT_MSG_SERVER_APP()
                                {
                                    Message = new MediusUniverseVariableInformationResponse()
                                    {
                                        MessageID = getUniverse_ExtraInfoRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusNoResult,
                                        InfoFilter = getUniverse_ExtraInfoRequest.InfoType,
                                        EndOfList = true
                                    }
                                }, clientChannel);
                            }
                        }
                        else
                        {
                            ServerConfiguration.LogWarn($"ApplicationID not compatible [{data.ApplicationId}]");

                            Queue(new RT_MSG_SERVER_APP()
                            {
                                Message = new MediusUniverseVariableInformationResponse()
                                {
                                    MessageID = getUniverse_ExtraInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusIncompatibleAppID,
                                    InfoFilter = getUniverse_ExtraInfoRequest.InfoType,
                                    EndOfList = true
                                }
                            }, clientChannel);
                        }

                        break;
                    }
                #endregion

                #region MediusGetUniverseInformationRequest
                case MediusGetUniverseInformationRequest getUniverseInfo:
                    {
                        int compAppId = MuisClass.Settings.CompatibleApplicationIds.Find(appId => appId == data.ApplicationId);

                        //Check if Client AppId equals the Appid in CompatibleAppId list
                        if (data.ApplicationId == compAppId)
                        {
                            if (MuisClass.Settings.Universes.TryGetValue(data.ApplicationId, out var infos))
                            {
                                //Send Standard/Variable Flow
                                foreach (var info in infos)
                                {
                                    var isLast = infos.LastOrDefault() == info;

                                    #region INFO_UNIVERSES
                                    // MUIS Standard Flow - Deprecated after Medius Client/Server Library 1.50
                                    if (getUniverseInfo.InfoType.HasFlag(MediusUniverseVariableInformationInfoFilter.INFO_UNIVERSES))
                                    {
                                        if (getUniverseInfo.InfoType == 0)
                                        {
                                            ServerConfiguration.LogWarn("InfoType not specified to return anything.");

                                            Queue(new RT_MSG_SERVER_APP()
                                            {
                                                Message = new MediusUniverseStatusListResponse()
                                                {
                                                    MessageID = getUniverseInfo.MessageID,
                                                    StatusCode = MediusCallbackStatus.MediusInvalidRequestMsg,
                                                    EndOfList = true,
                                                }
                                            }, clientChannel);
                                        }

                                        Queue(new RT_MSG_SERVER_APP()
                                        {
                                            Message = new MediusUniverseStatusListResponse()
                                            {
                                                MessageID = getUniverseInfo.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                UniverseName = info.Name,
                                                DNS = info.Endpoint,
                                                Port = info.Port,
                                                UniverseDescription = info.Description,
                                                Status = info.Status,
                                                UserCount = info.UserCount,
                                                MaxUsers = info.MaxUsers,
                                                EndOfList = true,
                                            }
                                        }, clientChannel);
                                        #endregion
                                    }
                                    else
                                    {
                                        #region InfoFilter = Null
                                        if (getUniverseInfo.InfoType == 0)
                                        {
                                            ServerConfiguration.LogWarn("InfoType not specified to return anything.");

                                            Queue(new RT_MSG_SERVER_APP()
                                            {
                                                Message = new MediusUniverseVariableInformationResponse()
                                                {
                                                    MessageID = getUniverseInfo.MessageID,
                                                    StatusCode = MediusCallbackStatus.MediusInvalidRequestMsg,
                                                    EndOfList = true,
                                                }
                                            }, clientChannel);
                                        }
                                        #endregion

                                        if (data.ApplicationId == 20374)
                                        {
                                            if (info.ExtendedInfo != null && info.ExtendedInfo != "")
                                            {
                                                string firstFiveElements = info.ExtendedInfo.Substring(0, Math.Min(5, info.ExtendedInfo.Length));

                                                try
                                                {
                                                    homeretailver = Double.Parse(firstFiveElements, CultureInfo.InvariantCulture);
                                                }
                                                catch (Exception)
                                                {
                                                    homeretailver = 0;
                                                }
                                            }
                                            else
                                                homeretailver = 0;
                                        }
                                        else if (data.ApplicationId == 20371)
                                        {
                                            if (info.ExtendedInfo != null && info.ExtendedInfo != "")
                                            {
                                                string firstFiveElements = info.ExtendedInfo.Substring(0, Math.Min(5, info.ExtendedInfo.Length));

                                                try
                                                {
                                                    homebetaver = Double.Parse(firstFiveElements, CultureInfo.InvariantCulture);
                                                }
                                                catch (Exception)
                                                {
                                                    homebetaver = 0;
                                                }
                                            }
                                            else
                                                homebetaver = 0;
                                        }

                                        #region SVOUrl
                                        if (getUniverseInfo.InfoType.HasFlag(MediusUniverseVariableInformationInfoFilter.INFO_SVO_URL))
                                        {
                                            ServerConfiguration.LogInfo($"[MUIS] - send svo info: [{MuisClass.Settings.Universes.ToArray().Length}]");

                                            Queue(new RT_MSG_SERVER_APP()
                                            {
                                                Message = new MediusUniverseSvoURLResponse()
                                                {
                                                    MessageID = getUniverseInfo.MessageID,
                                                    URL = info.SvoURL
                                                }
                                            }, clientChannel);
                                        }
                                        #endregion

                                        // Requires AntiCheat.

                                        if (File.Exists(Directory.GetCurrentDirectory() + $"/static/poke_config.json"))
                                        {
                                            try
                                            {
                                                JObject jsonObject = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(Directory.GetCurrentDirectory() + $"/static/poke_config.json"));

                                                if (jsonObject!= null && jsonObject.Properties() != null)
                                                {
                                                    foreach (var property in jsonObject.Properties())
                                                    {
                                                        if (int.Parse(property.Name) == data.ApplicationId)
                                                        {
                                                            JObject PokeOffsets = property.Value.ToObject<JObject>();

                                                            if (PokeOffsets!= null && PokeOffsets.Properties() != null)
                                                            {
                                                                foreach (var subProperty in PokeOffsets.Properties())
                                                                {
                                                                    uint offset = uint.Parse(subProperty.Name);
                                                                    uint valuetopatch = uint.Parse(subProperty.Value.ToString());

                                                                    Queue(new RT_MSG_SERVER_MEMORY_POKE()
                                                                    {
                                                                        start_Address = offset,
                                                                        Payload = BitConverter.GetBytes(valuetopatch),
                                                                        SkipEncryption = true

                                                                    }, clientChannel);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                ServerConfiguration.LogWarn($"[MUIS] - MemoryPoke failed to initialise! {ex}.");
                                            }
                                        }

                                        Queue(new RT_MSG_SERVER_APP()
                                        {
                                            Message = new MediusUniverseVariableInformationResponse()
                                            {
                                                MessageID = getUniverseInfo.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                InfoFilter = getUniverseInfo.InfoType,
                                                UniverseID = info.UniverseId,
                                                ExtendedInfo = info.ExtendedInfo,
                                                UniverseName = info.Name,
                                                UniverseDescription = info.Description,
                                                SvoURL = info.SvoURL,
                                                Status = info.Status,
                                                UserCount = info.UserCount,
                                                MaxUsers = info.MaxUsers,
                                                DNS = info.Endpoint,
                                                Port = info.Port,
                                                UniverseBilling = info.UniverseBilling,
                                                BillingSystemName = info.BillingSystemName,
                                                EndOfList = isLast
                                            }
                                        }, clientChannel);
                                    }

                                    #region News
                                    if (getUniverseInfo.InfoType.HasFlag(MediusUniverseVariableInformationInfoFilter.INFO_NEWS))
                                    {
                                        ServerConfiguration.LogInfo("[MUIS] - News bit set in request");

                                        Queue(new RT_MSG_SERVER_APP()
                                        {
                                            Message = new MediusUniverseNewsResponse()
                                            {
                                                MessageID = getUniverseInfo.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                News = "Simulated News",
                                                EndOfList = true
                                            }
                                        }, clientChannel);
                                    }
                                    #endregion

                                    ServerConfiguration.LogInfo($"[MUIS] - send univ info:  [{MuisClass.Settings.Universes.ToArray().Length}]");
                                }
                            }
                            else
                            {
                                ServerConfiguration.LogWarn($"[MUIS] - No universes out there.");

                                Queue(new RT_MSG_SERVER_APP()
                                {
                                    Message = new MediusUniverseVariableInformationResponse()
                                    {
                                        MessageID = getUniverseInfo.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusNoResult,
                                        InfoFilter = getUniverseInfo.InfoType,
                                        EndOfList = true
                                    }
                                }, clientChannel);
                            }
                        }
                        else
                        {
                            ServerConfiguration.LogWarn($"ApplicationID not compatible [{data.ApplicationId}]");

                            if (getUniverseInfo.InfoType.HasFlag(MediusUniverseVariableInformationInfoFilter.INFO_UNIVERSES))
                            {
                                Queue(new RT_MSG_SERVER_APP()
                                {
                                    Message = new MediusUniverseStatusListResponse()
                                    {
                                        MessageID = getUniverseInfo.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusIncompatibleAppID,
                                        EndOfList = true
                                    }
                                }, clientChannel);
                            }
                            else
                            {
                                Queue(new RT_MSG_SERVER_APP()
                                {
                                    Message = new MediusUniverseVariableInformationResponse()
                                    {
                                        MessageID = getUniverseInfo.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusIncompatibleAppID,
                                        InfoFilter = getUniverseInfo.InfoType,
                                        EndOfList = true
                                    }
                                }, clientChannel);
                            }
                        }
                        break;
                    }
                #endregion

                #region Channels

                case MediusChannelList_ExtraInfoRequest channelList_ExtraInfoRequest:
                    {
                        List<MediusChannelList_ExtraInfoResponse> channelResponses = new List<MediusChannelList_ExtraInfoResponse>();

                        // Deadlocked only uses this to connect to a non-game channel (lobby)
                        // So we'll filter by lobby here
                        /*
                        var channels = MUISStarter.Manager.GetChannelList(
                            data.ApplicationId,
                            channelList_ExtraInfoRequest.PageID,
                            channelList_ExtraInfoRequest.PageSize,
                            ChannelType.Lobby);
                        


                        foreach (var channel in channels)
                        {
                            channelResponses.Add(new MediusChannelList_ExtraInfoResponse()
                            {
                                MessageID = channelList_ExtraInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                MediusWorldID = channel.Id,
                                LobbyName = channel.Name,
                                GameWorldCount = (ushort)channel.GameCount,
                                PlayerCount = (ushort)channel.PlayerCount,
                                MaxPlayers = (ushort)channel.MaxPlayers,
                                GenericField1 = channel.GenericField1,
                                GenericField2 = channel.GenericField2,
                                GenericField3 = channel.GenericField3,
                                GenericField4 = channel.GenericField4,
                                GenericFieldLevel = channel.GenericFieldLevel,
                                SecurityLevel = channel.SecurityLevel,
                                EndOfList = false
                            });
                        }
                        */

                        channelResponses.Add(new MediusChannelList_ExtraInfoResponse()
                        {
                            MessageID = channelList_ExtraInfoRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            MediusWorldID = 1,
                            LobbyName = "US",
                            GameWorldCount = 0,
                            PlayerCount = 0,
                            MaxPlayers = 256,
                            GenericField1 = 0,
                            GenericField2 = 0,
                            GenericField3 = 0,
                            GenericField4 = 0,
                            GenericFieldLevel = 0,
                            SecurityLevel = MediusWorldSecurityLevelType.WORLD_SECURITY_NONE,
                            EndOfList = false
                        });

                        if (channelResponses.Count == 0)
                        {
                            Queue(new RT_MSG_SERVER_APP()
                            {
                                Message = new MediusChannelList_ExtraInfoResponse()
                                {
                                    MessageID = channelList_ExtraInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                }
                            }, clientChannel);
                        }
                        else
                        {
                            // Ensure the end of list flag is set
                            channelResponses[channelResponses.Count - 1].EndOfList = true;

                            // Add to responses
                            Queue(channelResponses, clientChannel);
                        }
                        break;
                    }

                #endregion

                #region Time
                case MediusGetServerTimeRequest getServerTimeRequest:
                    {
                        var time = DateTime.Now;

                        _ = GetTimeZone(time).ContinueWith((r) =>
                        {
                            if (r.IsCompletedSuccessfully)
                            {
                                //Fetched
                                Queue(new RT_MSG_SERVER_APP()
                                {
                                    Message = new MediusGetServerTimeResponse()
                                    {
                                        MessageID = getServerTimeRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        Local_server_timezone = r.Result,
                                    }
                                }, clientChannel);
                            }
                            else
                            {
                                //default
                                Queue(new RT_MSG_SERVER_APP()
                                {
                                    Message = new MediusGetServerTimeResponse()
                                    {
                                        MessageID = getServerTimeRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        Local_server_timezone = MediusTimeZone.MediusTimeZone_GMT,
                                    }
                                }, clientChannel);
                            }
                        });
                        break;
                    }
                #endregion

                default:
                    {
                        ServerConfiguration.LogWarn($"UNHANDLED MEDIUS MESSAGE: {message}");
                        break;
                    }
            }
        }

        #endregion

        #region Queue

        public void Queue(IEnumerable<BaseMediusMessage> messages, params IChannel[] clientChannels)
        {
            Queue(messages.Select(x => new RT_MSG_SERVER_APP() { Message = x }), clientChannels);
        }

        public void Queue(BaseScertMessage message, params IChannel[] clientChannels)
        {
            Queue(message, (IEnumerable<IChannel>)clientChannels);
        }

        public void Queue(BaseScertMessage message, IEnumerable<IChannel> clientChannels)
        {
            foreach (var clientChannel in clientChannels)
                if (clientChannel != null)
                    if (_channelDatas.TryGetValue(clientChannel.Id.AsLongText(), out var data))
                        data.SendQueue.Enqueue(message);
        }

        public void Queue(IEnumerable<BaseScertMessage> messages, params IChannel[] clientChannels)
        {
            Queue(messages, (IEnumerable<IChannel>)clientChannels);
        }

        public void Queue(IEnumerable<BaseScertMessage> messages, IEnumerable<IChannel> clientChannels)
        {
            foreach (var clientChannel in clientChannels)
                if (clientChannel != null)
                    if (_channelDatas.TryGetValue(clientChannel.Id.AsLongText(), out var data))
                        foreach (var message in messages)
                            data.SendQueue.Enqueue(message);
        }

        #endregion

        #region TimeZone
        public Task<MediusTimeZone> GetTimeZone(DateTime time)
        {

            var tz = TimeZoneInfo.Local;
            var tzInt = Convert.ToInt32(tz.Id);


            var tzStanName = tz.StandardName;

            /*
            if (tzTime. == 7200)
            {

            }
            */

            if (tzStanName == "CEST")
                return Task.FromResult(MediusTimeZone.MediusTimeZone_CEST);
            else if (tzInt == 83 && (tzInt + 1) == 83 && (tzInt + 2) == 84)
                return Task.FromResult(MediusTimeZone.MediusTimeZone_SWEDISHST);
            else if (tzInt == 70 && (tzInt + 1) == 83 && (tzInt + 2) == 84)
                return Task.FromResult(MediusTimeZone.MediusTimeZone_FST);
            else if (tzInt == 67 && (tzInt + 1) == 65 && (tzInt + 2) == 84)
                return Task.FromResult(MediusTimeZone.MediusTimeZone_CAT);
            else if (tzStanName == "SAST")
                return Task.FromResult(MediusTimeZone.MediusTimeZone_SAST);
            else if (tzInt == 69 && (tzInt + 1) == 65 && (tzInt + 2) == 84)
                return Task.FromResult(MediusTimeZone.MediusTimeZone_EET);
            else if (tzInt == 73 && (tzInt + 1) == 65 && (tzInt + 2) == 84)
                return Task.FromResult(MediusTimeZone.MediusTimeZone_ISRAELST);

            return Task.FromResult(MediusTimeZone.MediusTimeZone_GMT);
        }
        #endregion

        protected uint GenerateNewScertClientId()
        {
            return _clientCounter++;
        }
    }
}

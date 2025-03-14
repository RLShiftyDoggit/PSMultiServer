﻿using DotNetty.Transport.Channels;
using MultiServer.Addons.Horizon.RT.Common;
using MultiServer.Addons.Horizon.RT.Cryptography;
using MultiServer.Addons.Horizon.RT.Models;
using MultiServer.Addons.Horizon.RT.Models.Misc;
using MultiServer.Addons.Horizon.LIBRARY.Common;
using MultiServer.Addons.Horizon.LIBRARY.Database.Models;
using MultiServer.Addons.Horizon.LIBRARY.libAntiCheat;
using MultiServer.Addons.Horizon.MEDIUS.Config;
using MultiServer.Addons.Horizon.MEDIUS.Medius.Models;
using MultiServer.Addons.Horizon.MEDIUS.PluginArgs;
using System.Net;
using System.Security.Cryptography;
using MultiServer.Addons.Horizon.RT.Models.Lobby;
using MultiServer.PluginManager;

namespace MultiServer.Addons.Horizon.MEDIUS.Medius
{
    public class MLS : BaseMediusComponent
    {
        public override int TCPPort => MediusClass.Settings.MLSPort;
        public override int UDPPort => 0;

        public ServerSettings Settings = new ServerSettings();

        public MLS()
        {

        }

        //KILLZONE PS2 ONLY
        public ClientObject ReserveClient(MediusVersionServerRequest request)
        {
            var client = new ClientObject();
            client.BeginSession();
            return client;
        }

        public ClientObject ReserveClient(MediusSessionBeginRequest request)
        {
            var client = new ClientObject();
            client.BeginSession();
            return client;
        }

        public ClientObject ReserveClient1(MediusSessionBeginRequest1 request)
        {
            var client = new ClientObject();
            client.BeginSession();
            return client;
        }

        public ClientObject ReserveClient(MediusExtendedSessionBeginRequest request)
        {
            var client = new ClientObject();
            client.BeginSession();
            return client;
        }

        protected override async Task ProcessMessage(BaseScertMessage message, IChannel clientChannel, ChannelData data)
        {
            // Get ScertClient data
            var scertClient = clientChannel.GetAttribute(LIBRARY.Pipeline.Constants.SCERT_CLIENT).Get();
            var enableEncryption = MediusClass.GetAppSettingsOrDefault(data.ApplicationId).EnableEncryption;
            scertClient.CipherService.EnableEncryption = enableEncryption;

            switch (message)
            {
                case RT_MSG_CLIENT_HELLO clientHello:
                    {
                        // send hello
                        Queue(new RT_MSG_SERVER_HELLO() { RsaPublicKey = enableEncryption ? MediusClass.Settings.DefaultKey.N : Org.BouncyCastle.Math.BigInteger.Zero }, clientChannel);
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
                        List<int> pre108ServerComplete = new List<int>() { 10683, 10684, 10114, 10164, 10190, 10124, 10284, 10330, 10334, 10414, 10442, 10540, 10680 };
                        List<int> pre108NoServerComplete = new List<int>() { 10010, 10031, 10274 };

                        #region Compatible AppId
                        if (!MediusClass.Manager.IsAppIdSupported(clientConnectTcp.AppId))
                        {
                            ServerConfiguration.LogError($"Client {clientChannel.RemoteAddress} attempting to authenticate with incompatible app id {clientConnectTcp.AppId}");
                            await clientChannel.CloseAsync();
                            return;
                        }
                        #endregion

                        data.ApplicationId = clientConnectTcp.AppId;
                        scertClient.ApplicationID = clientConnectTcp.AppId;

                        data.ClientObject = MediusClass.Manager.GetClientByAccessToken(clientConnectTcp.AccessToken, clientConnectTcp.AppId);
                        if (data.ClientObject == null)
                        {
                            ServerConfiguration.LogWarn("CLIENTOBJECT NULL FALLBACK TO SESSIONKEY!!");
                            data.ClientObject = MediusClass.Manager.GetClientBySessionKey(clientConnectTcp.SessionKey, clientConnectTcp.AppId);
                        }

                        #region Client Object Null?
                        //If Client Object is null, then ignore
                        if (data.ClientObject == null)
                        {
                            ServerConfiguration.LogError($"IGNORING CLIENT {data} || {data.ClientObject}");
                            data.Ignore = true;
                        }
                        #endregion
                        else
                        {
                            data.ClientObject.OnConnected();

                            // Update our client object to use existing one
                            data.ClientObject.ApplicationId = clientConnectTcp.AppId;

                            #region if PS3
                            if (scertClient.IsPS3Client)
                            {
                                //CAC & Warhawk
                                if (data.ClientObject.ApplicationId == 20623 || data.ClientObject.ApplicationId == 20624 || data.ClientObject.ApplicationId == 20043 || data.ClientObject.ApplicationId == 20044)
                                {
                                    Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                                    {
                                        PlayerId = 0,
                                        ScertId = GenerateNewScertClientId(),
                                        PlayerCount = 0x0001,
                                        IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address
                                    }, clientChannel);
                                }
                                else
                                    Queue(new RT_MSG_SERVER_CONNECT_REQUIRE(), clientChannel);
                            }
                            #endregion
                            else if (scertClient.MediusVersion > 108 && scertClient.MediusVersion != 111 && scertClient.ApplicationID != 20624 && scertClient.ApplicationID != 11484)
                                Queue(new RT_MSG_SERVER_CONNECT_REQUIRE(), clientChannel);
                            else
                            {

                                Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                                {
                                    PlayerId = 0,
                                    ScertId = GenerateNewScertClientId(),
                                    PlayerCount = 0x0001,
                                    IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address
                                }, clientChannel);

                                //Older Medius titles do NOT use CRYPTKEY_GAME, newer ones have this.
                                if (scertClient.CipherService.EnableEncryption == true && scertClient.RsaAuthKey != null)
                                    Queue(new RT_MSG_SERVER_CRYPTKEY_GAME() { GameKey = scertClient.CipherService.GetPublicKey(CipherContext.RC_CLIENT_SESSION) }, clientChannel);

                                if (pre108ServerComplete.Contains(data.ApplicationId) && !pre108NoServerComplete.Contains(data.ApplicationId))
                                    Queue(new RT_MSG_SERVER_CONNECT_COMPLETE() { ClientCountAtConnect = 0x0001 }, clientChannel);

                                /*

                                 //If Frequency, TMBO, Socom 1, ATV Offroad Fury 2,  My Street, or Field Commander Beta then
                                 if (data.ApplicationId == 10010 || data.ApplicationId == 10031 || data.ApplicationId == 10274 || data.ApplicationId == 10540 || data.ApplicationId == 10284 || data.ApplicationId == 20190)
                                 {
                                     //Do NOT send hereCryptKey Game
                                     Queue(new RT_MSG_SERVER_CONNECT_ACCEPT_TCP()
                                     {
                                         PlayerId = 0,
                                         ScertId = GenerateNewScertClientId(),
                                         PlayerCount = 0x0001,
                                         IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address
                                     }, clientChannel);

                                     //If ATV Offroad Fury 2, complete connection
                                     if (pre108ServerComplete.Contains(data.ApplicationId))
                                     {
                                         Queue(new RT_MSG_SERVER_CONNECT_COMPLETE() { ClientCountAtConnect = 0x0001 }, clientChannel);
                                     }
                                 }
                                 else
                                 {
                                     // If RFOM, Starhawk
                                     if (data.ApplicationId == 20174 || data.ApplicationId == 20043 || data.ApplicationId == 22920)
                                     {
                                         //Do Nothing
                                     }
                                     else
                                     {
                                         //Older Medius titles do NOT use CRYPTKEY_GAME, newer ones have this.
                                         if (scertClient.CipherService.EnableEncryption != true)
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
                                     }

                                     if (scertClient.MediusVersion > 108 || pre108ServerComplete.Contains(data.ApplicationId))
                                     {
                                         Queue(new RT_MSG_SERVER_CONNECT_COMPLETE() { ClientCountAtConnect = 0x0001 }, clientChannel);
                                     }
                                 }
                                */
                            }

                        }

                        break;
                    }
                case RT_MSG_CLIENT_CONNECT_READY_REQUIRE clientConnectReadyRequire:
                    {
                        if (!scertClient.IsPS3Client)
                            Queue(new RT_MSG_SERVER_CRYPTKEY_GAME() { GameKey = scertClient.CipherService.GetPublicKey(CipherContext.RC_CLIENT_SESSION) }, clientChannel);
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

                        if (scertClient.MediusVersion > 108)
                            Queue(new RT_MSG_SERVER_ECHO(), clientChannel);
                        break;
                    }
                case RT_MSG_SERVER_ECHO serverEchoReply:
                    {

                        break;
                    }
                case RT_MSG_CLIENT_ECHO clientEcho:
                    {
                        if (data.ClientObject == null || !data.ClientObject.IsLoggedIn)
                            break;

                        Queue(new RT_MSG_CLIENT_ECHO() { Value = clientEcho.Value }, clientChannel);
                        break;
                    }
                case RT_MSG_CLIENT_APP_TOSERVER clientAppToServer:
                    {
                        await ProcessMediusMessage(clientAppToServer.Message, clientChannel, data);
                        break;
                    }

                case RT_MSG_CLIENT_DISCONNECT _:
                    {
                        //Medius 1.08 (Used on WRC 4) haven't a state
                        if (scertClient.MediusVersion <= 108)
                            await clientChannel.CloseAsync();
                        else
                            data.State = ClientState.DISCONNECTED;
                        await clientChannel.CloseAsync();

                        ServerConfiguration.LogInfo($"[MLS] - Client disconnected by request with no specific reason\n");
                        break;
                    }
                case RT_MSG_CLIENT_DISCONNECT_WITH_REASON clientDisconnectWithReason:
                    {
                        if (clientDisconnectWithReason.disconnectReason <= RT_MSG_CLIENT_DISCONNECT_REASON.RT_MSG_CLIENT_DISCONNECT_LENGTH_MISMATCH)
                            ServerConfiguration.LogInfo($"[MLS] - Disconnected by request with reason of {clientDisconnectWithReason.disconnectReason}\n");
                        else
                            ServerConfiguration.LogInfo($"[MLS] - Disconnected by request with (application specified) reason of {clientDisconnectWithReason.disconnectReason}\n");

                        data.State = ClientState.DISCONNECTED;
                        await clientChannel.CloseAsync();
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

        protected virtual async Task ProcessMediusMessage(BaseMediusMessage message, IChannel clientChannel, ChannelData data)
        {
            if (message == null)
                return;

            var appSettings = MediusClass.GetAppSettingsOrDefault(data.ApplicationId);

            switch (message)
            {
                #region Session End

                case MediusSessionEndRequest sessionEndRequest:
                    {
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} is trying to end session without an Client Object");

                        Queue(new RT_MSG_SERVER_APP()
                        {
                            Message = new MediusSessionEndResponse()
                            {
                                MessageID = sessionEndRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess
                            }
                        }, clientChannel);

                        // End session
                        data.ClientObject.EndSession();
                        data.ClientObject = null;
                        /*
                        if (data.ClientObject.IsLoggedIn)
                        {
                            ServerConfiguration.LogInfo($"SessionEnd Success");
                            await data.ClientObject.Logout();
                        }
                        */
                        break;
                    }

                #endregion

                #region DNAS CID Check
                case MediusDnasSignaturePost dnasSignaturePost:
                    {

                        if (Settings.DnasEnablePost != true)
                        {
                            //If DNAS Signature Post is the PS2/PSP/PS3 Console ID then continue
                            if (dnasSignaturePost.DnasSignatureType == MediusDnasCategory.DnasConsoleID)
                            {
                                data.MachineId = BitConverter.ToString(dnasSignaturePost.DnasSignature);

                                ServerConfiguration.LogInfo($"Posting ConsoleID - ConsoleSigSize={dnasSignaturePost.DnasSignatureLength}");

                                // Then post to the Database if logged in
                                if (data.ClientObject?.IsLoggedIn ?? false)
                                    await ServerConfiguration.Database.PostMachineId(data.ClientObject.AccountId, data.MachineId);
                            }

                            if (dnasSignaturePost.DnasSignatureType == MediusDnasCategory.DnasTitleID)
                                ServerConfiguration.LogInfo($"DnasSignaturePost Error - Invalid SignatureType");

                            if (dnasSignaturePost.DnasSignatureType == MediusDnasCategory.DnasDiskID)
                                ServerConfiguration.LogInfo($"Posting DiskID - DiskSigSize={dnasSignaturePost.DnasSignatureLength}");
                        }
                        else
                        {
                            //DnasEnablePost false, no Post;
                        }

                        break;
                    }
                #endregion

                #region Logout

                case MediusAccountLogoutRequest accountLogoutRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountLogoutRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountLogoutRequest} without being logged in.");

                        // Check token
                        if (accountLogoutRequest.SessionKey == data.ClientObject.SessionKey)
                        {
                            // Logout
                            await data.ClientObject.Logout();

                            ServerConfiguration.LogInfo($"Player {data.ClientObject.AccountName} has logged out.\n");

                            // Reply
                            data.ClientObject.Queue(new MediusAccountLogoutResponse()
                            {
                                MessageID = accountLogoutRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess
                            });
                        }
                        else
                        {
                            ServerConfiguration.LogWarn($"Failed to logout account {data.ClientObject.AccountName}.\n");

                            // Reply
                            data.ClientObject.Queue(new MediusAccountLogoutResponse()
                            {
                                MessageID = accountLogoutRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                        }

                        break;
                    }

                #endregion

                #region AccessLevel (2.12)

                case MediusGetAccessLevelInfoRequest getAccessLevelInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getAccessLevelInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getAccessLevelInfoRequest} without being logged in.");

                        //int adminAccessLevel = 4;

                        data.ClientObject.Queue(new MediusGetAccessLevelInfoResponse()
                        {
                            MessageID = getAccessLevelInfoRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            AccessLevel = MediusAccessLevelType.MEDIUS_ACCESSLEVEL_DEFAULT,
                        });
                        break;
                    }

                #endregion 

                #region Announcements / Policy

                case MediusGetAllAnnouncementsRequest getAllAnnouncementsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getAllAnnouncementsRequest} without a session.");

                        // Send to plugins
                        await MediusClass.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_GET_ALL_ANNOUNCEMENTS, new OnPlayerRequestArgs()
                        {
                            Player = data.ClientObject,
                            Request = getAllAnnouncementsRequest
                        });

                        await ServerConfiguration.Database.GetLatestAnnouncements(data.ApplicationId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null && r.Result.Length > 0)
                            {
                                List<MediusGetAnnouncementsResponse> responses = new List<MediusGetAnnouncementsResponse>();
                                foreach (var result in r.Result)
                                {
                                    responses.Add(new MediusGetAnnouncementsResponse()
                                    {
                                        MessageID = getAllAnnouncementsRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        Announcement = string.IsNullOrEmpty(result.AnnouncementTitle) ? $"{result.AnnouncementBody}" : $"{result.AnnouncementTitle}\n{result.AnnouncementBody}\n",
                                        AnnouncementID = result.Id,
                                        EndOfList = false
                                    });
                                }

                                responses[responses.Count - 1].EndOfList = true;
                                data.ClientObject.Queue(responses);
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGetAnnouncementsResponse()
                                {
                                    MessageID = getAllAnnouncementsRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    Announcement = "",
                                    AnnouncementID = 0,
                                    EndOfList = true
                                });
                            }
                        });
                        break;
                    }

                case MediusGetAnnouncementsRequest getAnnouncementsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getAnnouncementsRequest} without a session.");

                        // Send to plugins
                        await MediusClass.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_GET_ANNOUNCEMENTS, new OnPlayerRequestArgs()
                        {
                            Player = data.ClientObject,
                            Request = getAnnouncementsRequest
                        });

                        await ServerConfiguration.Database.GetLatestAnnouncement(data.ApplicationId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                data.ClientObject.Queue(new MediusGetAnnouncementsResponse()
                                {
                                    MessageID = getAnnouncementsRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    Announcement = string.IsNullOrEmpty(r.Result.AnnouncementTitle) ? $"{r.Result.AnnouncementBody}" : $"{r.Result.AnnouncementTitle}\n{r.Result.AnnouncementBody}\n",
                                    AnnouncementID = r.Result.Id,
                                    EndOfList = true
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGetAnnouncementsResponse()
                                {
                                    MessageID = getAnnouncementsRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    Announcement = "",
                                    AnnouncementID = 0,
                                    EndOfList = true
                                });
                            }
                        });

                        break;
                    }

                case MediusGetPolicyRequest getPolicyRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getPolicyRequest} without a session.");

                        // Send to plugins
                        await MediusClass.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_GET_POLICY, new OnPlayerRequestArgs()
                        {
                            Player = data.ClientObject,
                            Request = getPolicyRequest
                        });

                        switch (getPolicyRequest.Policy)
                        {
                            case MediusPolicyType.Privacy:
                                {
                                    await ServerConfiguration.Database.GetPolicy((int)MediusPolicyType.Privacy, data.ClientObject.ApplicationId).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result != null)
                                        {
                                            string txt = r.Result.EulaBody;
                                            if (!string.IsNullOrEmpty(r.Result.EulaTitle))
                                                txt = r.Result.EulaTitle + "\n" + txt;
                                            data.ClientObject.Queue(MediusGetPolicyResponse.FromText(getPolicyRequest.MessageID, txt));
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusGetPolicyResponse() { MessageID = getPolicyRequest.MessageID, StatusCode = MediusCallbackStatus.MediusSuccess, Policy = "", EndOfText = true });
                                        }
                                    });
                                    break;
                                }
                            case MediusPolicyType.Usage:
                                {
                                    await ServerConfiguration.Database.GetPolicy((int)MediusPolicyType.Usage, data.ClientObject.ApplicationId).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result != null)
                                        {
                                            string txt = r.Result.EulaBody;
                                            if (!string.IsNullOrEmpty(r.Result.EulaTitle))
                                                txt = r.Result.EulaTitle + "\n" + txt;
                                            data.ClientObject.Queue(MediusGetPolicyResponse.FromText(getPolicyRequest.MessageID, txt));
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusGetPolicyResponse() { MessageID = getPolicyRequest.MessageID, StatusCode = MediusCallbackStatus.MediusSuccess, Policy = "", EndOfText = true });
                                        }
                                    });

                                    break;
                                }
                        }

                        break;
                    }

                #endregion

                #region NpId (PS3)
                case MediusNpIdPostRequest NpIdPostRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {NpIdPostRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {NpIdPostRequest} without being logged in.");


                        NpIdDTO NpId = new NpIdDTO
                        {
                            AppId = data.ClientObject.ApplicationId,
                            data = NpIdPostRequest.data,
                            dummy = NpIdPostRequest.dummy,
                            term = NpIdPostRequest.term,
                            opt = NpIdPostRequest.opt,
                            reserved = NpIdPostRequest.reserved,
                        };

                        await ServerConfiguration.Database.PostNpId(NpId).ContinueWith((r) =>
                        {
                            if (r.IsCompletedSuccessfully && r.Result == true)
                            {
                                data.ClientObject.Queue(new MediusStatusResponse()
                                {
                                    Type = 0x6E,
                                    Class = NpIdPostRequest.PacketClass,
                                    MessageID = NpIdPostRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusStatusResponse()
                                {
                                    Type = 0x6E,
                                    Class = NpIdPostRequest.PacketClass,
                                    MessageID = NpIdPostRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError
                                });
                            }
                        });
                        break;
                    }


                case MediusNpIdsGetByAccountNamesRequest getNpIdsGetByAccountNamesRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getNpIdsGetByAccountNamesRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getNpIdsGetByAccountNamesRequest} without being logged in.");


                        string rawNpId = getNpIdsGetByAccountNamesRequest.AccountNames.FirstOrDefault();

                        char NpId;

                        if (rawNpId == null || rawNpId == string.Empty)
                            NpId = Convert.ToChar("VoodooPerson05");
                        else
                            NpId = Convert.ToChar(rawNpId);

                        var searchNpIdData = BitConverter.GetBytes(NpId);

                        NpIdDTO NpIdnew = new NpIdDTO
                        {
                            AppId = data.ClientObject.ApplicationId,
                            data = searchNpIdData,
                        };

                        await ServerConfiguration.Database.NpIdSearchByAccountNames(NpIdnew, data.ClientObject.ApplicationId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null && r.Result.Count > 0)
                            {

                                List<MediusNpIdsGetByAccountNamesResponse> responses = new List<MediusNpIdsGetByAccountNamesResponse>();
                                foreach (var result in r.Result)
                                {

                                    responses.Add(new MediusNpIdsGetByAccountNamesResponse()
                                    {
                                        MessageID = getNpIdsGetByAccountNamesRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        AccountName = result.data.ToString(),
                                        //SceNpId = result.SceNpId + 1,
                                        data = result.data,
                                        term = result.term,
                                        dummy = result.dummy,
                                        opt = result.opt,
                                        reserved = result.reserved,
                                        EndOfList = false
                                    });

                                }

                                responses[responses.Count - 1].EndOfList = true;
                                data.ClientObject.Queue(responses);
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusNpIdsGetByAccountNamesResponse()
                                {
                                    MessageID = getNpIdsGetByAccountNamesRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError,
                                    EndOfList = true
                                });
                            }
                        });


                        break;
                    }

                #endregion

                #region Match (PS3)
                //Unimplemented - Max of 14 SuperSets
                case MediusMatchGetSupersetListRequest matchGetSupersetListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {matchGetSupersetListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {matchGetSupersetListRequest} without being logged in.");
                        /*
                        if (data.ApplicationId == 22920) // Starhawk
                        {
                            byte[] payload = Convert.FromBase64String("http");

                            Queue(new RT_MSG_SERVER_MEMORY_POKE()
                            {
                                start_Address = 0xd0016b80,
                                MsgDataLen = payload.Length,
                                Payload = payload
                            }, clientChannel);
                        }
                        */


                        /*
                        data.ClientObject.Queue(new MediusMatchGetSupersetListResponse()
                        {
                            MessageID = matchGetSupersetListRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            EndOfList = true,
                            SupersetID = 1,
                            SupersetName = "Casual",
                            SupersetDescription = "M:PR Casual",
                        });
                        */
                        /*
                        await ServerConfiguration.Database.GetMatchmakingSupersets(data.ClientObject.ApplicationId).ContinueWith(r =>
                        {
                            List<MediusMatchGetSupersetListResponse> responses = new List<MediusMatchGetSupersetListResponse>();
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                responses.AddRange(r.Result.Where(x => x.SupersetID != 0)
                                    .Select(x => new MediusMatchGetSupersetListResponse()
                                    {
                                        MessageID = matchGetSupersetListRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        EndOfList = false,
                                        SupersetID = x.SupersetID,
                                        SupersetName = x.SupersetName,
                                        SupersetDescription = x.SupersetDescription,
                                        SupersetExtraInfo = x.SupersetExtraInfo,
                                    }));
                                foreach(var response in responses)
                                {
                                    ServerConfiguration.LogInfo($"{response.SupersetID}: {response.SupersetName}[x{responses.Count}]: {response.SupersetDescription}\n");
                                }

                            }

                            if(responses.Count > 14 )
                            {
                                Logger.Warn($"too many supersets");
                                responses.Add(new MediusMatchGetSupersetListResponse()
                                {
                                    MessageID = matchGetSupersetListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusTransactionTimedOut,
                                    EndOfList = true
                                });
                            }

                            if (responses.Count == 0)
                            {
                                ServerConfiguration.LogInfo("No supersets\n");
                                responses.Add(new MediusMatchGetSupersetListResponse()
                                {
                                    MessageID = matchGetSupersetListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }

                            responses[responses.Count - 1].EndOfList = true;
                            data.ClientObject.Queue(responses);
                        });
                        */


                        // Default - No Result
                        data.ClientObject.Queue(new MediusMatchGetSupersetListResponse()
                        {
                            MessageID = matchGetSupersetListRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            EndOfList = true,
                        });

                        break;
                    }

                case MediusMatchCreateGameRequest mediusMatchCreateGame:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {mediusMatchCreateGame} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {mediusMatchCreateGame} without being logged in.");

                        // validate name
                        if (!MediusClass.PassTextFilter(data.ClientObject.ApplicationId, TextFilterContext.GAME_NAME, Convert.ToString(mediusMatchCreateGame.GameName)))
                        {
                            data.ClientObject.Queue(new MediusMatchCreateGameResponse()
                            {
                                MessageID = mediusMatchCreateGame.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                            return;
                        }

                        // Send to plugins
                        await MediusClass.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_CREATE_GAME, new OnPlayerRequestArgs() { Player = data.ClientObject, Request = mediusMatchCreateGame });

                        await MediusClass.Manager.MatchCreateGame(data.ClientObject, mediusMatchCreateGame, clientChannel);
                        break;
                    }

                //Unimplemented
                case MediusMatchFindGameRequest matchFindGameRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {matchFindGameRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {matchFindGameRequest} without being logged in.");

                        uint gameCount = MediusClass.Manager.GetGameCount(data.ClientObject.ApplicationId);

                        if (gameCount == 0)
                        {

                            // Tell the client their now MatchingInProgress
                            data.ClientObject.Queue(new MediusMatchFindGameStatusResponse()
                            {
                                MessageID = matchFindGameRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusRequestAccepted,
                            });
                        }
                        else
                        {

                            // Tell the client their now MatchingInProgress
                            data.ClientObject.Queue(new MediusMatchFindGameStatusResponse()
                            {
                                MessageID = matchFindGameRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusMatchingInProgress,
                            });
                        }




                        break;
                    }

                case MediusMatchPartyRequest mediusMatchPartyRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {mediusMatchPartyRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {mediusMatchPartyRequest} without being logged in.");


                        // 6 MediusJoinAssignedGame
                        MediusMatchRosterInfo mediusMatchRosterInfo = new MediusMatchRosterInfo();

                        //var matchRoster = MediusClass.Manager.CalculateSizeOfMatchRoster(mediusMatchRosterInfo);

                        /*
                        data.ClientObject.Queue(new MediusMatchPartyResponse()
                        {
                            MessageID = mediusMatchPartyRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusJoinAssignedGame,
                            PluginSpecificStatusCode = 2,
                            GameWorldID = 0,
                            GamePassword = null,
                            GameHostType = MediusGameHostType.MediusGameHostClientServerUDP,
                            AddressList = new NetAddressList()
                            {
                                AddressList = new NetAddress[Constants.NET_ADDRESS_LIST_COUNT]
                                {
                                    new NetAddress() { Address = data.ClientObject.IP.ToString(), Port = 10079, AddressType = NetAddressType.NetAddressTypeExternal},
                                    new NetAddress() { AddressType = NetAddressType.NetAddressNone},
                                }
                            },
                            ApplicationDataSizeJAS = mediusMatchPartyRequest.ApplicationDataSize,
                            ApplicationDataJAS = mediusMatchPartyRequest.ApplicationData,
                            MatchRoster = matchRoster,

                            NumParties = 0,
                            Parties = 0,

                            NumPlayers = 0,
                            Players = 0
                        });
                        */
                        //** match make failed (no match)\n

                        /*
                        data.ClientObject.Queue(new MediusMatchPartyResponse()
                        {
                            MessageID = mediusMatchPartyRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusJoinAssignedGame,
                            PluginSpecificStatusCode = 0,

                            GameWorldID = 0,
                            //GamePassword = "",
                            GameHostType = MediusGameHostType.MediusGameHostClientServerUDP,
                            AddressList = new NetAddressList()
                            {
                                AddressList = new NetAddress[Constants.NET_ADDRESS_LIST_COUNT]
                                        {
                                            new NetAddress() { Address = MediusClass.LobbyServer.IPAddress.ToString(), Port = 10079, AddressType = NetAddressType.NetAddressTypeExternal},
                                            new NetAddress() { AddressType = NetAddressType.NetAddressNone},
                                        }
                            },
                            ApplicationDataSizeJAS = mediusMatchPartyRequest.ApplicationDataSize,
                            ApplicationDataJAS = mediusMatchPartyRequest.ApplicationData,
                            MatchRoster = matchRoster,

                            NumParties = 0,
                            Parties = 0,

                            NumPlayers = 0,
                            Players = 0
                        });
                        */


                        // 7 MediusMatchTypeHostGame7
                        data.ClientObject.Queue(new MediusMatchPartyResponse()
                        {
                            MessageID = mediusMatchPartyRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusMatchTypeHostGame,
                            PluginSpecificStatusCode = 0,
                            MatchGameID = mediusMatchPartyRequest.SupersetID,
                            ApplicationDataSizeHG = mediusMatchPartyRequest.ApplicationDataSize,
                            ApplicationDataHG = mediusMatchPartyRequest.ApplicationData,
                        });


                        /*
                        // 8 MediusMatchTypeReferral
                        data.ClientObject.Queue(new MediusMatchPartyResponse()
                        {
                            MessageID = mediusMatchPartyRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusMatchTypeReferral,
                            PluginSpecificStatusCode = 1,

                            MatchingWorldUID = MediusClass.Manager.GetOrCreateDefaultLobbyChannel(data.ApplicationId).Id,
                            ConnectInfo = new NetConnectionInfo()
                            {
                                AccessKey = data.ClientObject.Token,
                                SessionKey = data.ClientObject.SessionKey,
                                WorldID = MediusClass.Manager.GetOrCreateDefaultLobbyChannel(data.ApplicationId).Id,
                                ServerKey = MediusClass.GlobalAuthPublic,
                                AddressList = new NetAddressList()
                                {
                                    AddressList = new NetAddress[Constants.NET_ADDRESS_LIST_COUNT]
                                        {
                                            new NetAddress() { Address = MediusClass.LobbyServer.IPAddress.ToString(), Port = (uint)MediusClass.LobbyServer.TCPPort, AddressType = NetAddressType.NetAddressTypeExternal},
                                            new NetAddress() { AddressType = NetAddressType.NetAddressNone},
                                        }
                                },
                                Type = NetConnectionType.NetConnectionTypeClientServerTCP
                            }
                        });
                        */
                        break;
                    }
                /*
            case MediusMatchSetGameStateRequest mediusMatchSetGameStateRequest:
                {
                    // ERROR - Need a session
                    if (data.ClientObject == null)
                        throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {mediusMatchSetGameStateRequest} without a session.");

                    // ERROR -- Need to be logged in
                    if (!data.ClientObject.IsLoggedIn)
                        throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {mediusMatchSetGameStateRequest} without being logged in.");

                    //Not sure how we handle this yet but we take this anyway!
                    MediusMatchGameState matchGameState = mediusMatchSetGameStateRequest.MatchGameState;




                    //MediusMatchSetGameStateResponse
                    data.ClientObject.Queue(new MediusStatusResponse()
                    {
                        Type = 0x7A, 
                        Class = mediusMatchSetGameStateRequest.PacketClass,
                        MessageID = mediusMatchSetGameStateRequest.MessageID,
                        StatusCode = MediusCallbackStatus.MediusSuccess
                    });

                    break;
                }
                */
                #endregion

                #region Version Server

                case MediusVersionServerRequest mediusVersionServerRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {mediusVersionServerRequest} without a session.");

                        if (Settings.MediusServerVersionOverride == true)
                        {
                            #region F1 2005 PAL
                            // F1 2005 PAL SCES / F1 2005 PAL TCES
                            if (data.ApplicationId == 10954 || data.ApplicationId == 10952)
                            {
                                data.ClientObject.Queue(new MediusVersionServerResponse()
                                {
                                    MessageID = mediusVersionServerRequest.MessageID,
                                    VersionServer = "Medius Lobby Server Version 2.9.0009",
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                });
                            }
                            #endregion

                            #region Socom 1
                            else if (data.ApplicationId == 10274)
                            {
                                data.ClientObject.Queue(new MediusVersionServerResponse()
                                {
                                    MessageID = mediusVersionServerRequest.MessageID,
                                    VersionServer = "Medius Lobby Server Version 1.40.PRE8",
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                });
                            }
                            else if (data.ApplicationId == 10031) //TM:BO
                            {
                                data.ClientObject.Queue(new MediusVersionServerResponse()
                                {
                                    MessageID = mediusVersionServerRequest.MessageID,
                                    VersionServer = "Medius Lobby Server Version 1.41.0036",
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                });
                            }
                            else if (data.ApplicationId == 10010)
                            {
                                data.ClientObject.Queue(new MediusVersionServerResponse()
                                {
                                    MessageID = mediusVersionServerRequest.MessageID,
                                    VersionServer = "Medius Lobby Server Version 1.41.0000",
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                });
                            }
                            #endregion
                            //Default
                            else
                            {
                                data.ClientObject.Queue(new MediusVersionServerResponse()
                                {
                                    MessageID = mediusVersionServerRequest.MessageID,
                                    VersionServer = "Medius Lobby Server Version 3.05.201109161400",
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                });
                            }
                        }
                        else
                        {
                            // If MediusServerVersionOverride is false, we send our own Version String
                            data.ClientObject.Queue(new MediusVersionServerResponse()
                            {
                                MessageID = mediusVersionServerRequest.MessageID,
                                VersionServer = Settings.MLSVersion,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                            });
                        }

                        break;
                    }

                #endregion

                #region Account

                case MediusAccountGetIDRequest accountGetIdRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountGetIdRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountGetIdRequest} without being logged in.");

                        await ServerConfiguration.Database.GetAccountByName(accountGetIdRequest.AccountName, data.ClientObject.ApplicationId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                data.ClientObject.Queue(new MediusAccountGetIDResponse()
                                {
                                    MessageID = accountGetIdRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    AccountID = r.Result.AccountId
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusAccountGetIDResponse()
                                {
                                    MessageID = accountGetIdRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusAccountNotFound
                                });
                            }
                        });
                        break;
                    }

                case MediusAccountUpdatePasswordRequest accountUpdatePasswordRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountUpdatePasswordRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountUpdatePasswordRequest} without being logged in.");

                        // Post New Password to Database
                        await ServerConfiguration.Database.PostAccountUpdatePassword(data.ClientObject.AccountId, accountUpdatePasswordRequest.OldPassword, accountUpdatePasswordRequest.NewPassword).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusAccountUpdatePasswordStatusResponse()
                                {
                                    MessageID = accountUpdatePasswordRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusAccountUpdatePasswordStatusResponse()
                                {
                                    MessageID = accountUpdatePasswordRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError
                                });
                            }
                        });
                        break;
                    }

                case MediusAccountUpdateStatsRequest accountUpdateStatsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountUpdateStatsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {accountUpdateStatsRequest} without being logged in.");

                        await ServerConfiguration.Database.PostMediusStats(data.ClientObject.AccountId, Convert.ToBase64String(accountUpdateStatsRequest.Stats)).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusAccountUpdateStatsResponse()
                                {
                                    MessageID = accountUpdateStatsRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusAccountUpdateStatsResponse()
                                {
                                    MessageID = accountUpdateStatsRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError
                                });
                            }
                        });
                        break;
                    }

                #endregion

                #region Buddy List

                case MediusGetBuddyListRequest getBuddyListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getBuddyListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getBuddyListRequest} without being logged in.");

                        if (data.ClientObject.MediusVersion < 112)
                        {
                            await ServerConfiguration.Database.GetAccountById(data.ClientObject.AccountId).ContinueWith((r) =>
                            {
                                if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                    return;

                                if (r.IsCompletedSuccessfully && r.Result != null)
                                {
                                    // Responses
                                    List<MediusGetBuddyListResponse> friendListResponses = new List<MediusGetBuddyListResponse>();

                                    // Iterate through friends and build a response for each
                                    foreach (var friend in r.Result.Friends)
                                    {
                                        var friendClient = MediusClass.Manager.GetClientByAccountId(friend.AccountId, data.ClientObject.ApplicationId);
                                        friendListResponses.Add(new MediusGetBuddyListResponse()
                                        {
                                            MessageID = getBuddyListRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            AccountID = friend.AccountId,
                                            AccountName = friend.AccountName,
                                            PlayerStatus = (MediusPlayerStatus)friend.PlayerStatus,
                                            EndOfList = false
                                        });
                                    }

                                    // If we have any responses then send them
                                    if (friendListResponses.Count > 0)
                                    {
                                        // Ensure the last response is tagged as EndOfList
                                        friendListResponses[friendListResponses.Count - 1].EndOfList = true;

                                        // Send friends
                                        data.ClientObject.Queue(friendListResponses);
                                    }
                                    else
                                    {
                                        // No friends
                                        data.ClientObject.Queue(new MediusGetBuddyListResponse()
                                        {
                                            MessageID = getBuddyListRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            EndOfList = true
                                        });
                                    }
                                }
                                else
                                {
                                    // DB error
                                    data.ClientObject.Queue(new MediusGetBuddyListResponse()
                                    {
                                        MessageID = getBuddyListRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusPlayerNotPrivileged,
                                        EndOfList = true
                                    });
                                }
                            });
                        }
                        else
                        {
                            await ServerConfiguration.Database.GetAccountById(data.ClientObject.AccountId).ContinueWith((r) =>
                            {
                                if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                    return;

                                if (r.IsCompletedSuccessfully && r.Result != null)
                                {
                                    // Responses
                                    List<MediusGetBuddyListResponse> friendListResponses = new List<MediusGetBuddyListResponse>();

                                    // Iterate through friends and build a response for each
                                    foreach (var friend in r.Result.Friends)
                                    {
                                        var friendClient = MediusClass.Manager.GetClientByAccountId(friend.AccountId, data.ClientObject.ApplicationId);
                                        friendListResponses.Add(new MediusGetBuddyListResponse()
                                        {
                                            MessageID = getBuddyListRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            AccountID = friend.AccountId,
                                            AccountName = friend.AccountName,
                                            PlayerStatus = (MediusPlayerStatus)friend.PlayerStatus,
                                            EndOfList = false
                                        });
                                    }

                                    // If we have any responses then send them
                                    if (friendListResponses.Count > 0)
                                    {
                                        // Ensure the last response is tagged as EndOfList
                                        friendListResponses[friendListResponses.Count - 1].EndOfList = true;

                                        // Send friends
                                        data.ClientObject.Queue(friendListResponses);
                                    }
                                    else
                                    {
                                        // No friends
                                        data.ClientObject.Queue(new MediusGetBuddyListResponse()
                                        {
                                            MessageID = getBuddyListRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            EndOfList = true
                                        });
                                    }
                                }
                                else
                                {
                                    // DB error
                                    data.ClientObject.Queue(new MediusGetBuddyListResponse()
                                    {
                                        MessageID = getBuddyListRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusPlayerNotPrivileged,
                                        EndOfList = true
                                    });
                                }
                            });
                        }

                        break;
                    }

                case MediusBuddySetListRequest buddySetListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {buddySetListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {buddySetListRequest} without being logged in.");

                        var FriendsList = data.ClientObject.FriendsList;

                        string[] stringArrList = buddySetListRequest.List;
                        List<string> dblist = new List<string>(stringArrList.ToList());

                        //Fetch PS3 Buddy List from the current connected client
                        foreach (var accountName in buddySetListRequest.List)
                        {
                            await ServerConfiguration.Database.GetAccountByName(accountName, data.ClientObject.ApplicationId).ContinueWith((r) =>
                            {
                                if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                    return;

                                if (r.IsCompletedSuccessfully && r.Result != null)
                                {
                                    //Found in database so keep.
                                }
                                else
                                {
                                    if (ServerConfiguration.Database._settings.SimulatedMode == false)
                                    {
                                        //NotFound in Database so remove from list
                                        //dblist.Remove(accountName);
                                    }
                                }
                            });
                        }
                        data.ClientObject.FriendsListPS3 = dblist;

                        //If FriendsList on PS3 is null, return No Result.
                        if (data.ClientObject.FriendsListPS3 == null)
                        {
                            //If Friends list from NP is actually null, send No Result
                            data.ClientObject.Queue(new MediusBuddySetListResponse()
                            {
                                MessageID = buddySetListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                            });
                        }
                        else
                        {
                            // Success NP Buddy List is NOT NULL - Return Success!
                            data.ClientObject.Queue(new MediusBuddySetListResponse()
                            {
                                MessageID = buddySetListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                            });
                        }

                        break;
                    }

                #region MediusIgnoreSetListRequest
                case MediusIgnoreSetListRequest ignoreSetListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ignoreSetListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ignoreSetListRequest} without being logged in.");

                        string[] stringArrList = ignoreSetListRequest.List;
                        List<string> dblist = new List<string>(stringArrList.ToList());

                        //Fetch PS3 Buddy List from the current connected client
                        foreach (var accountName in ignoreSetListRequest.List)
                        {

                            await ServerConfiguration.Database.GetAccountByName(accountName, data.ClientObject.ApplicationId).ContinueWith((r) =>
                            {
                                if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                    return;

                                if (r.IsCompletedSuccessfully && r.Result != null)
                                {
                                    //Found in database so keep.
                                }
                                else
                                {
                                    //NotFound in Database so remove from list
                                    dblist.Remove(accountName);
                                }
                            });
                        }

                        data.ClientObject.FriendsListPS3 = dblist;


                        //If FriendsList on PS3 is null, return No Result.
                        if (data.ClientObject.FriendsListPS3 == null)
                        {
                            //If Friends list from NP is actually null, send No Result
                            data.ClientObject.Queue(new MediusBuddySetListResponse()
                            {
                                MessageID = ignoreSetListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                            });
                        }
                        else
                        {
                            // Success NP Buddy List is NOT NULL - Return Success!
                            data.ClientObject.Queue(new MediusBuddySetListResponse()
                            {
                                MessageID = ignoreSetListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                            });
                        }
                        break;
                    }
                #endregion

                #region MediusAddToBuddyListRequest
                case MediusAddToBuddyListRequest addToBuddyListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToBuddyListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToBuddyListRequest} without being logged in.");

                        data.ClientObject.OnConnected();

                        // Add
                        await ServerConfiguration.Database.AddBuddy(new BuddyDTO()
                        {
                            AccountId = data.ClientObject.AccountId,
                            BuddyAccountId = addToBuddyListRequest.AccountID
                        }).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusAddToBuddyListResponse()
                                {
                                    MessageID = addToBuddyListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });

                                _ = data.ClientObject.RefreshAccount();
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusAddToBuddyListResponse()
                                {
                                    MessageID = addToBuddyListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError
                                });
                            }
                        });
                        break;
                    }
                #endregion

                case MediusAddToBuddyListConfirmationRequest addToBuddyListConfirmationRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToBuddyListConfirmationRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToBuddyListConfirmationRequest} without being logged in.");

                        //
                        await ServerConfiguration.Database.GetAccountById(addToBuddyListConfirmationRequest.TargetAccountID).ContinueWith(r =>
                        {
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                ProcessAddToBuddyListConfirmationRequest(data.ClientObject, r.Result, addToBuddyListConfirmationRequest);
                            }
                            else
                            {
                                ServerConfiguration.LogWarn($"Failed to get TargetAccountId");
                            }
                        });

                        break;
                    }

                //GetBuddyPermission
                case MediusAddToBuddyListFwdConfirmationResponse addToBuddyListFwdConfirmationResponse:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToBuddyListFwdConfirmationResponse} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToBuddyListFwdConfirmationResponse} without being logged in.");

                        //
                        await ServerConfiguration.Database.GetAccountById(addToBuddyListFwdConfirmationResponse.OriginatorAccountID).ContinueWith((r) =>
                        {
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                ProcessAddToBuddyListConfirmationResponse(data.ClientObject, r.Result, addToBuddyListFwdConfirmationResponse);
                            }
                            else
                            {
                                ServerConfiguration.LogWarn($"Failed to get OriginatorAccountId");
                            }
                        });
                        break;
                    }

                case MediusGetBuddyInvitationsRequest getBuddyInvitationsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getBuddyInvitationsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getBuddyInvitationsRequest} without being logged in.");

                        try
                        {
                            // Responses
                            List<MediusGetBuddyInvitationsResponse> buddyInvitationsResponses = new List<MediusGetBuddyInvitationsResponse>();

                            await ServerConfiguration.Database.retrieveBuddyInvitations(data.ClientObject.ApplicationId, data.ClientObject.AccountId).ContinueWith(r =>
                            {
                                if (r.IsCompletedSuccessfully && r.Result != null && r.Result.Count > 0)
                                {
                                    foreach (var buddyInvitationPending in r.Result)
                                    {

                                        ServerConfiguration.LogWarn($"BuddyAddType [{buddyInvitationPending.addType}]");
                                        if (buddyInvitationPending.addType == (int)MediusBuddyAddType.AddSingle)
                                        {
                                            buddyInvitationsResponses.Add(new MediusGetBuddyInvitationsResponse
                                            {
                                                MessageID = getBuddyInvitationsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                AccountID = buddyInvitationPending.AccountId,
                                                AccountName = buddyInvitationPending.AccountName,
                                                AddType = MediusBuddyAddType.AddSingle,
                                                EndOfList = false
                                            });
                                        }
                                        else
                                        {
                                            buddyInvitationsResponses.Add(new MediusGetBuddyInvitationsResponse
                                            {
                                                MessageID = getBuddyInvitationsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                AccountID = buddyInvitationPending.AccountId,
                                                AccountName = buddyInvitationPending.AccountName,
                                                AddType = MediusBuddyAddType.AddSymmetric,
                                                EndOfList = false
                                            });
                                        }
                                    }

                                    ServerConfiguration.LogWarn($"buddyInvitationsResponses [{buddyInvitationsResponses.Count}]");

                                    // If we have any responses then send them
                                    if (buddyInvitationsResponses.Count > 0)
                                    {
                                        ServerConfiguration.LogWarn($"Sending Friend Invitations");

                                        // Ensure the last response is tagged as EndOfList
                                        buddyInvitationsResponses[buddyInvitationsResponses.Count - 1].EndOfList = true;
                                        // Send friends invitation 
                                        data.ClientObject.Queue(buddyInvitationsResponses);
                                    }
                                    else
                                    {
                                        // No friends
                                        data.ClientObject.Queue(new MediusGetBuddyInvitationsResponse()
                                        {
                                            MessageID = getBuddyInvitationsRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            EndOfList = true
                                        });
                                    }
                                }
                                else
                                {
                                    // No Invitations
                                    data.ClientObject.Queue(new MediusGetBuddyInvitationsResponse()
                                    {
                                        MessageID = getBuddyInvitationsRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusNoResult,
                                        EndOfList = true
                                    });
                                }
                            });
                        }
                        catch (Exception e)
                        {
                            ServerConfiguration.LogWarn($"Exception at {e}");
                        }
                        break;
                    }

                case MediusRemoveFromBuddyListRequest removeFromBuddyListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removeFromBuddyListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removeFromBuddyListRequest} without being logged in.");

                        // Remove
                        await ServerConfiguration.Database.RemoveBuddy(new BuddyDTO()
                        {
                            AccountId = data.ClientObject.AccountId,
                            BuddyAccountId = removeFromBuddyListRequest.AccountID
                        }).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusRemoveFromBuddyListResponse()
                                {
                                    MessageID = removeFromBuddyListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });

                                _ = data.ClientObject.RefreshAccount();
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusRemoveFromBuddyListResponse()
                                {
                                    MessageID = removeFromBuddyListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError
                                });
                            }
                        });
                        break;
                    }

                case MediusGetBuddyList_ExtraInfoRequest getBuddyList_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getBuddyList_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel},{data.ClientObject} sent {getBuddyList_ExtraInfoRequest} without being logged in.");


                        // Responses
                        List<MediusGetBuddyList_ExtraInfoResponse> friendListResponses = new List<MediusGetBuddyList_ExtraInfoResponse>();

                        if (data.ClientObject.MediusVersion >= 112 &&
                            data.ClientObject.ApplicationId == 20770 || //PSP R&C Should not go through this route
                            data.ClientObject.ApplicationId == 20774)
                        {

                            await ServerConfiguration.Database.GetAccountById(data.ClientObject.AccountId).ContinueWith((r) =>
                            {
                                if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                    return;

                                if (r.IsCompletedSuccessfully && r.Result != null)
                                {
                                    // Responses
                                    List<MediusGetBuddyList_ExtraInfoResponse> friendListResponses = new List<MediusGetBuddyList_ExtraInfoResponse>();

                                    if (data.ClientObject.MediusVersion == 113 && data.ClientObject.FriendsListPS3 == null)
                                    {
                                        // No friends
                                        data.ClientObject.Queue(new MediusGetBuddyList_ExtraInfoResponse()
                                        {
                                            MessageID = getBuddyList_ExtraInfoRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            EndOfList = true
                                        });
                                        return;
                                    }

                                    // Iterate through friends and build a response for each
                                    foreach (var friend in data.ClientObject.FriendsListPS3)
                                    {
                                        var friendClient = new ClientObject();

                                        var friendClientCached = ServerConfiguration.Database.GetAccountByName(friend, data.ClientObject.ApplicationId);
                                        friendClient = MediusClass.Manager.GetClientByAccountName(friend, data.ClientObject.ApplicationId);

                                        if (friendClient == null)
                                        {
                                            friendListResponses.Add(new MediusGetBuddyList_ExtraInfoResponse()
                                            {
                                                MessageID = getBuddyList_ExtraInfoRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                AccountID = friendClientCached?.Result?.AccountId ?? MediusClass.Manager.AnonymousAccountIDGenerator(MediusClass.Settings.AnonymousIDRangeSeed),
                                                AccountName = friend,
                                                OnlineState = new MediusPlayerOnlineState()
                                                {
                                                    ConnectStatus = (friendClient != null && friendClient.IsLoggedIn) ? friendClient.PlayerStatus : MediusPlayerStatus.MediusPlayerDisconnected,
                                                    MediusLobbyWorldID = -1,
                                                    MediusGameWorldID = -1,
                                                    GameName = friendClient?.CurrentGame?.GameName ?? "",
                                                    LobbyName = friendClient?.CurrentChannel?.Name ?? ""
                                                },
                                                EndOfList = false
                                            });
                                        }
                                        else
                                        {
                                            if (friendClient.CurrentGame != null)
                                            {
                                                var game = MediusClass.Manager.GetGameByGameId(friendClient.CurrentGame.Id);

                                                friendListResponses.Add(new MediusGetBuddyList_ExtraInfoResponse()
                                                {
                                                    MessageID = getBuddyList_ExtraInfoRequest.MessageID,
                                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                                    AccountID = friendClient.AccountId,
                                                    AccountName = friendClient.AccountName,
                                                    OnlineState = new MediusPlayerOnlineState()
                                                    {
                                                        ConnectStatus = (friendClient != null && friendClient.IsLoggedIn) ? friendClient.PlayerStatus : MediusPlayerStatus.MediusPlayerDisconnected,
                                                        MediusLobbyWorldID = friendClient?.CurrentChannel?.Id ?? MediusClass.Manager.GetOrCreateDefaultLobbyChannel(data.ApplicationId).Id,
                                                        MediusGameWorldID = friendClient?.CurrentGame?.Id ?? -1,
                                                        GameName = friendClient?.CurrentGame?.GameName ?? "",
                                                        LobbyName = friendClient?.CurrentChannel?.Name ?? ""
                                                    },
                                                    EndOfList = false
                                                });
                                            }

                                        }
                                    }

                                    // If we have any responses then send them
                                    if (friendListResponses.Count > 0)
                                    {
                                        // Ensure the last response is tagged as EndOfList
                                        friendListResponses[friendListResponses.Count - 1].EndOfList = true;

                                        // Send friends
                                        data.ClientObject.Queue(friendListResponses);
                                    }
                                    else
                                    {
                                        // No friends
                                        data.ClientObject.Queue(new MediusGetBuddyList_ExtraInfoResponse()
                                        {
                                            MessageID = getBuddyList_ExtraInfoRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            EndOfList = true
                                        });
                                    }
                                }
                                else
                                {
                                    // DB error
                                    data.ClientObject.Queue(new MediusGetBuddyList_ExtraInfoResponse()
                                    {
                                        MessageID = getBuddyList_ExtraInfoRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusDBError,
                                        EndOfList = true
                                    });
                                }
                            });

                        }
                        else
                        {


                            await ServerConfiguration.Database.GetAccountById(data.ClientObject.AccountId).ContinueWith((r) =>
                            {
                                if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                    return;

                                if (r.IsCompletedSuccessfully && r.Result != null)
                                {
                                    // Responses
                                    List<MediusGetBuddyList_ExtraInfoResponse> friendListResponses = new List<MediusGetBuddyList_ExtraInfoResponse>();

                                    // Iterate through friends and build a response for each
                                    foreach (var friend in r.Result.Friends)
                                    {
                                        var friendClient = MediusClass.Manager.GetClientByAccountId(friend.AccountId, data.ClientObject.ApplicationId);
                                        friendListResponses.Add(new MediusGetBuddyList_ExtraInfoResponse()
                                        {
                                            MessageID = getBuddyList_ExtraInfoRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            AccountID = friend.AccountId,
                                            AccountName = friend.AccountName,
                                            OnlineState = new MediusPlayerOnlineState()
                                            {
                                                ConnectStatus = (friendClient != null && friendClient.IsLoggedIn) ? friendClient.PlayerStatus : MediusPlayerStatus.MediusPlayerDisconnected,
                                                MediusLobbyWorldID = friendClient?.CurrentChannel?.Id ?? MediusClass.Manager.GetOrCreateDefaultLobbyChannel(data.ApplicationId).Id,
                                                MediusGameWorldID = friendClient?.CurrentGame?.Id ?? -1,
                                                GameName = friendClient?.CurrentGame?.GameName ?? "",
                                                LobbyName = friendClient?.CurrentChannel?.Name ?? ""
                                            },
                                            EndOfList = false
                                        });
                                    }

                                    // If we have any responses then send them
                                    if (friendListResponses.Count > 0)
                                    {
                                        /*
                                        if (friendListResponses[friendListResponses.Count].OnlineState.ConnectStatus == MediusPlayerStatus.MediusPlayerDisconnected)
                                        {
                                            // If a buddy from their friends list is offline, then we don't add them to the list.
                                            friendListResponses.RemoveAt(friendListResponses.Count - 1);
                                        }
                                        */
                                        // Ensure the last response is tagged as EndOfList
                                        friendListResponses[friendListResponses.Count - 1].EndOfList = true;

                                        // Send friends
                                        data.ClientObject.Queue(friendListResponses);
                                    }
                                    else
                                    {
                                        // No friends
                                        data.ClientObject.Queue(new MediusGetBuddyList_ExtraInfoResponse()
                                        {
                                            MessageID = getBuddyList_ExtraInfoRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            EndOfList = true
                                        });
                                    }
                                }
                                else
                                {
                                    // DB error
                                    data.ClientObject.Queue(new MediusGetBuddyList_ExtraInfoResponse()
                                    {
                                        MessageID = getBuddyList_ExtraInfoRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusDBError,
                                        EndOfList = true
                                    });
                                }
                            });



                        }



                        break;
                    }

                #endregion

                #region Ignore List

                case MediusGetIgnoreListRequest getIgnoreListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getIgnoreListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getIgnoreListRequest} without being logged in.");

                        // 
                        await ServerConfiguration.Database.GetAccountById(data.ClientObject.AccountId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                // Responses
                                List<MediusGetIgnoreListResponse> ignoredListResponses = new List<MediusGetIgnoreListResponse>();

                                // Iterate and send to client
                                foreach (var player in r.Result.Ignored)
                                {
                                    var playerClient = MediusClass.Manager.GetClientByAccountId(player.AccountId, data.ClientObject.ApplicationId);
                                    ignoredListResponses.Add(new MediusGetIgnoreListResponse()
                                    {
                                        MessageID = getIgnoreListRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        IgnoreAccountID = player.AccountId,
                                        IgnoreAccountName = player.AccountName,
                                        PlayerStatus = playerClient?.PlayerStatus ?? MediusPlayerStatus.MediusPlayerDisconnected,
                                        EndOfList = false
                                    });
                                }

                                // If we have any responses then send them
                                if (ignoredListResponses.Count > 0)
                                {
                                    // Ensure the last response is tagged as EndOfList
                                    ignoredListResponses[ignoredListResponses.Count - 1].EndOfList = true;

                                    // Send friends
                                    data.ClientObject.Queue(ignoredListResponses);
                                }
                                else
                                {
                                    // No ignored
                                    data.ClientObject.Queue(new MediusGetIgnoreListResponse()
                                    {
                                        MessageID = getIgnoreListRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusNoResult,
                                        EndOfList = true
                                    });
                                }
                            }
                            else
                            {
                                // DB error
                                data.ClientObject.Queue(new MediusGetIgnoreListResponse()
                                {
                                    MessageID = getIgnoreListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError,
                                    EndOfList = true
                                });
                            }
                        });
                        break;
                    }

                case MediusAddToIgnoreListRequest addToIgnoreList:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToIgnoreList} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {addToIgnoreList} without being logged in.");

                        // Add
                        await ServerConfiguration.Database.AddIgnored(new IgnoredDTO()
                        {
                            AccountId = data.ClientObject.AccountId,
                            IgnoredAccountId = addToIgnoreList.IgnoreAccountID
                        }).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusAddToIgnoreListResponse()
                                {
                                    MessageID = addToIgnoreList.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusAddToIgnoreListResponse()
                                {
                                    MessageID = addToIgnoreList.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError
                                });
                            }
                        });
                        break;
                    }

                case MediusRemoveFromIgnoreListRequest removeFromIgnoreListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removeFromIgnoreListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removeFromIgnoreListRequest} without being logged in.");

                        // Remove
                        await ServerConfiguration.Database.RemoveIgnored(new IgnoredDTO()
                        {
                            AccountId = data.ClientObject.AccountId,
                            IgnoredAccountId = removeFromIgnoreListRequest.IgnoreAccountID
                        }).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusRemoveFromIgnoreListResponse()
                                {
                                    MessageID = removeFromIgnoreListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusRemoveFromIgnoreListResponse()
                                {
                                    MessageID = removeFromIgnoreListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError
                                });
                            }
                        });
                        break;
                    }

                #endregion

                #region Ladder Stats

                case MediusUpdateLadderStatsRequest updateLadderStatsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateLadderStatsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateLadderStatsRequest} without being logged in.");

                        // pass to plugins
                        var pluginMessage = new OnPlayerWideStatsArgs()
                        {
                            Game = data.ClientObject.CurrentGame,
                            Player = data.ClientObject,
                            IsClan = updateLadderStatsRequest.LadderType == MediusLadderType.MediusLadderTypeClan,
                            WideStats = updateLadderStatsRequest.Stats
                        };
                        await MediusClass.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_POST_WIDE_STATS, pluginMessage);

                        // reject
                        if (pluginMessage.Reject)
                        {
                            data.ClientObject.Queue(new MediusUpdateLadderStatsResponse()
                            {
                                MessageID = updateLadderStatsRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                            break;
                        }

                        switch (updateLadderStatsRequest.LadderType)
                        {
                            case MediusLadderType.MediusLadderTypePlayer:
                                {

                                    await ServerConfiguration.Database.PostAccountLadderStats(new StatPostDTO()
                                    {
                                        AccountId = data.ClientObject.AccountId,
                                        Stats = pluginMessage.WideStats
                                    }).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result)
                                        {
                                            data.ClientObject.WideStats = pluginMessage.WideStats;
                                            data.ClientObject.Queue(new MediusUpdateLadderStatsResponse()
                                            {
                                                MessageID = updateLadderStatsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusUpdateLadderStatsResponse()
                                            {
                                                MessageID = updateLadderStatsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess
                                            });
                                        }
                                    });
                                    break;
                                }
                            case MediusLadderType.MediusLadderTypeClan:
                                {
                                    await ServerConfiguration.Database.PostClanLadderStats(data.ClientObject.AccountId,
                                        data.ClientObject.ClanId,
                                        pluginMessage.WideStats,
                                        data.ClientObject.ApplicationId)
                                    .ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result)
                                        {
                                            data.ClientObject.WideStats = pluginMessage.WideStats;
                                            data.ClientObject.Queue(new MediusUpdateLadderStatsResponse()
                                            {
                                                MessageID = updateLadderStatsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusUpdateLadderStatsResponse()
                                            {
                                                MessageID = updateLadderStatsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusDBError
                                            });
                                        }
                                    });
                                    break;
                                }
                            default:
                                {
                                    ServerConfiguration.LogWarn($"Unhandled MediusUpdateLadderStatsRequest {updateLadderStatsRequest}");
                                    break;
                                }
                        }
                        break;
                    }

                case MediusUpdateLadderStatsWideRequest updateLadderStatsWideRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateLadderStatsWideRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateLadderStatsWideRequest} without being logged in.");

                        // pass to plugins
                        var pluginMessage = new OnPlayerWideStatsArgs()
                        {
                            Game = data.ClientObject.CurrentGame,
                            Player = data.ClientObject,
                            IsClan = updateLadderStatsWideRequest.LadderType == MediusLadderType.MediusLadderTypeClan,
                            WideStats = updateLadderStatsWideRequest.Stats
                        };
                        await MediusClass.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_POST_WIDE_STATS, pluginMessage);

                        // reject
                        if (pluginMessage.Reject)
                        {
                            data.ClientObject.Queue(new MediusUpdateLadderStatsWideResponse()
                            {
                                MessageID = updateLadderStatsWideRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                            break;
                        }

                        switch (updateLadderStatsWideRequest.LadderType)
                        {
                            case MediusLadderType.MediusLadderTypePlayer:
                                {

                                    await ServerConfiguration.Database.PostAccountLadderStats(new StatPostDTO()
                                    {
                                        AccountId = data.ClientObject.AccountId,
                                        Stats = pluginMessage.WideStats
                                    }).ContinueWith((r) =>
                                    {
                                        if (data == null && data.ClientObject == null && !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result)
                                        {
                                            data.ClientObject.WideStats = pluginMessage.WideStats;
                                            data.ClientObject.Queue(new MediusUpdateLadderStatsWideResponse()
                                            {
                                                MessageID = updateLadderStatsWideRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusUpdateLadderStatsWideResponse()
                                            {
                                                MessageID = updateLadderStatsWideRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusDBError
                                            });
                                        }
                                    });
                                    break;
                                }
                            case MediusLadderType.MediusLadderTypeClan:
                                {
                                    await ServerConfiguration.Database.PostClanLadderStats(data.ClientObject.AccountId,
                                        data.ClientObject.ClanId,
                                        pluginMessage.WideStats,
                                        data.ClientObject.ApplicationId)
                                    .ContinueWith((r) =>
                                    {
                                        if (data == null && data.ClientObject == null && !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result)
                                        {
                                            data.ClientObject.WideStats = pluginMessage.WideStats;
                                            data.ClientObject.Queue(new MediusUpdateLadderStatsWideResponse()
                                            {
                                                MessageID = updateLadderStatsWideRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusUpdateLadderStatsWideResponse()
                                            {
                                                MessageID = updateLadderStatsWideRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusDBError
                                            });
                                        }
                                    });
                                    break;
                                }
                            default:
                                {
                                    ServerConfiguration.LogWarn($"Unhandled MediusUpdateLadderStatsWideRequest {updateLadderStatsWideRequest}");
                                    break;
                                }
                        }

                        break;
                    }


                case MediusGetLadderStatsRequest getLadderStatsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLadderStatsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLadderStatsRequest} without being logged in.");

                        switch (getLadderStatsRequest.LadderType)
                        {
                            case MediusLadderType.MediusLadderTypePlayer:
                                {
                                    await ServerConfiguration.Database.GetAccountById(getLadderStatsRequest.AccountID_or_ClanID).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result != null)
                                        {
                                            data.ClientObject.Queue(new MediusGetLadderStatsResponse()
                                            {
                                                MessageID = getLadderStatsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                Stats = Array.ConvertAll(r.Result.AccountStats, Convert.ToInt32)
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusGetLadderStatsResponse()
                                            {
                                                MessageID = getLadderStatsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusNoResult
                                            });
                                        }
                                    });
                                    break;
                                }
                            case MediusLadderType.MediusLadderTypeClan:
                                {
                                    await ServerConfiguration.Database.GetClanById(getLadderStatsRequest.AccountID_or_ClanID,
                                        data.ClientObject.ApplicationId)
                                    .ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result != null)
                                        {
                                            data.ClientObject.Queue(new MediusGetLadderStatsResponse()
                                            {
                                                MessageID = getLadderStatsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                Stats = r.Result.ClanWideStats
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusGetLadderStatsResponse()
                                            {
                                                MessageID = getLadderStatsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusNoResult
                                            });
                                        }
                                    });
                                    break;
                                }
                            default:
                                {
                                    ServerConfiguration.LogWarn($"Unhandled MediusGetLadderStatsRequest {getLadderStatsRequest}");
                                    break;
                                }
                        }

                        break;
                    }

                case MediusGetLadderStatsWideRequest getLadderStatsWideRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLadderStatsWideRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLadderStatsWideRequest} without being logged in.");

                        switch (getLadderStatsWideRequest.LadderType)
                        {
                            case MediusLadderType.MediusLadderTypePlayer:
                                {
                                    await ServerConfiguration.Database.GetAccountById(getLadderStatsWideRequest.AccountID_or_ClanID).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result != null)
                                        {
                                            data.ClientObject.Queue(new MediusGetLadderStatsWideResponse()
                                            {
                                                MessageID = getLadderStatsWideRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                AccountID_or_ClanID = r.Result.AccountId,
                                                Stats = r.Result.AccountWideStats
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusGetLadderStatsWideResponse()
                                            {
                                                MessageID = getLadderStatsWideRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusDBError
                                            });
                                        }
                                    });
                                    break;
                                }
                            case MediusLadderType.MediusLadderTypeClan:
                                {
                                    await ServerConfiguration.Database.GetClanById(getLadderStatsWideRequest.AccountID_or_ClanID,
                                        data.ClientObject.ApplicationId)
                                    .ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result != null)
                                        {
                                            data.ClientObject.Queue(new MediusGetLadderStatsWideResponse()
                                            {
                                                MessageID = getLadderStatsWideRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                AccountID_or_ClanID = r.Result.ClanId,
                                                Stats = r.Result.ClanWideStats
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusGetLadderStatsWideResponse()
                                            {
                                                MessageID = getLadderStatsWideRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusDBError
                                            });
                                        }
                                    });
                                    break;
                                }
                            default:
                                {
                                    ServerConfiguration.LogWarn($"Unhandled MediusGetLadderStatsWideRequest {getLadderStatsWideRequest}");
                                    break;
                                }
                        }
                        break;
                    }

                case MediusLadderListRequest ladderListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderListRequest} without being logged in.");


                        //
                        await ServerConfiguration.Database.GetLeaderboardList(ladderListRequest.StartPosition - 1, ladderListRequest.PageSize, data.ApplicationId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                var responses = new List<MediusLadderListResponse>(r.Result.Length);
                                foreach (var ladderEntry in r.Result)
                                {
                                    byte[] mediusStats = new byte[Constants.ACCOUNTSTATS_MAXLEN];
                                    try { var dbAccStats = Convert.FromBase64String(ladderEntry.MediusStats ?? ""); mediusStats = dbAccStats; } catch (Exception) { }
                                    responses.Add(new MediusLadderListResponse()
                                    {
                                        MessageID = ladderListRequest.MessageID,
                                        AccountID = ladderEntry.AccountId,
                                        AccountName = ladderEntry.AccountName,
                                        LadderPosition = (uint)(ladderEntry.Index + 1),
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        EndOfList = false
                                    });
                                }

                                if (responses.Count > 0)
                                {
                                    // Flag last item as EndOfList
                                    responses[responses.Count - 1].EndOfList = true;

                                    data.ClientObject.Queue(responses);
                                }
                                else
                                {
                                    data.ClientObject.Queue(new MediusLadderListResponse()
                                    {
                                        MessageID = ladderListRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusNoResult,
                                        EndOfList = true
                                    });
                                }
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusLadderListResponse()
                                {
                                    MessageID = ladderListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError,
                                    EndOfList = true
                                });
                            }
                        });
                        break;
                    }

                case MediusLadderList_ExtraInfoRequest0 ladderList_ExtraInfoRequest0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderList_ExtraInfoRequest0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderList_ExtraInfoRequest0} without being logged in.");

                        //Doing SOCOM style LadderList request, AppID %d

                        await ServerConfiguration.Database.GetLeaderboard(ladderList_ExtraInfoRequest0.LadderStatIndex + 1, (int)ladderList_ExtraInfoRequest0.StartPosition - 1, (int)ladderList_ExtraInfoRequest0.PageSize, data.ApplicationId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                var responses = new List<MediusLadderList_ExtraInfoResponse>(r.Result.Length);
                                foreach (var ladderEntry in r.Result)
                                {
                                    byte[] mediusStats = new byte[Constants.ACCOUNTSTATS_MAXLEN];
                                    try { var dbAccStats = Convert.FromBase64String(ladderEntry.MediusStats ?? ""); mediusStats = dbAccStats; } catch (Exception) { }
                                    var account = MediusClass.Manager.GetClientByAccountId(ladderEntry.AccountId, data.ClientObject.ApplicationId);
                                    responses.Add(new MediusLadderList_ExtraInfoResponse()
                                    {
                                        MessageID = ladderList_ExtraInfoRequest0.MessageID,
                                        AccountID = ladderEntry.AccountId,
                                        AccountName = ladderEntry.AccountName,
                                        AccountStats = mediusStats,
                                        LadderPosition = (uint)(ladderEntry.Index + 1),
                                        LadderStat = ladderEntry.StatValue,
                                        OnlineState = new MediusPlayerOnlineState()
                                        {
                                            ConnectStatus = account?.PlayerStatus ?? MediusPlayerStatus.MediusPlayerDisconnected,
                                            GameName = account?.CurrentGame?.GameName,
                                            LobbyName = account?.CurrentChannel?.Name ?? "",
                                            MediusGameWorldID = account?.CurrentGame?.Id ?? -1,
                                            MediusLobbyWorldID = account?.CurrentChannel?.Id ?? -1
                                        },
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        EndOfList = false
                                    });
                                }

                                if (responses.Count > 0)
                                {
                                    // Flag last item as EndOfList
                                    responses[responses.Count - 1].EndOfList = true;

                                    data.ClientObject.Queue(responses);
                                }
                                else
                                {
                                    ServerConfiguration.LogInfo("GetLadderListRequest_ExtraInfo - no result");
                                    data.ClientObject.Queue(new MediusLadderList_ExtraInfoResponse()
                                    {
                                        MessageID = ladderList_ExtraInfoRequest0.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusNoResult,
                                        EndOfList = true
                                    });
                                }
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusLadderList_ExtraInfoResponse()
                                {
                                    MessageID = ladderList_ExtraInfoRequest0.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError,
                                    EndOfList = true
                                });
                            }
                        });
                        break;
                    }

                case MediusLadderList_ExtraInfoRequest ladderList_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderList_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderList_ExtraInfoRequest} without being logged in.");

                        //Doing SOCOM style LadderList request, AppID %d

                        //
                        await ServerConfiguration.Database.GetLeaderboard(ladderList_ExtraInfoRequest.LadderStatIndex + 1, (int)ladderList_ExtraInfoRequest.StartPosition - 1, (int)ladderList_ExtraInfoRequest.PageSize, data.ApplicationId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                var responses = new List<MediusLadderList_ExtraInfoResponse>(r.Result.Length);
                                foreach (var ladderEntry in r.Result)
                                {
                                    byte[] mediusStats = new byte[Constants.ACCOUNTSTATS_MAXLEN];
                                    try { var dbAccStats = Convert.FromBase64String(ladderEntry.MediusStats ?? ""); mediusStats = dbAccStats; } catch (Exception) { }
                                    responses.Add(new MediusLadderList_ExtraInfoResponse()
                                    {
                                        MessageID = ladderList_ExtraInfoRequest.MessageID,
                                        AccountID = ladderEntry.AccountId,
                                        AccountName = ladderEntry.AccountName,
                                        AccountStats = mediusStats,
                                        LadderPosition = (uint)(ladderEntry.Index + 1),
                                        LadderStat = ladderEntry.StatValue,
                                        OnlineState = new MediusPlayerOnlineState()
                                        {
                                            ConnectStatus = MediusPlayerStatus.MediusPlayerDisconnected,
                                            MediusGameWorldID = -1,
                                            MediusLobbyWorldID = -1,
                                            LobbyName = null,
                                            GameName = null
                                        },
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        EndOfList = false
                                    });
                                }

                                if (responses.Count > 0)
                                {
                                    // Flag last item as EndOfList
                                    responses[responses.Count - 1].EndOfList = true;

                                    data.ClientObject.Queue(responses);
                                }
                                else
                                {
                                    ServerConfiguration.LogInfo("GetLadderListRequest_ExtraInfo - no result");
                                    data.ClientObject.Queue(new MediusLadderList_ExtraInfoResponse()
                                    {
                                        MessageID = ladderList_ExtraInfoRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusNoResult,
                                        OnlineState = new MediusPlayerOnlineState()
                                        {
                                            ConnectStatus = MediusPlayerStatus.MediusPlayerDisconnected,
                                            MediusGameWorldID = -1,
                                            MediusLobbyWorldID = -1,
                                            LobbyName = null,
                                            GameName = null
                                        },
                                        EndOfList = true
                                    });
                                }
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusLadderList_ExtraInfoResponse()
                                {
                                    MessageID = ladderList_ExtraInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError,
                                    OnlineState = new MediusPlayerOnlineState()
                                    {
                                        ConnectStatus = MediusPlayerStatus.MediusPlayerDisconnected,
                                        MediusGameWorldID = -1,
                                        MediusLobbyWorldID = -1,
                                        LobbyName = null,
                                        GameName = null
                                    },
                                    EndOfList = true
                                });
                            }
                        });
                        break;
                    }

                case MediusGetTotalUsersRequest getTotalUsersRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalUsersRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalUsersRequest} without being logged in.");

                        var channel = data.ClientObject.CurrentChannel;
                        if (channel == null)
                        {
                            data.ClientObject.Queue(new MediusGetTotalUsersResponse()
                            {
                                MessageID = getTotalUsersRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusWMError
                            });
                        }
                        else
                        {
                            // Success
                            data.ClientObject.Queue(new MediusGetTotalUsersResponse()
                            {
                                MessageID = getTotalUsersRequest.MessageID,
                                TotalInSystem = (uint)channel.Clients.Count(),
                                TotalInGame = (uint)channel.GameCount,
                                StatusCode = MediusCallbackStatus.MediusSuccess
                            });
                        }
                        break;
                    }

                case MediusGetTotalRankingsRequest getTotalRankingsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalRankingsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalRankingsRequest} without being logged in.");

                        // Process
                        switch (getTotalRankingsRequest.LadderType)
                        {
                            case MediusLadderType.MediusLadderTypeClan:
                                {
                                    await ServerConfiguration.Database.GetActiveClanCountByAppId(data.ClientObject.ApplicationId).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result.HasValue)
                                        {
                                            data.ClientObject.Queue(new MediusGetTotalRankingsResponse()
                                            {
                                                MessageID = getTotalRankingsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                TotalRankings = (uint)r.Result.Value
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusGetTotalRankingsResponse()
                                            {
                                                MessageID = getTotalRankingsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusDBError
                                            });
                                        }
                                    });
                                    break;
                                }
                            case MediusLadderType.MediusLadderTypePlayer:
                                {
                                    await ServerConfiguration.Database.GetActiveAccountCountByAppId(data.ClientObject.ApplicationId).ContinueWith((r) =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result.HasValue)
                                        {
                                            data.ClientObject.Queue(new MediusGetTotalRankingsResponse()
                                            {
                                                MessageID = getTotalRankingsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                TotalRankings = (uint)r.Result.Value
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new MediusGetTotalRankingsResponse()
                                            {
                                                MessageID = getTotalRankingsRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusDBError
                                            });
                                        }
                                    });
                                    break;
                                }
                        }
                        break;
                    }

                // For Legacy titles
                case MediusLadderPositionRequest ladderPositionRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderPositionRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderPositionRequest} without being logged in.");


                        await ServerConfiguration.Database.GetPlayerLeaderboard(ladderPositionRequest.AccountID).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                data.ClientObject.Queue(new MediusLadderPositionResponse()
                                {
                                    MessageID = ladderPositionRequest.MessageID,
                                    LadderPosition = (uint)r.Result.Index + 1,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusLadderPositionResponse()
                                {
                                    MessageID = ladderPositionRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult
                                });
                            }
                        });
                        break;
                    }

                case MediusLadderPosition_ExtraInfoRequest ladderPosition_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderPosition_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {ladderPosition_ExtraInfoRequest} without being logged in.");


                        await ServerConfiguration.Database.GetPlayerLeaderboardIndex(ladderPosition_ExtraInfoRequest.AccountID, ladderPosition_ExtraInfoRequest.LadderStatIndex + 1, data.ClientObject.ApplicationId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                data.ClientObject.Queue(new MediusLadderPosition_ExtraInfoResponse()
                                {
                                    MessageID = ladderPosition_ExtraInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    LadderPosition = (uint)r.Result.Index + 1,
                                    TotalRankings = (uint)r.Result.TotalRankedAccounts,
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusLadderPosition_ExtraInfoResponse()
                                {
                                    MessageID = ladderPosition_ExtraInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult
                                });
                            }
                        });
                        break;
                    }

                case MediusClanLadderListRequest clanLadderListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {clanLadderListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {clanLadderListRequest} without being logged in.");

                        await ServerConfiguration.Database.GetClanLeaderboard(clanLadderListRequest.ClanLadderStatIndex, (int)clanLadderListRequest.StartPosition - 1, (int)clanLadderListRequest.PageSize, data.ApplicationId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                var responses = new List<MediusClanLadderListResponse>(r.Result.Length);
                                foreach (var ladderEntry in r.Result)
                                {
                                    byte[] mediusStats = new byte[Constants.ACCOUNTSTATS_MAXLEN];
                                    try { var dbAccStats = Convert.FromBase64String(ladderEntry.MediusStats ?? ""); mediusStats = dbAccStats; } catch (Exception) { }
                                    responses.Add(new MediusClanLadderListResponse()
                                    {
                                        MessageID = clanLadderListRequest.MessageID,
                                        ClanID = ladderEntry.ClanId,
                                        ClanName = ladderEntry.ClanName,
                                        LadderPosition = (uint)(ladderEntry.Index + 1),
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        EndOfList = false
                                    });
                                }

                                if (responses.Count > 0)
                                {
                                    // Flag last item as EndOfList
                                    responses[responses.Count - 1].EndOfList = true;

                                    data.ClientObject.Queue(responses);
                                }
                                else
                                {
                                    data.ClientObject.Queue(new MediusClanLadderListResponse()
                                    {
                                        MessageID = clanLadderListRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusNoResult,
                                        EndOfList = true
                                    });
                                }
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusClanLadderListResponse()
                                {
                                    MessageID = clanLadderListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError,
                                    EndOfList = true
                                });
                            }
                        });

                        break;
                    }

                #endregion

                #region Player Info

                case MediusFindPlayerRequest findPlayerRequest:
                    {
                        ClientObject foundPlayer = null;

                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {findPlayerRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {findPlayerRequest} without being logged in.");

                        if (findPlayerRequest.SearchType == MediusPlayerSearchType.PlayerAccountID)
                            foundPlayer = MediusClass.Manager.GetClientByAccountId(findPlayerRequest.ID, data.ClientObject.ApplicationId);
                        else
                            foundPlayer = MediusClass.Manager.GetClientByAccountName(findPlayerRequest.Name, data.ClientObject.ApplicationId);

                        if (foundPlayer == null || !foundPlayer.IsLoggedIn)
                        {
                            data.ClientObject.Queue(new MediusFindPlayerResponse()
                            {
                                MessageID = findPlayerRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                AccountID = findPlayerRequest.ID,
                                AccountName = findPlayerRequest.Name,
                                EndOfList = true
                            });
                        }
                        else
                        {
                            var appIds = ServerConfiguration.Database.GetAppIds();
                            var appIdList = appIds.Result.ToList();
                            string appName = null;

                            foreach (var AppId in appIdList)
                            {
                                if (AppId.AppIds.Contains(data.ClientObject.ApplicationId))
                                    appName = AppId.Name;
                            };

                            data.ClientObject.Queue(new MediusFindPlayerResponse()
                            {
                                MessageID = findPlayerRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                ApplicationID = data.ApplicationId,
                                AccountID = foundPlayer.AccountId,
                                AccountName = foundPlayer.AccountName,
                                ApplicationType = (foundPlayer.PlayerStatus == MediusPlayerStatus.MediusPlayerInGameWorld) ? MediusApplicationType.MediusAppTypeGame : MediusApplicationType.LobbyChatChannel,
                                ApplicationName = appName,
                                MediusWorldID = (foundPlayer.PlayerStatus == MediusPlayerStatus.MediusPlayerInGameWorld) ? foundPlayer.CurrentGame?.Id ?? -1 : foundPlayer.CurrentChannel?.Id ?? -1,
                                EndOfList = true
                            });

                            /*
                            
                            List<MediusFindPlayerResponse> responses = new List<MediusFindPlayerResponse>();

                            responses.Select(x => new MediusFindPlayerResponse()
                            {
                                MessageID = findPlayerRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                ApplicationID = data.ApplicationId,
                                AccountID = foundPlayer.AccountId,
                                AccountName = foundPlayer.AccountName,
                                ApplicationType = (foundPlayer.PlayerStatus == MediusPlayerStatus.MediusPlayerInGameWorld) ? MediusApplicationType.MediusAppTypeGame : MediusApplicationType.LobbyChatChannel,
                                ApplicationName = appName,
                                MediusWorldID = (foundPlayer.PlayerStatus == MediusPlayerStatus.MediusPlayerInGameWorld) ? foundPlayer.CurrentGame?.Id ?? -1 : foundPlayer.CurrentChannel?.Id ?? -1,
                                EndOfList = false
                            }).ToList();

                            if (responses.Count > 0)
                            {
                                // Flag last item as EndOfList
                                responses[responses.Count - 1].EndOfList = true;

                                //
                                data.ClientObject.Queue(responses);
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusFindPlayerResponse()
                                {
                                    MessageID = findPlayerRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    ApplicationID = data.ClientObject.ApplicationId,
                                    EndOfList = true
                                });
                            }
                            */
                        }
                        break;
                    }

                case MediusPlayerInfoRequest playerInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {playerInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {playerInfoRequest} without being logged in.");

                        if (playerInfoRequest.AccountID == 0)
                        {
                            ServerConfiguration.LogInfo($"playerInfo accountId is 0!!");

                            data.ClientObject.Queue(new MediusPlayerInfoResponse()
                            {
                                MessageID = playerInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusAccountNotFound,
                                AccountName = null,
                                ApplicationID = data.ApplicationId,
                                PlayerStatus = MediusPlayerStatus.MediusPlayerDisconnected,
                                ConnectionClass = MediusConnectionType.Modem,
                                Stats = null
                            });

                            ServerConfiguration.LogInfo($"playerInfo response sent");
                        }
                        else
                        {

                            await ServerConfiguration.Database.GetAccountById(playerInfoRequest.AccountID).ContinueWith((r) =>
                            {
                                if (r.IsCompletedSuccessfully && r.Result != null)
                                {
                                    byte[] mediusStats = new byte[Constants.ACCOUNTSTATS_MAXLEN];
                                    try { var dbAccStats = Convert.FromBase64String(r.Result.MediusStats ?? ""); mediusStats = dbAccStats; } catch (Exception) { }
                                    var playerClientObject = MediusClass.Manager.GetClientByAccountId(r.Result.AccountId, data.ClientObject.ApplicationId);
                                    data?.ClientObject?.Queue(new MediusPlayerInfoResponse()
                                    {
                                        MessageID = playerInfoRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        AccountName = r.Result.AccountName,
                                        ApplicationID = data.ApplicationId,
                                        PlayerStatus = (playerClientObject != null && playerClientObject.IsLoggedIn) ? playerClientObject.PlayerStatus : MediusPlayerStatus.MediusPlayerDisconnected,
                                        ConnectionClass = data.ClientObject.MediusConnectionType,
                                        Stats = mediusStats
                                    });

                                    ServerConfiguration.LogInfo($"playerInfo accountId found!!");
                                }
                                else
                                {

                                    ServerConfiguration.LogInfo($"playerInfo accountId not found!!");

                                    data?.ClientObject?.Queue(new MediusPlayerInfoResponse()
                                    {
                                        MessageID = playerInfoRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        AccountName = null,
                                        ApplicationID = data.ApplicationId,
                                        PlayerStatus = MediusPlayerStatus.MediusPlayerDisconnected,
                                        ConnectionClass = MediusConnectionType.Modem,
                                        Stats = null
                                    });
                                }
                            });
                        }


                        break;
                    }

                case MediusUpdateUserState updateUserState:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateUserState} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateUserState} without being logged in.");

                        switch (updateUserState.UserAction)
                        {
                            //108
                            case MediusUserAction.KeepAlive:
                                {
                                    data.ClientObject.KeepAliveUntilNextConnection();
                                    break;
                                }
                            case MediusUserAction.JoinedChatWorld:
                                {
                                    ServerConfiguration.LogInfo($"Successfully JoinedChatWorld {data.ClientObject.AccountId}:{data.ClientObject.AccountName}");
                                    //await data.ClientObject.JoinChannel(MediusClass.Manager.GetOrCreateDefaultLobbyChannel(data.ApplicationId));
                                    break;
                                }
                            case MediusUserAction.LeftGameWorld:
                                {
                                    await data.ClientObject.LeaveGame(data.ClientObject.CurrentGame);

                                    ServerConfiguration.LogInfo($"Successfully LeftGameWorld {data.ClientObject.AccountId}:{data.ClientObject.AccountName}");
                                    MediusClass.AntiCheatPlugin.mc_anticheat_event_msg_UPDATEUSERSTATE(AnticheatEventCode.anticheatLEAVEGAME, data.ClientObject.WorldId, data.ClientObject.AccountId, MediusClass.AntiCheatClient, updateUserState, 256);
                                    break;
                                }
                            case MediusUserAction.LeftPartyWorld:
                                {
                                    await data.ClientObject.LeaveParty(data.ClientObject.CurrentParty);
                                    ServerConfiguration.LogInfo($"Successfully LeftPartyWorld {data.ClientObject.AccountId}:{data.ClientObject.AccountName}");
                                    break;
                                }
                        }

                        break;
                    }

                #endregion

                #region Clan

                #region CreateClan
                case MediusCreateClanRequest createClanRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createClanRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createClanRequest} without being logged in.");

                        // validate name
                        if (!MediusClass.PassTextFilter(data.ClientObject.ApplicationId, TextFilterContext.CLAN_NAME, createClanRequest.ClanName))
                        {
                            data.ClientObject.Queue(new MediusCreateClanResponse()
                            {
                                MessageID = createClanRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                            return;
                        }

                        if (data.ClientObject.ClanId == null)
                        {
                            await ServerConfiguration.Database.CreateClan(data.ClientObject.AccountId, createClanRequest.ClanName, data.ClientObject.ApplicationId, Convert.ToBase64String(new byte[Constants.CLANSTATS_MAXLEN])).ContinueWith((r) =>
                            {
                                if (r.IsCompletedSuccessfully && r.Result != null)
                                {
                                    // Reply with account id
                                    data.ClientObject.Queue(new MediusCreateClanResponse()
                                    {
                                        MessageID = createClanRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        ClanID = r.Result.ClanId
                                    });
                                    data.ClientObject.ClanId = r.Result.ClanId;
                                }
                                else
                                {
                                    // Reply error
                                    data.ClientObject.Queue(new MediusCreateClanResponse()
                                    {
                                        MessageID = createClanRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusClanNameInUse
                                    });
                                }
                            });
                        }
                        else
                        {
                            // If already leader of a Clan, don't make another! 
                            data.ClientObject.Queue(new MediusCreateClanResponse()
                            {
                                MessageID = createClanRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusAlreadyLeaderOfClan
                            });
                        }
                        break;
                    }
                #endregion

                case MediusClanRenameRequest clanRenameRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {clanRenameRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {clanRenameRequest} without being logged in.");

                        // ERROR -- Need clan and EnableNonClanLeaderToGetTeamChallenges set to false (or true for all memebrs to view challenges!)
                        if (!data.ClientObject.ClanId.HasValue)
                        {
                            ServerConfiguration.LogWarn("Not leader of clan. -  Only a clan leader can get Clan Team Challenges.");
                            data.ClientObject.Queue(new MediusStatusResponse()
                            {
                                Class = clanRenameRequest.PacketClass,
                                Type = 0x7D,
                                MessageID = clanRenameRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNotClanLeader,
                            });
                        }

                        //dummy
                        data.ClientObject.Queue(new MediusStatusResponse()
                        {
                            Class = clanRenameRequest.PacketClass,
                            Type = 0x7D,
                            MessageID = clanRenameRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });

                        /*
                        await ServerConfiguration.Database.EditClan(data.ClientObject.AccountId,
                            data.ClientObject.ClanId.Value,
                            0,
                            1,
                            data.ClientObject.ApplicationId)
                        .ContinueWith((r) =>
                        {
                            List<MediusGetMyClanMessagesResponse> responses = new List<MediusGetMyClanMessagesResponse>();
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                responses.AddRange(r.Result
                                    .Select(x => new MediusGetMyClanMessagesResponse()
                                    {
                                        MessageID = getMyClanMessagesRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        Message = x.Message,
                                        ClanID = data.ClientObject.ClanId.Value
                                    }))
                                    ;
                            }

                            if (responses.Count == 0)
                            {
                                responses.Add(new MediusGetMyClanMessagesResponse()
                                {
                                    MessageID = getMyClanMessagesRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    ClanID = data.ClientObject.ClanId.Value,
                                    EndOfList = true
                                });
                            }

                            responses[responses.Count - 1].EndOfList = true;
                            data.ClientObject.Queue(responses);
                        });
                        */
                        break;
                    }

                #region CheckMyClanInvitations
                case MediusCheckMyClanInvitationsRequest checkMyClanInvitationsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {checkMyClanInvitationsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {checkMyClanInvitationsRequest} without being logged in.");

                        await ServerConfiguration.Database.GetClanInvitationsByAccount(data.ClientObject.AccountId).ContinueWith(r =>
                        {
                            List<MediusCheckMyClanInvitationsResponse> responses = new List<MediusCheckMyClanInvitationsResponse>();
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                responses.AddRange(r.Result
                                    .Where(x => x.Invitation.ResponseStatus == 0) // only return undecided
                                    .Skip((checkMyClanInvitationsRequest.Start - 1) * checkMyClanInvitationsRequest.PageSize)
                                    .Take(checkMyClanInvitationsRequest.PageSize)
                                    .Select(x => new MediusCheckMyClanInvitationsResponse()
                                    {
                                        MessageID = checkMyClanInvitationsRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        ClanID = x.Invitation.ClanId,
                                        ClanInvitationID = x.Invitation.InvitationId,
                                        LeaderAccountID = x.LeaderAccountId,
                                        LeaderAccountName = x.LeaderAccountName,
                                        Message = x.Invitation.Message,
                                        ResponseStatus = (MediusClanInvitationsResponseStatus)x.Invitation.ResponseStatus
                                    }));
                            }

                            if (responses.Count == 0)
                            {
                                responses.Add(new MediusCheckMyClanInvitationsResponse()
                                {
                                    MessageID = checkMyClanInvitationsRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }

                            responses[responses.Count - 1].EndOfList = true;
                            data.ClientObject.Queue(responses);
                        });

                        break;
                    }
                #endregion

                #region RemovePlayerFromClan
                case MediusRemovePlayerFromClanRequest removePlayerFromClanRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removePlayerFromClanRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {removePlayerFromClanRequest} without being logged in.");

                        if (!data.ClientObject.ClanId.HasValue)
                        {
                            data.ClientObject.Queue(new MediusRemovePlayerFromClanResponse()
                            {
                                MessageID = removePlayerFromClanRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNotClanMember
                            });
                        }
                        else
                        {
                            await ServerConfiguration.Database.ClanLeave(data.ClientObject.AccountId,
                                removePlayerFromClanRequest.ClanID,
                                removePlayerFromClanRequest.PlayerAccountID,
                                data.ClientObject.ApplicationId)
                            .ContinueWith(r =>
                            {
                                if (r.IsCompletedSuccessfully && r.Result)
                                {
                                    data.ClientObject.Queue(new MediusRemovePlayerFromClanResponse()
                                    {
                                        MessageID = removePlayerFromClanRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                    });

                                    var clan = ServerConfiguration.Database.GetClanById(removePlayerFromClanRequest.ClanID,
                                        data.ClientObject.ApplicationId);

                                    QueueClanKickMessage(data, $"You have been kicked from clan {clan.Result.ClanName} | {clan.Result.ClanLeaderAccount}");
                                }
                                else
                                {
                                    data.ClientObject.Queue(new MediusRemovePlayerFromClanResponse()
                                    {
                                        MessageID = removePlayerFromClanRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusFail,
                                    });
                                }
                            });
                        }

                        break;
                    }
                #endregion

                case MediusGetMyClansRequest getMyClansRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyClansRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyClansRequest} without being logged in.");

                        await data.ClientObject.RefreshAccount().ContinueWith(t =>
                        {
                            if (!data.ClientObject.ClanId.HasValue)
                            {
                                byte[] Stats = new byte[Constants.CLANSTATS_MAXLEN];
                                data.ClientObject.Queue(new MediusGetMyClansResponse()
                                {
                                    MessageID = getMyClansRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    ClanID = -1,
                                    ApplicationID = data.ApplicationId,
                                    ClanName = "",
                                    LeaderAccountID = -1,
                                    LeaderAccountName = "",
                                    Stats = Stats,
                                    Status = MediusClanStatus.ClanDisbanded,
                                    EndOfList = true
                                });
                            }
                            else
                            {
                                _ = ServerConfiguration.Database.GetClans(data.ClientObject.ApplicationId).ContinueWith(r =>
                                {
                                    List<MediusGetMyClansResponse> responses = new List<MediusGetMyClansResponse>();
                                    if (r.IsCompletedSuccessfully && r.Result != null)
                                    {
                                        foreach (var clanIamIn in r.Result)
                                        {
                                            byte[] clanStats = new byte[Constants.CLANSTATS_MAXLEN];
                                            try { var dbAccStats = Convert.FromBase64String(clanIamIn.ClanStats.ToString() ?? ""); clanStats = dbAccStats; } catch (Exception) { }
                                            responses.AddRange(clanIamIn.ClanMemberAccounts
                                            .Where(x => x.AccountId == data.ClientObject.AccountId)
                                            .Skip((getMyClansRequest.Start - 1) * getMyClansRequest.PageSize)
                                            .Take(getMyClansRequest.PageSize)
                                            .Select(x => new MediusGetMyClansResponse()
                                            {
                                                MessageID = getMyClansRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                ClanID = clanIamIn.ClanId,
                                                ApplicationID = clanIamIn.AppId,
                                                ClanName = clanIamIn.ClanName,
                                                LeaderAccountID = clanIamIn.ClanLeaderAccount.AccountId,
                                                LeaderAccountName = clanIamIn.ClanLeaderAccount.AccountName,
                                                Stats = clanStats,
                                                Status = clanIamIn.IsDisbanded ? MediusClanStatus.ClanDisbanded : MediusClanStatus.ClanActive,
                                                EndOfList = false
                                            }));
                                        }

                                        /*
                                        responses.AddRange(r.Result.FirstOrDefault().ClanMemberAccounts
                                            .Where(x => x.AccountId == data.ClientObject.AccountId)
                                            .Skip((getMyClansRequest.Start - 1) * getMyClansRequest.PageSize)
                                            .Take(getMyClansRequest.PageSize)
                                            .Select(x => new MediusGetMyClansResponse()
                                            {
                                                MessageID = getMyClansRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                ClanID = r.Result.FirstOrDefault().ClanId,
                                                ApplicationID = r.Result.FirstOrDefault().AppId,
                                                ClanName = r.Result.FirstOrDefault().ClanName,
                                                LeaderAccountID = r.Result.FirstOrDefault().ClanLeaderAccount.AccountId,
                                                LeaderAccountName = r.Result.FirstOrDefault().ClanLeaderAccount.AccountName,
                                                Stats = r.Result.FirstOrDefault().ClanMediusStats,
                                                Status = r.Result.FirstOrDefault().IsDisbanded ? MediusClanStatus.ClanDisbanded : MediusClanStatus.ClanActive,
                                                EndOfList = false
                                            }));
                                        */
                                    }

                                    if (responses.Count == 0)
                                    {
                                        responses.Add(new MediusGetMyClansResponse()
                                        {
                                            MessageID = getMyClansRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            EndOfList = true
                                        });
                                    }

                                    responses[responses.Count - 1].EndOfList = true;
                                    data.ClientObject.Queue(responses);
                                });
                            }
                        });

                        break;
                    }

                case MediusGetClanMemberListRequest getClanMemberListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanMemberListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanMemberListRequest} without being logged in.");

                        await ServerConfiguration.Database.GetClanById(getClanMemberListRequest.ClanID,
                            data.ClientObject.ApplicationId)
                        .ContinueWith(r =>
                        {
                            List<MediusGetClanMemberListResponse> responses = new List<MediusGetClanMemberListResponse>();
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                responses.AddRange(r.Result.ClanMemberAccounts.Select(x =>
                                {
                                    var account = MediusClass.Manager.GetClientByAccountId(x.AccountId, data.ClientObject.ApplicationId);
                                    return new MediusGetClanMemberListResponse()
                                    {
                                        MessageID = getClanMemberListRequest.MessageID,
                                        AccountID = x.AccountId,
                                        AccountName = x.AccountName,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        EndOfList = false
                                    };
                                }));
                            }

                            if (responses.Count == 0)
                            {
                                responses.Add(new MediusGetClanMemberListResponse()
                                {
                                    MessageID = getClanMemberListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusClanNotFound,
                                    EndOfList = true
                                });
                            }

                            responses[responses.Count - 1].EndOfList = true;
                            data.ClientObject.Queue(responses);
                        });

                        break;
                    }

                case MediusGetClanMemberList_ExtraInfoRequest getClanMemberList_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanMemberList_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanMemberList_ExtraInfoRequest} without being logged in.");

                        await ServerConfiguration.Database.GetClanById(getClanMemberList_ExtraInfoRequest.ClanID,
                            data.ClientObject.ApplicationId)
                        .ContinueWith(r =>
                        {
                            List<MediusGetClanMemberList_ExtraInfoResponse> responses = new List<MediusGetClanMemberList_ExtraInfoResponse>();
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                responses.AddRange(r.Result.ClanMemberAccounts.Select(x =>
                                {
                                    var account = MediusClass.Manager.GetClientByAccountId(x.AccountId, data.ClientObject.ApplicationId);
                                    var cachedAccount = ServerConfiguration.Database.GetAccountById(x.AccountId);
                                    byte[] mediusStats = new byte[Constants.ACCOUNTSTATS_MAXLEN];
                                    try { var dbAccStats = Convert.FromBase64String(cachedAccount.Result.MediusStats ?? ""); mediusStats = dbAccStats; } catch (Exception) { }
                                    return new MediusGetClanMemberList_ExtraInfoResponse()
                                    {
                                        MessageID = getClanMemberList_ExtraInfoRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        AccountID = x.AccountId,
                                        AccountName = x.AccountName,
                                        Stats = mediusStats,
                                        OnlineState = new MediusPlayerOnlineState()
                                        {
                                            ConnectStatus = account?.PlayerStatus ?? MediusPlayerStatus.MediusPlayerDisconnected,
                                            GameName = account?.CurrentGame?.GameName,
                                            LobbyName = account?.CurrentChannel?.Name ?? "",
                                            MediusGameWorldID = account?.CurrentGame?.Id ?? -1,
                                            MediusLobbyWorldID = account?.CurrentChannel?.Id ?? -1
                                        },
                                        LadderStat = getClanMemberList_ExtraInfoRequest.LadderStatIndex + 1,
                                        LadderPosition = (uint)r.Result.ClanMemberAccounts.FindIndex(x => x == cachedAccount.Result),
                                        TotalRankings = (uint)r.Result.ClanMemberAccounts.Count(),
                                        EndOfList = false
                                    };
                                }));

                                if (ServerConfiguration.Database._settings.SimulatedMode == true)
                                    ServerConfiguration.LogInfo($"GetClanMemberListRequest_ExtraInfo (simulated) success --- {responses.Count} rows returned");

                                if (responses.Count == 0)
                                {
                                    responses.Add(new MediusGetClanMemberList_ExtraInfoResponse()
                                    {
                                        MessageID = getClanMemberList_ExtraInfoRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusClanNotFound,
                                        EndOfList = true
                                    });
                                }

                                responses[responses.Count - 1].EndOfList = true;
                                data.ClientObject.Queue(responses);
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGetClanMemberList_ExtraInfoResponse()
                                {
                                    MessageID = getClanMemberList_ExtraInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError,
                                    EndOfList = true
                                });
                            }

                        });
                        break;
                    }

                case MediusGetClanInvitationsSentRequest getClanInvitiationsSentRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanInvitiationsSentRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanInvitiationsSentRequest} without being logged in.");

                        await ServerConfiguration.Database.GetClanById(data.ClientObject.ClanId.Value,
                            data.ClientObject.ApplicationId)
                        .ContinueWith(r =>
                        {
                            List<MediusGetClanInvitationsSentResponse> responses = new List<MediusGetClanInvitationsSentResponse>();
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                responses.AddRange(r.Result.ClanMemberInvitations
                                    .Where(x => x.ResponseStatus == 0) // only return undecided
                                    .Skip((getClanInvitiationsSentRequest.Start - 1) * getClanInvitiationsSentRequest.PageSize)
                                    .Take(getClanInvitiationsSentRequest.PageSize)
                                    .Select(x => new MediusGetClanInvitationsSentResponse()
                                    {
                                        AccountID = x.TargetAccountId,
                                        AccountName = x.TargetAccountName,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        ResponseMsg = x.ResponseMessage,
                                        ResponseStatus = (MediusClanInvitationsResponseStatus)x.ResponseStatus,
                                        ResponseTime = x.ResponseTime,
                                        EndOfList = false
                                    }));
                            }

                            if (responses.Count == 0)
                            {
                                responses.Add(new MediusGetClanInvitationsSentResponse()
                                {
                                    MessageID = getClanInvitiationsSentRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }

                            responses[responses.Count - 1].EndOfList = true;
                            data.ClientObject.Queue(responses);
                        });
                        break;
                    }

                case MediusGetClanByIDRequest getClanByIdRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanByIdRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanByIdRequest} without being logged in.");

                        await ServerConfiguration.Database.GetClanById(getClanByIdRequest.ClanID,
                            data.ClientObject.ApplicationId)
                        .ContinueWith(r =>
                        {
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                data.ClientObject.Queue(new MediusGetClanByIDResponse()
                                {
                                    MessageID = getClanByIdRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    ApplicationID = r.Result.AppId,
                                    ClanName = r.Result.ClanName,
                                    LeaderAccountID = r.Result.ClanLeaderAccount.AccountId,
                                    LeaderAccountName = r.Result.ClanLeaderAccount.AccountName,
                                    Stats = Convert.FromBase64String(r.Result.ClanMediusStats),
                                    Status = r.Result.IsDisbanded ? MediusClanStatus.ClanDisbanded : MediusClanStatus.ClanActive
                                });
                            }
                            else if (getClanByIdRequest.ClanID == -1)
                            {
                                data.ClientObject.Queue(new MediusGetClanByIDResponse()
                                {
                                    MessageID = getClanByIdRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    ApplicationID = data.ClientObject.ApplicationId
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGetClanByIDResponse()
                                {
                                    MessageID = getClanByIdRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusClanNotFound,
                                    ApplicationID = data.ClientObject.ApplicationId
                                });
                            }
                        });
                        break;
                    }

                case MediusGetClanByNameRequest getClanByNameRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanByNameRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanByNameRequest} without being logged in.");

                        await ServerConfiguration.Database.GetClanByName(getClanByNameRequest.ClanName, data.ClientObject.ApplicationId).ContinueWith(r =>
                        {
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                data.ClientObject.Queue(new MediusGetClanByNameResponse()
                                {
                                    MessageID = getClanByNameRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    ClanID = r.Result.ClanId,
                                    LeaderAccountID = r.Result.ClanLeaderAccount.AccountId,
                                    LeaderAccountName = r.Result.ClanLeaderAccount.AccountName,
                                    Stats = Convert.FromBase64String(r.Result.ClanMediusStats),
                                    Status = r.Result.IsDisbanded ? MediusClanStatus.ClanDisbanded : MediusClanStatus.ClanActive
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGetClanByNameResponse()
                                {
                                    MessageID = getClanByNameRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusClanNotFound
                                });
                            }
                        });
                        break;
                    }

                case MediusRequestClanTeamChallengeRequest requestClanTeamChallengeRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {requestClanTeamChallengeRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {requestClanTeamChallengeRequest} without being logged in.");

                        // ERROR -- Need clan and EnableNonClanLeaderToGetTeamChallenges set to false (or true for all memebrs to view challenges!)
                        if (!data.ClientObject.ClanId.HasValue)
                        {
                            ServerConfiguration.LogWarn("Not leader of clan. -  Only a clan leader can get Clan Team Challenges.");
                            data.ClientObject.Queue(new MediusRequestClanTeamChallengeResponse()
                            {
                                MessageID = requestClanTeamChallengeRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNotClanLeader,
                            });
                        }

                        await ServerConfiguration.Database.RequestClanTeamChallenge(
                            (int)data.ClientObject.ClanId,  //ChallengerClanId
                            requestClanTeamChallengeRequest.ClanID, //AgainstClanId
                            data.ClientObject.AccountId, //ClanLeader?
                            requestClanTeamChallengeRequest.Message,
                            data.ClientObject.ApplicationId)
                        .ContinueWith(r =>
                        {
                            List<MediusRequestClanTeamChallengeResponse> responses = new List<MediusRequestClanTeamChallengeResponse>();
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                data.ClientObject.Queue(new RT_MSG_SERVER_APP()
                                {
                                    Message = new MediusRequestClanTeamChallengeResponse()
                                    {
                                        MessageID = requestClanTeamChallengeRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess
                                    }
                                });
                            }
                            else if (!r.IsCompletedSuccessfully)
                            {
                                responses.Add(new MediusRequestClanTeamChallengeResponse()
                                {
                                    MessageID = requestClanTeamChallengeRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError,
                                });
                            }
                        });

                        break;
                    }

                case MediusGetClanTeamChallengesRequest getClanTeamChallengesRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanTeamChallengesRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanTeamChallengesRequest} without being logged in.");

                        // ERROR -- Need clan and EnableNonClanLeaderToGetTeamChallenges set to false (or true for all memebrs to view challenges!)
                        if (!MediusClass.Settings.EnableNonClanLeaderToGetTeamChallenges && !data.ClientObject.ClanId.HasValue)
                        {
                            ServerConfiguration.LogWarn("Not leader of clan. -  Only a clan leader can get Clan Team Challenges.");
                            data.ClientObject.Queue(new MediusGetClanTeamChallengesResponse()
                            {
                                MessageID = getClanTeamChallengesRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNotClanLeader,
                                EndOfList = true
                            });
                        }

                        await ServerConfiguration.Database.GetClanTeamChallenges(getClanTeamChallengesRequest.ClanID,
                            data.ClientObject.AccountId,
                            getClanTeamChallengesRequest.Status,
                            getClanTeamChallengesRequest.Start,
                            getClanTeamChallengesRequest.PageSize,
                            data.ClientObject.ApplicationId)
                        .ContinueWith(r =>
                        {
                            List<MediusGetClanTeamChallengesResponse> responses = new List<MediusGetClanTeamChallengesResponse>();
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                responses.AddRange(r.Result
                                    .Where(x => x.ChallengerClanID == r.Result.FirstOrDefault().ChallengerClanID ||
                                        x.AgainstClanID == r.Result.FirstOrDefault().AgainstClanID
                                        && x.Status == (int)getClanTeamChallengesRequest.Status)
                                    .Skip((getClanTeamChallengesRequest.Start - 1) * getClanTeamChallengesRequest.PageSize)
                                    .Take(getClanTeamChallengesRequest.PageSize)
                                    .Select(x => new MediusGetClanTeamChallengesResponse()
                                    {
                                        MessageID = getClanTeamChallengesRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        ChallengerClanID = x.ChallengerClanID,
                                        AgainstClanID = x.AgainstClanID,
                                        Status = getClanTeamChallengesRequest.Status,
                                        ResponseTime = x.ResponseTime,
                                        ChallengeMsg = x.ChallengeMsg,
                                        ResponseMsg = x.ResponseMessage,
                                        EndOfList = false,
                                        ClanChallengeID = x.ClanChallengeId
                                    }));
                            }

                            if (responses.Count == 0)
                            {
                                data.ClientObject.Queue(new MediusGetClanTeamChallengesResponse()
                                {
                                    MessageID = getClanTeamChallengesRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    Status = getClanTeamChallengesRequest.Status,
                                    EndOfList = true
                                });
                            }
                            else
                            {
                                responses[responses.Count - 1].EndOfList = true;
                                data.ClientObject.Queue(responses);
                            }

                        });

                        break;
                    }

                case MediusRespondToClanTeamChallengeRequest respondToClanTeamChallengeRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {respondToClanTeamChallengeRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {respondToClanTeamChallengeRequest} without being logged in.");

                        // ERROR -- Need clan and EnableNonClanLeaderToGetTeamChallenges set to false (or true for all memebrs to view challenges!)
                        if (!data.ClientObject.ClanId.HasValue)
                        {
                            ServerConfiguration.LogWarn("Not leader of clan. -  Only a clan leader can respond to Clan Team Challenges.");
                            data.ClientObject.Queue(new MediusRespondToClanInvitationResponse()
                            {
                                MessageID = respondToClanTeamChallengeRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNotClanLeader,
                            });
                        }

                        await ServerConfiguration.Database.RespondClanTeamChallenge(
                            respondToClanTeamChallengeRequest.ClanChallengeID, //AgainstClanId
                            respondToClanTeamChallengeRequest.ChallengeStatus,
                            data.ClientObject.AccountId, //ClanLeader?
                            respondToClanTeamChallengeRequest.Message,
                            data.ClientObject.ApplicationId)
                        .ContinueWith(r =>
                        {
                            if (r.IsCompletedSuccessfully && r.Result != false)
                            {
                                data.ClientObject.Queue(new RT_MSG_SERVER_APP()
                                {
                                    Message = new MediusRespondClanTeamChallengeResponse()
                                    {
                                        MessageID = respondToClanTeamChallengeRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess
                                    }
                                });
                            }
                            else if (!r.IsCompletedSuccessfully)
                            {
                                data.ClientObject.Queue(new RT_MSG_SERVER_APP()
                                {
                                    Message = new MediusRespondClanTeamChallengeResponse()
                                    {
                                        MessageID = respondToClanTeamChallengeRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusDBError
                                    }
                                });
                            }
                        });

                        break;
                    }

                case MediusRevokeClanTeamChallengeRequest revokeClanTeamChallengeRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {revokeClanTeamChallengeRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {revokeClanTeamChallengeRequest} without being logged in.");

                        if (!data.ClientObject.ClanId.HasValue)
                        {
                            ServerConfiguration.LogWarn("Not leader of clan. -  Only a clan leader can respond to Clan Team Challenges.");
                            data.ClientObject.Queue(new MediusRequestClanTeamChallengeResponse()
                            {
                                MessageID = revokeClanTeamChallengeRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNotClanLeader,
                            });
                        }

                        await ServerConfiguration.Database.RevokeClanTeamChallenge(
                            revokeClanTeamChallengeRequest.ClanChallengeID,
                            data.ClientObject.AccountId, //ClanLeader?
                            data.ClientObject.ApplicationId)
                        .ContinueWith(r =>
                        {
                            if (r.IsCompletedSuccessfully && r.Result != false)
                            {
                                data.ClientObject.Queue(new RT_MSG_SERVER_APP()
                                {
                                    Message = new MediusRevokeClanTeamChallengeResponse()
                                    {
                                        MessageID = revokeClanTeamChallengeRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess
                                    }
                                });
                            }
                            else if (!r.IsCompletedSuccessfully)
                            {
                                data.ClientObject.Queue(new RT_MSG_SERVER_APP()
                                {
                                    Message = new MediusRevokeClanTeamChallengeResponse()
                                    {
                                        MessageID = revokeClanTeamChallengeRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusDBError
                                    }
                                });
                            }
                        });

                        break;
                    }

                case MediusUpdateClanLadderStatsWide_DeltaRequest updateClanLaddersStatsWideDeltaRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateClanLaddersStatsWideDeltaRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateClanLaddersStatsWideDeltaRequest} without being logged in.");

                        if (MediusClass.Settings.EnableClanLaddersDeltaOpenAccess == false)
                        {
                            ServerConfiguration.LogWarn("Update Clan Ladders Stats Wide Delta (Open Access) not enabled.");
                            data.ClientObject.Queue(new MediusUpdateClanLadderStatsWide_DeltaResponse()
                            {
                                MessageID = updateClanLaddersStatsWideDeltaRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFeatureNotEnabled
                            });
                        }
                        else
                        {
                            ServerConfiguration.LogInfo($"Update Clan Ladder Stats Delta (Open Access)");
                            if (ServerConfiguration.Database._settings.SimulatedMode != false)
                            {
                                ServerConfiguration.LogWarn("MediusUpdateClanLadderStatsWide_Delta Success (DB DISABLED)");
                                data.ClientObject.Queue(new MediusUpdateClanLadderStatsWide_DeltaResponse()
                                {
                                    MessageID = updateClanLaddersStatsWideDeltaRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusFeatureNotEnabled
                                });
                            }
                            else
                            {

                                // pass to plugins
                                var pluginMessage = new OnPlayerWideStatsArgs()
                                {
                                    Game = data.ClientObject.CurrentGame,
                                    Player = data.ClientObject,
                                    IsClan = true,
                                    WideStats = updateClanLaddersStatsWideDeltaRequest.Stats
                                };
                                await MediusClass.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_POST_WIDE_STATS, pluginMessage);

                                // reject
                                if (pluginMessage.Reject)
                                {
                                    data.ClientObject.Queue(new MediusUpdateClanLadderStatsWide_DeltaResponse()
                                    {
                                        MessageID = updateClanLaddersStatsWideDeltaRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusFail
                                    });
                                    break;
                                }

                                await ServerConfiguration.Database.PostClanLadderStats(data.ClientObject.AccountId,
                                    data.ClientObject.ClanId,

                                    pluginMessage.WideStats,
                                    data.ClientObject.ApplicationId)
                                    .ContinueWith(r =>
                                    {
                                        if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                            return;

                                        if (r.IsCompletedSuccessfully && r.Result != false)
                                        {
                                            ServerConfiguration.LogInfo("Updated Clan Ladder stats (Delta)");
                                            data.ClientObject.WideStats = pluginMessage.WideStats;
                                            data.ClientObject.Queue(new RT_MSG_SERVER_APP()
                                            {
                                                Message = new MediusUpdateClanLadderStatsWide_DeltaResponse()
                                                {
                                                    MessageID = updateClanLaddersStatsWideDeltaRequest.MessageID,
                                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                                }
                                            });
                                        }
                                        else
                                        {
                                            data.ClientObject.Queue(new RT_MSG_SERVER_APP()
                                            {
                                                Message = new MediusUpdateClanLadderStatsWide_DeltaResponse()
                                                {
                                                    MessageID = updateClanLaddersStatsWideDeltaRequest.MessageID,
                                                    StatusCode = MediusCallbackStatus.MediusDBError
                                                }
                                            });
                                        }
                                    });
                            }
                        }
                        break;
                    }

                case MediusClanLadderPositionRequest getClanLadderPositionRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanLadderPositionRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getClanLadderPositionRequest} without being logged in.");

                        await ServerConfiguration.Database.GetClanLeaderboardIndex(getClanLadderPositionRequest.ClanID, getClanLadderPositionRequest.ClanLadderStatIndex, data.ClientObject.ApplicationId).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                data.ClientObject.Queue(new MediusClanLadderPositionResponse()
                                {
                                    MessageID = getClanLadderPositionRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    LadderPosition = (uint)r.Result.Index + 1,
                                    TotalRankings = (uint)r.Result.TotalRankedClans
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusClanLadderPositionResponse()
                                {
                                    MessageID = getClanLadderPositionRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusClanNotFound
                                });
                            }
                        });
                        break;
                    }

                case MediusUpdateClanStatsRequest updateClanStatsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateClanStatsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {updateClanStatsRequest} without being logged in.");

                        await ServerConfiguration.Database.PostClanMediusStats(updateClanStatsRequest.ClanID,
                            Convert.ToBase64String(updateClanStatsRequest.Stats),
                            data.ClientObject.ApplicationId)
                            .ContinueWith((r) =>
                            {
                                if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                    return;

                                if (r.IsCompletedSuccessfully && r.Result)
                                {
                                    data.ClientObject.Queue(new MediusUpdateClanStatsResponse()
                                    {
                                        MessageID = updateClanStatsRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess
                                    });
                                }
                                else
                                {
                                    data.ClientObject.Queue(new MediusUpdateClanStatsResponse()
                                    {
                                        MessageID = updateClanStatsRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusClanNotFound
                                    });
                                }
                            });

                        break;
                    }

                case MediusInvitePlayerToClanRequest invitePlayerToClanRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {invitePlayerToClanRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {invitePlayerToClanRequest} without being logged in.");

                        // ERROR -- Need clan
                        if (!data.ClientObject.ClanId.HasValue)
                        {
                            ServerConfiguration.LogWarn("Not leader of clan.  Only a clan leader can invite a player to the clan");
                            data.ClientObject.Queue(new MediusInvitePlayerToClanResponse()
                            {
                                MessageID = invitePlayerToClanRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNotClanLeader
                            });
                        }

                        await ServerConfiguration.Database.CreateClanInvitation(data.ClientObject.AccountId, data.ClientObject.ClanId.Value, invitePlayerToClanRequest.PlayerAccountID, invitePlayerToClanRequest.InviteMessage).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusInvitePlayerToClanResponse()
                                {
                                    MessageID = invitePlayerToClanRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusInvitePlayerToClanResponse()
                                {
                                    MessageID = invitePlayerToClanRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusFail
                                });
                            }
                        });
                        break;
                    }

                case MediusRespondToClanInvitationRequest respondToClanInvitationRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {respondToClanInvitationRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {respondToClanInvitationRequest} without being logged in.");

                        await ServerConfiguration.Database.RespondToClanInvitation(data.ClientObject.AccountId, respondToClanInvitationRequest.ClanInvitationID, respondToClanInvitationRequest.Message, (int)respondToClanInvitationRequest.Response).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusRespondToClanInvitationResponse()
                                {
                                    MessageID = respondToClanInvitationRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusRespondToClanInvitationResponse()
                                {
                                    MessageID = respondToClanInvitationRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusFail
                                });
                            }
                        });
                        break;
                    }

                case MediusRevokeClanInvitationRequest revokeClanInvitationRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {revokeClanInvitationRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {revokeClanInvitationRequest} without being logged in.");

                        // ERROR -- Need clan
                        if (!data.ClientObject.ClanId.HasValue)
                        {
                            ServerConfiguration.LogWarn("Not leader of clan.  Only the leader can revoke the invitation sent");
                            data.ClientObject.Queue(new MediusInvitePlayerToClanResponse()
                            {
                                MessageID = revokeClanInvitationRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNotClanLeader
                            });
                        }
                        await ServerConfiguration.Database.RevokeClanInvitation(data.ClientObject.AccountId, data.ClientObject.ClanId.Value, revokeClanInvitationRequest.PlayerAccountID).ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusRevokeClanInvitationResponse()
                                {
                                    MessageID = revokeClanInvitationRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusRevokeClanInvitationResponse()
                                {
                                    MessageID = revokeClanInvitationRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusFail
                                });
                            }
                        });
                        break;
                    }



                case MediusDisbandClanRequest disbandClanRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {disbandClanRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {disbandClanRequest} without being logged in.");

                        // ERROR -- Need clan
                        if (data.ClientObject.ClanId != disbandClanRequest.ClanID)
                        {
                            // User was not the leader of the Clan they requested to disband.
                            data.ClientObject.Queue(new MediusDisbandClanResponse()
                            {
                                MessageID = disbandClanRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNotClanLeader
                            });
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {disbandClanRequest} without being the leader of clan to disband.");
                        }

                        await ServerConfiguration.Database.DeleteClan(data.ClientObject.AccountId,
                            disbandClanRequest.ClanID,
                            data.ClientObject.ApplicationId)
                        .ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusDisbandClanResponse()
                                {
                                    MessageID = disbandClanRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusDisbandClanResponse()
                                {
                                    MessageID = disbandClanRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusFail
                                });
                            }
                        });

                        break;
                    }

                case MediusGetMyClanMessagesRequest getMyClanMessagesRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyClanMessagesRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyClanMessagesRequest} without being logged in.");

                        // ERROR -- Need clan
                        if (data.ClientObject.ClanId != getMyClanMessagesRequest.ClanID)
                        {
                            ServerConfiguration.LogWarn("Not leader of clan.  Only a clan leader can invite a player to the clan");
                            // User was not the leader of the Clan they requested to disband.
                            data.ClientObject.Queue(new MediusGetMyClanMessagesResponse()
                            {
                                MessageID = getMyClanMessagesRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNotClanLeader
                            });
                        }
                        else
                        {
                            await ServerConfiguration.Database.GetClanMessages(data.ClientObject.AccountId,
                                data.ClientObject.ClanId.Value,
                                getMyClanMessagesRequest.Start,
                                getMyClanMessagesRequest.PageSize,
                                data.ClientObject.ApplicationId)
                            .ContinueWith((r) =>
                            {
                                List<MediusGetMyClanMessagesResponse> responses = new List<MediusGetMyClanMessagesResponse>();
                                if (r.IsCompletedSuccessfully && r.Result != null)
                                {
                                    /*
                                    char[] ch = new char[r.Result.FirstOrDefault().Message.Length];

                                    // Copy character by character into array 
                                    for (int i = 0; i < r.Result.FirstOrDefault().Message.Length; i++)
                                    {
                                        ch[i] = r.Result.FirstOrDefault().Message[i];
                                    }
                                    */
                                    responses.AddRange(r.Result
                                        .Select(x => new MediusGetMyClanMessagesResponse()
                                        {
                                            MessageID = getMyClanMessagesRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            Message = r.Result.FirstOrDefault().Message,
                                            ClanID = data.ClientObject.ClanId.Value,
                                            ClanMessageID = 1
                                        }));
                                }

                                if (responses.Count == 0)
                                {
                                    responses.Add(new MediusGetMyClanMessagesResponse()
                                    {
                                        MessageID = getMyClanMessagesRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusNoResult,
                                        ClanID = data.ClientObject.ClanId.Value,
                                        EndOfList = true
                                    });
                                }

                                responses[responses.Count - 1].EndOfList = true;
                                data.ClientObject.Queue(responses);
                            });

                        }
                        break;
                    }

                case MediusGetAllClanMessagesRequest getAllClanMessagesRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getAllClanMessagesRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getAllClanMessagesRequest} without being logged in.");

                        // ERROR -- Need clan
                        if (!data.ClientObject.ClanId.HasValue)
                        {
                            ServerConfiguration.LogWarn("Not leader of clan.  Only a clan leader can invite a player to the clan");
                            data.ClientObject.Queue(new MediusGetAllClanMessagesResponse()
                            {
                                MessageID = getAllClanMessagesRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNotClanLeader,
                                EndOfList = true
                            });
                        }

                        await ServerConfiguration.Database.GetClanMessages(data.ClientObject.AccountId,
                            data.ClientObject.ClanId.Value,
                            getAllClanMessagesRequest.Start,
                            getAllClanMessagesRequest.PageSize,
                            data.ClientObject.ApplicationId)
                        .ContinueWith((r) =>
                        {
                            List<MediusGetAllClanMessagesResponse> responses = new List<MediusGetAllClanMessagesResponse>();
                            if (r.IsCompletedSuccessfully && r.Result != null)
                            {
                                responses.AddRange(r.Result
                                    .Select(x => new MediusGetAllClanMessagesResponse()
                                    {
                                        MessageID = getAllClanMessagesRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        ClanMessageID = x.Id,
                                        Message = x.Message,
                                        Status = MediusClanMessageStatus.ClanMessageRead
                                    }));
                            }

                            if (responses.Count == 0)
                            {
                                responses.Add(new MediusGetAllClanMessagesResponse()
                                {
                                    MessageID = getAllClanMessagesRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }

                            responses[responses.Count - 1].EndOfList = true;
                            data.ClientObject.Queue(responses);
                        });

                        break;
                    }

                case MediusSendClanMessageRequest sendClanMessageRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {sendClanMessageRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {sendClanMessageRequest} without being logged in.");

                        // ERROR -- Need clan
                        if (!data.ClientObject.ClanId.HasValue)
                        {
                            ServerConfiguration.LogWarn("Not leader of clan.  Only a clan leader can invite a player to the clan");
                            data.ClientObject.Queue(new MediusSendClanMessageResponse()
                            {
                                MessageID = sendClanMessageRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNotClanLeader
                            });
                        }

                        // validate message
                        if (!MediusClass.PassTextFilter(data.ClientObject.ApplicationId, TextFilterContext.CLAN_MESSAGE, sendClanMessageRequest.Message))
                        {
                            data.ClientObject.Queue(new MediusSendClanMessageResponse()
                            {
                                MessageID = sendClanMessageRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                            return;
                        }

                        await ServerConfiguration.Database.ClanAddMessage(data.ClientObject.AccountId,
                            data.ClientObject.ClanId.Value,
                            sendClanMessageRequest.Message,
                            data.ClientObject.ApplicationId)
                        .ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusSendClanMessageResponse()
                                {
                                    MessageID = sendClanMessageRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusSendClanMessageResponse()
                                {
                                    MessageID = sendClanMessageRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusFail
                                });
                            }
                        });

                        break;
                    }

                case MediusModifyClanMessageRequest modifyClanMessageRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {modifyClanMessageRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {modifyClanMessageRequest} without being logged in.");

                        // ERROR -- Need clan
                        if (!data.ClientObject.ClanId.HasValue)
                        {
                            ServerConfiguration.LogWarn("Not leader of clan.  Only a clan leader can invite a player to the clan");
                            data.ClientObject.Queue(new MediusModifyClanMessageResponse()
                            {
                                MessageID = modifyClanMessageRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNotClanLeader
                            });
                        }

                        // validate message
                        if (!MediusClass.PassTextFilter(data.ClientObject.ApplicationId, TextFilterContext.CLAN_MESSAGE, modifyClanMessageRequest.NewMessage))
                        {
                            data.ClientObject.Queue(new MediusModifyClanMessageResponse()
                            {
                                MessageID = modifyClanMessageRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                            return;
                        }

                        await ServerConfiguration.Database.ClanEditMessage(data.ClientObject.AccountId,
                            data.ClientObject.ClanId.Value,
                            modifyClanMessageRequest.ClanMessageID,
                            modifyClanMessageRequest.NewMessage,
                            data.ClientObject.ApplicationId).
                        ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusModifyClanMessageResponse()
                                {
                                    MessageID = modifyClanMessageRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusModifyClanMessageResponse()
                                {
                                    MessageID = modifyClanMessageRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusFail
                                });
                            }
                        });

                        break;
                    }

                case MediusDeleteClanMessageRequest deleteClanMessageRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {deleteClanMessageRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {deleteClanMessageRequest} without being logged in.");

                        // ERROR -- Need clan
                        if (!data.ClientObject.ClanId.HasValue)
                        {
                            ServerConfiguration.LogWarn("Not leader of clan.  Only a clan leader can invite a player to the clan");
                            data.ClientObject.Queue(new MediusInvitePlayerToClanResponse()
                            {
                                MessageID = deleteClanMessageRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNotClanLeader
                            });
                        }

                        await ServerConfiguration.Database.ClanDeleteMessage(data.ClientObject.AccountId,
                            data.ClientObject.ClanId.Value,
                            deleteClanMessageRequest.ClanMessageID,
                            data.ClientObject.ApplicationId).
                        ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusDeleteClanMessageResponse()
                                {
                                    MessageID = deleteClanMessageRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusDeleteClanMessageResponse()
                                {
                                    MessageID = deleteClanMessageRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError
                                });
                            }
                        });
                        break;
                    }

                case MediusTransferClanLeadershipRequest transferClanLeadershipRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {transferClanLeadershipRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {transferClanLeadershipRequest} without being logged in.");

                        // ERROR -- Need clan
                        if (!data.ClientObject.ClanId.HasValue)
                        {
                            // User was not the leader of the Clan they requested to disband.
                            data.ClientObject.Queue(new MediusDisbandClanResponse()
                            {
                                MessageID = transferClanLeadershipRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNotClanLeader
                            });

                            ServerConfiguration.LogError($"INVALID OPERATION: {clientChannel} sent {transferClanLeadershipRequest} without being the leader of clan to disband.");
                        }

                        await ServerConfiguration.Database.ClanTransferLeadership(data.ClientObject.AccountId,
                            data.ClientObject.ClanId.Value,
                            transferClanLeadershipRequest.NewLeaderAccountID,
                            data.ClientObject.ApplicationId)
                        .ContinueWith((r) =>
                        {
                            if (data == null || data.ClientObject == null || !data.ClientObject.IsConnected)
                                return;

                            if (r.IsCompletedSuccessfully && r.Result)
                            {
                                data.ClientObject.Queue(new MediusTransferClanLeadershipResponse()
                                {
                                    MessageID = transferClanLeadershipRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusTransferClanLeadershipResponse()
                                {
                                    MessageID = transferClanLeadershipRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusFail
                                });
                            }
                        });

                        break;
                    }

                #endregion

                #region Party (PS3)

                case MediusPartyCreateRequest partyCreateRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {partyCreateRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {partyCreateRequest} without being logged in.");

                        // validate name
                        if (!MediusClass.PassTextFilter(data.ClientObject.ApplicationId, TextFilterContext.GAME_NAME, Convert.ToString(partyCreateRequest.PartyName)))
                        {
                            data.ClientObject.Queue(new MediusPartyCreateResponse()
                            {
                                MessageID = partyCreateRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                            return;
                        }

                        // Send to plugins
                        //await MediusClass.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_CREATE_PARTY, new OnPlayerRequestArgs() { Player = data.ClientObject, Request = partyCreateRequest });

                        await MediusClass.Manager.CreateParty(data.ClientObject, partyCreateRequest);
                        break;
                    }

                case MediusPartyPlayerReport partyPlayerReport:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {partyPlayerReport} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {partyPlayerReport} without being logged in.");

                        if (data.ClientObject.CurrentParty?.Id == partyPlayerReport.MediusWorldID &&
                            data.ClientObject.SessionKey == partyPlayerReport.SessionKey)
                            data.ClientObject.CurrentParty?.OnPartyPlayerReport(partyPlayerReport);

                        break;
                    }

                case MediusPartyJoinByIndexRequest partyJoinByIndex:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {partyJoinByIndex} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {partyJoinByIndex} without being logged in.");


                        await MediusClass.Manager.joinParty(data.ClientObject, partyJoinByIndex, clientChannel);

                        break;
                    }

                #endregion

                #region ReassignGameMediusWorldID (P2P Host Migration)
                case MediusReassignGameMediusWorldID reassignGameMediusWorldID:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {reassignGameMediusWorldID} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {reassignGameMediusWorldID} without being logged in.");

                        // ERROR -- Need to be in a game
                        if (!data.ClientObject.IsInGame)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {reassignGameMediusWorldID} without being in a game.");

                        int iCurrentMediusWorldID = -1;
                        int iNewMediusWorldID = -1;
                        int result = data.ClientObject.CurrentGame.ReassignGameMediusWorldID(reassignGameMediusWorldID);
                        if (result != 0)
                        {
                            iCurrentMediusWorldID = reassignGameMediusWorldID.OldMediusWorldID;
                            iNewMediusWorldID = result;

                            data.ClientObject.Queue(new MediusReassignGameMediusWorldID()
                            {
                                OldMediusWorldID = iCurrentMediusWorldID,
                                NewMediusWorldID = iNewMediusWorldID,
                            });

                            ServerConfiguration.LogInfo($"Sent new MediusWorldID[{iNewMediusWorldID}]");
                        }
                        break;
                    }
                #endregion

                #region Game

                case MediusGetTotalGamesRequest getTotalGamesRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalGamesRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalGamesRequest} without being logged in.");

                        int gameCount = MediusClass.Manager.GetGameCountAppId(getTotalGamesRequest.ApplicationId);

                        data.ClientObject.Queue(new MediusGetTotalGamesResponse()
                        {
                            MessageID = getTotalGamesRequest.MessageID,
                            Total = gameCount,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });
                        break;
                    }

                #region GameList
                case MediusGameListRequest gameListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameListRequest} without being logged in.");

                        if (data.ClientObject.ApplicationId == 10538 || data.ClientObject.ApplicationId == 10190)
                        {
                            ServerConfiguration.LogInfo("AppId GameList Check");
                            var gameList = MediusClass.Manager.GetGameListAppId(
                               data.ClientObject.ApplicationId,
                               gameListRequest.PageID,
                               gameListRequest.PageSize)
                            .Select(x => new MediusGameListResponse()
                            {
                                MessageID = gameListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,

                                MediusWorldID = x.Id,
                                GameName = x.GameName,
                                WorldStatus = x.WorldStatus,
                                GameHostType = x.GameHostType,
                                PlayerCount = (ushort)x.PlayerCount,
                                EndOfList = false
                            }).ToArray();

                            // Make last end of list
                            if (gameList.Length > 0)
                            {
                                gameList[gameList.Length - 1].EndOfList = true;

                                // Add to responses
                                data.ClientObject.Queue(gameList);
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGameListResponse()
                                {
                                    MessageID = gameListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }
                        }
                        else
                        {
                            ServerConfiguration.LogInfo("Filtered GameList Check");
                            var gameList = MediusClass.Manager.GetGameList(
                               data.ClientObject.ApplicationId,
                               gameListRequest.PageID,
                               gameListRequest.PageSize,
                               data.ClientObject.GameListFilters)
                            .Select(x => new MediusGameListResponse()
                            {
                                MessageID = gameListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,

                                MediusWorldID = x.Id,
                                GameName = x.GameName,
                                WorldStatus = x.WorldStatus,
                                GameHostType = x.GameHostType,
                                PlayerCount = (ushort)x.PlayerCount,
                                EndOfList = false
                            }).ToArray();

                            // Make last end of list
                            if (gameList.Length > 0)
                            {
                                gameList[gameList.Length - 1].EndOfList = true;

                                // Add to responses
                                data.ClientObject.Queue(gameList);
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGameListResponse()
                                {
                                    MessageID = gameListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }
                        }


                        break;
                    }

                case MediusGameList_ExtraInfoRequest gameList_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameList_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameList_ExtraInfoRequest} without being logged in.");

                        List<int> FilteredGameLists = new List<int>() { 21924, 10994, 11203, 11204 };
                        List<int> NonFilteredGameLists = new List<int>() { 20770, 20623, 20624, 20764, 22920 };

                        //By Filter
                        if (FilteredGameLists.Contains(data.ClientObject.ApplicationId))
                        {
                            var gameList = MediusClass.Manager.GetGameList(
                               data.ClientObject.ApplicationId,
                               gameList_ExtraInfoRequest.PageID,
                               gameList_ExtraInfoRequest.PageSize,
                               data.ClientObject.GameListFilters)
                            .Select(x => new MediusGameList_ExtraInfoResponse()
                            {
                                MessageID = gameList_ExtraInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,

                                GameHostType = x.GameHostType,
                                GameLevel = x.GameLevel,
                                GameName = x.GameName,
                                GameStats = x.GameStats,
                                GenericField1 = x.GenericField1,
                                GenericField2 = x.GenericField2,
                                GenericField3 = x.GenericField3,
                                GenericField4 = x.GenericField4,
                                GenericField5 = x.GenericField5,
                                GenericField6 = x.GenericField6,
                                GenericField7 = x.GenericField7,
                                GenericField8 = x.GenericField8,
                                MaxPlayers = (ushort)x.MaxPlayers,
                                MediusWorldID = x.Id,
                                MinPlayers = (ushort)x.MinPlayers,
                                PlayerCount = (ushort)x.PlayerCount,
                                PlayerSkillLevel = x.PlayerSkillLevel,
                                RulesSet = x.RulesSet,
                                SecurityLevel = (string.IsNullOrEmpty(x.GamePassword) ? MediusWorldSecurityLevelType.WORLD_SECURITY_NONE : MediusWorldSecurityLevelType.WORLD_SECURITY_PLAYER_PASSWORD),
                                WorldStatus = x.WorldStatus,
                                EndOfList = false
                            }).ToArray();

                            // Make last end of list
                            if (gameList.Length > 0)
                            {
                                gameList[gameList.Length - 1].EndOfList = true;

                                // Add to responses
                                data.ClientObject.Queue(gameList);
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGameList_ExtraInfoResponse()
                                {
                                    MessageID = gameList_ExtraInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }
                        }

                        // Size Matters  20770, CAC 20623, 20624
                        else if (NonFilteredGameLists.Contains(data.ClientObject.ApplicationId))
                        {

                            int Count = MediusClass.Manager.GetGameCountAppId(data.ClientObject.ApplicationId);
                            ServerConfiguration.LogWarn($"Count: {Count}");

                            var gameList = MediusClass.Manager.GetGameListAppId(
                                data.ClientObject.ApplicationId,
                                gameList_ExtraInfoRequest.PageID,
                                gameList_ExtraInfoRequest.PageSize)
                                .Select(x => new MediusGameList_ExtraInfoResponse()
                                {
                                    MessageID = gameList_ExtraInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,

                                    GameHostType = x.GameHostType,
                                    GameLevel = x.GameLevel,
                                    GameName = x.GameName,
                                    GameStats = x.GameStats,
                                    GenericField1 = x.GenericField1,
                                    GenericField2 = x.GenericField2,
                                    GenericField3 = x.GenericField3,
                                    GenericField4 = x.GenericField4,
                                    GenericField5 = x.GenericField5,
                                    GenericField6 = x.GenericField6,
                                    GenericField7 = x.GenericField7,
                                    GenericField8 = x.GenericField8,
                                    MaxPlayers = (ushort)x.MaxPlayers,
                                    MediusWorldID = x.Id,
                                    MinPlayers = (ushort)x.MinPlayers,
                                    PlayerCount = (ushort)x.PlayerCount,
                                    PlayerSkillLevel = x.PlayerSkillLevel,
                                    RulesSet = x.RulesSet,
                                    SecurityLevel = (string.IsNullOrEmpty(x.GamePassword) ? MediusWorldSecurityLevelType.WORLD_SECURITY_NONE : MediusWorldSecurityLevelType.WORLD_SECURITY_PLAYER_PASSWORD),
                                    WorldStatus = x.WorldStatus,
                                    EndOfList = false
                                }).ToArray();

                            // Make last end of list
                            if (gameList.Length > 0)
                            {
                                gameList[gameList.Length - 1].EndOfList = true;

                                // Add to responses
                                data.ClientObject.Queue(gameList);
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGameList_ExtraInfoResponse()
                                {
                                    MessageID = gameList_ExtraInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }
                        }

                        //No Filter
                        else
                        {
                            int Count = MediusClass.Manager.GetGameCountAppId(data.ClientObject.ApplicationId);
                            ServerConfiguration.LogWarn($"Count: {Count}");

                            var gameList = MediusClass.Manager.GetGameListAppId(
                                data.ClientObject.ApplicationId,
                                gameList_ExtraInfoRequest.PageID,
                                gameList_ExtraInfoRequest.PageSize)
                            .Select(x => new MediusGameList_ExtraInfoResponse()
                            {
                                MessageID = gameList_ExtraInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,

                                GameHostType = x.GameHostType,
                                GameLevel = x.GameLevel,
                                GameName = x.GameName,
                                GameStats = x.GameStats,
                                GenericField1 = x.GenericField1,
                                GenericField2 = x.GenericField2,
                                GenericField3 = x.GenericField3,
                                GenericField4 = x.GenericField4,
                                GenericField5 = x.GenericField5,
                                GenericField6 = x.GenericField6,
                                GenericField7 = x.GenericField7,
                                GenericField8 = x.GenericField8,
                                MaxPlayers = (ushort)x.MaxPlayers,
                                MediusWorldID = x.Id,
                                MinPlayers = (ushort)x.MinPlayers,
                                PlayerCount = (ushort)x.PlayerCount,
                                PlayerSkillLevel = x.PlayerSkillLevel,
                                RulesSet = x.RulesSet,
                                SecurityLevel = (string.IsNullOrEmpty(x.GamePassword) ? MediusWorldSecurityLevelType.WORLD_SECURITY_NONE : MediusWorldSecurityLevelType.WORLD_SECURITY_PLAYER_PASSWORD),
                                WorldStatus = x.WorldStatus,
                                EndOfList = false
                            }).ToArray();

                            // Make last end of list
                            if (gameList.Length > 0)
                            {
                                gameList[gameList.Length - 1].EndOfList = true;

                                // Add to responses
                                data.ClientObject.Queue(gameList);
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGameList_ExtraInfoResponse()
                                {
                                    MessageID = gameList_ExtraInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }
                        }

                        break;
                    }

                case MediusGameList_ExtraInfoRequest0 gameList_ExtraInfoRequest0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameList_ExtraInfoRequest0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameList_ExtraInfoRequest0} without being logged in.");

                        if (data.ClientObject.ApplicationId == 10952 || data.ClientObject.ApplicationId == 10954 || data.ClientObject.ApplicationId == 11234 || data.ClientObject.ApplicationId == 10394)
                        {
                            var gameList = MediusClass.Manager.GetGameListAppId(
                            data.ClientObject.ApplicationId,
                            gameList_ExtraInfoRequest0.PageID,
                            gameList_ExtraInfoRequest0.PageSize)
                            .Select(x => new MediusGameList_ExtraInfoResponse0()
                            {
                                MessageID = gameList_ExtraInfoRequest0.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,

                                GameHostType = x.GameHostType,
                                GameLevel = x.GameLevel,
                                GameName = x.GameName,
                                GameStats = x.GameStats,
                                GenericField1 = x.GenericField1,
                                GenericField2 = x.GenericField2,
                                GenericField3 = x.GenericField3,
                                MaxPlayers = (ushort)x.MaxPlayers,
                                MediusWorldID = x.Id,
                                MinPlayers = (ushort)x.MinPlayers,
                                PlayerCount = (ushort)x.PlayerCount,
                                PlayerSkillLevel = x.PlayerSkillLevel,
                                RulesSet = x.RulesSet,
                                SecurityLevel = (string.IsNullOrEmpty(x.GamePassword) ? MediusWorldSecurityLevelType.WORLD_SECURITY_NONE : MediusWorldSecurityLevelType.WORLD_SECURITY_PLAYER_PASSWORD),
                                WorldStatus = x.WorldStatus,
                                EndOfList = false
                            }).ToArray();

                            // Make last end of list
                            if (gameList.Length > 0)
                            {
                                gameList[gameList.Length - 1].EndOfList = true;

                                // Add to responses
                                data.ClientObject.Queue(gameList);
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGameList_ExtraInfoResponse0()
                                {
                                    MessageID = gameList_ExtraInfoRequest0.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }
                        }
                        else
                        {
                            var gameList = MediusClass.Manager.GetGameList(
                            data.ClientObject.ApplicationId,
                            gameList_ExtraInfoRequest0.PageID,
                            gameList_ExtraInfoRequest0.PageSize,
                            data.ClientObject.GameListFilters)
                            .Select(x => new MediusGameList_ExtraInfoResponse0()
                            {
                                MessageID = gameList_ExtraInfoRequest0.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,

                                GameHostType = x.GameHostType,
                                GameLevel = x.GameLevel,
                                GameName = x.GameName,
                                GameStats = x.GameStats,
                                GenericField1 = x.GenericField1,
                                GenericField2 = x.GenericField2,
                                GenericField3 = x.GenericField3,
                                MaxPlayers = (ushort)x.MaxPlayers,
                                MediusWorldID = x.Id,
                                MinPlayers = (ushort)x.MinPlayers,
                                PlayerCount = (ushort)x.PlayerCount,
                                PlayerSkillLevel = x.PlayerSkillLevel,
                                RulesSet = x.RulesSet,
                                SecurityLevel = (string.IsNullOrEmpty(x.GamePassword) ? MediusWorldSecurityLevelType.WORLD_SECURITY_NONE : MediusWorldSecurityLevelType.WORLD_SECURITY_PLAYER_PASSWORD),
                                WorldStatus = x.WorldStatus,
                                EndOfList = false
                            }).ToArray();

                            // Make last end of list
                            if (gameList.Length > 0)
                            {
                                gameList[gameList.Length - 1].EndOfList = true;

                                // Add to responses
                                data.ClientObject.Queue(gameList);
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGameList_ExtraInfoResponse0()
                                {
                                    MessageID = gameList_ExtraInfoRequest0.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }
                        }


                        break;
                    }
                #endregion

                #region Game Info
                case MediusGameInfoRequest gameInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameInfoRequest} without being logged in.");

                        var game = MediusClass.Manager.GetGameByGameId(gameInfoRequest.MediusWorldID);
                        if (game == null)
                        {
                            data.ClientObject.Queue(new MediusGameInfoResponse()
                            {
                                MessageID = gameInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusGameNotFound
                            });
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusGameInfoResponse()
                            {
                                MessageID = gameInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                ApplicationID = data.ApplicationId,
                                MaxPlayers = (ushort)game.MaxPlayers,
                                MinPlayers = (ushort)game.MinPlayers,
                                GameHostType = game.GameHostType,
                                GameLevel = game.GameLevel,
                                GameName = game.GameName,
                                GameStats = game.GameStats,
                                GenericField1 = game.GenericField1,
                                GenericField2 = game.GenericField2,
                                GenericField3 = game.GenericField3,
                                GenericField4 = game.GenericField4,
                                GenericField5 = game.GenericField5,
                                GenericField6 = game.GenericField6,
                                GenericField7 = game.GenericField7,
                                GenericField8 = game.GenericField8,
                                PlayerCount = (ushort)game.PlayerCount,
                                PlayerSkillLevel = game.PlayerSkillLevel,
                                RulesSet = game.RulesSet,
                                WorldStatus = game.WorldStatus,
                            });
                        }
                        break;
                    }

                case MediusGameInfoRequest0 gameInfoRequest0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameInfoRequest0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameInfoRequest0} without being logged in.");

                        var game = MediusClass.Manager.GetGameByGameId(gameInfoRequest0.MediusWorldID);
                        if (game == null)
                        {
                            data.ClientObject.Queue(new MediusGameInfoResponse0()
                            {
                                MessageID = gameInfoRequest0.MessageID,
                                StatusCode = MediusCallbackStatus.MediusGameNotFound
                            });
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusGameInfoResponse0()
                            {
                                MessageID = gameInfoRequest0.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,

                                GameHostType = game.GameHostType,
                                GameLevel = game.GameLevel,
                                GameName = game.GameName,
                                GameStats = game.GameStats,
                                GenericField1 = game.GenericField1,
                                GenericField2 = game.GenericField2,
                                GenericField3 = game.GenericField3,
                                MaxPlayers = (ushort)game.MaxPlayers,
                                MinPlayers = (ushort)game.MinPlayers,
                                PlayerCount = (ushort)game.PlayerCount,
                                PlayerSkillLevel = game.PlayerSkillLevel,
                                RulesSet = game.RulesSet,
                                WorldStatus = game.WorldStatus,
                                ApplicationID = data.ApplicationId
                            });
                        }

                        break;
                    }
                #endregion

                case MediusLobbyWorldPlayerListRequest lobbyWorldPlayerListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {lobbyWorldPlayerListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                        {
                            data.ClientObject.Queue(new MediusLobbyWorldPlayerListResponse()
                            {
                                MessageID = lobbyWorldPlayerListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusPlayerNotPrivileged,
                                EndOfList = true
                            });

                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {lobbyWorldPlayerListRequest} without being logged in.");
                        }

                        List<MediusLobbyWorldPlayerListResponse> lobbyWorldPlayerListResponses = new List<MediusLobbyWorldPlayerListResponse>();


                        var channel = MediusClass.Manager.GetChannelByChannelId(lobbyWorldPlayerListRequest.MediusWorldID, data.ClientObject.ApplicationId);
                        if (channel == null)
                        {
                            data.ClientObject.Queue(new MediusLobbyWorldPlayerListResponse()
                            {
                                MessageID = lobbyWorldPlayerListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                PlayerStatus = MediusPlayerStatus.MediusPlayerDisconnected,
                                AccountID = -1,
                                AccountName = null,
                                Stats = new byte[Constants.ACCOUNTSTATS_MAXLEN],
                                ConnectionClass = MediusConnectionType.Modem,
                                EndOfList = true
                            });
                        }
                        else
                        {
                            var lobbyPlayersFound = channel.Clients.Where(x => x != null && x.IsConnected).ToList();

                            foreach (var lobbyPlayer in lobbyPlayersFound)
                            {
                                await ServerConfiguration.Database.GetAccountById(lobbyPlayer.AccountId).ContinueWith((r) =>
                                {
                                    if (r.IsCompletedSuccessfully && r.Result != null)
                                    {
                                        byte[] mediusStats = new byte[Constants.ACCOUNTSTATS_MAXLEN];
                                        try { var dbAccStats = Convert.FromBase64String(r.Result.MediusStats ?? ""); mediusStats = dbAccStats; } catch (Exception) { }
                                        var playerClientObject = MediusClass.Manager.GetClientByAccountId(lobbyPlayer.AccountId, data.ClientObject.ApplicationId);
                                        lobbyWorldPlayerListResponses.Add(new MediusLobbyWorldPlayerListResponse()
                                        {
                                            MessageID = lobbyWorldPlayerListRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            PlayerStatus = playerClientObject.PlayerStatus,
                                            AccountID = r.Result.AccountId,
                                            AccountName = r.Result.AccountName,
                                            Stats = mediusStats,
                                            ConnectionClass = lobbyPlayer.MediusConnectionType,
                                            EndOfList = false,
                                        }); ;
                                    }
                                    else
                                    {
                                        data.ClientObject.Queue(new MediusLobbyWorldPlayerListResponse()
                                        {
                                            MessageID = lobbyWorldPlayerListRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusDBError,
                                            EndOfList = false,
                                        });
                                    }
                                });


                            }

                            if (lobbyWorldPlayerListResponses.Count == 0)
                            {
                                data.ClientObject.Queue(new MediusLobbyWorldPlayerListResponse()
                                {
                                    MessageID = lobbyWorldPlayerListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    PlayerStatus = MediusPlayerStatus.MediusPlayerDisconnected,
                                    AccountID = -1,
                                    AccountName = null,
                                    Stats = new byte[Constants.ACCOUNTSTATS_MAXLEN],
                                    ConnectionClass = MediusConnectionType.Modem,
                                    EndOfList = false,
                                });
                            }
                            else
                            {

                                // Set last end of list
                                lobbyWorldPlayerListResponses[lobbyWorldPlayerListResponses.Count - 1].EndOfList = true;
                                data.ClientObject.Queue(lobbyWorldPlayerListResponses);
                            }

                        }

                        /*
                        if (data.ClientObject.ApplicationId == 20244) //NBA 07 
                        {
                            
                        } else {
                            var game = MediusClass.Manager.GetGameByGameId(lobbyWorldPlayerListRequest.MediusWorldID);
                            if (game == null)
                            {
                                data.ClientObject.Queue(new MediusLobbyWorldPlayerListResponse()
                                {
                                    MessageID = lobbyWorldPlayerListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult
                                });
                            }
                            else
                            {
                                var playerList = game.Clients.Where(x => x != null && x.InGame && x.Client.IsConnected).Select(x => new MediusLobbyWorldPlayerListResponse()
                                {
                                    MessageID = lobbyWorldPlayerListRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    PlayerStatus = x.Client.PlayerStatus,
                                    AccountID = x.Client.AccountId,
                                    AccountName = x.Client.AccountName,
                                    Stats = x.Client.Stats,
                                    ConnectionClass = MediusConnectionType.Ethernet,
                                    EndOfList = false
                                }).ToArray();

                                // Set last end of list
                                if (playerList.Length > 0)
                                    playerList[playerList.Length - 1].EndOfList = true;
                                else
                                {
                                    playerList[playerList.Length - 1].EndOfList = true;
                                }
                                data.ClientObject.Queue(playerList);
                            }
                        }
                        */

                        /*
                         if (client == null)
                        {

                        }
                        else
                        {
                            var playerList = client.Clients.Where(x => x != null && x.InGame && x.Client.IsConnected).Select(x => new MediusLobbyWorldPlayerListResponse()
                            {
                                MessageID = lobbyWorldPlayerListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                PlayerStatus = x.Client.PlayerStatus,
                                AccountID = x.Client.AccountId,
                                AccountName = x.Client.AccountName,
                                Stats = x.Client.Stats,
                                ConnectionClass = MediusConnectionType.Ethernet,
                                EndOfList = false
                            }).ToArray();

                            // Set last end of list
                            if (playerList.Length > 0)
                                playerList[playerList.Length - 1].EndOfList = true;
                            else { playerList[playerList.Length - 1].EndOfList = true; }
                            data.ClientObject.Queue(playerList);
                        var game1 = MediusClass.Manager.GetGameByGameId(lobbyWorldPlayerListRequest.MediusWorldID);
                        if (game1 == null)
                        {
                            data.ClientObject.Queue(new MediusLobbyWorldPlayerListResponse()
                            {
                                MessageID = lobbyWorldPlayerListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusGameNotFound
                            });
                        }
                        else
                        {
                            var playerList = game1.Clients.Where(x => x != null && x.InGame && x.Client.IsConnected).Select(x => new MediusLobbyWorldPlayerListResponse()
                            {
                                MessageID = lobbyWorldPlayerListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                AccountID = x.Client.AccountId,
                                AccountName = x.Client.AccountName,
                                ConnectionClass = MediusConnectionType.Ethernet,
                                EndOfList = false
                            }).ToArray();

                            // Set last end of list
                            if (playerList.Length > 0)
                                playerList[playerList.Length - 1].EndOfList = true;
                            else { playerList[playerList.Length - 1].EndOfList = true; }
                            data.ClientObject.Queue(playerList);
                        }
                        */

                        break;
                    }

                case MediusFindWorldByNameRequest findWorldByNameRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {findWorldByNameRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {findWorldByNameRequest} without being logged in.");

                        var channel = MediusClass.Manager.GetWorldByName(findWorldByNameRequest.Name);

                        if (channel == null)
                        {
                            ServerConfiguration.LogWarn($"World name not found: {findWorldByNameRequest.Name}");

                            data.ClientObject.Queue(new MediusFindWorldByNameResponse()
                            {
                                MessageID = findWorldByNameRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true,
                            });
                        }
                        else
                        {
                            ServerConfiguration.LogWarn($"World Clients in {channel.Name} : {channel.Clients.Count()}");

                            string findWorldType = "Find Game World";
                            if (findWorldByNameRequest.WorldType < 0)
                            {
                                if (findWorldByNameRequest.WorldType == MediusFindWorldType.FindLobbyWorld)
                                    findWorldType = "Find Lobby World";
                                else
                                {
                                    findWorldType = "Find All Worlds";
                                    if (findWorldByNameRequest.WorldType != MediusFindWorldType.FIndAllWorlds)
                                        findWorldType = "Unknown find type";
                                }

                                ServerConfiguration.LogInfo($"WorldType: {findWorldByNameRequest.WorldType} ({findWorldType})");
                            }

                            var appIds = ServerConfiguration.Database.GetAppIds();
                            var appIdList = appIds.Result.ToList();
                            string appName = null;

                            foreach (var AppId in appIdList)
                            {
                                if (AppId.AppIds.Contains(data.ClientObject.ApplicationId))
                                    appName = AppId.Name;
                            };

                            #region FindGameWorld
                            if (findWorldByNameRequest.WorldType == MediusFindWorldType.FindGameWorld)
                            {
                                var gameWorldNameList = channel.Channels.Where(x => x.AppType == MediusApplicationType.MediusAppTypeGame)
                                    .Select(x => new MediusFindWorldByNameResponse()
                                    {
                                        MessageID = findWorldByNameRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        ApplicationID = data.ClientObject.ApplicationId,
                                        ApplicationName = appName,
                                        ApplicationType = MediusApplicationType.MediusAppTypeGame,
                                        MediusWorldID = MediusClass.Manager.GetChannelByChannelName(findWorldByNameRequest.Name, data.ApplicationId).Id,
                                        WorldName = channel.Name,
                                        WorldStatus = channel.WorldStatus,
                                        EndOfList = false
                                    }).ToArray();

                                // Set last end of list
                                if (gameWorldNameList.Length > 0)
                                    gameWorldNameList[gameWorldNameList.Length - 1].EndOfList = true;

                                ServerConfiguration.LogInfo($"GetWorldByName - {gameWorldNameList.Length} results returned");

                                data.ClientObject.Queue(gameWorldNameList);
                            }
                            #endregion

                            #region FindLobbyWorld
                            if (findWorldByNameRequest.WorldType == MediusFindWorldType.FindLobbyWorld)
                            {
                                var lobbyNameList = channel.Channels.Where(x => x.AppType == MediusApplicationType.LobbyChatChannel)
                                .Select(x => new MediusFindWorldByNameResponse()
                                {
                                    MessageID = findWorldByNameRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    ApplicationID = data.ClientObject.ApplicationId,
                                    ApplicationName = appName,
                                    ApplicationType = MediusApplicationType.LobbyChatChannel,
                                    MediusWorldID = MediusClass.Manager.GetChannelByChannelName(findWorldByNameRequest.Name, data.ClientObject.ApplicationId).Id,
                                    WorldName = channel.Name,
                                    WorldStatus = channel.WorldStatus,
                                    EndOfList = false
                                }).ToArray();

                                // Set last end of list
                                if (lobbyNameList.Length > 0)
                                    lobbyNameList[lobbyNameList.Length - 1].EndOfList = true;

                                ServerConfiguration.LogInfo($"GetWorldByName - {lobbyNameList.Length} results returned");

                                data.ClientObject.Queue(lobbyNameList);
                            }
                            #endregion
                        }
                        break;
                    }

                case MediusGameWorldPlayerListRequest gameWorldPlayerListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameWorldPlayerListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {gameWorldPlayerListRequest} without being logged in.");

                        var game = MediusClass.Manager.GetGameByGameId(gameWorldPlayerListRequest.MediusWorldID);

                        if (game == null)
                        {
                            data.ClientObject.Queue(new MediusGameWorldPlayerListResponse()
                            {
                                MessageID = gameWorldPlayerListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusGameNotFound,
                                EndOfList = true
                            });
                        }
                        else
                        {
                            ServerConfiguration.LogWarn($"Game Clients in {game.GameName} : {game.Clients.Count()}:{game.Clients.FirstOrDefault().Client.AccountName}");

                            var playerList = game.Clients.Where(x => x != null || x.InGame && x.Client.IsConnected).Select(x => new MediusGameWorldPlayerListResponse()
                            {
                                MessageID = gameWorldPlayerListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                AccountID = x.Client.AccountId,
                                AccountName = x.Client.AccountName,
                                Stats = x.Client.AccountStats,
                                ConnectionClass = x.Client.MediusConnectionType,
                                EndOfList = false
                            }).ToArray();

                            // Set last end of list
                            if (playerList.Length > 0)
                                playerList[playerList.Length - 1].EndOfList = true;

                            data.ClientObject.Queue(playerList);
                        }

                        break;
                    }

                #region GameListFilter

                case MediusGetGameListFilterRequest getGameListFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getGameListFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getGameListFilterRequest} without being logged in.");

                        var filters = data.ClientObject.GameListFilters;
                        if (data.ApplicationId == 10984)
                        {
                            if (filters == null || filters.Count == 0)
                            {
                                data.ClientObject.Queue(new MediusGetGameListFilterResponse0()
                                {
                                    MessageID = getGameListFilterRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }
                            else
                            {
                                // Generate messages per filter
                                var filterResponses = filters.Select(x => new MediusGetGameListFilterResponse0()
                                {
                                    MessageID = getGameListFilterRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    FilterField = x.FilterField,
                                    ComparisonOperator = x.ComparisonOperator,
                                    BaselineValue = (int)x.BaselineValue,
                                    EndOfList = false
                                }).ToList();

                                // Set end of list
                                filterResponses[filterResponses.Count - 1].EndOfList = true;

                                // Add to responses
                                data.ClientObject.Queue(filterResponses);
                            }
                        }
                        else
                        {
                            if (filters == null || filters.Count == 0)
                            {
                                data.ClientObject.Queue(new MediusGetGameListFilterResponse()
                                {
                                    MessageID = getGameListFilterRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusNoResult,
                                    EndOfList = true
                                });
                            }
                            else
                            {
                                // Generate messages per filter
                                var filterResponses = filters.Select(x => new MediusGetGameListFilterResponse()
                                {
                                    MessageID = getGameListFilterRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    BaselineValue = (int)x.BaselineValue,
                                    ComparisonOperator = x.ComparisonOperator,
                                    FilterField = x.FilterField,
                                    FilterID = x.FieldID,
                                    Mask = x.Mask,
                                    EndOfList = false
                                }).ToList();

                                // Set end of list
                                filterResponses[filterResponses.Count - 1].EndOfList = true;

                                // Add to responses
                                data.ClientObject.Queue(filterResponses);
                            }
                        }

                        break;
                    }

                case MediusSetGameListFilterRequest setGameListFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setGameListFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setGameListFilterRequest} without being logged in.");

                        // Set filter
                        if (data.ClientObject != null)
                        {
                            var filter = data.ClientObject.SetGameListFilter(setGameListFilterRequest);

                            if (data.ClientObject.ApplicationId == 10782)
                            {

                                // Give reply
                                data.ClientObject.Queue(new MediusSetGameListFilterResponse()
                                {
                                    MessageID = setGameListFilterRequest.MessageID,
                                    StatusCode = filter == null ? MediusCallbackStatus.MediusSetGameListFilterFailed : MediusCallbackStatus.MediusSuccess,
                                    FilterID = filter.FieldID
                                });

                                /*
                                // Give reply
                                data.ClientObject.Queue(new MediusSetGameListFilterResponse0()
                                {
                                    MessageID = setGameListFilterRequest.MessageID,
                                    StatusCode = filter == null ? MediusCallbackStatus.MediusSetGameListFilterFailed : MediusCallbackStatus.MediusSuccess,
                                });
                                */
                            }
                            else
                            {
                                // Give reply
                                data.ClientObject.Queue(new MediusSetGameListFilterResponse()
                                {
                                    MessageID = setGameListFilterRequest.MessageID,
                                    StatusCode = filter == null ? MediusCallbackStatus.MediusSetGameListFilterFailed : MediusCallbackStatus.MediusSuccess,
                                    FilterID = filter?.FieldID ?? 0
                                });
                            }

                        }
                        else
                        {
                            // Give reply
                            data.ClientObject.Queue(new MediusSetGameListFilterResponse()
                            {
                                MessageID = setGameListFilterRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusPlayerNotPrivileged,
                                FilterID = 0
                            });
                        }

                        break;
                    }

                case MediusSetGameListFilterRequest0 setGameListFilterRequest0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setGameListFilterRequest0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setGameListFilterRequest0} without being logged in.");

                        // Set filter
                        var filter = data.ClientObject.SetGameListFilter(setGameListFilterRequest0);

                        // Give reply
                        data.ClientObject.Queue(new MediusSetGameListFilterResponse0()
                        {
                            MessageID = setGameListFilterRequest0.MessageID,
                            StatusCode = filter == null ? MediusCallbackStatus.MediusFail : MediusCallbackStatus.MediusSuccess,
                        });
                        break;
                    }

                case MediusClearGameListFilterRequest clearGameListFilterRequest:
                    {
                        // ERROR - Need a session 
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {clearGameListFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {clearGameListFilterRequest} without being logged in.");

                        // Remove
                        data.ClientObject.ClearGameListFilter(clearGameListFilterRequest.FilterID);

                        // 
                        data.ClientObject.Queue(new MediusClearGameListFilterResponse()
                        {
                            MessageID = clearGameListFilterRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });

                        break;
                    }

                case MediusClearGameListFilterRequest0 clearGameListFilterRequest0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {clearGameListFilterRequest0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {clearGameListFilterRequest0} without being logged in.");

                        // Remove
                        data.ClientObject.ClearGameListFilter(clearGameListFilterRequest0.FilterID);

                        // 
                        data.ClientObject.Queue(new MediusClearGameListFilterResponse()
                        {
                            MessageID = clearGameListFilterRequest0.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });

                        break;
                    }
                #endregion

                #region CreateGame
                case MediusCreateGameRequest createGameRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createGameRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createGameRequest} without being logged in.");

                        // validate name
                        if (!MediusClass.PassTextFilter(data.ClientObject.ApplicationId, TextFilterContext.GAME_NAME, Convert.ToString(createGameRequest.GameName)))
                        {
                            data.ClientObject.Queue(new MediusCreateGameResponse()
                            {
                                MessageID = createGameRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                            return;
                        }

                        // Send to plugins
                        await MediusClass.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_CREATE_GAME, new OnPlayerRequestArgs() { Player = data.ClientObject, Request = createGameRequest });

                        await MediusClass.Manager.CreateGame(data.ClientObject, createGameRequest);
                        break;
                    }

                case MediusCreateGameRequest0 createGameRequest0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createGameRequest0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createGameRequest0} without being logged in.");

                        // validate name
                        if (!MediusClass.PassTextFilter(data.ClientObject.ApplicationId, TextFilterContext.GAME_NAME, Convert.ToString(createGameRequest0.GameName)))
                        {
                            data.ClientObject.Queue(new MediusCreateGameResponse()
                            {
                                MessageID = createGameRequest0.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                            return;
                        }

                        // Send to plugins
                        await MediusClass.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_CREATE_GAME, new OnPlayerRequestArgs() { Player = data.ClientObject, Request = createGameRequest0 });

                        await MediusClass.Manager.CreateGame(data.ClientObject, createGameRequest0);
                        break;
                    }

                case MediusCreateGameRequest1 createGameRequest1:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createGameRequest1} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createGameRequest1} without being logged in.");

                        // validate name
                        if (!MediusClass.PassTextFilter(data.ClientObject.ApplicationId, TextFilterContext.GAME_NAME, Convert.ToString(createGameRequest1.GameName)))
                        {
                            data.ClientObject.Queue(new MediusCreateGameResponse()
                            {
                                MessageID = createGameRequest1.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                            return;
                        }

                        if (data.ClientObject.ApplicationId == 10994 && createGameRequest1.MaxPlayers == 16)
                        {
                            ServerConfiguration.LogWarn("JaKXO: Overriding request GameHostType"); //Change request for CLAN_CHAT in Jak X
                            createGameRequest1.GameHostType = MediusGameHostType.MediusGameHostClientServer;
                        }

                        // Send to plugins
                        await MediusClass.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_CREATE_GAME, new OnPlayerRequestArgs() { Player = data.ClientObject, Request = createGameRequest1 });

                        await MediusClass.Manager.CreateGame1(data.ClientObject, createGameRequest1);
                        break;
                    }
                #endregion

                #region JoinGame
                case MediusJoinGameRequest joinGameRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinGameRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinGameRequest} without being logged in.");

                        // Send to plugins
                        await MediusClass.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_JOIN_GAME, new OnPlayerRequestArgs() { Player = data.ClientObject, Request = joinGameRequest });

                        await MediusClass.Manager.JoinGame(data.ClientObject, joinGameRequest);
                        break;
                    }

                case MediusJoinGameRequest0 joinGameRequest0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinGameRequest0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinGameRequest0} without being logged in.");

                        // Send to plugins
                        await MediusClass.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_JOIN_GAME, new OnPlayerRequestArgs() { Player = data.ClientObject, Request = joinGameRequest0 });

                        MediusClass.Manager.JoinGame0(data.ClientObject, joinGameRequest0);
                        break;
                    }
                #endregion

                #region Reports
                case MediusWorldReport0 worldReport0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {worldReport0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {worldReport0} without being logged in.");

                        if (data.ClientObject.CurrentGame != null)
                            await data.ClientObject.CurrentGame.OnWorldReport0(worldReport0);
                        break;
                    }

                case MediusWorldReport worldReport:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {worldReport} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {worldReport} without being logged in.");

                        if (data.ClientObject.CurrentGame != null)
                            await data.ClientObject.CurrentGame.OnWorldReport(worldReport);
                        break;
                    }

                case MediusPlayerReport playerReport:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {playerReport} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel},{data.ClientObject} sent {playerReport} without being logged in.");

                        if (data.ClientObject == null)
                            ServerConfiguration.LogWarn($"PlayerUpdate Failed ({data.ClientObject}) Unable to update player cache");
                        else
                        {
                            if (playerReport.Stats == data.ClientObject.AccountStats)
                                ServerConfiguration.LogInfo($"Ignoring a player report with unchanged account stats (AccountID={data.ClientObject.AccountId} MediusWorldID={playerReport.MediusWorldID})");
                            else
                            {
                                MediusClass.AntiCheatPlugin.mc_anticheat_event_msg_PLAYERREPORT(AnticheatEventCode.anticheatPLAYERREPORT, playerReport.MediusWorldID, data.ClientObject.AccountId, MediusClass.AntiCheatClient, playerReport.Stats, 256);

                                if (data.ClientObject.CurrentGame?.Id == playerReport.MediusWorldID &&
                                    data.ClientObject.SessionKey == playerReport.SessionKey)
                                {
                                    data.ClientObject.OnPlayerReport(playerReport);
                                    ServerConfiguration.LogInfo($"Player was updated on Game World (AccountID={data.ClientObject.AccountId} MediusWorldID={playerReport.MediusWorldID})");
                                }
                            }
                        }

                        break;
                    }

                case MediusEndGameReport endGameReport:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {endGameReport} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {endGameReport} without being logged in.");

                        if (data.ClientObject.CurrentGame != null)
                            await data.ClientObject.CurrentGame.OnEndGameReport(endGameReport);

                        break;
                    }
                #endregion
                #endregion

                #region Channel

                case MediusGetLobbyPlayerNames_ExtraInfoRequest getLobbyPlayerNames_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLobbyPlayerNames_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLobbyPlayerNames_ExtraInfoRequest} without being logged in.");

                        var channel = data.ClientObject.CurrentChannel;
                        if (channel == null)
                        {
                            data.ClientObject.Queue(new MediusGetLobbyPlayerNames_ExtraInfoResponse()
                            {
                                MessageID = getLobbyPlayerNames_ExtraInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusWMError,
                                AccountID = 0,
                                AccountName = "NONE",
                                EndOfList = true
                            });
                        }
                        else if (channel.PlayerCount <= 0)
                        {
                            data.ClientObject.Queue(new MediusGetLobbyPlayerNames_ExtraInfoResponse()
                            {
                                MessageID = getLobbyPlayerNames_ExtraInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            });
                        }
                        else
                        {
                            var results = channel.Clients.Where(x => x.IsConnected).Select(x => new MediusGetLobbyPlayerNames_ExtraInfoResponse()
                            {
                                MessageID = getLobbyPlayerNames_ExtraInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                AccountID = x.AccountId,
                                AccountName = x.AccountName,
                                OnlineState = new MediusPlayerOnlineState()
                                {
                                    ConnectStatus = x.PlayerStatus,
                                    GameName = x.CurrentGame?.GameName,
                                    LobbyName = x.CurrentChannel?.Name,
                                    MediusGameWorldID = x.CurrentGame?.Id ?? -1,
                                    MediusLobbyWorldID = x.CurrentChannel?.Id ?? -1
                                },
                                EndOfList = false
                            }).ToArray();

                            if (results.Length > 0)
                                results[results.Length - 1].EndOfList = true;

                            data.ClientObject.Queue(results);
                        }

                        break;
                    }

                case MediusGetLobbyPlayerNamesRequest getLobbyPlayerNamesRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLobbyPlayerNamesRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLobbyPlayerNamesRequest} without being logged in.");

                        var channel = data.ClientObject.CurrentChannel;
                        if (channel == null)
                        {
                            data.ClientObject.Queue(new MediusGetLobbyPlayerNamesResponse()
                            {
                                MessageID = getLobbyPlayerNamesRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            });
                        }
                        else
                        {
                            var results = channel.Clients.Where(x => x.IsConnected).Select(x => new MediusGetLobbyPlayerNamesResponse()
                            {
                                MessageID = getLobbyPlayerNamesRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                AccountID = x.AccountId,
                                AccountName = x.AccountName,
                                EndOfList = false
                            }).ToArray();

                            if (results.Length > 0)
                                results[results.Length - 1].EndOfList = true;

                            data.ClientObject.Queue(results);
                        }

                        break;
                    }

                case MediusGetWorldSecurityLevelRequest getWorldSecurityLevelRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getWorldSecurityLevelRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getWorldSecurityLevelRequest} without being logged in.");

                        data.ClientObject.Queue(new MediusGetWorldSecurityLevelResponse()
                        {
                            MessageID = getWorldSecurityLevelRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            MediusWorldID = getWorldSecurityLevelRequest.MediusWorldID,
                            AppType = MediusApplicationType.MediusAppTypeGame,
                            SecurityLevel = MediusWorldSecurityLevelType.WORLD_SECURITY_NONE,
                        });

                        /*
                        //Fetch Channel by MediusID and AppID
                        var channel = MediusClass.Manager.GetChannelByChannelId(getWorldSecurityLevelRequest.MediusWorldID, data.ClientObject.ApplicationId);

                        Logger.Warn($"MediusWorldID: {getWorldSecurityLevelRequest.MediusWorldID}\nAppType: {channel.AppType}\nSecurityLevel: {channel.SecurityLevel}");

                        if(getWorldSecurityLevelRequest.AppType != MediusApplicationType.LobbyChatChannel ||
                           getWorldSecurityLevelRequest.AppType != MediusApplicationType.MediusAppTypeGame)
                        {
                            #region IncorrectAppType
                            data.ClientObject.Queue(new MediusGetWorldSecurityLevelResponse()
                            {
                                MessageID = getWorldSecurityLevelRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusInvalidRequestMsg,
                                MediusWorldID = getWorldSecurityLevelRequest.MediusWorldID,
                                AppType = MediusApplicationType.ExtraMediusApplicationType,
                                SecurityLevel = channel.SecurityLevel
                            });
                            #endregion
                        }
                        else {

                            if (getWorldSecurityLevelRequest.AppType == MediusApplicationType.LobbyChatChannel)
                            {
                                #region LobbyChatChannel
                                if (channel != null)
                                {
                                    //Send back Successful SecurityLevel and AppType for the correct Channel
                                    data.ClientObject.Queue(new MediusGetWorldSecurityLevelResponse()
                                    {
                                        MessageID = getWorldSecurityLevelRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        MediusWorldID = getWorldSecurityLevelRequest.MediusWorldID,
                                        AppType = MediusApplicationType.LobbyChatChannel,
                                        SecurityLevel = channel.SecurityLevel
                                    });
                                } else
                                {
                                    //Send back Successful SecurityLevel and AppType for the correct Channel
                                    data.ClientObject.Queue(new MediusGetWorldSecurityLevelResponse()
                                    {
                                        MessageID = getWorldSecurityLevelRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusWMError,
                                        MediusWorldID = getWorldSecurityLevelRequest.MediusWorldID,
                                        AppType = MediusApplicationType.LobbyChatChannel,
                                        SecurityLevel = MediusWorldSecurityLevelType.WORLD_SECURITY_NONE,
                                    });
                                }
                                #endregion
                            }
                            else {
                                #region MediusAppTypeGame
                                if (channel != null)
                                {
                                    //Send back Successful SecurityLevel and AppType for the correct Channel
                                    data.ClientObject.Queue(new MediusGetWorldSecurityLevelResponse()
                                    {
                                        MessageID = getWorldSecurityLevelRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        MediusWorldID = getWorldSecurityLevelRequest.MediusWorldID,
                                        AppType = MediusApplicationType.MediusAppTypeGame,
                                        SecurityLevel = channel.SecurityLevel
                                    });
                                }
                                else
                                {
                                    //Send back Successful SecurityLevel and AppType for the correct Channel
                                    data.ClientObject.Queue(new MediusGetWorldSecurityLevelResponse()
                                    {
                                        MessageID = getWorldSecurityLevelRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusWMError,
                                        MediusWorldID = getWorldSecurityLevelRequest.MediusWorldID,
                                        AppType = MediusApplicationType.MediusAppTypeGame,
                                        SecurityLevel = MediusWorldSecurityLevelType.WORLD_SECURITY_NONE,
                                    });
                                }
                                #endregion
                            }
                        }
                        */
                        break;
                    }

                case MediusSetLobbyWorldFilterRequest setLobbyWorldFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setLobbyWorldFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setLobbyWorldFilterRequest} without being logged in.");

                        if (data.ClientObject != null)
                        {
                            //Set the Lobby World Filter on the player client object
                            await data.ClientObject.SetLobbyWorldFilter(setLobbyWorldFilterRequest);

                            data.ClientObject.Queue(new MediusSetLobbyWorldFilterResponse()
                            {
                                MessageID = setLobbyWorldFilterRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                FilterMask1 = setLobbyWorldFilterRequest.FilterMask1,
                                FilterMask2 = setLobbyWorldFilterRequest.FilterMask2,
                                FilterMask3 = setLobbyWorldFilterRequest.FilterMask3,
                                FilterMask4 = setLobbyWorldFilterRequest.FilterMask4,
                                FilterMaskLevel = setLobbyWorldFilterRequest.FilterMaskLevel,
                                LobbyFilterType = setLobbyWorldFilterRequest.LobbyFilterType
                            });
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusSetLobbyWorldFilterResponse()
                            {
                                MessageID = setLobbyWorldFilterRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                        }


                        break;
                    }

                case MediusSetLobbyWorldFilterRequest1 setLobbyWorldFilterRequest1:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setLobbyWorldFilterRequest1} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setLobbyWorldFilterRequest1} without being logged in.");

                        if (data.ClientObject != null)
                        {
                            //Set the Lobby World Filter on the player client object
                            await data.ClientObject.SetLobbyWorldFilter(setLobbyWorldFilterRequest1);

                            data.ClientObject.Queue(new MediusSetLobbyWorldFilterResponse()
                            {
                                MessageID = setLobbyWorldFilterRequest1.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                FilterMask1 = setLobbyWorldFilterRequest1.FilterMask1,
                                FilterMask2 = setLobbyWorldFilterRequest1.FilterMask2,
                                FilterMask3 = setLobbyWorldFilterRequest1.FilterMask3,
                                FilterMask4 = setLobbyWorldFilterRequest1.FilterMask4,
                                FilterMaskLevel = setLobbyWorldFilterRequest1.FilterMaskLevel,
                                LobbyFilterType = setLobbyWorldFilterRequest1.LobbyFilterType
                            });
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusSetLobbyWorldFilterResponse()
                            {
                                MessageID = setLobbyWorldFilterRequest1.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                        }
                        break;
                    }

                #region CreateChannel
                case MediusCreateChannelRequest createChannelRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createChannelRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createChannelRequest} without being logged in.");

                        // Create channel
                        Channel channel = new Channel(createChannelRequest);

                        // Check for channel with same name
                        var existingChannel = MediusClass.Manager.GetChannelByChannelName(channel.Name, channel.ApplicationId);
                        if (existingChannel != null)
                        {
                            // Send to client
                            data.ClientObject.Queue(new MediusCreateChannelResponse()
                            {
                                MessageID = createChannelRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusChannelNameExists,
                                MediusWorldID = existingChannel.Id
                            });
                        }
                        else
                        {
                            // Add
                            await MediusClass.Manager.AddChannel(channel);

                            // Send to client
                            data.ClientObject.Queue(new MediusCreateChannelResponse()
                            {
                                MessageID = createChannelRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                MediusWorldID = channel.Id
                            });
                        }
                        break;
                    }

                case MediusCreateChannelRequest0 createChannelRequest0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createChannelRequest0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createChannelRequest0} without being logged in.");

                        // Create channel
                        Channel channel = new Channel(createChannelRequest0);

                        // Check for channel with same name
                        var existingChannel = MediusClass.Manager.GetChannelByChannelName(channel.Name, channel.ApplicationId);
                        if (existingChannel != null)
                        {
                            // Send to client
                            data.ClientObject.Queue(new MediusCreateChannelResponse()
                            {
                                MessageID = createChannelRequest0.MessageID,
                                StatusCode = MediusCallbackStatus.MediusChannelNameExists,
                                MediusWorldID = existingChannel.Id
                            });
                        }
                        else
                        {
                            // Add
                            await MediusClass.Manager.AddChannel(channel);

                            // Send to client
                            data.ClientObject.Queue(new MediusCreateChannelResponse()
                            {
                                MessageID = createChannelRequest0.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                MediusWorldID = channel.Id
                            });
                        }
                        break;
                    }

                case MediusCreateChannelRequest1 createChannelRequest1:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createChannelRequest1} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {createChannelRequest1} without being logged in.");

                        // Create channel
                        Channel channel = new Channel(createChannelRequest1);

                        if (createChannelRequest1.MaxPlayers > 257)
                            channel.MaxPlayers = createChannelRequest1.MaxPlayers;

                        // Check for channel with same name
                        var existingChannel = MediusClass.Manager.GetChannelByChannelName(channel.Name, channel.ApplicationId);
                        //Logger.Warn($"existingChannelId {existingChannel.Id} || channelId {channel.Id}");
                        if (existingChannel != null)
                        {
                            // Send to client
                            data.ClientObject.Queue(new MediusCreateChannelResponse()
                            {
                                MessageID = createChannelRequest1.MessageID,
                                StatusCode = MediusCallbackStatus.MediusWMError,
                                MediusWorldID = existingChannel.Id
                            });
                        }
                        else
                        {
                            if (createChannelRequest1.LobbyName.StartsWith("CLAN_"))
                            {
                                ServerConfiguration.LogInfo($"SFO_HACK:Overriding Clan Lobby {createChannelRequest1.LobbyName} MaxPlayers from {createChannelRequest1.MaxPlayers} to {MediusClass.Settings.SFOOverrideClanLobbyMaxPlayers}");
                                createChannelRequest1.MaxPlayers = MediusClass.Settings.SFOOverrideClanLobbyMaxPlayers;
                            }
                            /*
                            if(data.ClientObject.CharacterEncoding == MediusCharacterEncodingType.MediusCharacterEncoding_UTF8)
                            {
                                UTF8Encoding utf8 = new UTF8Encoding();
                                byte[] encodedBytes = utf8.GetBytes(channel.Name);
                            }
                            */
                            // Add
                            await MediusClass.Manager.AddChannel(channel);

                            // Send to client
                            data.ClientObject.Queue(new MediusCreateChannelResponse()
                            {
                                MessageID = createChannelRequest1.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                MediusWorldID = channel.Id
                            });
                        }
                        break;
                    }
                #endregion

                case MediusJoinChannelRequest joinChannelRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinChannelRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinChannelRequest} without being logged in.");

                        List<int> notSecure = new List<int>() { 10010, 10190 };

                        var channel = MediusClass.Manager.GetChannelByChannelId(joinChannelRequest.MediusWorldID, data.ClientObject.ApplicationId);
                        if (channel == null)
                        {
                            ServerConfiguration.LogWarn($"{data.ClientObject.AccountName} attempting to join non-existent channel {joinChannelRequest}");

                            data.ClientObject.Queue(new MediusJoinChannelResponse()
                            {
                                MessageID = joinChannelRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusChannelNotFound
                            });
                        }
                        else if (channel.SecurityLevel == MediusWorldSecurityLevelType.WORLD_SECURITY_PLAYER_PASSWORD && joinChannelRequest.LobbyChannelPassword != channel.Password)
                        {
                            data.ClientObject.Queue(new MediusJoinChannelResponse()
                            {
                                MessageID = joinChannelRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusInvalidPassword
                            });
                        }
                        else
                        {
                            ServerConfiguration.LogInfo($"Channel Joining: {channel.Name} Generic Fields: {channel.GenericField1} {channel.GenericField2} {channel.GenericField3} {channel.GenericField4} {channel.GenericFieldLevel} Type: {channel.Type}");

                            // Join new channel
                            await data.ClientObject.JoinChannel(channel);

                            // Indicate the client is connecting to a different part of Medius
                            data.ClientObject.KeepAliveUntilNextConnection();

                            if (notSecure.Contains(data.ClientObject.ApplicationId))
                            {
                                data.ClientObject.Queue(new MediusJoinChannelResponse()
                                {
                                    MessageID = joinChannelRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    ConnectInfo = new NetConnectionInfo()
                                    {
                                        AccessKey = data.ClientObject.Token,
                                        SessionKey = data.ClientObject.SessionKey,
                                        WorldID = channel.Id,
                                        ServerKey = new RSA_KEY(),
                                        AddressList = new NetAddressList()
                                        {
                                            AddressList = new NetAddress[Constants.NET_ADDRESS_LIST_COUNT]
                                            {
                                                new NetAddress() { Address = MediusClass.LobbyServer.IPAddress.ToString(), Port = MediusClass.LobbyServer.TCPPort, AddressType = NetAddressType.NetAddressTypeExternal},
                                                new NetAddress() { AddressType = NetAddressType.NetAddressNone},
                                            }
                                        },
                                        Type = NetConnectionType.NetConnectionTypeClientServerTCP
                                    }
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusJoinChannelResponse()
                                {
                                    MessageID = joinChannelRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    ConnectInfo = new NetConnectionInfo()
                                    {
                                        AccessKey = data.ClientObject.Token,
                                        SessionKey = data.ClientObject.SessionKey,
                                        WorldID = channel.Id,
                                        ServerKey = MediusClass.GlobalAuthPublic,
                                        AddressList = new NetAddressList()
                                        {
                                            AddressList = new NetAddress[Constants.NET_ADDRESS_LIST_COUNT]
                                            {
                                            new NetAddress() { Address = MediusClass.LobbyServer.IPAddress.ToString(), Port = MediusClass.LobbyServer.TCPPort, AddressType = NetAddressType.NetAddressTypeExternal},
                                            new NetAddress() { AddressType = NetAddressType.NetAddressNone},
                                            }
                                        },
                                        Type = NetConnectionType.NetConnectionTypeClientServerTCP
                                    }
                                });
                            }

                        }
                        break;
                    }

                case MediusJoinLeastPopulatedChannelRequest joinLeastPopulatedChannelRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinLeastPopulatedChannelRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {joinLeastPopulatedChannelRequest} without being logged in.");

                        var channel = MediusClass.Manager.GetChannelLeastPoplated(data.ClientObject.ApplicationId);
                        if (channel == null)
                        {
                            // Log
                            ServerConfiguration.LogWarn($"{data.ClientObject.AccountName} attempting to join non-existent channel {joinLeastPopulatedChannelRequest}");

                            data.ClientObject.Queue(new MediusJoinLeastPopulatedChannelResponse()
                            {
                                MessageID = joinLeastPopulatedChannelRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusChannelNotFound
                            });
                        }
                        else
                        {
                            ServerConfiguration.LogInfo($"Joining Least Populated Channel: {channel.Name} PlayerCount {channel.PlayerCount} Generic Fields: {channel.GenericField1} {channel.GenericField2} {channel.GenericField3} {channel.GenericField4} {channel.GenericFieldLevel} Type: {channel.Type}");

                            // Join new channel
                            await data.ClientObject.JoinChannel(channel);

                            // Indicate the client is connecting to a different part of Medius
                            data.ClientObject.KeepAliveUntilNextConnection();

                            data.ClientObject.Queue(new MediusJoinLeastPopulatedChannelResponse()
                            {
                                MessageID = joinLeastPopulatedChannelRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                ConnectInfo = new NetConnectionInfo()
                                {
                                    AccessKey = data.ClientObject.Token,
                                    SessionKey = data.ClientObject.SessionKey,
                                    WorldID = channel.Id,
                                    ServerKey = MediusClass.GlobalAuthPublic,
                                    AddressList = new NetAddressList()
                                    {
                                        AddressList = new NetAddress[Constants.NET_ADDRESS_LIST_COUNT]
                                        {
                                            new NetAddress() { Address = MediusClass.LobbyServer.IPAddress.ToString(), Port = MediusClass.LobbyServer.TCPPort, AddressType = NetAddressType.NetAddressTypeExternal},
                                            new NetAddress() { AddressType = NetAddressType.NetAddressNone},
                                        }
                                    },
                                    Type = NetConnectionType.NetConnectionTypeClientServerTCP
                                }
                            });

                        }
                        break;
                    }

                case MediusChannelInfoRequest channelInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelInfoRequest} without being logged in.");

                        // Find channel
                        var channel = MediusClass.Manager.GetChannelByChannelId(channelInfoRequest.MediusWorldID, data.ClientObject.ApplicationId);
                        if (channel == null)
                        {
                            // No channels
                            data.ClientObject.Queue(new MediusChannelInfoResponse()
                            {
                                MessageID = channelInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult
                            });
                        }
                        else
                        {
                            data.ClientObject.KeepAliveUntilNextConnection();

                            int activePlayerCount;
                            if (MediusClass.Settings.SFOOverrideLobbyPlayerCountThreshold > 0)
                            {
                                activePlayerCount = channel.PlayerCount;
                                if (channel.PlayerCount >= MediusClass.Settings.SFOOverrideLobbyPlayerCountThreshold)
                                {
                                    activePlayerCount = channel.MaxPlayers;
                                    ServerConfiguration.LogInfo($"SFO_HACK:Overriding Lobby ActivePlayerCount from {channel.PlayerCount} to {activePlayerCount}");
                                }
                            }
                            else
                                activePlayerCount = channel.PlayerCount;

                            data.ClientObject.Queue(new MediusChannelInfoResponse()
                            {
                                MessageID = channelInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                LobbyName = channel.Name,
                                ActivePlayerCount = activePlayerCount,
                                MaxPlayers = channel.MaxPlayers
                            });
                        }
                        break;
                    }

                case MediusChannelListRequest channelListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelListRequest} without being logged in.");

                        List<MediusChannelListResponse> channelResponses = new List<MediusChannelListResponse>();

                        IEnumerable<Channel> lobbyChannels = null;

                        //If PS Home Dev/Retail we Filter
                        if (data.ClientObject.ApplicationId == 20371 || data.ClientObject.ApplicationId == 20374)
                        {
                            lobbyChannels = MediusClass.Manager.GetChannelListFiltered(
                                data.ClientObject.ApplicationId,
                                channelListRequest.PageID,
                                channelListRequest.PageSize,
                                ChannelType.Lobby,
                                data.ClientObject.FilterMask1,
                                data.ClientObject.FilterMask2,
                                data.ClientObject.FilterMask3,
                                data.ClientObject.FilterMask4,
                                data.ClientObject.FilterMaskLevel
                            );
                        }
                        else
                        //Default
                        {
                            lobbyChannels = MediusClass.Manager.GetChannelList(
                                data.ClientObject.ApplicationId,
                                channelListRequest.PageID,
                                channelListRequest.PageSize,
                                ChannelType.Lobby
                            );
                        }


                        foreach (var channel in lobbyChannels)
                        {
                            channelResponses.Add(new MediusChannelListResponse()
                            {
                                MessageID = channelListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                MediusWorldID = channel.Id,
                                LobbyName = channel.Name,
                                PlayerCount = channel.PlayerCount,
                                EndOfList = false
                            });
                        }

                        if (channelResponses.Count == 0)
                        {
                            // Return none
                            data.ClientObject.Queue(new MediusChannelListResponse()
                            {
                                MessageID = channelListRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            });
                        }
                        else
                        {
                            // Ensure the end of list flag is set
                            channelResponses[channelResponses.Count - 1].EndOfList = true;

                            // Add to responses
                            data.ClientObject.Queue(channelResponses);
                        }


                        break;
                    }

                case MediusGetTotalChannelsRequest getTotalChannelsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalChannelsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getTotalChannelsRequest} without being logged in.");

                        data.ClientObject.Queue(new MediusGetTotalChannelsResponse()
                        {
                            MessageID = getTotalChannelsRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            Total = MediusClass.Manager.GetChannelCount(ChannelType.Lobby, data.ClientObject.ApplicationId)
                        });
                        break;
                    }

                case MediusChannelList_ExtraInfoRequest1 channelList_ExtraInfoRequest1:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelList_ExtraInfoRequest1} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelList_ExtraInfoRequest1} without a being logged in.");

                        List<MediusChannelList_ExtraInfoResponse> channelResponses = new List<MediusChannelList_ExtraInfoResponse>();

                        // Deadlocked only uses this to connect to a non-game channel (lobby)
                        // So we'll filter by lobby here
                        var channels = MediusClass.Manager.GetChannelList(
                            data.ClientObject.ApplicationId,
                            channelList_ExtraInfoRequest1.PageID,
                            channelList_ExtraInfoRequest1.PageSize,
                            ChannelType.Lobby);

                        foreach (var channel in channels)
                        {
                            channelResponses.Add(new MediusChannelList_ExtraInfoResponse()
                            {
                                MessageID = channelList_ExtraInfoRequest1.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                MediusWorldID = channel.Id,
                                LobbyName = channel.Name,
                                GameWorldCount = (ushort)channel.GameCount,
                                PlayerCount = (ushort)channel.PlayerCount,
                                MaxPlayers = (ushort)channel.MaxPlayers,
                                GenericField1 = (uint)channel.GenericField1,
                                GenericField2 = (uint)channel.GenericField2,
                                GenericField3 = (uint)channel.GenericField3,
                                GenericField4 = (uint)channel.GenericField4,
                                GenericFieldLevel = channel.GenericFieldLevel,
                                SecurityLevel = channel.SecurityLevel,
                                EndOfList = false
                            });
                        }

                        if (channelResponses.Count == 0)
                        {
                            // Return none
                            data.ClientObject.Queue(new MediusChannelList_ExtraInfoResponse()
                            {
                                MessageID = channelList_ExtraInfoRequest1.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            });
                        }
                        else
                        {
                            // Ensure the end of list flag is set
                            channelResponses[channelResponses.Count - 1].EndOfList = true;

                            // Add to responses
                            data.ClientObject.Queue(channelResponses);
                        }
                        break;
                    }

                case MediusChannelList_ExtraInfoRequest0 channelList_ExtraInfoRequest0:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelList_ExtraInfoRequest0} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelList_ExtraInfoRequest0} without being logged in.");

                        List<MediusChannelList_ExtraInfoResponse> channelResponses = new List<MediusChannelList_ExtraInfoResponse>();

                        // Deadlocked only uses this to connect to a non-game channel (lobby)
                        // So we'll filter by lobby here
                        var channels = MediusClass.Manager.GetChannelList(
                            data.ClientObject.ApplicationId,
                            channelList_ExtraInfoRequest0.PageID,
                            channelList_ExtraInfoRequest0.PageSize,
                            ChannelType.Lobby);

                        foreach (var channel in channels)
                        {
                            channelResponses.Add(new MediusChannelList_ExtraInfoResponse()
                            {
                                MessageID = channelList_ExtraInfoRequest0.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                MediusWorldID = channel.Id,
                                LobbyName = channel.Name,
                                GameWorldCount = (ushort)channel.GameCount,
                                PlayerCount = (ushort)channel.PlayerCount,
                                MaxPlayers = (ushort)channel.MaxPlayers,
                                GenericField1 = (uint)channel.GenericField1,
                                GenericField2 = (uint)channel.GenericField2,
                                GenericField3 = (uint)channel.GenericField3,
                                GenericField4 = (uint)channel.GenericField4,
                                GenericFieldLevel = channel.GenericFieldLevel,
                                SecurityLevel = channel.SecurityLevel,
                                EndOfList = false
                            });
                        }

                        if (channelResponses.Count == 0)
                        {
                            // Return none
                            data.ClientObject.Queue(new MediusChannelList_ExtraInfoResponse()
                            {
                                MessageID = channelList_ExtraInfoRequest0.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            });
                        }
                        else
                        {
                            // Ensure the end of list flag is set
                            channelResponses[channelResponses.Count - 1].EndOfList = true;

                            // Add to responses
                            data.ClientObject.Queue(channelResponses);
                        }
                        break;
                    }

                case MediusChannelList_ExtraInfoRequest channelList_ExtraInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelList_ExtraInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {channelList_ExtraInfoRequest} without being logged in.");

                        List<MediusChannelList_ExtraInfoResponse> channelResponses = new List<MediusChannelList_ExtraInfoResponse>();

                        if (data.ClientObject.FilterMaskLevel == 0)
                        {
                            // Deadlocked only uses this to connect to a non-game channel (lobby)
                            // So we'll filter by lobby here
                            var channels = MediusClass.Manager.GetChannelList(
                                data.ClientObject.ApplicationId,
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
                                    GenericField1 = (uint)channel.GenericField1,
                                    GenericField2 = (uint)channel.GenericField2,
                                    GenericField3 = (uint)channel.GenericField3,
                                    GenericField4 = (uint)channel.GenericField4,
                                    GenericFieldLevel = channel.GenericFieldLevel,
                                    SecurityLevel = channel.SecurityLevel,
                                    EndOfList = false
                                });
                            }
                        }
                        else
                        {
                            // Deadlocked only uses this to connect to a non-game channel (lobby)
                            // So we'll filter by lobby here
                            var channels = MediusClass.Manager.GetChannelListFiltered(
                                data.ClientObject.ApplicationId,
                                channelList_ExtraInfoRequest.PageID,
                                channelList_ExtraInfoRequest.PageSize,
                                ChannelType.Lobby,
                                data.ClientObject.FilterMask1,
                                data.ClientObject.FilterMask2,
                                data.ClientObject.FilterMask3,
                                data.ClientObject.FilterMask4,
                                data.ClientObject.FilterMaskLevel);

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
                                    GenericField1 = (uint)channel.GenericField1,
                                    GenericField2 = (uint)channel.GenericField2,
                                    GenericField3 = (uint)channel.GenericField3,
                                    GenericField4 = (uint)channel.GenericField4,
                                    GenericFieldLevel = channel.GenericFieldLevel,
                                    SecurityLevel = channel.SecurityLevel,
                                    EndOfList = false
                                });
                            }
                        }

                        if (channelResponses.Count == 0)
                        {
                            // Return none
                            data.ClientObject.Queue(new MediusChannelList_ExtraInfoResponse()
                            {
                                MessageID = channelList_ExtraInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                                EndOfList = true
                            });
                        }
                        else
                        {
                            // Ensure the end of list flag is set
                            channelResponses[channelResponses.Count - 1].EndOfList = true;

                            // Add to responses
                            data.ClientObject.Queue(channelResponses);
                        }

                        break;
                    }

                #endregion

                #region Co-Locations
                case MediusGetLocationsRequest getLocationsRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLocationsRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getLocationsRequest} without a being logged in.");

                        ServerConfiguration.LogInfo($"Get Locations Request Received Sessionkey: {getLocationsRequest.SessionKey}");
                        await ServerConfiguration.Database.GetLocations(data.ClientObject.ApplicationId).ContinueWith(r =>
                        {
                            var locations = r.Result;

                            if (r.IsCompletedSuccessfully)
                            {
                                if (locations == null || locations.Length == 0)
                                {
                                    data.ClientObject.Queue(new MediusGetLocationsResponse()
                                    {
                                        MessageID = getLocationsRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusNoResult,
                                        EndOfList = true
                                    });
                                }
                                else
                                {
                                    var responses = locations.Select(x => new MediusGetLocationsResponse()
                                    {
                                        MessageID = getLocationsRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess,
                                        LocationId = x.Id,
                                        LocationName = x.Name
                                    }).ToList();

                                    ServerConfiguration.LogInfo("GetLocationsRequest  success");
                                    ServerConfiguration.LogInfo($"NumLocations returned[{responses.Count}]");

                                    responses[responses.Count - 1].EndOfList = true;
                                    data.ClientObject.Queue(responses);
                                }
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusGetLocationsResponse()
                                {
                                    MessageID = getLocationsRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusDBError,
                                    LocationId = -1,
                                    LocationName = "0",
                                    EndOfList = true
                                });
                            }
                        });
                        break;
                    }

                #endregion

                #region Medius File Services
                #region FileList
                case MediusFileListRequest fileListRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileListRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileListRequest} without being logged in.");

                        await ServerConfiguration.Database.getFileList(data.ClientObject.ApplicationId,
                            fileListRequest.FileNameBeginsWith,
                            fileListRequest.OwnerByID
                            ).ContinueWith(r => {
                                if (r.IsCompletedSuccessfully && r.Result != null && r.Result.Count > 0)
                                {
                                    List<MediusFileListResponse> fileListExtResponses = new List<MediusFileListResponse>();

                                    var rootPath = MediusClass.GetFileAppIdPath(data.ClientObject.ApplicationId);
                                    foreach (var fileReturned in r.Result)
                                    {
                                        ServerConfiguration.LogWarn($"Files returns: {r.Result.Count}");

                                        var filesListExt = MediusClass.Manager.GetFilesListExt(rootPath,
                                                fileReturned.FileName,
                                                fileListRequest.PageSize,
                                                fileListRequest.StartingEntryNumber,
                                                data.ClientObject.ApplicationId);


                                        fileListExtResponses.Add(new MediusFileListResponse()
                                        {
                                            MessageID = fileListRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            MediusFileToList = new MediusFile
                                            {
                                                FileName = fileReturned.FileName,
                                                ServerChecksum = fileReturned.ServerChecksum,
                                                FileID = fileReturned.FileID,
                                                FileSize = fileReturned.FileSize,
                                                CreationTimeStamp = fileReturned.CreationTimeStamp,
                                                OwnerID = fileReturned.OwnerID,
                                                GroupID = fileReturned.GroupID,
                                                OwnerPermissionRWX = fileReturned.OwnerPermissionRWX,
                                                GroupPermissionRWX = fileReturned.GroupPermissionRWX,
                                                GlobalPermissionRWX = fileReturned.GlobalPermissionRWX,
                                                ServerOperationID = fileReturned.ServerOperationID,
                                            },
                                            EndOfList = false,
                                        });

                                    }

                                    if (fileListExtResponses.Count == 0)
                                    {
                                        // Return none
                                        data.ClientObject.Queue(new MediusFileListResponse()
                                        {
                                            MessageID = fileListRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            MediusFileToList = new MediusFile
                                            {

                                            },
                                            EndOfList = true,
                                        });
                                    }
                                    else
                                    {
                                        // Ensure the end of list flag is set
                                        fileListExtResponses[fileListExtResponses.Count - 1].EndOfList = true;

                                        // Add to responses
                                        data.ClientObject.Queue(fileListExtResponses);
                                    }
                                }
                                else
                                {
                                    #region Default (MediusDBError) [NORESULTS]
                                    {
                                        // Return none
                                        data.ClientObject.Queue(new MediusFileListResponse()
                                        {
                                            MessageID = fileListRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            MediusFileToList = new MediusFile
                                            {

                                            },
                                            EndOfList = true,
                                        });
                                    }
                                    #endregion
                                }
                            });

                        break;
                    }
                #endregion

                #region FileListExt
                case MediusFileListExtRequest fileListExtRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileListExtRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileListExtRequest} without being logged in.");

                        List<MediusFileListExtResponse> fileListExtResponses = new List<MediusFileListExtResponse>();

                        await ServerConfiguration.Database.getFileListExt(data.ClientObject.ApplicationId,
                            fileListExtRequest.FileNameBeginsWith,
                            fileListExtRequest.OwnerByID,
                            fileListExtRequest.metaData
                            ).ContinueWith(r => {
                                if (r.IsCompletedSuccessfully && r.Result != null && r.Result.Count > 0)
                                {

                                    //var rootPath = MediusClass.GetFileAppIdPath(data.ClientObject.ApplicationId);
                                    foreach (var fileReturned in r.Result)
                                    {
                                        /*
                                        var filesListExt = MediusClass.Manager.GetFilesListExt(rootPath,
                                                fileReturned.FileName,
                                                fileListExtRequest.PageSize,
                                                fileListExtRequest.StartingEntryNumber,
                                                data.ClientObject.ApplicationId);
                                        */

                                        ServerConfiguration.LogWarn($"Files returns Test2: {r.Result.Count} ");
                                        fileListExtResponses.Add(new MediusFileListExtResponse()
                                        {
                                            MessageID = fileListExtRequest.MessageID,
                                            MetaValue = fileListExtRequest.metaData.Value,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            MediusFileInfo = new MediusFile
                                            {
                                                FileName = fileReturned.FileName,
                                                ServerChecksum = fileReturned.ServerChecksum,
                                                FileID = fileReturned.FileID,
                                                FileSize = fileReturned.FileSize,
                                                CreationTimeStamp = fileReturned.CreationTimeStamp,
                                                OwnerID = fileReturned.OwnerID,
                                                GroupID = fileReturned.GroupID,
                                                OwnerPermissionRWX = fileReturned.OwnerPermissionRWX,
                                                GroupPermissionRWX = fileReturned.GroupPermissionRWX,
                                                GlobalPermissionRWX = fileReturned.GlobalPermissionRWX,
                                                ServerOperationID = fileReturned.ServerOperationID,
                                            },
                                            EndOfList = false,
                                        });

                                    }

                                    switch (fileListExtRequest.sortBy)
                                    {
                                        case MediusFileSortBy.MFSortByNothing:
                                            {
                                                ServerConfiguration.LogInfo("Not Sorting!");
                                                return;
                                            }
                                        case MediusFileSortBy.MFSortByName:
                                            {
                                                ServerConfiguration.LogInfo("Sorting By Name!");
                                                fileListExtResponses.Sort((x, y) => string.Compare(x.MediusFileInfo.FileName, y.MediusFileInfo.FileName));
                                                return;
                                            }
                                    }

                                    // Sort by MEDIUS_ASCENDING
                                    if (fileListExtRequest.sortOrder == MediusSortOrder.MEDIUS_ASCENDING)
                                    {
                                        fileListExtResponses.OrderBy(x => x);
                                    }
                                    else // MEDIUS_DESCENDING
                                    {
                                        fileListExtResponses.OrderByDescending(x => x);
                                    }

                                    if (fileListExtResponses.Count == 0)
                                    {
                                        // Return none
                                        data.ClientObject.Queue(new MediusFileListExtResponse()
                                        {
                                            MediusFileInfo = new MediusFile
                                            {

                                            },
                                            MetaValue = null,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            MessageID = fileListExtRequest.MessageID,
                                            EndOfList = true,
                                        });
                                    }

                                    // Ensure the end of list flag is set
                                    fileListExtResponses[fileListExtResponses.Count - 1].EndOfList = true;

                                    // Add to responses
                                    data.ClientObject.Queue(fileListExtResponses);


                                    /*
                                    #region NBA 07 PS3
                                    //If its NBA 07 PS3
                                    if (data.ApplicationId == 20244)
                                    {
                                        string nba07Path = rootPath + @"\NBA07\";
                                        if (nba07Path != null)
                                        {
                                            var filesList = MediusClass.Manager.GetFilesList(nba07Path,
                                                fileListExtRequest.FileNameBeginsWith,
                                                fileListExtRequest.PageSize,
                                                fileListExtRequest.StartingEntryNumber);

                                            foreach (var file in filesList)
                                            {
                                                fileListExtResponses.Add(new MediusFileListExtResponse()
                                                {
                                                    MessageID = fileListExtRequest.MessageID,
                                                    MetaValue = "",
                                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                                    MediusFileToList = new MediusFile
                                                    {
                                                        Filename = file.Filename,
                                                        ServerChecksum = file.ServerChecksum,
                                                        FileID = file.FileID,
                                                        FileSize = file.FileSize,
                                                        CreationTimeStamp = file.CreationTimeStamp,
                                                    },
                                                    EndOfList = false,
                                                });
                                            }

                                            if (fileListExtResponses.Count == 0)
                                            {
                                                // Return none
                                                data.ClientObject.Queue(new MediusFileListResponse()
                                                {
                                                    MessageID = fileListExtRequest.MessageID,
                                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                                    MediusFileToList = new MediusFile
                                                    {

                                                    },
                                                    EndOfList = true,
                                                });
                                            }
                                            else
                                            {
                                                // Ensure the end of list flag is set
                                                fileListExtResponses[fileListExtResponses.Count - 1].EndOfList = true;

                                                // Add to responses
                                                data.ClientObject.Queue(fileListExtResponses);
                                            }
                                        }
                                    }
                                    #endregion
                                    */

                                }
                                else
                                {
                                    #region Default (MediusDBError) [NORESULTS]
                                    {
                                        // Return none
                                        data.ClientObject.Queue(new MediusFileListExtResponse()
                                        {
                                            MediusFileInfo = new MediusFile
                                            {

                                            },
                                            MetaValue = null,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            MessageID = fileListExtRequest.MessageID,
                                            EndOfList = true,
                                        });
                                    }
                                    #endregion
                                }
                            });
                        break;
                    }
                #endregion

                #region FileGetAttributesRequest
                case MediusFileGetAttributesRequest fileGetAttributesRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileGetAttributesRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileGetAttributesRequest} without being logged in.");

                        FileDTO fileDTO = new FileDTO()
                        {
                            AppId = data.ClientObject.ApplicationId,
                            FileName = fileGetAttributesRequest.MediusFileInfo.FileName,
                            ServerChecksum = fileGetAttributesRequest.MediusFileInfo.ServerChecksum.ToString(),
                            FileID = fileGetAttributesRequest.MediusFileInfo.FileID,
                            FileSize = fileGetAttributesRequest.MediusFileInfo.FileSize,
                            CreationTimeStamp = fileGetAttributesRequest.MediusFileInfo.CreationTimeStamp,
                            OwnerID = fileGetAttributesRequest.MediusFileInfo.OwnerID,
                            GroupID = fileGetAttributesRequest.MediusFileInfo.GroupID,
                            OwnerPermissionRWX = fileGetAttributesRequest.MediusFileInfo.OwnerPermissionRWX,
                            GroupPermissionRWX = fileGetAttributesRequest.MediusFileInfo.GroupPermissionRWX,
                            GlobalPermissionRWX = fileGetAttributesRequest.MediusFileInfo.GlobalPermissionRWX,
                            ServerOperationID = fileGetAttributesRequest.MediusFileInfo.ServerOperationID,
                        };

                        await ServerConfiguration.Database.GetFileAttributes(fileDTO).ContinueWith(r =>
                        {
                            var path = MediusClass.GetFileSystemPath(data.ClientObject.ApplicationId, fileGetAttributesRequest.MediusFileInfo.FileName);

                            data.ClientObject.Queue(new MediusFileGetAttributesResponse()
                            {
                                MediusFileInfo = new MediusFile()
                                {
                                    FileName = fileGetAttributesRequest.MediusFileInfo.FileName,
                                },
                                MediusFileAttributesResponse = new MediusFileAttributes()
                                {
                                    NumberAccesses = 0,
                                    StreamableFlag = 0,
                                },
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                MessageID = fileGetAttributesRequest.MessageID,
                            });
                        });
                        break;
                    }
                #endregion

                #region FileGetMetaDataRequest
                case MediusFileGetMetaDataRequest fileGetMetaDataRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileGetMetaDataRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileGetMetaDataRequest} without being logged in.");


                        List<MediusFileGetMetaDataResponse> fileGetMetaDataResponses = new List<MediusFileGetMetaDataResponse>();

                        /*
                        var path = MediusClass.GetFileSystemPath(data.ClientObject.ApplicationId, fileGetMetaDataRequest.MediusFileInfo.FileName);
                        if (path == null)
                        {
                            data.ClientObject.Queue(new MediusFileGetMetaDataResponse()
                            {
                                MessageID = fileGetMetaDataRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusNoResult,
                            });
                            break;
                        }
                        else
                        {

                        }
                        */


                        FileDTO fileDTO = new FileDTO()
                        {
                            AppId = data.ClientObject.ApplicationId,
                            FileName = fileGetMetaDataRequest.MediusFileInfo.FileName,
                            ServerChecksum = fileGetMetaDataRequest.MediusFileInfo.ServerChecksum.ToString(),
                            FileID = fileGetMetaDataRequest.MediusFileInfo.FileID,
                            FileSize = fileGetMetaDataRequest.MediusFileInfo.FileSize,
                            CreationTimeStamp = fileGetMetaDataRequest.MediusFileInfo.CreationTimeStamp,
                            OwnerID = fileGetMetaDataRequest.MediusFileInfo.OwnerID,
                            GroupID = fileGetMetaDataRequest.MediusFileInfo.GroupID,
                            OwnerPermissionRWX = fileGetMetaDataRequest.MediusFileInfo.OwnerPermissionRWX,
                            GroupPermissionRWX = fileGetMetaDataRequest.MediusFileInfo.GroupPermissionRWX,
                            GlobalPermissionRWX = fileGetMetaDataRequest.MediusFileInfo.GlobalPermissionRWX,
                            ServerOperationID = fileGetMetaDataRequest.MediusFileInfo.ServerOperationID,
                            fileMetaDataDTO = new FileMetaDataDTO()
                            {
                                AppId = data.ClientObject.ApplicationId,
                                FileID = fileGetMetaDataRequest.MediusFileInfo.FileID,
                                Key = fileGetMetaDataRequest.MediusMetaDataRequestedKey.Key,
                                Value = fileGetMetaDataRequest.MediusMetaDataRequestedKey.Value
                            }
                        };

                        await ServerConfiguration.Database.GetFileMetaData(data.ClientObject.ApplicationId,
                            fileGetMetaDataRequest.MediusFileInfo.FileName,
                            fileGetMetaDataRequest.MediusMetaDataRequestedKey.Key).ContinueWith(r =>
                            {
                                if (r.IsCompletedSuccessfully && r.Result != null & r.Result.Count > 0)
                                {
                                    foreach (var fileMetaData in r.Result)
                                    {
                                        fileGetMetaDataResponses.Add(new MediusFileGetMetaDataResponse()
                                        {
                                            MessageID = fileGetMetaDataRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            MediusFileInfo = new MediusFile
                                            {
                                                FileID = fileGetMetaDataRequest.MediusFileInfo.FileID,
                                                ServerChecksum = fileGetMetaDataRequest.MediusFileInfo.ServerChecksum,
                                                FileName = fileGetMetaDataRequest.MediusFileInfo.FileName,
                                                FileSize = fileGetMetaDataRequest.MediusFileInfo.FileSize,
                                                CreationTimeStamp = fileGetMetaDataRequest.MediusFileInfo.CreationTimeStamp,
                                                OwnerID = fileGetMetaDataRequest.MediusFileInfo.OwnerID,
                                                GroupID = fileGetMetaDataRequest.MediusFileInfo.GroupID,
                                                OwnerPermissionRWX = fileGetMetaDataRequest.MediusFileInfo.OwnerPermissionRWX,
                                                GroupPermissionRWX = fileGetMetaDataRequest.MediusFileInfo.GroupPermissionRWX,
                                                GlobalPermissionRWX = fileGetMetaDataRequest.MediusFileInfo.GlobalPermissionRWX,
                                                ServerOperationID = fileGetMetaDataRequest.MediusFileInfo.ServerOperationID,
                                            },
                                            MediusMetaDataResponseKey = new MediusFileMetaData
                                            {
                                                Key = fileMetaData.Key,
                                                Value = fileMetaData.Value,
                                            },
                                            EndOfList = false,
                                        });
                                    }

                                    if (fileGetMetaDataResponses.Count == 0)
                                    {
                                        // Return none
                                        data.ClientObject.Queue(new MediusFileGetMetaDataResponse()
                                        {
                                            MessageID = fileGetMetaDataRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            MediusFileInfo = new MediusFile
                                            {

                                            },
                                            MediusMetaDataResponseKey = new MediusFileMetaData
                                            {

                                            },
                                            EndOfList = true,
                                        });
                                    }

                                    // Ensure the end of list flag is set
                                    fileGetMetaDataResponses[fileGetMetaDataResponses.Count - 1].EndOfList = true;

                                    // Add to responses
                                    data.ClientObject.Queue(fileGetMetaDataResponses);
                                }
                                else
                                {
                                    // Return none
                                    data.ClientObject.Queue(new MediusFileGetMetaDataResponse()
                                    {
                                        MessageID = fileGetMetaDataRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusDBError,
                                        MediusFileInfo = new MediusFile
                                        {

                                        },
                                        MediusMetaDataResponseKey = new MediusFileMetaData
                                        {

                                        },
                                        EndOfList = true,
                                    });
                                }
                            });
                        break;
                    }
                #endregion

                #region FileUpdateMetaDataRequest
                case MediusFileUpdateMetaDataRequest fileUpdateMetaDataRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileUpdateMetaDataRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileUpdateMetaDataRequest} without being logged in.");

                        var path = MediusClass.GetFileSystemPath(data.ClientObject.ApplicationId, fileUpdateMetaDataRequest.MediusFileInfo.FileName);
                        if (path == null)
                        {
                            data.ClientObject.Queue(new MediusFileUpdateMetaDataResponse()
                            {
                                MessageID = fileUpdateMetaDataRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFileInternalAccessError,
                            });
                            break;
                        }
                        else
                        {
                            FileDTO fileDTO = new FileDTO()
                            {
                                AppId = data.ClientObject.ApplicationId,
                                FileName = fileUpdateMetaDataRequest.MediusFileInfo.FileName,
                                ServerChecksum = fileUpdateMetaDataRequest.MediusFileInfo.ServerChecksum.ToString(),
                                FileID = fileUpdateMetaDataRequest.MediusFileInfo.FileID,
                                FileSize = fileUpdateMetaDataRequest.MediusFileInfo.FileSize,
                                CreationTimeStamp = fileUpdateMetaDataRequest.MediusFileInfo.CreationTimeStamp,
                                OwnerID = fileUpdateMetaDataRequest.MediusFileInfo.OwnerID,
                                GroupID = fileUpdateMetaDataRequest.MediusFileInfo.GroupID,
                                OwnerPermissionRWX = fileUpdateMetaDataRequest.MediusFileInfo.OwnerPermissionRWX,
                                GroupPermissionRWX = fileUpdateMetaDataRequest.MediusFileInfo.GroupPermissionRWX,
                                GlobalPermissionRWX = fileUpdateMetaDataRequest.MediusFileInfo.GlobalPermissionRWX,
                                ServerOperationID = fileUpdateMetaDataRequest.MediusFileInfo.ServerOperationID,
                                fileMetaDataDTO = new FileMetaDataDTO()
                                {
                                    AppId = data.ClientObject.ApplicationId,
                                    FileID = fileUpdateMetaDataRequest.MediusFileInfo.FileID,
                                    Key = fileUpdateMetaDataRequest.MediusUpdateMetaData.Key,
                                    Value = fileUpdateMetaDataRequest.MediusUpdateMetaData.Value
                                }
                            };

                            var updatedFileMetaData = ServerConfiguration.Database.UpdateFileMetaData(fileDTO).ContinueWith((r) =>
                            {
                                if (r.IsCompletedSuccessfully && r.Result != false)
                                {
                                    List<MediusFileUpdateMetaDataResponse> fileUpdateMetaDataResponses = new List<MediusFileUpdateMetaDataResponse>
                                        {
                                            new MediusFileUpdateMetaDataResponse()
                                            {
                                                MessageID = fileUpdateMetaDataRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                MediusFile = new MediusFile
                                                {
                                                    FileID = fileUpdateMetaDataRequest.MediusFileInfo.FileID,
                                                    ServerChecksum = fileUpdateMetaDataRequest.MediusFileInfo.ServerChecksum,
                                                    FileName = fileUpdateMetaDataRequest.MediusFileInfo.FileName,
                                                    FileSize = fileUpdateMetaDataRequest.MediusFileInfo.FileSize,
                                                    CreationTimeStamp = fileUpdateMetaDataRequest.MediusFileInfo.CreationTimeStamp,
                                                    OwnerID = fileUpdateMetaDataRequest.MediusFileInfo.OwnerID,
                                                    GroupID = fileUpdateMetaDataRequest.MediusFileInfo.GroupID,
                                                    OwnerPermissionRWX = fileUpdateMetaDataRequest.MediusFileInfo.OwnerPermissionRWX,
                                                    GroupPermissionRWX = fileUpdateMetaDataRequest.MediusFileInfo.GroupPermissionRWX,
                                                    GlobalPermissionRWX = fileUpdateMetaDataRequest.MediusFileInfo.GlobalPermissionRWX,
                                                    ServerOperationID = fileUpdateMetaDataRequest.MediusFileInfo.ServerOperationID,
                                                },
                                                EndOfList = false,
                                            }
                                        };

                                    if (fileUpdateMetaDataResponses.Count == 0)
                                    {
                                        // Return none
                                        data.ClientObject.Queue(new MediusFileUpdateMetaDataResponse()
                                        {
                                            MessageID = fileUpdateMetaDataRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            MediusFile = new MediusFile
                                            {

                                            },
                                            EndOfList = true,
                                        });
                                    }
                                    else
                                    {
                                        // Ensure the end of list flag is set
                                        fileUpdateMetaDataResponses[fileUpdateMetaDataResponses.Count - 1].EndOfList = true;

                                        // Add to responses
                                        data.ClientObject.Queue(fileUpdateMetaDataResponses);
                                    }
                                }
                                else
                                {
                                    // Return DBsError
                                    data.ClientObject.Queue(new MediusFileUpdateMetaDataResponse()
                                    {
                                        MessageID = fileUpdateMetaDataRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusDBError,
                                        MediusFile = new MediusFile
                                        {

                                        },
                                        EndOfList = true,
                                    });
                                }
                            });
                        }

                        break;
                    }
                #endregion

                #region FileUpdateAuxMetaDataRequest
                case MediusFileUpdateAuxMetaDataRequest fileUpdateMetaDataRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileUpdateMetaDataRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileUpdateMetaDataRequest} without being logged in.");

                        var path = MediusClass.GetFileSystemPath(data.ClientObject.ApplicationId, fileUpdateMetaDataRequest.MediusFileInfo.FileName);
                        if (path == null)
                        {
                            data.ClientObject.Queue(new MediusFileUpdateMetaDataResponse()
                            {
                                MessageID = fileUpdateMetaDataRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFileInternalAccessError,
                            });
                            break;
                        }
                        else
                        {
                            FileDTO fileDTO = new FileDTO()
                            {
                                AppId = data.ClientObject.ApplicationId,
                                FileName = fileUpdateMetaDataRequest.MediusFileInfo.FileName,
                                ServerChecksum = fileUpdateMetaDataRequest.MediusFileInfo.ServerChecksum.ToString(),
                                FileID = fileUpdateMetaDataRequest.MediusFileInfo.FileID,
                                FileSize = fileUpdateMetaDataRequest.MediusFileInfo.FileSize,
                                CreationTimeStamp = fileUpdateMetaDataRequest.MediusFileInfo.CreationTimeStamp,
                                OwnerID = fileUpdateMetaDataRequest.MediusFileInfo.OwnerID,
                                GroupID = fileUpdateMetaDataRequest.MediusFileInfo.GroupID,
                                OwnerPermissionRWX = fileUpdateMetaDataRequest.MediusFileInfo.OwnerPermissionRWX,
                                GroupPermissionRWX = fileUpdateMetaDataRequest.MediusFileInfo.GroupPermissionRWX,
                                GlobalPermissionRWX = fileUpdateMetaDataRequest.MediusFileInfo.GlobalPermissionRWX,
                                ServerOperationID = fileUpdateMetaDataRequest.MediusFileInfo.ServerOperationID,
                                fileMetaDataDTO = new FileMetaDataDTO()
                                {
                                    AppId = data.ClientObject.ApplicationId,
                                    FileID = fileUpdateMetaDataRequest.MediusFileInfo.FileID,
                                    Key = fileUpdateMetaDataRequest.MediusUpdateMetaData.Key,
                                    Value = fileUpdateMetaDataRequest.MediusUpdateMetaData.Value
                                }
                            };

                            var updatedFileMetaData = ServerConfiguration.Database.UpdateFileMetaData(fileDTO).ContinueWith((r) =>
                            {
                                if (r.IsCompletedSuccessfully && r.Result != false)
                                {
                                    List<MediusFileUpdateMetaDataResponse> fileUpdateMetaDataResponses = new List<MediusFileUpdateMetaDataResponse>
                                        {
                                            new MediusFileUpdateMetaDataResponse()
                                            {
                                                MessageID = fileUpdateMetaDataRequest.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                                MediusFile = new MediusFile
                                                {
                                                    FileID = fileUpdateMetaDataRequest.MediusFileInfo.FileID,
                                                    ServerChecksum = fileUpdateMetaDataRequest.MediusFileInfo.ServerChecksum,
                                                    FileName = fileUpdateMetaDataRequest.MediusFileInfo.FileName,
                                                    FileSize = fileUpdateMetaDataRequest.MediusFileInfo.FileSize,
                                                    CreationTimeStamp = fileUpdateMetaDataRequest.MediusFileInfo.CreationTimeStamp,
                                                    OwnerID = fileUpdateMetaDataRequest.MediusFileInfo.OwnerID,
                                                    GroupID = fileUpdateMetaDataRequest.MediusFileInfo.GroupID,
                                                    OwnerPermissionRWX = fileUpdateMetaDataRequest.MediusFileInfo.OwnerPermissionRWX,
                                                    GroupPermissionRWX = fileUpdateMetaDataRequest.MediusFileInfo.GroupPermissionRWX,
                                                    GlobalPermissionRWX = fileUpdateMetaDataRequest.MediusFileInfo.GlobalPermissionRWX,
                                                    ServerOperationID = fileUpdateMetaDataRequest.MediusFileInfo.ServerOperationID,
                                                },
                                                EndOfList = false,
                                            }
                                        };

                                    if (fileUpdateMetaDataResponses.Count == 0)
                                    {
                                        // Return none
                                        data.ClientObject.Queue(new MediusFileUpdateMetaDataResponse()
                                        {
                                            MessageID = fileUpdateMetaDataRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusNoResult,
                                            MediusFile = new MediusFile
                                            {

                                            },
                                            EndOfList = true,
                                        });
                                    }
                                    else
                                    {
                                        // Ensure the end of list flag is set
                                        fileUpdateMetaDataResponses[fileUpdateMetaDataResponses.Count - 1].EndOfList = true;

                                        // Add to responses
                                        data.ClientObject.Queue(fileUpdateMetaDataResponses);
                                    }
                                }
                                else
                                {
                                    // Return DBsError
                                    data.ClientObject.Queue(new MediusFileUpdateMetaDataResponse()
                                    {
                                        MessageID = fileUpdateMetaDataRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusDBError,
                                        EndOfList = true,
                                    });
                                }
                            });
                        }

                        break;
                    }
                #endregion

                #region FileCreate
                case MediusFileCreateRequest fileCreateRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileCreateRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileCreateRequest} without being logged in.");

                        var path = MediusClass.GetFileSystemPath(data.ClientObject.ApplicationId, fileCreateRequest.MediusFileToCreate.FileName);
                        if (path == null)
                        {
                            data.ClientObject.Queue(new MediusFileCreateResponse()
                            {
                                MessageID = fileCreateRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFileInternalAccessError,
                                MediusFileInfo = null,
                            });
                            break;
                        }

                        if (File.Exists(path))
                        {
                            data.ClientObject.Queue(new MediusFileCreateResponse()
                            {
                                MessageID = fileCreateRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFileAlreadyExists
                            });
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(path));

                            //Create File
                            using (var fs = File.Create(path))
                            {
                                fs.Write(new byte[fileCreateRequest.MediusFileToCreate.FileSize]);
                            }

                            #region MediusFileGenerateChecksum
                            //Generate Checksum for it
                            using (var md5 = MD5.Create())
                            {
                                ServerConfiguration.LogInfo($"Generating file checksum for {path}");
                                using (var stream = File.OpenRead(path))
                                {
                                    ServerConfiguration.LogInfo($"md5 checksum generated: {BitConverter.ToString(md5.ComputeHash(stream))}");
                                    string serverCheckSumGenerated = BitConverter.ToString(md5.ComputeHash(stream));


                                    ServerConfiguration.LogWarn($"{serverCheckSumGenerated} checksum CHECK");
                                    FileDTO fileDTO = new FileDTO()
                                    {
                                        AppId = data.ClientObject.ApplicationId,
                                        FileName = fileCreateRequest.MediusFileToCreate.FileName,
                                        ServerChecksum = string.Join("", serverCheckSumGenerated),
                                        FileID = fileCreateRequest.MediusFileToCreate.FileID,
                                        FileSize = fileCreateRequest.MediusFileToCreate.FileSize,
                                        CreationTimeStamp = fileCreateRequest.MediusFileToCreate.CreationTimeStamp,
                                        OwnerID = fileCreateRequest.MediusFileToCreate.OwnerID,
                                        GroupID = fileCreateRequest.MediusFileToCreate.GroupID,
                                        OwnerPermissionRWX = fileCreateRequest.MediusFileToCreate.OwnerPermissionRWX,
                                        GroupPermissionRWX = fileCreateRequest.MediusFileToCreate.GroupPermissionRWX,
                                        GlobalPermissionRWX = fileCreateRequest.MediusFileToCreate.GlobalPermissionRWX,
                                        ServerOperationID = fileCreateRequest.MediusFileToCreate.ServerOperationID,
                                        fileAttributesDTO = new FileAttributesDTO()
                                        {
                                            AppId = data.ClientObject.ApplicationId,
                                            FileName = fileCreateRequest.MediusFileToCreate.FileName,
                                            Description = fileCreateRequest.MediusFileCreateAttributes.Description,
                                            LastChangedByUserID = fileCreateRequest.MediusFileCreateAttributes.LastChangedByUserID,
                                            LastChangedTimeStamp = fileCreateRequest.MediusFileCreateAttributes.LastChangedTimeStamp,
                                            NumberAccesses = fileCreateRequest.MediusFileCreateAttributes.NumberAccesses,
                                            StreamableFlag = fileCreateRequest.MediusFileCreateAttributes.StreamableFlag,
                                            StreamingDataRate = fileCreateRequest.MediusFileCreateAttributes.StreamingDataRate,
                                        }
                                    };

                                    if (ServerConfiguration.Database._settings.SimulatedMode == true)
                                    {
                                        data.ClientObject.Queue(new MediusFileCreateResponse()
                                        {
                                            MessageID = fileCreateRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusFeatureNotEnabled
                                        });
                                    }
                                    else
                                    {
                                        await ServerConfiguration.Database.createFile(fileDTO).ContinueWith(r =>
                                        {
                                            if (r.IsCompletedSuccessfully && r.Result != false)
                                            {
                                                data.ClientObject.Queue(new MediusFileCreateResponse()
                                                {
                                                    MessageID = fileCreateRequest.MessageID,
                                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                                    MediusFileInfo = new MediusFile()
                                                    {
                                                        FileID = fileDTO.FileID,
                                                        ServerChecksum = serverCheckSumGenerated,
                                                        FileName = fileCreateRequest.MediusFileToCreate.FileName,
                                                        FileSize = fileCreateRequest.MediusFileToCreate.FileSize,
                                                        CreationTimeStamp = fileCreateRequest.MediusFileToCreate.CreationTimeStamp,
                                                        OwnerID = fileCreateRequest.MediusFileToCreate.OwnerID,
                                                        GroupID = fileCreateRequest.MediusFileToCreate.GroupID,
                                                        OwnerPermissionRWX = fileCreateRequest.MediusFileToCreate.OwnerPermissionRWX,
                                                        GroupPermissionRWX = fileCreateRequest.MediusFileToCreate.GroupPermissionRWX,
                                                        GlobalPermissionRWX = fileCreateRequest.MediusFileToCreate.GlobalPermissionRWX,
                                                        ServerOperationID = fileCreateRequest.MediusFileToCreate.ServerOperationID
                                                    }
                                                });

                                                data.ClientObject.mediusFileToUpload = fileCreateRequest.MediusFileToCreate;
                                            }
                                            else
                                            {
                                                data.ClientObject.Queue(new MediusFileCreateResponse()
                                                {
                                                    MessageID = fileCreateRequest.MessageID,
                                                    StatusCode = MediusCallbackStatus.MediusFileAlreadyExists
                                                });
                                            }
                                        });
                                    }
                                }
                            }
                            #endregion


                        }
                        break;
                    }
                #endregion

                #region FileUploadRequest
                case MediusFileUploadRequest fileUploadRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileUploadRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileUploadRequest} without being logged in.");

                        //Task.Run(async () =>
                        //{
                        //    int j = 0;
                        //    var totalSize = fileUploadRequest.MediusFileInfo.FileSize;
                        //    for (int i = 0; i < totalSize; )
                        //    {


                        //        i += Constants.MEDIUS_FILE_MAX_DOWNLOAD_DATA_SIZE;
                        //    }
                        //});


                        if (ServerConfiguration.Database._settings.SimulatedMode == true)
                        {
                            ServerConfiguration.LogWarn($"DB is not enabled.. cannot service upload request");
                            data.ClientObject.Queue(new MediusFileUploadServerRequest()
                            {
                                MessageID = fileUploadRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFeatureNotEnabled,
                                iXferStatus = MediusFileXferStatus.Error
                            });
                        }
                        else
                        {
                            var path = MediusClass.GetFileSystemPath(data.ClientObject.ApplicationId, fileUploadRequest.MediusFileInfo.FileName);
                            if (path == null)
                            {
                                if (ServerConfiguration.Database._settings.SimulatedMode == true)
                                {
                                    data.ClientObject.Queue(new MediusFileUploadServerRequest()
                                    {
                                        MessageID = fileUploadRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusFeatureNotEnabled,
                                        iXferStatus = MediusFileXferStatus.Error
                                    });
                                }
                                else
                                {
                                    data.ClientObject.Queue(new MediusFileUploadServerRequest()
                                    {
                                        MessageID = fileUploadRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusFileInternalAccessError,
                                        iXferStatus = MediusFileXferStatus.Error
                                    });
                                }
                                break;
                            }

                            Directory.CreateDirectory(Path.GetDirectoryName(path));

                            var stream = File.Open(path, FileMode.OpenOrCreate);
                            data.ClientObject.Upload = new UploadState()
                            {
                                FileId = fileUploadRequest.MediusFileInfo.FileID,
                                Stream = stream,
                                //TotalSize = fileUploadRequest.UiDataSize
                                TotalSize = (int)fileUploadRequest.MediusFileInfo.FileSize
                            };

                            data.ClientObject.Queue(new MediusFileUploadServerRequest()
                            {
                                MessageID = fileUploadRequest.MessageID,
                                iPacketNumber = 0,
                                iReqStartByteIndex = 0,
                                iXferStatus = MediusFileXferStatus.Initial,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                            });
                        }
                        break;
                    }
                #endregion

                #region FileUploadResponse
                case MediusFileUploadResponse fileUploadResponse:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileUploadResponse} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileUploadResponse} without being logged in.");

                        var uploadState = data.ClientObject.Upload;

                        if (fileUploadResponse.iXferStatus >= MediusFileXferStatus.End)
                            break;


                        try
                        {
                            ServerConfiguration.LogInfo($"Bytes Received Total [{uploadState.BytesReceived}] < [{uploadState.TotalSize}]");
                            uploadState.Stream.Seek(fileUploadResponse.iStartByteIndex, SeekOrigin.Begin);
                            uploadState.Stream.Write(fileUploadResponse.Data, 0, fileUploadResponse.iDataSize);
                            uploadState.BytesReceived += fileUploadResponse.iDataSize;
                            uploadState.PacketNumber++;

                            if (uploadState.BytesReceived < uploadState.TotalSize)
                            {
                                data.ClientObject.Queue(new MediusFileUploadServerRequest()
                                {
                                    MessageID = fileUploadResponse.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    iPacketNumber = uploadState.PacketNumber,
                                    iReqStartByteIndex = uploadState.BytesReceived,
                                    iXferStatus = MediusFileXferStatus.Mid
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusFileUploadServerRequest()
                                {
                                    MessageID = fileUploadResponse.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess,
                                    iPacketNumber = uploadState.PacketNumber,
                                    iReqStartByteIndex = uploadState.BytesReceived,
                                    iXferStatus = MediusFileXferStatus.End
                                });
                            }
                        }
                        catch
                        {
                            data.ClientObject.Queue(new MediusFileUploadServerRequest()
                            {
                                MessageID = fileUploadResponse.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFileInternalAccessError,
                                iPacketNumber = uploadState.PacketNumber,
                                iReqStartByteIndex = uploadState.BytesReceived,
                                iXferStatus = MediusFileXferStatus.Error
                            });
                        }
                        break;
                    }
                #endregion

                #region FileCancelOperationRequest
                case MediusFileCancelOperationRequest fileCancelOperationRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileCancelOperationRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileCancelOperationRequest} without being logged in.");

                        if (data.ClientObject.mediusFileToUpload != null)
                        {

                            //If the player of this file is the owner continue, otherwise they don't have permissions.
                            if (data.ClientObject.mediusFileToUpload.OwnerID != fileCancelOperationRequest.MediusFileInfo.OwnerID)
                            {
                                data.ClientObject.Queue(new MediusFileCancelOperationResponse()
                                {
                                    MessageID = fileCancelOperationRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusFileNoPermissions
                                });
                            }

                            if (data.ClientObject.Upload?.FileId == fileCancelOperationRequest.MediusFileInfo.FileID)
                            {
                                data.ClientObject.Upload.Stream?.Close();
                                data.ClientObject.Upload = null;

                                data.ClientObject.Queue(new MediusFileCancelOperationResponse()
                                {
                                    MessageID = fileCancelOperationRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusFileCancelOperationResponse()
                                {
                                    MessageID = fileCancelOperationRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusFail
                                });
                            }
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusFileCancelOperationResponse()
                            {
                                MessageID = fileCancelOperationRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess
                            });
                        }
                        break;
                    }
                #endregion

                #region FileCloseRequest
                case MediusFileCloseRequest fileCloseRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileCloseRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileCloseRequest} without being logged in.");

                        //If the player of this file is the owner continue, otherwise they don't have permissions.
                        if (data.ClientObject.Upload?.FileId == fileCloseRequest.MediusFileInfo.FileID)
                        {
                            data.ClientObject.Upload.Stream?.Close();
                            data.ClientObject.Upload = null;

                            data.ClientObject.Queue(new MediusFileCloseResponse()
                            {
                                MessageID = fileCloseRequest.MessageID,
                                MediusFileInfo = fileCloseRequest.MediusFileInfo,
                                StatusCode = MediusCallbackStatus.MediusSuccess
                            });
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusFileCloseResponse()
                            {
                                MessageID = fileCloseRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusDBError
                            });
                        }
                        break;
                    }
                #endregion

                #region FileDeleteRequest
                case MediusFileDeleteRequest fileDeleteRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileDeleteRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileDeleteRequest} without being logged in.");

                        var path = MediusClass.GetFileSystemPath(data.ClientObject.ApplicationId, fileDeleteRequest.MediusFileToDelete.FileName);
                        if (path == null)
                        {
                            data.ClientObject.Queue(new MediusFileDeleteResponse()
                            {
                                MessageID = fileDeleteRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusDataDoesNotExist,
                            });
                            break;
                        }
                        else
                        {
                            #region Default
                            //MediusFileInternalAccessError, MediusDBError

                            FileDTO fileDTO = new FileDTO()
                            {
                                FileName = fileDeleteRequest.MediusFileToDelete.FileName
                            };

                            await ServerConfiguration.Database.deleteFile(fileDTO).ContinueWith(r => {

                                if (r.IsCompletedSuccessfully && r.Result != false)
                                {
                                    File.Delete(path);
                                    ServerConfiguration.LogWarn($"file deleting {path}");

                                    data.ClientObject.Queue(new MediusFileDeleteResponse()
                                    {
                                        MessageID = fileDeleteRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusSuccess
                                    });
                                }
                                else
                                {
                                    data.ClientObject.Queue(new MediusFileDeleteResponse()
                                    {
                                        MessageID = fileDeleteRequest.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusDBError
                                    });
                                }
                            });
                            #endregion
                        }
                        break;
                    }
                #endregion

                #region FileDownload
                case MediusFileDownloadRequest fileDownloadRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileDownloadRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {fileDownloadRequest} without being logged in.");

                        var rootPath = MediusClass.GetFileSystemPath(data.ClientObject.ApplicationId, fileDownloadRequest.MediusFileInfo.FileName);
                        if (rootPath == null)
                        {
                            data.ClientObject.Queue(new MediusFileDownloadResponse()
                            {
                                MessageID = fileDownloadRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFileDoesNotExist,
                                iXferStatus = MediusFileXferStatus.Error
                            });
                            break;
                        }

                        if (File.Exists(rootPath))
                        {
                            var bytes = File.ReadAllBytes(rootPath);
                            int j = 0;
                            for (int i = 0; i < bytes.Length; i += Constants.MEDIUS_FILE_MAX_DOWNLOAD_DATA_SIZE)
                            {
                                var len = bytes.Length - i;
                                if (len > Constants.MEDIUS_FILE_MAX_DOWNLOAD_DATA_SIZE)
                                    len = Constants.MEDIUS_FILE_MAX_DOWNLOAD_DATA_SIZE;

                                var msg = new MediusFileDownloadResponse()
                                {
                                    MessageID = fileDownloadRequest.MessageID,
                                    iDataSize = len,
                                    iPacketNumber = j,
                                    iXferStatus = j == 0 ? MediusFileXferStatus.Initial : ((len + i) >= bytes.Length ? MediusFileXferStatus.End : MediusFileXferStatus.Mid),
                                    iStartByteIndex = i,
                                    StatusCode = MediusCallbackStatus.MediusSuccess
                                };
                                Array.Copy(bytes, i, msg.Data, 0, len);

                                data.ClientObject.Queue(msg);

                                ++j;
                            }

                            /*

                            var bytes = File.ReadAllBytes(rootPath);
                            int j = 0; // Packet #

                            for (int i = 0; i < bytes.Length; i += Constants.MEDIUS_FILE_MAX_DOWNLOAD_DATA_SIZE)
                            {
                                var len = bytes.Length;
                                if (len > Constants.MEDIUS_FILE_MAX_DOWNLOAD_DATA_SIZE)
                                    len = Constants.MEDIUS_FILE_MAX_DOWNLOAD_DATA_SIZE;

                                if ((len + i) >= bytes.Length)
                                {

                                    var msgLessThanTotal = new MediusFileDownloadResponse()
                                    {
                                        MessageID = fileDownloadRequest.MessageID,
                                        Data = bytes,
                                        iDataSize = len,
                                        iPacketNumber = j,
                                        iXferStatus = MediusFileXferStatus.End,
                                        iStartByteIndex = i,
                                        StatusCode = MediusCallbackStatus.MediusSuccess
                                    };
                                    
                                    if (fileDownloadRequest.MediusFileInfo.FileName.StartsWith("stats_"))
                                    {
                                        ServerConfiguration.LogInfo($"[SF:DM] Saving File Stats to cached player");
                                        data.ClientObject.OnFileDownloadResponse(msgLessThanTotal);
                                    };
                                    
                                    data.ClientObject.Queue(msgLessThanTotal);

                                    ServerConfiguration.LogInfo($"LAST FILE CONTENT SENT! {len}");

                                    len = bytes.Length - i;
                                } else
                                {

                                    var msg = new MediusFileDownloadResponse()
                                    {
                                        MessageID = fileDownloadRequest.MessageID,
                                        Data = bytes,
                                        iDataSize = len,
                                        iPacketNumber = j,

                                        iXferStatus = j == 0 ? MediusFileXferStatus.Initial : MediusFileXferStatus.Mid,
                                        //iXferStatus = j == 0 ? MediusFileXferStatus.Initial : ((len + i) >= bytes.Length ? MediusFileXferStatus.End : MediusFileXferStatus.Mid),
                                        iStartByteIndex = i,
                                        StatusCode = MediusCallbackStatus.MediusSuccess
                                    };

                                    data.ClientObject.Queue(msg);
                                    len = bytes.Length - i;
                                }

                                ++j;
                            }
                            */

                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusFileDownloadResponse()
                            {
                                MessageID = fileDownloadRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFileDoesNotExist
                            });
                        }
                        break;
                    }

                #endregion
                #endregion

                #region Chat / Binary Message

                //Deprecated past Medius 2.10
                case MediusChatToggleRequest chatToggleRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {chatToggleRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {chatToggleRequest} without being logged in.");



                        data.ClientObject.Queue(new MediusChatToggleResponse()
                        {
                            MessageID = chatToggleRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });

                        break;
                    }

                case MediusGenericChatSetFilterRequest genericChatSetFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {genericChatSetFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {genericChatSetFilterRequest} without being logged in.");

                        data.ClientObject.Queue(new MediusGenericChatSetFilterResponse()
                        {
                            MessageID = genericChatSetFilterRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            ChatFilter = new MediusGenericChatFilter()
                            {
                                GenericChatFilterBitfield = genericChatSetFilterRequest.GenericChatFilter.GenericChatFilterBitfield
                            }
                        });
                        break;
                    }

                case MediusSetAutoChatHistoryRequest setAutoChatHistoryRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setAutoChatHistoryRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {setAutoChatHistoryRequest} without being logged in.");



                        data.ClientObject.Queue(new MediusSetAutoChatHistoryResponse()
                        {
                            MessageID = setAutoChatHistoryRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });
                        break;
                    }

                case MediusGenericChatMessage1 genericChatMessage:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {genericChatMessage} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {genericChatMessage} without being logged in.");

                        // validate message
                        if (!MediusClass.PassTextFilter(data.ClientObject.ApplicationId, TextFilterContext.CHAT, genericChatMessage.Message))
                            return;

                        //MediusClass.AntiCheatPlugin.mc_anticheat_event_msg(AnticheatEventCode.anticheatCHATMESSAGE, data.ClientObject.WorldId, data.ClientObject.AccountId, MediusClass.AntiCheatClient, (IMediusRequest)genericChatMessage, 256);

                        await ProcessGenericChatMessage(clientChannel, data.ClientObject, genericChatMessage);
                        break;
                    }

                case MediusGenericChatMessage genericChatMessage:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {genericChatMessage} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {genericChatMessage} without being logged in.");

                        // validate message
                        if (!MediusClass.PassTextFilter(data.ClientObject.ApplicationId, TextFilterContext.CHAT, genericChatMessage.Message))
                            return;

                        //MediusClass.AntiCheatPlugin.mc_anticheat_event_msg(AnticheatEventCode.anticheatCHATMESSAGE, data.ClientObject.WorldId, data.ClientObject.AccountId, MediusClass.AntiCheatClient, (IMediusRequest)genericChatMessage, 256);

                        //log to syslog

                        await ProcessGenericChatMessage(clientChannel, data.ClientObject, genericChatMessage);
                        break;
                    }

                case MediusChatMessage chatMessage:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {chatMessage} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {chatMessage} without being logged in.");

                        // validate message
                        if (!MediusClass.PassTextFilter(data.ClientObject.ApplicationId, TextFilterContext.CHAT, chatMessage.Message))
                            return;

                        //MediusClass.AntiCheatPlugin.mc_anticheat_event_msg(AnticheatEventCode.anticheatCHATMESSAGE, data.ClientObject.WorldId, data.ClientObject.AccountId, MediusClass.AntiCheatClient, chatMessage, 256);

                        await ProcessChatMessage(clientChannel, data.ClientObject, chatMessage.MessageID, chatMessage);
                        break;
                    }

                case MediusBinaryMessage binaryMessage:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {binaryMessage} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {binaryMessage} without being logged in.");

                        switch (binaryMessage.MessageType)
                        {
                            case MediusBinaryMessageType.BroadcastBinaryMsg:
                                {
                                    var iNumPlayersReturned = data.ClientObject.CurrentChannel.PlayerCount;
                                    if (iNumPlayersReturned > 256)
                                    {
                                        ServerConfiguration.LogWarn("iNumPlayersReturned <= 256");
                                    }
                                    else if (iNumPlayersReturned > 0)
                                    {
                                        data.ClientObject.CurrentChannel?.BroadcastBinaryMessage(data.ClientObject, binaryMessage);
                                    }
                                    else
                                    {
                                        ServerConfiguration.LogWarn("No players found to send binary msg to.");
                                    }
                                    break;
                                }
                            case MediusBinaryMessageType.TargetBinaryMsg:
                                {
                                    ServerConfiguration.LogInfo($"Sending targeted binary message Target AccountID {binaryMessage.TargetAccountID}");

                                    var target = MediusClass.Manager.GetClientByAccountId(binaryMessage.TargetAccountID, data.ClientObject.ApplicationId);

                                    if (target == null)
                                    {
                                        ServerConfiguration.LogInfo($"BinaryMsg target not found in cache for AccountID {binaryMessage.TargetAccountID}");
                                    }
                                    else
                                    {
                                        target?.Queue(new MediusBinaryFwdMessage()
                                        {
                                            MessageType = binaryMessage.MessageType,
                                            OriginatorAccountID = data.ClientObject.AccountId,
                                            Message = binaryMessage.Message
                                        });
                                    }
                                    break;
                                }
                            case MediusBinaryMessageType.BroadcastBinaryMsgAcrossEntireUniverse:
                                {
                                    ServerConfiguration.LogInfo($"Sending BroadcastBinaryMsgAcrossEntireUniverse({binaryMessage.Message}) binary message ");

                                    break;
                                }
                            default:
                                {
                                    ServerConfiguration.LogWarn($"Unhandled binary message type {binaryMessage.MessageType}");
                                    break;
                                }
                        }
                        break;
                    }

                case MediusBinaryMessage1 binaryMessage:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {binaryMessage} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {binaryMessage} without being logged in.");


                        //Binary Msg Handler Error: [%d]: Player not privileged or MUM erro
                        switch (binaryMessage.MessageType)
                        {

                            case MediusBinaryMessageType.BroadcastBinaryMsg:
                                {
                                    data.ClientObject.CurrentChannel?.BroadcastBinaryMessage(data.ClientObject, binaryMessage);
                                    break;
                                }
                            case MediusBinaryMessageType.TargetBinaryMsg:
                                {
                                    var target = MediusClass.Manager.GetClientByAccountId(binaryMessage.TargetAccountID, data.ClientObject.ApplicationId);

                                    if (target != null)
                                    {
                                        target?.Queue(new MediusBinaryFwdMessage1()
                                        {
                                            MessageType = binaryMessage.MessageType,
                                            OriginatorAccountID = data.ClientObject.AccountId,
                                            Message = binaryMessage.Message
                                        });
                                    }
                                    else
                                    {
                                        ServerConfiguration.LogInfo("No players found to send binary msg to");
                                    }

                                    break;
                                }

                            case MediusBinaryMessageType.BroadcastBinaryMsgAcrossEntireUniverse:
                                {

                                    //MUMBinaryFwdFromLobby
                                    //MUMBinaryFwdFromLobby() Error %d MID %s, Orig AID %d, Whisper Target AID %d
                                    ServerConfiguration.LogInfo($"Sending BroadcastBinaryMsgAcrossEntireUniverse(%d) binary message (%d)");
                                    break;
                                }
                            default:
                                {
                                    ServerConfiguration.LogWarn($"Unhandled binary message type {binaryMessage.MessageType}");
                                    break;
                                }
                        }
                        break;
                    }

                case MediusCrossChatMessage crossChatMessage:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {crossChatMessage} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {crossChatMessage} without being logged in.");

                        var target = MediusClass.Manager.GetClientByAccountId(crossChatMessage.TargetAccountID, data.ClientObject.ApplicationId);
                        if (target != null)
                        {
                            target?.Queue(new MediusCrossChatFwdMessage()
                            {
                                MessageID = crossChatMessage.MessageID,
                                OriginatorAccountID = data.ClientObject.AccountId,
                                TargetRoutingDmeWorldID = crossChatMessage.TargetRoutingDmeWorldID,
                                SourceDmeWorldID = crossChatMessage.SourceDmeWorldID,

                                msgType = crossChatMessage.msgType,
                                Contents = crossChatMessage.Contents
                            });
                        }
                        else
                            ServerConfiguration.LogWarn("No player found to send crossChat msg to");

                        break;
                    }

                #endregion

                #region Text Filter

                case MediusTextFilterRequest textFilterRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {textFilterRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {textFilterRequest} without being logged in.");

                        if (textFilterRequest.TextFilterType == MediusTextFilterType.MediusTextFilterPassFail)
                        {
                            if (MediusClass.PassTextFilter(data.ClientObject.ApplicationId, TextFilterContext.DEFAULT, textFilterRequest.Text))
                            {
                                data.ClientObject.Queue(new MediusTextFilterResponse()
                                {
                                    MessageID = textFilterRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusPass,
                                    Text = textFilterRequest.Text
                                });
                            }
                            else
                            {
                                data.ClientObject.Queue(new MediusTextFilterResponse()
                                {
                                    MessageID = textFilterRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusTextStringInvalid
                                });
                            }
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusTextFilterResponse()
                            {
                                MessageID = textFilterRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess,
                                Text = MediusClass.FilterTextFilter(data.ClientObject.ApplicationId, TextFilterContext.DEFAULT, textFilterRequest.Text).Trim()
                            });
                        }

                        break;
                    }

                //CAC
                case MediusTextFilterRequest1 textFilterRequest1:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {textFilterRequest1} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {textFilterRequest1} without being logged in.");


                        if (MediusClass.PassTextFilter(data.ClientObject.ApplicationId, TextFilterContext.DEFAULT, Convert.ToString(textFilterRequest1.Text)))
                        {
                            char[] ch = new char[textFilterRequest1.Text.Length];

                            // Copy character by character into array 
                            for (int i = 0; i < textFilterRequest1.Text.Length; i++)
                            {
                                ch[i] = textFilterRequest1.Text[i];
                            }

                            data.ClientObject.Queue(new MediusTextFilterResponse1()
                            {
                                MessageID = textFilterRequest1.MessageID,
                                StatusCode = MediusCallbackStatus.MediusPass,
                                TextSize = textFilterRequest1.TextSize,
                                Text = ch,
                            });
                        }
                        else
                        {
                            data.ClientObject.Queue(new MediusTextFilterResponse1()
                            {
                                MessageID = textFilterRequest1.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFail
                            });
                        }
                        break;
                    }

                #endregion

                #region Token
                case MediusTokenRequest tokenRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {tokenRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {tokenRequest} without being logged in.");

                        if (ServerConfiguration.Database._settings.SimulatedMode == true)
                        {
                            ServerConfiguration.LogInfo("TokenRequest DB Disabled Success");
                            data.ClientObject.Queue(new MediusStatusResponse()
                            {
                                Class = tokenRequest.PacketClass,
                                Type = 0x41,
                                MessageID = tokenRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess
                            });
                        }
                        else
                        {

                            TokenRequestDTO tokenRequestDTO = new TokenRequestDTO();

                            tokenRequestDTO.TokenToReplace = "0";
                            tokenRequestDTO.TokenCategory = tokenRequest.TokenCategory;
                            tokenRequestDTO.EntityID = tokenRequest.EntityID;
                            tokenRequestDTO.SubmitterAccountID = data.ClientObject.AccountId;
                            tokenRequestDTO.Token = tokenRequest.Token;

                            switch (tokenRequest.TokenAction)
                            {
                                case MediusTokenActionType.MediusAddToken:
                                    {
                                        //ServerConfiguration.Database.TokenAdd(tokenRequestDTO, data.ClientObject.ApplicationId).ContinueWith(r => { });

                                        return;
                                    }
                                case MediusTokenActionType.MediusUpdateToken:
                                    {
                                        tokenRequestDTO.TokenToReplace = tokenRequest.TokenToReplace;
                                        //ServerConfiguration.Database.UpdateToken(tokenRequestDTO, data.ClientObject.ApplicationId).ContinueWith(r => { });
                                        return;
                                    }
                                case MediusTokenActionType.MediusRemoveToken:
                                    {
                                        //ServerConfiguration.Database.RemoveToken(tokenRequestDTO, data.ClientObject.ApplicationId).ContinueWith(r => { });
                                        return;
                                    }
                                default:
                                    {
                                        ServerConfiguration.LogWarn($"MediusUniqueCallbackTokenRequestHandler: Error Unrecognized Token Action {tokenRequest.TokenAction}");
                                        data.ClientObject.Queue(new MediusStatusResponse()
                                        {
                                            Class = tokenRequest.PacketClass,
                                            Type = 0x41,
                                            MessageID = tokenRequest.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusInvalidRequestMsg
                                        });
                                        return;
                                    }
                            }
                        }

                        break;
                    }

                #endregion

                #region PostDebugInfo
                case MediusPostDebugInfoRequest postDebugInfoRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {postDebugInfoRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {postDebugInfoRequest} without being logged in.");

                        if (Settings.PostDebugInfoEnable == false)
                        {
                            ServerConfiguration.LogWarn("PostDebugInfo feature not enabled");
                            data.ClientObject.Queue(new MediusPostDebugInfoResponse
                            {
                                MessageID = postDebugInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusFeatureNotEnabled
                            });
                        }
                        else
                        {
                            if (data.ClientObject.SessionKey != null)
                            {
                                ServerConfiguration.LogWarn($"PostDebugInfo Unable to retrieve player from cache {data.ClientObject.AccountName}");
                                data.ClientObject.Queue(new MediusPostDebugInfoResponse
                                {
                                    MessageID = postDebugInfoRequest.MessageID,
                                    StatusCode = MediusCallbackStatus.MediusCacheFailure
                                });
                            }
                            //Post DebugInfo to file
                            ServerConfiguration.LogInfo($"POST_INFO: SKey[{data.ClientObject.SessionKey}] Message[{postDebugInfoRequest.Message}]");
                            data.ClientObject.Queue(new MediusPostDebugInfoResponse
                            {
                                MessageID = postDebugInfoRequest.MessageID,
                                StatusCode = MediusCallbackStatus.MediusSuccess
                            });
                        }
                        break;
                    }
                #endregion

                #region Utils

                case MediusUtilGetTotalGamesFilteredRequest utilGetTotalGamesFilteredRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {utilGetTotalGamesFilteredRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {utilGetTotalGamesFilteredRequest} without being logged in.");

                        data.ClientObject.Queue(new MediusUtilGetTotalGamesFilteredResponse
                        {
                            MessageID = utilGetTotalGamesFilteredRequest.MessageID,
                            Total = (int)MediusClass.Manager.GetGameCount(data.ClientObject.ApplicationId),
                            StatusCode = MediusCallbackStatus.MediusSuccess
                        });
                        break;
                    }

                #endregion

                #region GetMyIP 

                case MediusGetMyIPRequest getMyIpRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyIpRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getMyIpRequest} without being logged in.");

                        // Ban Mac Check if their in-game
                        bool isMacBanned = await ServerConfiguration.Database.GetIsMacBanned(data.MachineId);

                        if (isMacBanned)
                        {
                            #region isMacBanned?
                            ServerConfiguration.LogInfo($"getMyIp: Connected User MAC Banned: {isMacBanned}");

                            if (isMacBanned)
                            {

                                // Account is banned
                                // Tell the client you're no longer privileged
                                data?.ClientObject?.Queue(new MediusGetMyIPResponse()
                                {
                                    MessageID = getMyIpRequest.MessageID,
                                    IP = null,
                                    StatusCode = MediusCallbackStatus.MediusPlayerNotPrivileged
                                });

                                ServerConfiguration.LogInfo($"Get My IP Request Handler Error: Player Not Privileged");

                                // Send ban message
                                QueueBanMessage(data, "You have been MAC Banned");
                            }
                            #endregion
                        }
                        else
                        {
                            #region Send IP & Success 
                            //Send back other Client's Address 
                            data.ClientObject.Queue(new MediusGetMyIPResponse()
                            {
                                MessageID = getMyIpRequest.MessageID,
                                IP = (clientChannel.RemoteAddress as IPEndPoint)?.Address,
                                StatusCode = MediusCallbackStatus.MediusSuccess
                            });
                            #endregion
                        }

                        break;
                    }
                #endregion

                #region GetServerTime

                case MediusGetServerTimeRequest getServerTimeRequest:
                    {
                        // ERROR - Need a session
                        if (data.ClientObject == null)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getServerTimeRequest} without a session.");

                        // ERROR -- Need to be logged in
                        if (!data.ClientObject.IsLoggedIn)
                            throw new InvalidOperationException($"INVALID OPERATION: {clientChannel} sent {getServerTimeRequest} without being logged in.");


                        data.ClientObject.Queue(new MediusGetServerTimeResponse()
                        {
                            MessageID = getServerTimeRequest.MessageID,
                            StatusCode = MediusCallbackStatus.MediusSuccess,
                            GMT_time = (int)Utils.GetUnixTime(),
                            Local_server_timezone = MediusTimeZone.MediusTimeZone_GMT
                        });
                        break;
                    }

                #endregion

                default:
                    {
                        ServerConfiguration.LogWarn($"Unhandled Medius Message: {message}");
                        break;
                    }
            }
        }

        #region ProcessAddToBuddyListConfirmation 
        private void ProcessAddToBuddyListConfirmationRequest(ClientObject clientObject, AccountDTO accountToAdd, MediusAddToBuddyListConfirmationRequest addToBuddyListConfirmation)
        {
            var channel = clientObject.CurrentChannel;
            var accountClientToAdd = MediusClass.Manager.GetClientByAccountId(accountToAdd.AccountId, clientObject.ApplicationId);

            // ERROR -- Need to be logged in
            if (!clientObject.IsLoggedIn)
                return;

            // Need to be in a channel
            if (channel == null)
                return;

            switch (addToBuddyListConfirmation.AddType)
            {
                case MediusBuddyAddType.AddSingle:
                    {
                        if (accountClientToAdd != null && clientObject.ApplicationId != 20214)
                        {
                            ServerConfiguration.LogInfo($"AnswerAddToBuddyListConfirmationRequest: Target player found in cache. forwarding to ClientIndex [{clientObject.AccountId}] WorldID [{clientObject.WorldId}]");
                            ServerConfiguration.LogInfo($"accountToAdd [{accountClientToAdd.AccountName}] is online, forwarding request!");
                            //channel.AddToBuddyListConfirmationSingleRequest(clientObject, accountClientToAdd, addToBuddyListConfirmation);

                            if (accountClientToAdd.IsLoggedIn == true)
                            {
                                accountClientToAdd.Queue(new MediusAddToBuddyListFwdConfirmationRequest()
                                {
                                    MessageID = addToBuddyListConfirmation.MessageID,
                                    OriginatorAccountID = clientObject.AccountId,
                                    OriginatorAccountName = clientObject.AccountName,
                                    AddType = MediusBuddyAddType.AddSingle,
                                });
                            }
                        }
                        else
                        {
                            ServerConfiguration.LogWarn($"accountToAdd [{accountClientToAdd.AccountName}] is being added to list!");
                            AccountRelationInviteDTO buddy = new AccountRelationInviteDTO()
                            {
                                AccountId = clientObject.AccountId,
                                AccountName = clientObject.AccountName,
                                BuddyAccountId = accountToAdd.AccountId,
                                AppId = clientObject.ApplicationId,
                                addType = (int)MediusBuddyAddType.AddSingle,
                            };

                            _ = ServerConfiguration.Database.addBuddyInvitation(buddy).ContinueWith(r =>
                            {
                                if (r.IsCompletedSuccessfully && r.Result != false)
                                    ServerConfiguration.LogInfo($"accountToAdd added to DB successfully!");
                                else
                                    ServerConfiguration.LogWarn($"accountToAdd Failed to add!");

                            });
                        }
                        break;

                    }
                case MediusBuddyAddType.AddSymmetric:
                    {
                        if (accountClientToAdd != null && clientObject.ApplicationId != 20214)
                        {
                            ServerConfiguration.LogInfo($"AnswerAddToBuddyListConfirmationRequest: Target player found in cache. forwarding to ClientIndex [{clientObject.AccountId}] WorldID [{clientObject.WorldId}]");
                            ServerConfiguration.LogInfo($"accountToAdd [{accountClientToAdd.AccountName}] is online, forwarding request!");
                            //channel.AddToBuddyListConfirmationSymmetricRequest(clientObject, accountClientToAdd, addToBuddyListConfirmation);
                            if (accountClientToAdd.IsLoggedIn == true)
                            {
                                accountClientToAdd.Queue(new MediusAddToBuddyListFwdConfirmationRequest()
                                {
                                    MessageID = addToBuddyListConfirmation.MessageID,
                                    OriginatorAccountID = clientObject.AccountId,
                                    OriginatorAccountName = clientObject.AccountName,
                                    AddType = MediusBuddyAddType.AddSymmetric,
                                });
                            }
                        }
                        else
                        {
                            ServerConfiguration.LogWarn($"accountToAdd [{accountClientToAdd.AccountName}] is being added to list!");

                            AccountRelationInviteDTO buddy = new AccountRelationInviteDTO()
                            {
                                AccountId = clientObject.AccountId,
                                AccountName = clientObject.AccountName,
                                BuddyAccountId = accountToAdd.AccountId,
                                AppId = clientObject.ApplicationId,
                                addType = (int)MediusBuddyAddType.AddSymmetric
                            };

                            _ = ServerConfiguration.Database.addBuddyInvitation(buddy).ContinueWith(r =>
                            {
                                if (r.IsCompletedSuccessfully && r.Result != false)
                                    ServerConfiguration.LogInfo($"accountToAdd added to DB successfully!");
                                else
                                    ServerConfiguration.LogWarn($"accountToAdd Failed to add!");
                            });
                        }
                        break;
                    }
                default:
                    {
                        ServerConfiguration.LogWarn($"Unhandled add to buddy list confirmation message type: {addToBuddyListConfirmation.AddType}");
                        break;
                    }
            }


            // Send to plugins
            //await MediusClass.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_CHAT_MESSAGE, new OnPlayerChatMessageArgs() { Channel = channel, Player = clientObject, Message = chatMessage });

            return;
        }

        private void ProcessAddToBuddyListConfirmationResponse(ClientObject clientObject, AccountDTO accountToAdd, MediusAddToBuddyListFwdConfirmationResponse addToBuddyListConfirmationResponse)
        {
            var channel = clientObject.CurrentChannel;
            var accountClientToAdd = MediusClass.Manager.GetClientByAccountId(accountToAdd.AccountId, clientObject.ApplicationId);
            //var accountClientToAdd = channel.Clients.Where(x => x.AccountName == accountToAdd.AccountName).First();
            // ERROR -- Need to be logged in
            if (!clientObject.IsLoggedIn)
                return;

            if (addToBuddyListConfirmationResponse.StatusCode == MediusCallbackStatus.MediusRequestAccepted)
            {
                if (channel == null || clientObject == null || !clientObject.IsConnected)
                    return;

                switch (addToBuddyListConfirmationResponse.AddType)
                {
                    case MediusBuddyAddType.AddSingle:
                        {
                            // Add
                            _ = ServerConfiguration.Database.AddBuddy(new BuddyDTO()
                            {
                                AccountId = clientObject.AccountId,
                                BuddyAccountId = addToBuddyListConfirmationResponse.OriginatorAccountID
                            }).ContinueWith((ab) =>
                            {
                                if (channel == null || clientObject == null || !clientObject.IsConnected)
                                    return;

                                if (ab.IsCompletedSuccessfully && ab.Result)
                                {
                                    ServerConfiguration.LogInfo($"AnswerAddToBuddyListConfirmationResponse: Originator player found in cache. adding [{accountToAdd.AccountId}] to [{clientObject.AccountId}] buddy list");
                                    if (accountClientToAdd.IsLoggedIn == true)
                                    {
                                        accountClientToAdd.Queue(new MediusAddToBuddyListConfirmationResponse()
                                        {
                                            MessageID = addToBuddyListConfirmationResponse.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusSuccess,
                                            TargetAccountID = accountClientToAdd.AccountId,
                                            TargetAccountName = accountClientToAdd.AccountName
                                        });
                                    }

                                }
                                else
                                {

                                    clientObject.Queue(new MediusAddToBuddyListConfirmationResponse()
                                    {
                                        MessageID = addToBuddyListConfirmationResponse.MessageID,
                                        StatusCode = MediusCallbackStatus.MediusDBError
                                    });
                                }
                            });

                            //channel.AddToBuddyListConfirmationSingleResponse(clientObject, accountClientToAdd, addToBuddyListConfirmationResponse);
                            break;

                        }
                    case MediusBuddyAddType.AddSymmetric:
                        {
                            // Add
                            _ = ServerConfiguration.Database.AddBuddy(new BuddyDTO()
                            {
                                AccountId = clientObject.AccountId,
                                BuddyAccountId = addToBuddyListConfirmationResponse.OriginatorAccountID
                            }).ContinueWith((ab) =>
                            {
                                // Add
                                _ = ServerConfiguration.Database.AddBuddy(new BuddyDTO()
                                {
                                    AccountId = addToBuddyListConfirmationResponse.OriginatorAccountID,
                                    BuddyAccountId = clientObject.AccountId
                                }).ContinueWith((abr) =>
                                {

                                    if (channel == null || clientObject == null || !clientObject.IsConnected)
                                        return;

                                    if (ab.IsCompletedSuccessfully && ab.Result && abr.IsCompletedSuccessfully && abr.Result)
                                    {
                                        ServerConfiguration.LogInfo($"AnswerAddToBuddyListConfirmationResponse: Originator player found in cache. adding [{accountToAdd.AccountId}] to [{clientObject.AccountId}] buddy list");

                                        if (accountClientToAdd.IsLoggedIn == true)
                                        {
                                            accountClientToAdd.Queue(new MediusAddToBuddyListConfirmationResponse()
                                            {
                                                MessageID = addToBuddyListConfirmationResponse.MessageID,
                                                StatusCode = MediusCallbackStatus.MediusRequestAccepted,
                                                TargetAccountID = accountClientToAdd.AccountId,
                                                TargetAccountName = accountClientToAdd.AccountName
                                            });
                                        }

                                    }
                                    else
                                    {
                                        clientObject.Queue(new MediusAddToBuddyListConfirmationResponse()
                                        {
                                            MessageID = addToBuddyListConfirmationResponse.MessageID,
                                            StatusCode = MediusCallbackStatus.MediusDBError
                                        });
                                    }
                                });
                            });

                            //channel.AddToBuddyListConfirmationSymmetricResponse(clientObject, (ClientObject)accountClientToAdd, addToBuddyListConfirmationResponse);
                            break;
                        }
                    default:
                        {
                            ServerConfiguration.LogWarn($"Unhandled add to buddy list confirmation message type: {addToBuddyListConfirmationResponse.AddType}");
                            break;
                        }
                }


            }
            else if (addToBuddyListConfirmationResponse.StatusCode == MediusCallbackStatus.MediusRequestDenied)
            {
                AccountRelationInviteDTO accountRelationInviteDTO = new AccountRelationInviteDTO()
                {
                    AccountId = clientObject.AccountId,
                    BuddyAccountId = addToBuddyListConfirmationResponse.OriginatorAccountID,
                    AppId = clientObject.ApplicationId
                };

                ServerConfiguration.Database.deleteBuddyInvitation(accountRelationInviteDTO).ContinueWith(r =>
                {
                    if (r.IsCompletedSuccessfully && r.Result)
                    {
                        accountClientToAdd.Queue(new MediusAddToBuddyListConfirmationResponse()
                        {
                            MessageID = addToBuddyListConfirmationResponse.MessageID,
                            StatusCode = MediusCallbackStatus.MediusRequestDenied,
                            TargetAccountID = accountClientToAdd.AccountId,
                            TargetAccountName = accountClientToAdd.AccountName
                        });

                        _ = clientObject.RefreshAccount();
                    }
                    else
                    {
                        accountClientToAdd.Queue(new MediusAddToBuddyListConfirmationResponse()
                        {
                            MessageID = addToBuddyListConfirmationResponse.MessageID,
                            StatusCode = MediusCallbackStatus.MediusDBError,
                            TargetAccountID = accountClientToAdd.AccountId,
                            TargetAccountName = accountClientToAdd.AccountName
                        });
                    }
                });
            }

            // Send to plugins
            //await MediusClass.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_CHAT_MESSAGE, new OnPlayerChatMessageArgs() { Channel = channel, Player = clientObject, Message = chatMessage });

            return;
        }

        #endregion

        #region ProcessChatMessage
        private async Task ProcessChatMessage(IChannel clientChannel, ClientObject clientObject, MessageId msgId, IMediusChatMessage chatMessage)
        {
            try
            {
                var channel = clientObject.CurrentChannel;
                var game = clientObject.CurrentGame;
                var currentClanId = clientObject.ClanId;
                var allPlayers = channel.Clients;
                var allInClan = channel.Clients.Where(x => x.ClanId != currentClanId);
                var allButSender = channel.Clients.Where(x => x != clientObject);
                var targetPlayer = channel.Clients.FirstOrDefault(x => x.AccountId == chatMessage.TargetID);

                List<BaseScertMessage> chatResponses = new List<BaseScertMessage>();

                // Need to be logged in
                if (!clientObject.IsLoggedIn)
                    return;

                // Need to be in a channel
                if (channel == null)
                    return;

                // Send to plugins
                await MediusClass.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_CHAT_MESSAGE, new OnPlayerChatMessageArgs() { Channel = channel, Player = clientObject, Message = chatMessage });

                switch (chatMessage.MessageType)
                {
                    //Missing BroadcastAcrossEntireUniverse and Buddy
                    case MediusChatMessageType.Broadcast:
                        {
                            if (allButSender.Count() > 0 || chatMessage.TargetID == -1) //iNumPlayersReturned
                            {
                                // Relay
                                foreach (var target in allButSender)
                                {
                                    target.Queue(new MediusChatFwdMessage()
                                    {
                                        MessageID = msgId,
                                        OriginatorAccountID = clientObject.AccountId,
                                        OriginatorAccountName = clientObject.AccountName,
                                        MessageType = chatMessage.MessageType,
                                        Message = chatMessage.Message
                                    });
                                }
                            }
                            else
                                ServerConfiguration.LogWarn($"0 players found to send chat msg to");
                            break;
                        }
                    case MediusChatMessageType.Whisper:
                        {
                            // Send to
                            targetPlayer?.Queue(new MediusChatFwdMessage()
                            {
                                MessageID = msgId,
                                OriginatorAccountID = clientObject.AccountId,
                                OriginatorAccountName = clientObject.AccountName,
                                MessageType = chatMessage.MessageType,
                                Message = chatMessage.Message
                            });
                            break;
                        }
                    case MediusChatMessageType.Clan:
                        {
                            // Relay
                            foreach (var targetInClan in allInClan)
                            {
                                targetInClan.Queue(new MediusChatFwdMessage()
                                {
                                    MessageID = msgId,
                                    OriginatorAccountID = clientObject.AccountId,
                                    OriginatorAccountName = clientObject.AccountName,
                                    MessageType = chatMessage.MessageType,
                                    Message = chatMessage.Message
                                });
                            }
                            break;
                        }
                    default:
                        {
                            ServerConfiguration.LogWarn($"Unhandled generic chat message type: {chatMessage.MessageType} {chatMessage}");
                            break;
                        }
                }
            }
            catch (Exception)
            {
                ServerConfiguration.LogError("$Failed to get object reference: {ex}");
            }
        }
        #endregion

        #region ProcessGenericChatMessage
        private async Task ProcessGenericChatMessage(IChannel clientChannel, ClientObject clientObject, IMediusChatMessage chatMessage)
        {
            var channel = clientObject.CurrentChannel;
            var game = clientObject.CurrentGame;
            var currentClanId = clientObject.ClanId;
            var allPlayers = channel.Clients;
            var allInClan = channel.Clients.Where(x => x.ClanId == currentClanId);
            var allButSender = channel.Clients.Where(x => x != clientObject);
            List<BaseScertMessage> chatResponses = new List<BaseScertMessage>();

            // ERROR -- Need to be logged in
            if (!clientObject.IsLoggedIn)
                return;

            // Need to be in a channel
            if (channel == null)
                return;

            // Send to plugins
            await MediusClass.Plugins.OnEvent(PluginEvent.MEDIUS_PLAYER_ON_CHAT_MESSAGE, new OnPlayerChatMessageArgs() { Channel = channel, Player = clientObject, Message = chatMessage });

            switch (chatMessage.MessageType)
            {
                //Missing BroadcastAcrossEntireUniverse and Buddy
                case MediusChatMessageType.Broadcast:
                    {
                        // Relay
                        channel.BroadcastChatMessage(allButSender, clientObject, chatMessage.Message);
                        break;
                    }
                case MediusChatMessageType.Whisper:
                    {
                        //Whisper
                        channel.WhisperChatMessage(allButSender, clientObject, chatMessage.Message);
                        break;
                    }
                case MediusChatMessageType.Clan:
                    {
                        //Clan
                        channel.ClanChatMessage(allInClan, clientObject, chatMessage.Message);
                        break;
                    }
                default:
                    {
                        ServerConfiguration.LogWarn($"Unhandled generic chat message type:{chatMessage.MessageType} {chatMessage}");
                        break;
                    }
            }
        }
        #endregion

        public Task<ONLINE_STATUS_TYPE> MediusChatStatusToOnlineStatus(MediusChatStatus status)
        {
            ONLINE_STATUS_TYPE onlineStatusType;

            switch (status)
            {
                case MediusChatStatus.MediusChatStatusNoResponse:
                    onlineStatusType = ONLINE_STATUS_TYPE.OFFLINE;
                    break;
                case MediusChatStatus.MediusChatStatusAvailable:
                    onlineStatusType = ONLINE_STATUS_TYPE.AVAILABLE;
                    break;
                case MediusChatStatus.MediusChatStatusPrivate:
                    onlineStatusType = ONLINE_STATUS_TYPE.PRIVATE;
                    break;
                case MediusChatStatus.MediusChatStatusAway:
                    onlineStatusType = ONLINE_STATUS_TYPE.AWAY;
                    break;
                case MediusChatStatus.MediusChatStatusIdle:
                    onlineStatusType = ONLINE_STATUS_TYPE.IDLE;
                    break;
                case MediusChatStatus.MediusChatStatusStaging:
                    onlineStatusType = ONLINE_STATUS_TYPE.STAGING;
                    break;
                case MediusChatStatus.MediusChatStatusLoading:
                    onlineStatusType = ONLINE_STATUS_TYPE.LOADING;
                    break;
                case MediusChatStatus.MediusChatStatusInGame:
                    onlineStatusType = ONLINE_STATUS_TYPE.IN_GAME;
                    break;
                case MediusChatStatus.MediusChatStatusChatHost:
                    onlineStatusType = ONLINE_STATUS_TYPE.CHAT_HOST;
                    break;
                case MediusChatStatus.MediusChatStatusChatClient:
                    onlineStatusType = ONLINE_STATUS_TYPE.CHAT_CLIENT;
                    break;
                default:
                    onlineStatusType = ONLINE_STATUS_TYPE.OFFLINE;
                    break;
            }
            return Task.FromResult(onlineStatusType);
        }

        public Task<MediusChatStatus> OnlineStatusToMediusChatStatus(ONLINE_STATUS_TYPE status)
        {
            MediusChatStatus onlineChatStatus;

            switch (status)
            {
                case ONLINE_STATUS_TYPE.OFFLINE:
                    onlineChatStatus = MediusChatStatus.MediusChatStatusNoResponse;
                    break;
                case ONLINE_STATUS_TYPE.AVAILABLE:
                    onlineChatStatus = MediusChatStatus.MediusChatStatusAvailable;
                    break;
                case ONLINE_STATUS_TYPE.PRIVATE:
                    onlineChatStatus = MediusChatStatus.MediusChatStatusPrivate;
                    break;
                case ONLINE_STATUS_TYPE.AWAY:
                    onlineChatStatus = MediusChatStatus.MediusChatStatusAway;
                    break;
                case ONLINE_STATUS_TYPE.IDLE:
                    onlineChatStatus = MediusChatStatus.MediusChatStatusIdle;
                    break;
                case ONLINE_STATUS_TYPE.STAGING:
                    onlineChatStatus = MediusChatStatus.MediusChatStatusStaging;
                    break;
                case ONLINE_STATUS_TYPE.LOADING:
                    onlineChatStatus = MediusChatStatus.MediusChatStatusLoading;
                    break;
                case ONLINE_STATUS_TYPE.IN_GAME:
                    onlineChatStatus = MediusChatStatus.MediusChatStatusInGame;
                    break;
                case ONLINE_STATUS_TYPE.CHAT_HOST:
                    onlineChatStatus = MediusChatStatus.MediusChatStatusChatHost;
                    break;
                case ONLINE_STATUS_TYPE.CHAT_CLIENT:
                    onlineChatStatus = MediusChatStatus.MediusChatStatusChatClient;
                    break;
                default:
                    onlineChatStatus = MediusChatStatus.MediusChatStatusNoResponse;
                    break;
            }
            return Task.FromResult(onlineChatStatus);
        }

        public void anticheatCreateLobbyNotify(int appId, ClientObject client, string LobbyName, Action src, string LobbyPassword)
        {
            AnticheatEvent_CreateLobbyWorld anticheatEvent_CreateLobbyWorld = new AnticheatEvent_CreateLobbyWorld();
            anticheatEvent_CreateLobbyWorld.LobbyName = LobbyName;
            anticheatEvent_CreateLobbyWorld.LobbyPassword = LobbyPassword;

            MediusClass.AntiCheatPlugin.mc_anticheat_event_msg_CREATELOBBYWORLD(AnticheatEventCode.anticheatCREATELOBBYWORLD, client.WorldId, client.AccountId, MediusClass.AntiCheatClient, anticheatEvent_CreateLobbyWorld, 96);
        }

        public DMEObject GetFreeMPS(int appId)
        {
            try
            {
                return _scertHandler.Group
                    .Select(x => _channelDatas[x.Id.AsLongText()]?.ClientObject)
                    .Where(x => x is DMEObject && x != null && (x.ApplicationId == appId || x.ApplicationId == 0))
                    .MinBy(x => (x as DMEObject).CurrentWorlds) as DMEObject;
            }
            catch (Exception ex)
            {
                ServerConfiguration.LogError("No Medius Proxy Server assigned to this AppId", ex);
            }

            return null;
        }
    }
}

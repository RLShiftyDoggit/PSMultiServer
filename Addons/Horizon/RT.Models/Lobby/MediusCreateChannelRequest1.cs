using MultiServer.Addons.Horizon.RT.Common;
using MultiServer.Addons.Horizon.LIBRARY.Common.Stream;

namespace MultiServer.Addons.Horizon.RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobbyExt, MediusLobbyExtMessageIds.CreateChannel1)]
    public class MediusCreateChannelRequest1 : BaseLobbyExtMessage, IMediusRequest
    {

        public override byte PacketType => (byte)MediusLobbyExtMessageIds.CreateChannel1;

        public MessageId MessageID { get; set; }

        public string SessionKey; // SESSIONKEY_MAXLEN
        public int ApplicationID;
        public int MaxPlayers;
        public string LobbyName; // LOBBYNAME_MAXLEN
        public string LobbyPassword; // LOBBYPASSWORD_MAXLEN

        public override void Deserialize(MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(2);
            ApplicationID = reader.ReadInt32();
            MaxPlayers = reader.ReadInt32();
            LobbyName = reader.ReadString(Constants.LOBBYNAME_MAXLEN);
            LobbyPassword = reader.ReadString(Constants.LOBBYPASSWORD_MAXLEN);
        }

        public override void Serialize(MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[2]);
            writer.Write(ApplicationID);
            writer.Write(MaxPlayers);
            writer.Write(LobbyName, Constants.LOBBYNAME_MAXLEN);
            writer.Write(LobbyPassword, Constants.LOBBYPASSWORD_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"SessionKey: {SessionKey} " +
                $"ApplicationID: {ApplicationID} " +
                $"MaxPlayers: {MaxPlayers} " +
                $"LobbyName: {LobbyName} " +
                $"LobbyPassword: {LobbyPassword} ";
        }
    }
}

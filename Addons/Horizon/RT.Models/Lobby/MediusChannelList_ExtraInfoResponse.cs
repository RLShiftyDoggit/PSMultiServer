using MultiServer.Addons.Horizon.RT.Common;
using MultiServer.Addons.Horizon.LIBRARY.Common.Stream;

namespace MultiServer.Addons.Horizon.RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobby, MediusLobbyMessageIds.ChannelList_ExtraInfoResponse)]
    public class MediusChannelList_ExtraInfoResponse : BaseLobbyMessage, IMediusResponse
    {
        public override byte PacketType => (byte)MediusLobbyMessageIds.ChannelList_ExtraInfoResponse;

        public bool IsSuccess => StatusCode >= 0;

        public MessageId MessageID { get; set; }

        public MediusCallbackStatus StatusCode;
        public int MediusWorldID;
        public ushort PlayerCount;
        public ushort MaxPlayers;
        public ushort GameWorldCount;
        public MediusWorldSecurityLevelType SecurityLevel;
        public uint GenericField1;
        public uint GenericField2;
        public uint GenericField3;
        public uint GenericField4;
        public MediusWorldGenericFieldLevelType GenericFieldLevel;
        public string LobbyName; // LOBBYNAME_MAXLEN
        public bool EndOfList;

        public override void Deserialize(MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            MediusWorldID = reader.ReadInt32();
            PlayerCount = reader.ReadUInt16();
            MaxPlayers = reader.ReadUInt16();

            // Older Pre 1.50 Medius titles didn't include this
            if (reader.MediusVersion > 108) 
            {
                GameWorldCount = reader.ReadUInt16();
                reader.ReadBytes(2);
            }

            SecurityLevel = reader.Read<MediusWorldSecurityLevelType>();
            GenericField1 = reader.ReadUInt32();

            //WRC4 uses these fields
            if (reader.MediusVersion > 108 || reader.AppId == 10304 || reader.AppId == 10202)
            {
                GenericField2 = reader.ReadUInt32();
                GenericField3 = reader.ReadUInt32();
                GenericField4 = reader.ReadUInt32();
                GenericFieldLevel = reader.Read<MediusWorldGenericFieldLevelType>();
            }

            LobbyName = reader.ReadString(Constants.LOBBYNAME_MAXLEN);
            EndOfList = reader.ReadBoolean();
            reader.ReadBytes(3);
        }

        public override void Serialize(MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(MediusWorldID);
            writer.Write(PlayerCount);
            writer.Write(MaxPlayers);

            if (writer.MediusVersion > 108)
            {
                writer.Write(GameWorldCount);
                writer.Write(new byte[2]);
            }

            writer.Write(SecurityLevel);
            writer.Write(GenericField1);

            if (writer.MediusVersion > 108 || writer.AppId == 10304 || writer.AppId == 10202)
            {
                writer.Write(GenericField2);
                writer.Write(GenericField3);
                writer.Write(GenericField4);
                writer.Write(GenericFieldLevel);
            }
            writer.Write(LobbyName, Constants.LOBBYNAME_MAXLEN);
            writer.Write(EndOfList);
            writer.Write(new byte[3]);
        }

        public override string ToString()
        {
                return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"StatusCode: {StatusCode} " +
                $"MediusWorldID: {MediusWorldID} " +
                $"PlayerCount: {PlayerCount} " +
                $"MaxPlayers: {MaxPlayers} " +
                $"GameWorldCount: {GameWorldCount} " +
                $"SecurityLevel: {SecurityLevel} " +
                $"GenericField1: {GenericField1:X8} " +
                $"GenericField2: {GenericField2:X8} " +
                $"GenericField3: {GenericField3:X8} " +
                $"GenericField4: {GenericField4:X8} " +
                $"GenericFieldLevel: {GenericFieldLevel} " +
                $"LobbyName: {LobbyName} " +
                $"EndOfList: {EndOfList}";
        }
    }
}
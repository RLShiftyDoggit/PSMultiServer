using MultiServer.Addons.Horizon.RT.Common;
using MultiServer.Addons.Horizon.LIBRARY.Common.Stream;

namespace MultiServer.Addons.Horizon.RT.Models
{
    /// <summary>
    /// Introduced in Medius 1.42
    /// </summary>
	[MediusMessage(NetMessageClass.MessageClassLobby, MediusLobbyMessageIds.RemoveFromIgnoreListResponse)]
    public class MediusRemoveFromIgnoreListResponse : BaseLobbyMessage, IMediusResponse
    {
        public override byte PacketType => (byte)MediusLobbyMessageIds.RemoveFromIgnoreListResponse;

        public bool IsSuccess => StatusCode >= 0;

        public MessageId MessageID { get; set; }

        public MediusCallbackStatus StatusCode;

        public override void Deserialize(MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
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
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
             $"StatusCode:{StatusCode}";
        }
    }
}

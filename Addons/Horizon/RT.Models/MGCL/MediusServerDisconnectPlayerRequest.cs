using MultiServer.Addons.Horizon.RT.Common;
using MultiServer.Addons.Horizon.LIBRARY.Common.Stream;

namespace MultiServer.Addons.Horizon.RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobbyReport, MediusMGCLMessageIds.ServerDisconnectPlayerRequest)]
    public class MediusServerDisconnectPlayerRequest : BaseMGCLMessage, IMediusRequest
    {

        public override byte PacketType => (byte)MediusMGCLMessageIds.ServerDisconnectPlayerRequest;

        public MessageId MessageID { get; set; }
        public int DmeWorldID;
        public int DmeClientIndex;

        public override void Deserialize(MessageReader reader)
        {
            base.Deserialize(reader);

            MessageID = reader.Read<MessageId>();
            reader.ReadBytes(3);
            DmeWorldID = reader.ReadInt32();
            DmeClientIndex = reader.ReadInt32();
        }

        public override void Serialize(MessageWriter writer)
        {
            base.Serialize(writer);

            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(new byte[3]);
            writer.Write(DmeWorldID);
            writer.Write(DmeClientIndex);
        }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"DmeWorldID: {DmeWorldID} " +
                $"DmeClientIndex: {DmeClientIndex}";
        }
    }
}
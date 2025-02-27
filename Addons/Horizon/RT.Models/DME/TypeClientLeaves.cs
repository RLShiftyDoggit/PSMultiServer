using MultiServer.Addons.Horizon.RT.Common;
using MultiServer.Addons.Horizon.LIBRARY.Common.Stream;

namespace MultiServer.Addons.Horizon.RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassDME, MediusDmeMessageIds.ClientLeaves)]
    public class TypeClientLeaves : BaseDMEMessage
    {

        public override byte PacketType => (byte)MediusDmeMessageIds.ClientLeaves;

        public byte ClientIndex;

        public override void Deserialize(MessageReader reader)
        {
            base.Deserialize(reader);

            ClientIndex = reader.ReadByte();
        }

        public override void Serialize(MessageWriter writer)
        {
            base.Serialize(writer);

            writer.Write(ClientIndex);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"ClientIndex: {ClientIndex}";
        }
    }
}
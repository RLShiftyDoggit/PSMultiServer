using MultiServer.Addons.Horizon.RT.Common;
using MultiServer.Addons.Horizon.LIBRARY.Common.Stream;

namespace MultiServer.Addons.Horizon.RT.Models
{
    public class RawScertMessage : BaseScertMessage
    {

        private readonly RT_MSG_TYPE _id;
        public override RT_MSG_TYPE Id => _id;

        public byte[] Contents { get; set; } = null;


        public RawScertMessage(RT_MSG_TYPE id)
        {
            _id = id;
        }

        #region Serialization

        public override void Deserialize(MessageReader reader)
        {
            // 
            Contents = reader.ReadRest();
        }

        public override void Serialize(MessageWriter writer)
        {
            if (Contents != null)
                writer.Write(Contents);
        }

        #endregion

        public override string ToString()
        {
            return base.ToString() + $" Id:{Id} (0x{(short)Id:X2}) Contents:{BitConverter.ToString(Contents)}";
        }
    }
}

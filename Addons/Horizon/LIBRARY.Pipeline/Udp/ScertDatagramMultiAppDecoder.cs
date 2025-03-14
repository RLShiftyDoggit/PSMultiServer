﻿using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using MultiServer.Addons.Horizon.RT.Models;

namespace MultiServer.Addons.Horizon.LIBRARY.Pipeline.Udp
{
    public class ScertDatagramMultiAppDecoder : MessageToMessageDecoder<ScertDatagramPacket>
    {
        /// <summary>
        ///     Create a new instance.
        /// </summary>
        public ScertDatagramMultiAppDecoder()
        {

        }

        protected override void Decode(IChannelHandlerContext context, ScertDatagramPacket input, List<object> output)
        {
            try
            {
                if (input.Message is RT_MSG_CLIENT_MULTI_APP_TOSERVER multiApp)
                {
                    foreach (var message in multiApp.Messages)
                        output.Add(new ScertDatagramPacket(message, input.Destination, input.Source));
                }
                else
                {
                    output.Add(input);
                }
            }
            catch (Exception ex)
            {
                ServerConfiguration.LogWarn(ex.ToString());
            }
        }
    }
}

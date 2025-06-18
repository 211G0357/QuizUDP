using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Servidor.Models;

namespace Servidor.Services
{
    public class ServidorUDP
    {
        UdpClient servidor;
        public ServidorUDP()
        {
            servidor = new(54003);
            servidor.EnableBroadcast = true;

            Thread hilo = new(RecibirRespuesta);
            hilo.IsBackground = true;
            hilo.Start();
        }
        public event Action<string, string, string>? RespuestaRecibida;

        void RecibirRespuesta()
        {
            while (true)
            {
                IPEndPoint cliente = new(IPAddress.Any, 0);

                byte[] buffer = servidor.Receive(ref cliente);

                var json = Encoding.UTF8.GetString(buffer);
                var rect = JsonSerializer.Deserialize<Respuesta>(json);

                if (rect != null)
                {
                    RespuestaRecibida?.Invoke(rect.nombre, rect.respuesta, rect.EndpointId);
                }
            }
        }
    }
}

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Sockets;
//using System.Net;
//using System.Text;
//using System.Text.Json;
//using System.Threading.Tasks;
//using Servidor.Models;

//namespace Servidor.Services
//{
//    public class ServidorUDP
//    {
//        UdpClient servidor;

//        public ServidorUDP()
//        {
//            servidor = new UdpClient(60001); 
//            servidor.EnableBroadcast = true;

//            Thread hilo = new(RecibirRespuesta)
//            {
//                IsBackground = true
//            };
//            hilo.Start();
//        }

//        public event Action<string, string, string>? RespuestaRecibida;

//        void RecibirRespuesta()
//        {
//            while (true)
//            {
//                IPEndPoint cliente = new(IPAddress.Any, 0);
//                byte[] buffer = servidor.Receive(ref cliente);

//                var json = Encoding.UTF8.GetString(buffer);
//                var rect = JsonSerializer.Deserialize<Respuesta>(json);

//                if (rect != null)
//                {
//                    RespuestaRecibida?.Invoke(rect.nombre, rect.respuesta, rect.EndpointId);
//                }
//            }
//        }
//    }
//    }

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using Servidor.Models;

namespace Servidor.Services
{
    public class ServidorUDP
    {
        private readonly UdpClient servidor;

        public event Action<string, string, string>? RespuestaRecibida;

        public ServidorUDP()
        {
            servidor = new UdpClient(60001); // Puerto donde escucha el servidor
            servidor.EnableBroadcast = true;

            Thread hilo = new Thread(RecibirRespuesta)
            {
                IsBackground = true
            };
            hilo.Start();
        }

        private void RecibirRespuesta()
        {
            while (true)
            {
                try
                {
                    IPEndPoint cliente = new IPEndPoint(IPAddress.Any, 0);
                    byte[] buffer = servidor.Receive(ref cliente);
                    string mensaje = Encoding.UTF8.GetString(buffer);

                    // Responder a PING
                    if (mensaje == "PING")
                    {
                        Console.WriteLine($"PING recibido desde {cliente.Address}");
                        byte[] respuesta = Encoding.UTF8.GetBytes("PONG");
                        servidor.Send(respuesta, respuesta.Length, cliente);
                        continue;
                    }

                    // Procesar una respuesta real
                    var rect = JsonSerializer.Deserialize<Respuesta>(mensaje);

                    if (rect != null)
                    {
                        Console.WriteLine($"Respuesta recibida de {rect.nombre}: {rect.respuesta}");
                        RespuestaRecibida?.Invoke(rect.nombre, rect.respuesta, rect.EndpointId);
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[JSON] Error al deserializar mensaje: {ex.Message}");
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"[Socket] Error al recibir datos: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[General] Error: {ex.Message}");
                }
            }
        }
    }
}



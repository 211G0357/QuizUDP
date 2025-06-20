using Cliente.Models;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Cliente.Services
{
    public class ClienteUDP
    {
        private readonly UdpClient cliente;
        private string endpointId;

        public string Servidor { get; set; } = "0.0.0.0"; 

        public ClienteUDP()
        {
            cliente = new UdpClient();
            cliente.EnableBroadcast = true;

            cliente.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

           
            var localEndpoint = (cliente.Client.LocalEndPoint as IPEndPoint)!;
            endpointId = $"{localEndpoint.Address}:{localEndpoint.Port}";
        }

        public void Enviar(Respuesta res)
        {
            if (string.IsNullOrWhiteSpace(Servidor))
            {
                Console.WriteLine("Error: IP del servidor no está establecida.");
                return;
            }

        
            res.EndpointId = endpointId;

            var json = JsonSerializer.Serialize(res);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            var ipe = new IPEndPoint(IPAddress.Parse(Servidor), 60001);
            cliente.Send(buffer, buffer.Length, ipe);

            Console.WriteLine($"Enviado a {ipe}: {json}");
        }

        public void Cerrar()
        {
            cliente.Close();
        }
    }
}

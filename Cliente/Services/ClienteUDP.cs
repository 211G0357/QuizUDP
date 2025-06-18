using Cliente.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cliente.Services
{
    public class ClienteUDP
    {
        UdpClient cliente;
        IPEndPoint conexion;
        private string endpointId;

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
            conexion = new(IPAddress.Broadcast, 60001); 
            res.EndpointId = endpointId;
            var json = JsonSerializer.Serialize(res);
            byte[] mensajeBinario = Encoding.UTF8.GetBytes(json ?? "Error#404XD");
            cliente.Send(mensajeBinario, mensajeBinario.Length, conexion);
        }

        public void Cerrar()
        {
            cliente.Close();
        }
    }
    }

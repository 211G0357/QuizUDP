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
        private IPEndPoint conexion;
        private readonly string endpointId;

        public Action<object?, string> NotificacionRecibida { get; internal set; }

        public event EventHandler<string>? PingRespondido;

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
            conexion = new IPEndPoint(IPAddress.Broadcast, 60001);
            res.IpCliente = endpointId;

            var json = JsonSerializer.Serialize(res);
            byte[] mensajeBinario = Encoding.UTF8.GetBytes(json ?? "Error");
            cliente.Send(mensajeBinario, mensajeBinario.Length, conexion);
        }

        public void EnviarPing(string ipDestino)
        {
            Task.Run(async () =>
            {
                using var pingCliente = new UdpClient(0); // SOLO ESTE SOCKET
                try
                {
                    IPEndPoint endPoint = new(IPAddress.Parse(ipDestino), 60001);

                    string mensajePing = JsonSerializer.Serialize(new
                    {
                        tipo = "Ping",
                        ipCliente = endpointId
                    });

                    byte[] datos = Encoding.UTF8.GetBytes(mensajePing);
                    await pingCliente.SendAsync(datos, datos.Length, endPoint);

                    // Esperar respuesta en el mismo socket
                    pingCliente.Client.ReceiveTimeout = 3000;
                    var from = new IPEndPoint(IPAddress.Any, 0);
                    var response = pingCliente.Receive(ref from);
                    string texto = Encoding.UTF8.GetString(response);

                    if (texto.Contains("Pong"))
                        PingRespondido?.Invoke(this, "Pong");
                    else
                        PingRespondido?.Invoke(this, "PingTimeout");
                }
                catch
                {
                    PingRespondido?.Invoke(this, "PingTimeout");
                }
            });
        }

        public void Cerrar()
        {
            cliente.Close();
        }
    }
    }

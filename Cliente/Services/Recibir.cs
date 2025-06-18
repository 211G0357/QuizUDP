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
    public class Recibir
    {
        private readonly UdpClient clienteReceptor;

        public Recibir()
        {
            clienteReceptor = new UdpClient(60000); 
            clienteReceptor.EnableBroadcast = true;

            Thread hilo = new(RecibirRespuesta)
            {
                IsBackground = true
            };
            hilo.Start();
        }

        public event Action<Pregunta>? RespuestaRecibida;

        private void RecibirRespuesta()
        {
            while (true)
            {
                try
                {
                    IPEndPoint remitente = new(IPAddress.Any, 0);
                    byte[] buffer = clienteReceptor.Receive(ref remitente);

                    string json = Encoding.UTF8.GetString(buffer);
                    var pregunta = JsonSerializer.Deserialize<Pregunta>(json);
                    if (pregunta != null)
                    {
                        RespuestaRecibida?.Invoke(pregunta);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al recibir la pregunta: {ex.Message}");
                }
            }
        }
    }
    }

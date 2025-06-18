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
    public class Enviar
    {
        UdpClient servidor;
        IPEndPoint conexion;
        public Enviar()
        {
            servidor = new UdpClient();
            servidor.EnableBroadcast = true;

        }
        public void EnviarPregunta(Pregunta pregunta)
        {
            try
            {
                conexion = new(IPAddress.Broadcast, 54001);
                var json = JsonSerializer.Serialize(pregunta);
                byte[] mensajeBinario = Encoding.UTF8.GetBytes(json ?? "Error#404XD");
                servidor.Send(mensajeBinario, mensajeBinario.Length, conexion);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar la pregunta: {ex.Message}");
            }

        }
    }
}

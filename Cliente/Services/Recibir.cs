using System.Net.Sockets;
using System.Net;
using System.Text.Json;
using System.Text;
using Cliente.Models;
public class Recibir
{
    private readonly UdpClient clienteReceptor;
    private IPEndPoint? servidorEndpoint;

    public event Action<Pregunta>? RespuestaRecibida;

    public Recibir()
    {
        clienteReceptor = new UdpClient(60000); // Escucha aquí
        clienteReceptor.EnableBroadcast = true;

        // Enviar broadcast PING
        Task.Run(() =>
        {
            using var udpPing = new UdpClient();
            udpPing.EnableBroadcast = true;

            byte[] datos = Encoding.UTF8.GetBytes("PING");
            IPEndPoint broadcast = new IPEndPoint(IPAddress.Broadcast, 60001);
            udpPing.Send(datos, datos.Length, broadcast);
        });

        // Iniciar hilo de escucha
        Thread hilo = new Thread(RecibirRespuesta) { IsBackground = true };
        hilo.Start();
    }

    private void RecibirRespuesta()
    {
        while (true)
        {
            try
            {
                IPEndPoint remitente = new IPEndPoint(IPAddress.Any, 0);
                byte[] buffer = clienteReceptor.Receive(ref remitente);
                string mensaje = Encoding.UTF8.GetString(buffer);

                if (mensaje == "PONG")
                {
                    Console.WriteLine($"Servidor detectado: {remitente.Address}");
                    servidorEndpoint = new IPEndPoint(remitente.Address, 60001);
                    continue;
                }

                var pregunta = JsonSerializer.Deserialize<Pregunta>(mensaje);
                if (pregunta != null)
                {
                    RespuestaRecibida?.Invoke(pregunta);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al recibir: {ex.Message}");
            }
        }
    }
}

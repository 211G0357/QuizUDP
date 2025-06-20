using Cliente.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cliente.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Cliente.ViewModels
{
    public partial class ClienteViewModel : ObservableObject
    {
        private ClienteUDP cliente;

        Recibir clienteRecibir = new(); // ✅ Ejemplo IP

        [ObservableProperty]
        private string _vistaActual = "Nombre";

        [ObservableProperty]
        private List<string> _opciones = new();

        [ObservableProperty]
        private Respuesta _respuesta = new();

        [ObservableProperty]
        private Pregunta _pregunta = new();

        [ObservableProperty]
        private int tiempoRestante = 15;

        private DispatcherTimer timer;

        [ObservableProperty]
        bool botones = false;

        [ObservableProperty]
        private bool _yaRespondio = false;

        [ObservableProperty]
        private string _mensajeEstado = "";

        public string IP { get; set; } = "0.0.0.0"; 

        public ClienteUDP ClienteUDP
        {
            get => cliente;
            private set => SetProperty(ref cliente, value);
        }

        public ClienteViewModel()
        {
            ClienteUDP = new();
            EnviarCommand = new RelayCommand(Enviar);
            ContinuarCommand = new RelayCommand(ContinuarAResponder);
            SeleccionarOpcionCommand = new RelayCommand<string>(SeleccionarOpcion);

            clienteRecibir.RespuestaRecibida += ClienteRecibir_RespuestaRecibida;

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Tiempo;
        }

        public void InicializarConexion()
        {
            ClienteUDP.Servidor = IP;
        }


        private bool EsIpValida(string ip)
        {
            return System.Net.IPAddress.TryParse(ip, out _);
        }
        private async Task<bool> VerificarServidorAsync(string ip)
        {
            using var udpClient = new UdpClient();
            udpClient.Client.ReceiveTimeout = 1000; // 1 segundo
            try
            {
                var endpoint = new IPEndPoint(IPAddress.Parse(ip), 60001); // puerto del servidor
                byte[] datos = Encoding.UTF8.GetBytes("PING");
                await udpClient.SendAsync(datos, datos.Length, endpoint);

                var result = await udpClient.ReceiveAsync(); // espera respuesta
                string respuesta = Encoding.UTF8.GetString(result.Buffer);
                return respuesta == "PONG";
            }
            catch
            {
                return false;
            }
        }

        private async void ContinuarAResponder()
        {
            if (!string.IsNullOrWhiteSpace(Respuesta.nombre) && EsIpValida(IP))
            {
                MensajeEstado = "Verificando conexión con el servidor...";

                bool conectado = await VerificarServidorAsync(IP);
                if (!conectado)
                {
                    MensajeEstado = "No se pudo establecer conexión con el servidor. Verifica la IP.";
                    return;
                }

                InicializarConexion(); // ClienteUDP.Servidor = IP;
                VistaActual = "Respuestas";
                Debug.WriteLine($"Nombre ingresado: {Respuesta.nombre}, IP: {IP}");
                MensajeEstado = $"Conectado al servidor {IP}";
            }
            else
            {
                MensajeEstado = "Por favor, ingresa una IP válida.";
            }
        }


        //private void ContinuarAResponder()
        //{
        //    if (!string.IsNullOrWhiteSpace(Respuesta.nombre) && !string.IsNullOrWhiteSpace(IP))
        //    {
        //        InicializarConexion();
        //        VistaActual = "Respuestas";
        //        Debug.WriteLine($"Nombre ingresado: {Respuesta.nombre}, IP: {IP}");
        //    }
        //    else
        //    {
        //        MensajeEstado = "Por favor, ingresa la IP.";
        //    }
        //}

        private void ClienteRecibir_RespuestaRecibida(Pregunta obj)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (obj != null)
                {
                    Pregunta = obj;

                    if (obj.Respuesta == "TIEMPO_FEEDBACK")
                    {
                        timer.Stop();
                        Botones = false;

                        if (YaRespondio)
                        {
                            bool esCorrecta = obj.Enunciado.Contains(Respuesta.respuesta);
                            MensajeEstado = esCorrecta ?
                                "¡Correcto! 🎉" :
                                $"Incorrecto. {obj.Enunciado}";
                        }
                        else
                        {
                            MensajeEstado = $"No respondiste. {obj.Enunciado}";
                        }
                        return;
                    }

                    tiempoRestante = 15;
                    if (timer.IsEnabled)
                        timer.Stop();

                    timer.Start();
                    Botones = false;
                    YaRespondio = false;
                    MensajeEstado = "Tiempo para leer la pregunta (5 segundos)...";
                    Respuesta.respuesta = "";
                }
            });
        }

        private void Tiempo(object? sender, EventArgs e)
        {
            if (TiempoRestante > 0)
            {
                TiempoRestante--;
                if (TiempoRestante == 10)
                {
                    Botones = true;
                    MensajeEstado = "¡Puedes responder ahora! Tienes 10 segundos para contestar.";
                }
            }
            else
            {
                timer.Stop();
                Botones = false;
                if (!YaRespondio)
                {
                    MensajeEstado = "¡Se acabó el tiempo!";
                }
            }
        }

        public ICommand EnviarCommand { get; }
        public ICommand ContinuarCommand { get; }
        public ICommand SeleccionarOpcionCommand { get; }

        private void Enviar()
        {
            if (!YaRespondio && Botones && !string.IsNullOrEmpty(Respuesta.respuesta))
            {
                ClienteUDP.Enviar(Respuesta);
                YaRespondio = true;
                Botones = false;
                MensajeEstado = "¡Respuesta enviada correctamente!";
            }
        }

        private void SeleccionarOpcion(string opcion)
        {
            if (!YaRespondio && Botones)
            {
                Respuesta.respuesta = opcion;
                OnPropertyChanged(nameof(Respuesta));
            }
        }
    }
}

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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Cliente.ViewModels
{
    public partial class ClienteViewModel : ObservableObject
    {
        private readonly ClienteUDP cliente = new();
        Recibir clienteRecibir = new();

        [ObservableProperty]
        private string _vistaActual = "Nombre";

        [ObservableProperty]
        private List<string> _opciones = new();

        [ObservableProperty]
        private Respuesta _Respuesta = new();

        [ObservableProperty]
        private Pregunta _Pregunta = new();

        [ObservableProperty]
        private int tiempoRestante = 15;

        private DispatcherTimer timer;

        [ObservableProperty]
        bool botones = false;

        [ObservableProperty]
        private bool _yaRespondio = false;

        [ObservableProperty]
        private string _mensajeEstado = "";

        public ClienteViewModel()
        {
            EnviarCommand = new RelayCommand(Enviar);
            ContinuarCommand = new RelayCommand(ContinuarAResponder);
            SeleccionarOpcionCommand = new RelayCommand<string>(SeleccionarOpcion);
            clienteRecibir.RespuestaRecibida += ClienteRecibir_RespuestaRecibida;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Tiempo;
        }

        private void ContinuarAResponder()
        {
            Debug.WriteLine($"Nombre ingresado: {Respuesta.nombre}");
            if (!string.IsNullOrWhiteSpace(Respuesta.nombre))
            {
                Debug.WriteLine("Cambiando a vista de respuestas");
                VistaActual = "Respuestas";
            }
        }

        private void ClienteRecibir_RespuestaRecibida(Pregunta obj)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (obj != null)
                {
                    Pregunta = obj;
                    
                    // Verificar si es tiempo de feedback
                    if (obj.Respuesta == "TIEMPO_FEEDBACK")
                    {
                        timer.Stop();
                        Botones = false;
                        
                        // Si el usuario respondió, mostrar si fue correcta o incorrecta
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

                    // Si no es feedback, es una nueva pregunta
                    tiempoRestante = 15;
                    if (timer.IsEnabled)
                    {
                        timer.Stop();
                    }
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
                if (TiempoRestante == 10)  // Cuando han pasado 5 segundos (quedan 10 segundos)
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
                cliente.Enviar(Respuesta);
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

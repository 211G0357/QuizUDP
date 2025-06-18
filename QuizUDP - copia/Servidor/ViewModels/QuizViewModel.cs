using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servidor.Models;
using Servidor.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows;
using System.IO;

namespace Servidor.ViewModels
{
    public enum Vistas { Preguntas, Resultados }

    public partial class QuizViewModel : ObservableObject
    {
        public ObservableCollection<Alumno> Alumnos { get; set; } = new();
        public List<Pregunta> Preguntas { get; set; } = new();
        private int preguntaActual = 0;
        private HashSet<string> respuestasActuales = new();

        [ObservableProperty]
        private Vistas vista = Vistas.Preguntas;

        [ObservableProperty]
        private Pregunta _pregunta = new();

        [ObservableProperty]
        private int tiempoRestante;

        private DispatcherTimer timer;

        public ICommand IniciarCommand { get; }
        public ICommand RegresarCommand { get; }

        ServidorUDP server = new();
        private readonly Enviar serverEnviar = new();

        public QuizViewModel()
        {
            CargarPreguntas();
            IniciarCommand = new RelayCommand(IniciarQuiz);
            RegresarCommand = new RelayCommand(Regresar);
            server.RespuestaRecibida += Server_RespuestaRecibida;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Tiempo;
        }

        private void Regresar()
        {
            timer.Stop();
            Vista = Vistas.Preguntas;
        }

        private void Tiempo(object? sender, EventArgs e)
        {
            if (TiempoRestante > 0)
            {
                TiempoRestante--;
            }
            else
            {
                if (preguntaActual < Preguntas.Count - 1)
                {
                    Pregunta Pre = new Pregunta
                    {
                        Enunciado = "Respuesta Correcta: " + Pregunta.Respuesta,
                        Opciones = new List<string>(),
                        Respuesta = "TIEMPO_FEEDBACK" 
                    };
                    serverEnviar.EnviarPregunta(Pre);
                    
                    Task.Delay(3000).ContinueWith(_ =>
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            if (preguntaActual < Preguntas.Count - 1)
                            {
                                preguntaActual++;
                                respuestasActuales.Clear();
                                Pregunta = Preguntas[preguntaActual];
                                Pregunta NuevaPre = new Pregunta
                                {
                                    Enunciado = Pregunta.Enunciado,
                                    Opciones = Pregunta.Opciones,
                                    Respuesta = ""
                                };
                                serverEnviar.EnviarPregunta(NuevaPre);
                                TiempoRestante = 15;
                            }
                        });
                    });
                }
                else
                {
                    Pregunta Pre = new Pregunta
                    {
                        Enunciado = "Respuesta Correcta: " + Pregunta.Respuesta,
                        Opciones = new List<string>(),
                        Respuesta = "TIEMPO_FEEDBACK"
                    };
                    serverEnviar.EnviarPregunta(Pre);

                    Task.Delay(3000).ContinueWith(_ =>
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            timer.Stop();
                            Vista = Vistas.Resultados;
                        });
                    });
                }
            }
        }

        private void CargarPreguntas()
        {
            try
            {
                string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preguntas.json");
                string json = File.ReadAllText(ruta);
                Preguntas = JsonSerializer.Deserialize<List<Pregunta>>(json) ?? new List<Pregunta>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar las preguntas: {ex.Message}");
            }
        }

        private void IniciarQuiz()
        {
            if (Preguntas == null || Preguntas.Count == 0)
            {
                MessageBox.Show("No hay preguntas cargadas", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            preguntaActual = 0;
            Alumnos.Clear();
            respuestasActuales.Clear();
            Pregunta = Preguntas[preguntaActual];
            Pregunta Pre = new Pregunta
            {
                Enunciado = Pregunta.Enunciado,
                Opciones = Pregunta.Opciones,
                Respuesta = ""
            };
            serverEnviar.EnviarPregunta(Pre);
            TiempoRestante = 15;
            timer.Start();
        }

        private void Server_RespuestaRecibida(string nombre, string respuesta, string endpointId)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (!respuestasActuales.Contains(endpointId) && TiempoRestante <= 10 && TiempoRestante > 0)
                {
                    respuestasActuales.Add(endpointId);
                    var alumno = Alumnos.FirstOrDefault(a => a.EndpointId == endpointId);

                    if (alumno == null)
                    {
                        alumno = new Alumno 
                        { 
                            Nombre = nombre,
                            EndpointId = endpointId
                        };
                        Alumnos.Add(alumno);
                    }

                    alumno.Respuestas.Add(respuesta);

                    if (Preguntas.Count > preguntaActual && respuesta == Pregunta.Respuesta)
                    {
                        alumno.Puntuacion++;
                    }
                }
            });
        }
    }
}

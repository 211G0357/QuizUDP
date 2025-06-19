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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Servidor.ViewModels
{
    public enum Vistas { Preguntas, Resultados, RespuestasParciales}

    public partial class QuizViewModel : ObservableObject
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ObservableCollection<Alumno> Alumnos { get; set; } = new();
        public ObservableCollection<ResultadoParcialItem> ResultadosParciales { get; set; } = new();
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
                timer.Stop();
                MostrarResultadosParciales();
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
            ResultadosParciales.Clear();
            respuestasActuales.Clear();
            Pregunta = Preguntas[preguntaActual];

            serverEnviar.EnviarPregunta(new Pregunta
            {
                Enunciado = Pregunta.Enunciado,
                Opciones = Pregunta.Opciones,
                Respuesta = ""
            });

            TiempoRestante = 15;
            Vista = Vistas.Preguntas;
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
        private void MostrarResultadosParciales()
        {
            // Detener el timer mientras mostramos resultados
            timer.Stop();

            // Procesar las respuestas con índice seguro
            var alumnosConRespuesta = Alumnos.Where(a => a.Respuestas.Count > preguntaActual).ToList();

            var resultados = new
            {
                PreguntaTexto = Pregunta.Enunciado,
                RespuestaCorrecta = Pregunta.Respuesta,
                Acertaron = alumnosConRespuesta.Where(a => a.Respuestas[preguntaActual] == Pregunta.Respuesta).ToList(),
                Fallaron = alumnosConRespuesta.Where(a => a.Respuestas[preguntaActual] != Pregunta.Respuesta).ToList(),
                NoRespondieron = Alumnos.Except(alumnosConRespuesta).ToList()
            };

            // Limpiar y construir la colección de resultados
            ResultadosParciales.Clear();

            // Añadir pregunta y respuesta correcta
            ResultadosParciales.Add(new ResultadoParcialItem
            {
                Titulo = "Pregunta",
                Contenido = resultados.PreguntaTexto
            });

            ResultadosParciales.Add(new ResultadoParcialItem
            {
                Titulo = "Respuesta correcta",
                Contenido = resultados.RespuestaCorrecta
            });

            // Añadir resultados de alumnos
            ResultadosParciales.Add(new ResultadoParcialItem
            {
                Titulo = "Alumnos que acertaron",
                Contenido = resultados.Acertaron.Any() ?
                    string.Join(", ", resultados.Acertaron.Select(a => a.Nombre)) :
                    "Ningún alumno acertó"
            });

            ResultadosParciales.Add(new ResultadoParcialItem
            {
                Titulo = "Alumnos que fallaron",
                Contenido = resultados.Fallaron.Any() ?
                    string.Join(", ", resultados.Fallaron.Select(a => a.Nombre)) :
                    "Ningún alumno falló"
            });

            ResultadosParciales.Add(new ResultadoParcialItem
            {
                Titulo = "Alumnos que no respondieron",
                Contenido = resultados.NoRespondieron.Any() ?
                    string.Join(", ", resultados.NoRespondieron.Select(a => a.Nombre)) :
                    "Todos los alumnos respondieron"
            });

            // Cambiar a la vista de resultados parciales
            Vista = Vistas.RespuestasParciales;
            OnPropertyChanged(nameof(ResultadosParciales)); // Notificar cambio

            // Configurar el temporizador para la siguiente pregunta (5 segundos)
            Task.Delay(5000).ContinueWith(_ =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (preguntaActual < Preguntas.Count - 1)
                    {
                        AvanzarASiguientePregunta();
                    }
                    else
                    {
                        Finalizar();
                    }
                });
            });
        }
        private void AvanzarASiguientePregunta()
        {
            preguntaActual++;
            respuestasActuales.Clear();
            Pregunta = Preguntas[preguntaActual];

            // Reiniciar el temporizador
            TiempoRestante = 15;
            OnPropertyChanged(nameof(TiempoRestante));

            // Enviar nueva pregunta
            serverEnviar.EnviarPregunta(new Pregunta
            {
                Enunciado = Pregunta.Enunciado,
                Opciones = Pregunta.Opciones,
                Respuesta = ""
            });

            // Cambiar a vista de pregunta
            Vista = Vistas.Preguntas;
            timer.Start();
        }
        private void SiguientePregunta()
        {
            preguntaActual++;
            respuestasActuales.Clear();
            Pregunta = Preguntas[preguntaActual];

            // Enviar nueva pregunta a los clientes
            serverEnviar.EnviarPregunta(new Pregunta
            {
                Enunciado = Pregunta.Enunciado,
                Opciones = Pregunta.Opciones,
                Respuesta = "" // No enviar la respuesta correcta
            });

            TiempoRestante = 15;
            Vista = Vistas.Preguntas; // Asumo que esta es la vista para preguntas activas
            timer.Start();
        }
        private void Finalizar()
        {
            try
            {
                // 1. Detener temporizadores
                timer.Stop();

                // 2. Calcular estadísticas finales
                CalcularEstadisticas();

                // 3. Preparar datos para la vista de resultados
                PrepararResultadosFinales();

                // 4. Reiniciar estado del quiz
                ReiniciarEstadoQuiz();

                // 5. Cambiar a vista de resultados
                Vista = Vistas.Resultados;

                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al finalizar el quiz: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalcularEstadisticas()
        {
            int totalPreguntas = Preguntas.Count;

            foreach (var alumno in Alumnos)
            {
                int aciertos = 0;
                for (int i = 0; i < Math.Min(totalPreguntas, alumno.Respuestas.Count); i++)
                {
                    if (alumno.Respuestas[i] == Preguntas[i].Respuesta)
                    {
                        aciertos++;
                    }
                }

                alumno.Puntuacion = aciertos;
                alumno.PorcentajeAciertos = totalPreguntas > 0 ?
                    (double)aciertos / totalPreguntas * 100 : 0;
            }
        }

        private void PrepararResultadosFinales()
        {
            // Ordenar alumnos por puntuación (y luego por nombre en caso de empate)
            var alumnosOrdenados = Alumnos
                .OrderByDescending(a => a.Puntuacion)
                .ThenBy(a => a.Nombre)
                .ToList();

            Alumnos.Clear();
            foreach (var alumno in alumnosOrdenados)
            {
                Alumnos.Add(alumno);
            }
        }

        private void ReiniciarEstadoQuiz()
        {
            preguntaActual = 0;
            respuestasActuales.Clear();
            TiempoRestante = 0;
            OnPropertyChanged(nameof(TiempoRestante));
        }

        



    }
}

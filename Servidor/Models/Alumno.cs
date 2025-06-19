using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servidor.Models
{
    public partial class Alumno : ObservableObject
    {
        [ObservableProperty]
        string nombre;

        [ObservableProperty]
        int puntuacion;

        [ObservableProperty]
        string endpointId;

        public ObservableCollection<string> Respuestas { get; set; } = new();

        public string NombreCompleto => string.IsNullOrEmpty(EndpointId) ?
            Nombre :
            $"{Nombre} [{EndpointId}]";

        public double PorcentajeAciertos { get; internal set; }

      
    }
}

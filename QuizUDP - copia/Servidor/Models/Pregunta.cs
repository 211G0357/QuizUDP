using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servidor.Models
{
    public class Pregunta
    {
        public string Enunciado { get; set; } = "";
        public List<string> Opciones { get; set; } = new();
        public string Respuesta { get; set; } = "";
    }
}

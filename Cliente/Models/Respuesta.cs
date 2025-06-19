using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Cliente.Models
{
    public class Respuesta
    {
        public string nombre { get; set; } = "";
        public string respuesta { get; set; } = "";
        public IPEndPoint? IpCliente { get; set; }  // Almacenará IP:Puerto
    }
}

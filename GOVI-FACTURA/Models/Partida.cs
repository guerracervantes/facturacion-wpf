using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GOVI_FACTURA.Models
{
    internal class Partida
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Importe => Cantidad * Precio;
    }
}

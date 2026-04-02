using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GOVI_FACTURA.Models
{
       public class Cliente
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string RFC { get; set; }
        public string Direccion { get; set; }
    }
}

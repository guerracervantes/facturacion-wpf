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
        public int EstadoId { get; set; }
        public int CiudadId { get; set; }
        public int CondicionVentaId { get; set; }

        public string Telefono { get; set; }

        public decimal LimiteCredito { get; set; }
        public decimal ImporteAutorizado { get; set; }

        public string Estado { get; set; }
        public string Ciudad { get; set; }
        public string CondicionVenta { get; set; }
        // segunda pestaña
        public string UsoCFDI { get; set; }
        public string RegimenFiscal { get; set; }
        public string RazonSocial { get; set; }

        public int VendedorId { get; set; }
        public string VendedorNombre { get; set; }

        public string Consignado1 { get; set; }
        public string Consignado2 { get; set; }
        public string UsoCFDI2 { get; set; }
        public int Tarjeta { get; set; }
        public string Colonia { get; set; }
        public string CodigoPostal { get; set; }
        public string Email { get; set; }
    }
}

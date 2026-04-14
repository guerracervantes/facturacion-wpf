using System.Collections.Generic;
using System.Data;
using System.Windows.Media.Media3D;
using GOVI_FACTURA.Models;
using Microsoft.Data.SqlClient;

namespace GOVI_FACTURA.Services
{

    public class ClienteService
    {
        //variables globales 

        private DbService db = new DbService();
        public int CondicionVentaId { get; set; }
        public string CondicionVenta { get; set; }


        // 🔍 BUSCAR CLIENTES (PARA EL MODAL)
        public List<Cliente> BuscarClientes(string filtro)
        {
            List<Cliente> lista = new List<Cliente>();

            using (var conn = db.GetConnection())
            {
                conn.Open();

                string query = @"
                SELECT iClienteId, strNombre, strRFC
                FROM Cliente
                WHERE 
                    strNombre LIKE @filtro OR
                    strRFC LIKE @filtro OR
                    CAST(iClienteId AS VARCHAR) LIKE @filtro";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@filtro", "%" + filtro + "%");

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Cliente
                            {
                                Id = (int)reader["iClienteId"],
                                Nombre = reader["strNombre"].ToString(),
                                RFC = reader["strRFC"].ToString()
                            });
                        }
                    }
                }
            }

            return lista;
        }

        // 🔎 BUSCAR CLIENTE POR ID
        public Cliente ObtenerCliente(int id)
        {
            using (var conn = db.GetConnection())
            {
                conn.Open();

                using (var cmd = new SqlCommand("clienteBusca_Contado_REG_FISCAL_RAZON_SOCIAL", conn))
                {
                    int almacenId = ObtenerAlmacenId();
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@iCliente", id);
                    cmd.Parameters.AddWithValue("@iAlmacenId", almacenId);
                    cmd.Parameters.AddWithValue("@STRNOMBRE", "");

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var cliente = new Cliente
                            {
                                Id = (int)reader["iClienteId"],
                                Nombre = reader["strNombre"].ToString(),
                                RFC = reader["strRFC"].ToString(),
                                Direccion = reader["strDireccion"].ToString(),

                                EstadoId = Convert.ToInt32(reader["iEstadoId"]),
                                CiudadId = Convert.ToInt32(reader["iCiudadId"]),
                                CondicionVentaId = Convert.ToInt32(reader["iCondicionPagoId"]),

                                Telefono = reader["strTelefonos"]?.ToString(),

                                LimiteCredito = reader["fLimiteCredito"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["fLimiteCredito"]) : 0,

                                ImporteAutorizado = reader["fImporteAutorizado"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["fImporteAutorizado"]) : 0,
                               
                                UsoCFDI = reader["iUso_CFDI_33"]?.ToString(),
                                RegimenFiscal = reader["iRegimenid"]?.ToString(),
                                RazonSocial = reader["strRazonSocial"]?.ToString(),
                                VendedorId = Convert.ToInt32(reader["iVendedorId"])
                            };

                            // 🔥 AQUÍ HACEMOS LO QUE VB HACÍA
                            cliente.Estado = ObtenerNombreEstado(cliente.EstadoId);
                            cliente.Ciudad = ObtenerNombreCiudad(cliente.CiudadId);
                            cliente.CondicionVenta = ObtenerCondicionVenta(cliente.CondicionVentaId);
                            cliente.VendedorNombre = ObtenerNombreVendedor(cliente.VendedorId);
                            cliente.UsoCFDI2 = ObtenerUsoCFDIDescripcion(cliente.UsoCFDI);
                            cliente.RegimenFiscal = ObtenerRegimenDescripcion(cliente.RegimenFiscal);
                           

                            return cliente;
                        }
                    }
                }
            }

            return null;
        }

        private int ObtenerAlmacenId()
        {
            using (var conn = db.GetConnection())
            {
                conn.Open();

                string query = "SELECT TOP 1 iAlmacenId FROM ALMACEN_FACTURACION";

                using (var cmd = new SqlCommand(query, conn))
                {
                    var result = cmd.ExecuteScalar();

                    if (result != null)
                        return Convert.ToInt32(result);
                }
            }

            return 1; // fallback por si algo falla
        }

        public string ObtenerCondicionVentaAlmacenTexto()
        {
            using (var conn = db.GetConnection())
            {
                conn.Open();

                string query = @"
        SELECT TOP 1 cv.strDescripcion
        FROM ALMACEN a
        JOIN CONDICIONVENTA cv 
            ON a.iCondicionVenta = cv.iCondicionVentaId";

                using (var cmd = new SqlCommand(query, conn))
                {
                    var result = cmd.ExecuteScalar();
                    return result?.ToString().ToUpper() ?? "";
                }
            }
        }
        public bool EsClienteContado(int clienteId)
        {
            using (var conn = db.GetConnection())
            {
                conn.Open();

                int almacenId = ObtenerAlmacenId();

                string query = @"     SELECT strValor
        FROM ALMACEN_PARAMETRO
        WHERE iParametroId = 1 AND iAlmacenId = @almacenId";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@almacenId", almacenId);

                    var result = cmd.ExecuteScalar();

                    if (result != null && int.TryParse(result.ToString(), out int clienteContadoId))
                    {
                        return clienteId == clienteContadoId;
                    }
                }
            }

            return false;
        }
        public string ObtenerNombreEstado(int estadoId)
        {
            using (var conn = db.GetConnection())
            {
                conn.Open();

                string query = "SELECT strDescripcion FROM Estado WHERE iEstadoId = @id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", estadoId);

                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "";
                }
            }
        }

        public string ObtenerNombreCiudad(int ciudadId)
        {
            using (var conn = db.GetConnection())
            {
                conn.Open();

                string query = "SELECT strDescripcion FROM Ciudad WHERE iCiudadId = @id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", ciudadId);

                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "";
                }
            }
        }
        public string ObtenerCondicionVenta(int clienteId)
        {
            using (var conn = db.GetConnection())
            {
                conn.Open();

                using (var cmd = new SqlCommand("ClinteBuscaCondicionPago", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ilienteid", clienteId);

                    var result = cmd.ExecuteScalar();

                    return (result != null && result != DBNull.Value)
                        ? result.ToString()
                        : "SIN CONDICION";
                }
            }
        }

        public decimal ObtenerSaldo(int clienteId)
{
    using (var conn = db.GetConnection())
    {
        conn.Open();

        int almacenId = ObtenerAlmacenId();

        using (var cmd = new SqlCommand("clienteSaldos", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@iClienteId", clienteId);
            cmd.Parameters.AddWithValue("@iAlmacenId", almacenId);

            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return reader["SumaDeSaldoActual"] != DBNull.Value
                        ? Convert.ToDecimal(reader["SumaDeSaldoActual"])
                        : 0;
                }
            }
        }
    }

    // 🔥 SI NO HAY REGISTRO → SALDO = 0
    return 0;
}

        public string ObtenerNombreVendedor(int vendedorId)
        {
            using (var conn = db.GetConnection())
            {
                conn.Open();

                string query = "SELECT strNombre FROM VENDEDOR WHERE iVendedorId = @id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", vendedorId);

                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "";
                }
            }
        }

        public string ObtenerUsoCFDIDescripcion(string id)
        {
            using (var conn = db.GetConnection())
            {
                conn.Open();

                string query = @"
        SELECT strUsoCFDI + ' - ' + strDescripcion
        FROM USO_CFDI
        WHERE iusocdfid = @id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "";
                }
            }
        }

        public string ObtenerRegimenDescripcion(string id)
        {
            using (var conn = db.GetConnection())
            {
                conn.Open();

                string query = @"
        SELECT strRegimen + ' - ' + strDescripcion
        FROM REGIMEN_FISCAL
        WHERE iRegimenid = @id";   // 👈 CAMBIO AQUI

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "";
                }
            }
        }
        public decimal ObtenerSaldoMonedero(int clienteId, int tarjeta)
        {
            using (var conn = db.GetConnection())
            {
                conn.Open();

                using (var cmd = new SqlCommand("GOVI_PREMIOS_BUSCA_SALDO_BONO", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    int almacenId = ObtenerAlmacenId();

                    cmd.Parameters.AddWithValue("@iAlmacenid", almacenId);
                    cmd.Parameters.AddWithValue("@iClienteid", clienteId);
                    cmd.Parameters.AddWithValue("@iTarjeta", tarjeta);

                    var result = cmd.ExecuteScalar();

                    return result != null && result != DBNull.Value
                        ? Convert.ToDecimal(result)
                        : 0;
                }
            }
        }
        // saldo de govi premios
        public decimal ObtenerSaldoDisponible(int clienteId, int tarjeta)
        {
            using (var conn = db.GetConnection())
            {
                conn.Open();

                using (var cmd = new SqlCommand("GOVI_PREMIOS_BUSCA_PROMOCIONALES_22", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    int almacenId = ObtenerAlmacenId();

                    cmd.Parameters.AddWithValue("@iAlmacenid", almacenId);
                    cmd.Parameters.AddWithValue("@iClienteid", clienteId);
                    cmd.Parameters.AddWithValue("@iTarjeta", tarjeta);

                    var result = cmd.ExecuteScalar();

                    return result != null && result != DBNull.Value
                        ? Convert.ToDecimal(result)
                        : 0;
                }
            }
        }
        // ahor la tarjeta 
        public int ObtenerTarjeta(int clienteId)
        {
            using (var conn = db.GetConnection())
            {
                conn.Open();

                string query = "SELECT iTarjeta FROM CLIENTE WHERE iClienteId = @id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", clienteId);

                    var result = cmd.ExecuteScalar();

                    return result != null && result != DBNull.Value
                        ? Convert.ToInt32(result)
                        : 0;
                }
            }
        }

        //siempre deja esos dos }
    }
}
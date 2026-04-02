using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using GOVI_FACTURA.Models;

namespace GOVI_FACTURA.Services
{

    public class ClienteService
    {
        //variables globales 

        private DbService db = new DbService();
       

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

                string query = @"SELECT 
                                    iClienteId,
                                    strNombre,
                                    strRFC,
                                    strDireccion
                                 FROM Cliente
                                 WHERE iClienteId = @id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Cliente
                            {
                                Id = (int)reader["iClienteId"],
                                Nombre = reader["strNombre"].ToString(),
                                RFC = reader["strRFC"].ToString(),
                                Direccion = reader["strDireccion"].ToString()
                            };
                        }
                    }
                }
            }

            return null;
        }

       
        

        //siempre deja esos dos }
    }
}
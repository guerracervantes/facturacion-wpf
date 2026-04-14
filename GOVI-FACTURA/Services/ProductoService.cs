using System;
using System.Data;
using GOVI_FACTURA.Models;
using Microsoft.Data.SqlClient;

namespace GOVI_FACTURA.Services
{
    public class ProductoService
    {
        private DbService db = new DbService();

        public Producto BuscarProducto(string codigo)
        {
            using (var conn = db.GetConnection())
            {
                conn.Open();

                using (var cmd = new SqlCommand("productoBusca", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@strProductoId", codigo);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Producto
                            {
                                Id = reader["strProductoId"].ToString(),
                                Descripcion = reader["strDescripcion"].ToString()
                            };
                        }
                    }
                }
            }

            return null;
        }
    }
}
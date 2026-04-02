using Microsoft.Data.SqlClient;

namespace GOVI_FACTURA.Services
{
    public class ClienteEspecialService
    {
        private DbService db = new DbService();

        private int EjecutarSP(string nombreSP)
        {
            using (var conn = db.GetConnection())
            {
                conn.Open();

                using (var cmd = new SqlCommand(nombreSP, conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    var result = cmd.ExecuteScalar();

                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }

        // case para combos
        public class ComboItem
        {
            public int Id { get; set; }
            public string Descripcion { get; set; }
        }

        // METODO PARA CBM
        private List<ComboItem> EjecutarCatalogo(string sp)
        {
            var lista = new List<ComboItem>();

            using (var conn = db.GetConnection())
            {
                conn.Open();

                using (var cmd = new SqlCommand(sp, conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new ComboItem
                            {
                                Id = Convert.ToInt32(dr[0]),
                                Descripcion = dr[1].ToString()
                            });
                        }
                    }
                }
            }

            return lista;
        }
        // ✅ ESTE SÍ FUNCIONA
        public List<ComboItem> ObtenerFormaPago()
        {
            return EjecutarCatalogo("formaPagoCatalogo");
        }

        public List<ComboItem> ObtenerUsoCFDI()
        {
            return EjecutarCatalogo("USOSCFDI_Catalogo");
        }
        //

        public int ObtenerMSI()
        {
            return EjecutarSP("Cliente_MSI_BUSCA");
        }

        public int ObtenerConfort()
        {
            return EjecutarSP("Cliente_MSI_BUSCA_COMFORT");
        }

        public int ObtenerContado2()
        {
            return EjecutarSP("Cliente_MSI_BUSCA_CONTADO");
        }

        public int ObtenerBuenFin()
        {
            return EjecutarSP("Cliente_buen_fin_busca");
        }

        public int ObtenerClienteBuenFinBase()
        {
            using (var conn = db.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand("SELECT iClienteid FROM CLIENTE_BUEN_FIN", conn);

                var result = cmd.ExecuteScalar();

                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

    }
}
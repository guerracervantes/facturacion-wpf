using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data.SqlClient;

namespace GOVI_FACTURA
{
    public partial class ComprasFiliales : Window
    {
        int gAlmacen = 4;

        private string connectionString = "Data Source=192.168.1.195;Initial Catalog=chapultepec;User ID=servicios;Password=Pa$$w0rd; Encrypt=False; TrustServerCertificate=True;";

        public ComprasFiliales()
        {
            InitializeComponent();
            Loaded += Window_Loaded;
        }

        // =========================================
        // 🔥 LOAD
        // =========================================
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            btnGrabar.IsEnabled = false;

            txtFecha.Text = DateTime.Now.ToString("dd/MM/yyyy");

            CargarProveedores();
            CargarCedis();
        }

        // =========================================
        // 🔥 CAMBIO DE MODO (RADIOBUTTONS)
        // =========================================
        private void Modo_Checked(object sender, RoutedEventArgs e)
        {
            CargarProveedores(); // 🔥 cambia SP dinámicamente
        }

        // =========================================
        // 🔥 CARGAR PROVEEDORES (DINÁMICO)
        // =========================================
        private void CargarProveedores()
        {
            var lista = new List<dynamic>();

            string sp = "proveedorCatalogo_FILIALES";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(sp, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@iAlmacenId", gAlmacen);

                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    lista.Add(new
                    {
                        Id = Convert.ToInt32(dr["iProveedorId"]),
                        Nombre = dr["strNombre"].ToString()
                    });
                }
            }

            cmbProveedor.ItemsSource = lista;
            cmbProveedor.DisplayMemberPath = "Nombre";
            cmbProveedor.SelectedValuePath = "Id";
        }

        // =========================================
        // 🔥 CARGAR CEDIS
        // =========================================
        private void CargarCedis()
        {
            var lista = new List<dynamic>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("almacenCatalogoCEDIS", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    lista.Add(new
                    {
                        Id = Convert.ToInt32(dr["iAlmacenId"]),
                        Nombre = dr["strDescripcion"].ToString()
                    });
                }
            }

            cmbCedis.ItemsSource = lista;
            cmbCedis.DisplayMemberPath = "Nombre";
            cmbCedis.SelectedValuePath = "Id";
        }

        // =========================================
        // 🔥 CARGAR CONEXION
        // =========================================
        private string GetConnectionStringFromGlobal(int almacenId)
        {
            string conexionGlobal = "Data Source=192.168.1.206;Initial Catalog=INTER_GOVI_SAP;User ID=servicios;Password=Pa$$w0rd;Encrypt=False;TrustServerCertificate=True;";

            using (SqlConnection conn = new SqlConnection(conexionGlobal))
            {
                SqlCommand cmd = new SqlCommand(@"
            SELECT IP, BaseDatos, Usuario, Password 
            FROM ServidoresRegistrados 
            WHERE iAlmacenId = @almacen", conn);

                cmd.Parameters.AddWithValue("@almacen", almacenId);

                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    string ip = dr["IP"].ToString();
                    string db = dr["BaseDatos"].ToString();
                    string user = dr["Usuario"].ToString();
                    string pass = dr["Password"].ToString();

                    // 🔥 aquí armas el connection string dinámico
                    return $"Data Source={ip};Initial Catalog={db};User ID={user};Password={pass};Encrypt=False;TrustServerCertificate=True;";
                }
                else
                {
                    throw new Exception("No existe configuración para el almacén seleccionado");
                }
            }
        }

        // =========================================
        // 🔥 BOTÓN BUSCAR
        // =========================================
        private void btnBuscar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbCedis.SelectedItem == null && rbSAP.IsChecked != true)
                {
                    MessageBox.Show("Seleccione el CEDIS");
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtCliente.Text))
                {
                    MessageBox.Show("Se requiere el número de cliente");
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtFactura.Text))
                {
                    MessageBox.Show("Ingrese el número de documento");
                    return;
                }

                btnGrabar.IsEnabled = true;
                gAlmacen = ObtenerAlmacen();

                int nuevoFolio = ObtenerUltimoFolio() + 1;
                txtOrdenCompra.Text = nuevoFolio.ToString();
                
                BuscarResurtidoAutomatico();
                CalcularTotales();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // =========================================
        // 🔥 RESURTIDO AUTOMÁTICO 
        // =========================================
        private void BuscarResurtidoAutomatico()
        {
            dgProductos.ItemsSource = null;
            dgProductos.Items.Clear();

            var lista = new List<tablaResurtido>();

            if (rbSAP.IsChecked == true)
                lista = ObtenerResurtido_HANA();

            else if (rbSucursales.IsChecked == true)
                lista = ObtenerResurtido_Sucursal();

            else if (rbAutomotive.IsChecked == true)
                lista = ObtenerResurtido_Automotive();

            dgProductos.ItemsSource = lista;
            dgProductos.Items.Refresh();
        }

        // =========================================
        // 🔥 HANA (DOBLE SP)
        // =========================================
        private List<tablaResurtido> ObtenerResurtido_HANA()
        {
            var lista = new List<tablaResurtido>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                SqlCommand insert = new SqlCommand(
                    "surtidoGeneraCapturaAutomatica_RFC_factura_con_cliente_hana_CTE_SAP",
                    conn);

                insert.CommandType = CommandType.StoredProcedure;
                insert.Parameters.AddWithValue("@ifolio", txtFactura.Text);
                insert.Parameters.AddWithValue("@iClienteId", txtCliente.Text);
                insert.ExecuteNonQuery();

                SqlCommand select = new SqlCommand(
                    "SurtidoGeneraCapturaAutomatica_factura_con_cliente_hana_inserta_RFC",
                    conn);

                select.CommandType = CommandType.StoredProcedure;
                select.Parameters.AddWithValue("@ifolio", Convert.ToInt32(txtFactura.Text));
                select.Parameters.AddWithValue("@iClienteid", Convert.ToInt32(txtCliente.Text));
                SqlDataReader dr = select.ExecuteReader();

                while (dr.Read())
                {
                    lista.Add(new tablaResurtido
                    {
                        NombreProducto = dr["DESCPROD"].ToString(), 
                        Marca = dr["DESCMCA"].ToString(),          

                        Cantidad = Convert.ToInt32(dr["iCantidad"]), 
                        Precio = Convert.ToDecimal(dr["fPrecio"]),   

                        // en tu SP ya viene subtotal
                        Importe = Convert.ToDecimal(dr["fSubTotal"]), 

                        // estos NO existen en este SP → déjalos en 0
                        Descuento = 0,
                        TotalSinDescuento = Convert.ToDecimal(dr["fSubTotal"]),
                        Impuesto = 0
                    });
                }
            }

            return lista;
        }

        // =========================================
        // 🔥 SUCURSAL (1 SP)
        // =========================================
        private List<tablaResurtido> ObtenerResurtido_Sucursal()
        {
            var lista = new List<tablaResurtido>();

            string sp = "";

            if (ObtenerTipoDocumento() == "FACTURA")
                sp = "SurtidoGeneraCapturaAutomatica_factura_con_cliente_hana_DESCUENTO";

            else if (ObtenerTipoDocumento() == "REMISION")
                sp = "SurtidoGeneraCapturaAutomaticaRemision_FILIALES_con_cliente_DESCUENTO";

            if (string.IsNullOrEmpty(sp))
            {
                MessageBox.Show("Seleccione tipo de documento");
                return lista;
            }

            int almacenSeleccionado = (int)cmbCedis.SelectedValue;

            string dynamicConnection = GetConnectionStringFromGlobal(almacenSeleccionado);

            using (SqlConnection conn = new SqlConnection(dynamicConnection))
            {
                SqlCommand cmd = new SqlCommand(sp, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@iFolio", SqlDbType.Int).Value = Convert.ToInt32(txtFactura.Text);
                cmd.Parameters.Add("@iClienteId", SqlDbType.Int).Value = Convert.ToInt32(txtCliente.Text);

                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                if (!dr.HasRows)
                {
                    MessageBox.Show($"❌ Sin datos en almacén {almacenSeleccionado}");
                    return lista;
                }

                while (dr.Read())
                {
                    lista.Add(new tablaResurtido
                    {
                        ProductoId = dr["strProductoId"].ToString(), // 🔥 CLAVE REAL
                        NombreProducto = dr["DESCPROD"].ToString(),  // visual

                        MarcaId = Convert.ToInt32(dr["MARCAID"]),
                        Marca = dr["DESCMCA"].ToString(),

                        Cantidad = Convert.ToInt32(dr["iCantidad"]),
                        Precio = Convert.ToDecimal(dr["fPrecio"]),
                        Importe = Convert.ToDecimal(dr["fSubTotal"]),

                        Descuento = dr["DESCUENTO"] != DBNull.Value ? Convert.ToDecimal(dr["DESCUENTO"]) : 0,
                        TotalSinDescuento = dr["TOTALSINDESC"] != DBNull.Value ? Convert.ToDecimal(dr["TOTALSINDESC"]) : 0,
                        Impuesto = dr["IMPUESTO"] != DBNull.Value ? Convert.ToDecimal(dr["IMPUESTO"]) : 0
                    });
                }
            }
            return lista;
        }

        // =========================================
        // 🔥 AUTOMOTIVE (DOBLE SP)
        // =========================================
        private List<tablaResurtido> ObtenerResurtido_Automotive()
        {
            var lista = new List<tablaResurtido>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                SqlCommand insert = new SqlCommand(
                    "SurtidoGeneraCapturaAutomatica_RFC_factura_con_cliente_hana_CTE_SAP_AUTOMOTIVE_2",
                    conn);

                insert.CommandType = CommandType.StoredProcedure;
                insert.Parameters.AddWithValue("@ifolio", txtFactura.Text);
                insert.Parameters.AddWithValue("@iClienteId", txtCliente.Text);
                insert.ExecuteNonQuery();

                SqlCommand select = new SqlCommand(
                    "SurtidoGeneraCapturaAutomatica_factura_con_cliente_hana_inserta_RFC",
                    conn);

                select.CommandType = CommandType.StoredProcedure;
                select.Parameters.AddWithValue("@ifolio", Convert.ToInt32(txtFactura.Text));
                select.Parameters.AddWithValue("@iClienteid", Convert.ToInt32(txtCliente.Text));
                SqlDataReader dr = select.ExecuteReader();

                while (dr.Read())
                {
                    lista.Add(new tablaResurtido
                    {
                        NombreProducto = dr["DESCPROD"].ToString(), 
                        Marca = dr["DESCMCA"].ToString(),        

                        Cantidad = Convert.ToInt32(dr["iCantidad"]),
                        Precio = Convert.ToDecimal(dr["fPrecio"]),   

                        // en tu SP ya viene subtotal
                        Importe = Convert.ToDecimal(dr["fSubTotal"]), 

                        // estos NO existen en este SP → déjalos en 0
                        Descuento = 0,
                        TotalSinDescuento = Convert.ToDecimal(dr["fSubTotal"]),
                        Impuesto = 0
                    });
                }
            }

            return lista;
        }

        // =========================================
        // 🔥 FOLIO (SIMULADO)
        // =========================================
        private int ObtenerUltimoFolio()
        {
            int folio = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("ordencompraUltimoFolio", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@iAlmacenId", 4);

                conn.Open();
                var result = cmd.ExecuteScalar();

                if (result != null)
                    folio = Convert.ToInt32(result);
            }

            return folio;
        }

        // =========================================
        // 🔥 OBTENER TIPO DOCUMENTO
        // =========================================
        private string ObtenerTipoDocumento()
        {
            if (rbFactura.IsChecked == true)
                return "FACTURA";

            if (rbRemision.IsChecked == true)
                return "REMISION";

            return "";
        }

        // =========================================
        // 🔥 OBTENER ALMACÉN
        // =========================================
        private int ObtenerAlmacen()
        {
            if (rbSAP.IsChecked == true)
                return 1;

            if (cmbCedis.SelectedValue != null)
                return (int)cmbCedis.SelectedValue;

            return gAlmacen;
        }
        // =========================================
        // 🔥 TOTALES
        // =========================================
        private void CalcularTotales()
        {
            var lista = dgProductos.ItemsSource as List<tablaResurtido>;
            if (lista == null) return;

            decimal subtotal = lista.Sum(x => x.Importe);
            decimal descuento = lista.Sum(x => x.Descuento);
            decimal totalSinDesc = lista.Sum(x => x.TotalSinDescuento);
            decimal iva = lista.Sum(x => x.Impuesto);

            decimal total = totalSinDesc + iva;

            txtSubTotal.Text = subtotal.ToString("N2");
            txtDescuento.Text = descuento.ToString("N2");
            txtSubTotalSinDesc.Text = totalSinDesc.ToString("N2");
            txtIVA.Text = iva.ToString("N2");
            txtTotal.Text = total.ToString("N2");
        }
        private void btnGrabar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lista = dgProductos.ItemsSource as List<tablaResurtido>;

                if (lista == null || lista.Count == 0)
                {
                    MessageBox.Show("No hay productos para guardar");
                    return;
                }

                 MessageBox.Show("Proveedor seleccionado: " + cmbProveedor.SelectedValue);

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // =========================================
                    // 🔥 1. GUARDAR ENCABEZADO
                    // =========================================
                    SqlCommand cmd = new SqlCommand(
                        "OrdencompraInsertaAUTORIZACION_FACPROV_RETENCION_DESCUENTO_V1",
                        conn);

                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@iAlmacenId", 4);
                    cmd.Parameters.AddWithValue("@iOrdenId", Convert.ToInt32(txtOrdenCompra.Text));
                    cmd.Parameters.AddWithValue("@iProveedorId", cmbProveedor.SelectedValue);
                    cmd.Parameters.AddWithValue("@iRequisicion", 0);

                    cmd.Parameters.AddWithValue("@fSubTotal", Convert.ToDecimal(txtSubTotal.Text));
                    cmd.Parameters.AddWithValue("@fImpuesto", Convert.ToDecimal(txtIVA.Text));
                    cmd.Parameters.AddWithValue("@fTotal", Convert.ToDecimal(txtTotal.Text));

                    cmd.Parameters.AddWithValue("@dfechallegada", DateTime.Now);
                    cmd.Parameters.AddWithValue("@strAutorizacion", "AUTO");

                    cmd.Parameters.AddWithValue("@iFacturaProveedorid", Convert.ToDouble(txtFactura.Text));
                    cmd.Parameters.AddWithValue("@fRetencion", 0);
                    cmd.Parameters.AddWithValue("@iStatus", 1);

                    cmd.Parameters.AddWithValue("@fDescuento", Convert.ToDecimal(txtDescuento.Text));
                    cmd.Parameters.AddWithValue("@fsubtotalsinDescuento", Convert.ToDecimal(txtSubTotalSinDesc.Text));

                    // 🔥 ESTE ES CLAVE (CEDIS seleccionado)
                    cmd.Parameters.AddWithValue("@iAlmacenCliente", cmbCedis.SelectedValue);

                    cmd.Parameters.AddWithValue("@iClienteid", txtCliente.Text);

                    cmd.ExecuteNonQuery();

                    // =========================================
                    // 🔥 2. GUARDAR DETALLE
                    // =========================================
                    foreach (var item in lista)
                    {
                        SqlCommand det = new SqlCommand(
                            "OrdencompraDetalleInserta_DESCUENTO",
                            conn);

                        det.CommandType = CommandType.StoredProcedure;

                        det.Parameters.AddWithValue("@iAlmacenId", 4);
                        det.Parameters.AddWithValue("@strProductoId", item.ProductoId);
                        det.Parameters.AddWithValue("@iProveedorId", cmbProveedor.SelectedValue);
                        det.Parameters.AddWithValue("@iMarcaId", item.MarcaId);
                        det.Parameters.AddWithValue("@iOrdenId", Convert.ToInt32(txtOrdenCompra.Text));

                        det.Parameters.AddWithValue("@iCantidad", item.Cantidad);
                        det.Parameters.AddWithValue("@iCantidadSurtida", 0);

                        det.Parameters.AddWithValue("@fPrecio", item.Precio);
                        det.Parameters.AddWithValue("@fImporte", item.Importe);

                        det.Parameters.AddWithValue("@iOrdenCaptura", 1);
                        det.Parameters.AddWithValue("@strlocalizacionId", "");

                        det.Parameters.AddWithValue("@iventaglobal", 0);
                        det.Parameters.AddWithValue("@iVenta", 0);
                        det.Parameters.AddWithValue("@iInventario", 0);

                        // 🔥 MESES
                        for (int i = 1; i <= 12; i++)
                            det.Parameters.AddWithValue("@iMES" + i, 0);

                        for (int i = 1; i <= 12; i++)
                            det.Parameters.AddWithValue("@iMESANT" + i, 0);

                        det.Parameters.AddWithValue("@descripcion_producto", item.NombreProducto);
                        det.Parameters.AddWithValue("@fprecionuevo", item.Precio);
                        det.Parameters.AddWithValue("@iServicio", 0);
                        det.Parameters.AddWithValue("@iFamilia", 0);
                        det.Parameters.AddWithValue("@iMarcaVehiculo", 0);
                        det.Parameters.AddWithValue("@iModeloVehiculo", 0);
                        det.Parameters.AddWithValue("@iAñoVehiculo", 0);
                        det.Parameters.AddWithValue("@iconceptoCompra", 0);
                        det.Parameters.AddWithValue("@TiposGarantias", 0);
                        det.Parameters.AddWithValue("@strObservaciones", 0);
                        det.Parameters.AddWithValue("@iInventarioCedis", 0);
                        det.Parameters.AddWithValue("@strProductoProveedorid", 0);

                        det.Parameters.AddWithValue("@fRetencion", 0);
                        det.Parameters.AddWithValue("@iStatus", 0);
                        det.Parameters.AddWithValue("@iFacturaProveedorid", Convert.ToDouble(txtFactura.Text));

                        det.Parameters.AddWithValue("@fDescuento", item.Descuento);
                        det.Parameters.AddWithValue("@fsubtotalsinDescuento", item.TotalSinDescuento);
                        det.Parameters.AddWithValue("@fImpuesto", item.Impuesto);

                        det.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("✅ Orden de compra guardada correctamente");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }
    }

    // =========================================
    // 🔥 MODELO GRID
    // =========================================
    public class tablaResurtido
    {
        public string NombreProducto { get; set; }
        public string ProductoId { get; set; }
        public string Marca { get; set; }
        public int MarcaId { get; set; }        
        public decimal DescuentoMarca { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Importe { get; set; }
        public decimal Descuento { get; set; }
        public decimal TotalSinDescuento { get; set; }
        public decimal Impuesto { get; set; }
    }
}
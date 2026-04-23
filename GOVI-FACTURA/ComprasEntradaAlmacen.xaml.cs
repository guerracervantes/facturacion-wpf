using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace GOVI_FACTURA
{
    public partial class ComprasEntradaAlmacen : Window
    {

        // =========================
        // 🧠 VARIABLES GLOBALES
        // =========================

        private string connectionString = "Data Source=192.168.1.195;Initial Catalog=chapultepec;User ID=servicios;Password=Pa$$w0rd; Encrypt=False; TrustServerCertificate=True;";

        ObservableCollection<DetalleCompra> listaDetalle = new ObservableCollection<DetalleCompra>();

        int iAlmacenId = 1;
        int iProveedorId = 0;
        double iCompraId = 0;

        public ComprasEntradaAlmacen()
        {
            InitializeComponent();
            dgDetalle.ItemsSource = listaDetalle;
        }
        // =========================
        // 🔘 BOTÓN GUARDAR
        // =========================
        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            GuardarCompra();
        }
        private void txtOrdenCompra_LostFocus(object sender, RoutedEventArgs e)
        {
            CargarOrdenCompra();
        }
        private void CargarOrdenCompra()
        {
            try
            {
                if (!int.TryParse(txtOrdenCompra.Text, out int ordenCompra))
                    return;

                listaDetalle.Clear();

                using (SqlConnection cn = new SqlConnection(connectionString))
                {
                    cn.Open();

                    // =========================
                    // 🧾 CABECERA (PROVEEDOR)
                    // =========================
                    SqlCommand cmd = new SqlCommand(@"
                SELECT TOP 1 
                    oc.iProveedorId,
                    p.strNombre,
                    p.strDireccion,
                    p.iCiudadId,
                    p.iEstadoId,
                    p.strRFC,
                    p.strTelefono
                FROM ORDEN_COMPRA oc
                INNER JOIN PROVEEDOR p ON p.iProveedorId = oc.iProveedorId
                WHERE oc.iOrdenId = @orden", cn);

                    cmd.Parameters.AddWithValue("@orden", ordenCompra);

                    SqlDataReader dr = cmd.ExecuteReader();

                    if (!dr.HasRows)
                    {
                        MessageBox.Show("La orden de compra no existe");

                        txtProveedor.Clear();
                        txtNombreProv.Clear();
                        txtDireccion.Clear();
                        txtCiudad.Clear();
                        txtEstado.Clear();
                        txtRFC.Clear();
                        txtTelefono.Clear();

                        dr.Close();
                        return;
                    }
                    dr.Read();

                    iProveedorId = Convert.ToInt32(dr["iProveedorId"]);

                    txtProveedor.Text = dr["iProveedorId"].ToString();
                    txtNombreProv.Text = dr["strNombre"].ToString();
                    txtDireccion.Text = dr["strDireccion"].ToString();
                    txtCiudad.Text = dr["iCiudadId"].ToString();
                    txtEstado.Text = dr["iEstadoId"].ToString();
                    txtRFC.Text = dr["strRFC"].ToString();
                    txtTelefono.Text = dr["strTelefono"].ToString();

                    dr.Close();

                    // =========================
                    // 📦 DETALLE PRODUCTOS
                    // =========================
                    SqlCommand cmdDet = new SqlCommand(@"
                    SELECT 
                        d.strProductoId,
                        d.iMarcaId,
                        p.strDescripcion,
                        d.iCantidad,
                        ISNULL(i.iCantidadActual,0) as Inventario
                    FROM DETALLE_ORDEN_COMPRA d
                    INNER JOIN PRODUCTO p 
                        ON p.strProductoId = d.strProductoId
                    LEFT JOIN INVENTARIO i 
                        ON i.strProductoId = d.strProductoId 
                        AND i.iMarcaId = d.iMarcaId
                        AND i.iAlmacenId = @almacen
                    WHERE d.iOrdenId = @orden", cn);

                    cmdDet.Parameters.AddWithValue("@orden", ordenCompra);
                    cmdDet.Parameters.AddWithValue("@almacen", iAlmacenId);

                    SqlDataReader drDet = cmdDet.ExecuteReader();

                    while (drDet.Read())
                    {
                        listaDetalle.Add(new DetalleCompra
                        {
                            Producto = drDet["strProductoId"].ToString(),
                            Marca = Convert.ToInt32(drDet["iMarcaId"]),
                            Descripcion = drDet["strDescripcion"].ToString(),
                            Inventario = Convert.ToInt32(drDet["Inventario"]),
                            PendienteOC = Convert.ToInt32(drDet["iCantidad"]),
                            CantidadSurtida = 0,
                            Precio = 0,
                            Importe = 0,
                            Loc = "",
                            Descuento = 0,
                            TotalSinIVA = 0,
                            IVA = 0,
                            Retencion = 0
                        });
                    }

                    drDet.Close();
                    MessageBox.Show("OC cargada correctamente");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar OC: " + ex.Message);
            }
        }
        // =========================
        // 🔥 MÉTODO PRINCIPAL
        // =========================
        private void GuardarCompra()
        {
            try
            {
                if (listaDetalle.Count == 0)
                {
                    MessageBox.Show("No hay productos");
                    return;
                }
                if (listaDetalle.All(x => x.CantidadSurtida == 0))
                {
                    MessageBox.Show("No hay cantidades capturadas");
                    return;
                }
                if (!int.TryParse(txtProveedor.Text, out iProveedorId))
                {
                    MessageBox.Show("Proveedor inválido");
                    return;
                }
                if (!int.TryParse(txtOrdenCompra.Text, out int ordenCompra))
                {
                    MessageBox.Show("Orden de compra inválida");
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtFactura.Text))
                {
                    MessageBox.Show("Falta Factura");
                    return;
                }
                // =========================
                // 🔎 FILTRAR DETALLE
                // =========================
                var listaFiltrada = listaDetalle.Where(x => x.CantidadSurtida > 0).ToList();

                if (listaFiltrada.Count == 0)
                {
                    MessageBox.Show("No hay productos válidos para guardar");
                    return;
                }

                decimal subtotal = 0;
                decimal iva = 0;
                decimal retencion = 0;

                foreach (var item in listaFiltrada)
                {
                    if (item.CantidadSurtida > item.PendienteOC)
                        throw new Exception("Excede lo pendiente en OC");

                    subtotal += item.TotalSinIVA;
                    iva += item.IVA;
                    retencion += item.Retencion;
                }

                decimal total = listaFiltrada.Sum(x => x.TotalSinIVA + x.IVA - x.Retencion);

                // =========================
                // 🧾 CABECERA
                // =========================
                object[,] cDetalle = new object[1, 20];

                cDetalle[0, 0] = iAlmacenId;
                cDetalle[0, 1] = iProveedorId;
                cDetalle[0, 2] = iCompraId;
                cDetalle[0, 3] = ordenCompra; cDetalle[0, 4] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cDetalle[0, 5] = subtotal;
                decimal descuento = 0;
                decimal.TryParse(txtDescuento.Text, out descuento);
                cDetalle[0, 6] = descuento; cDetalle[0, 7] = 0;
                cDetalle[0, 8] = 0;
                cDetalle[0, 9] = subtotal;
                cDetalle[0, 10] = subtotal;
                cDetalle[0, 11] = iva;
                cDetalle[0, 12] = total;
                cDetalle[0, 13] = 1;
                cDetalle[0, 14] = txtFactura.Text;
                cDetalle[0, 15] = 0;
                cDetalle[0, 16] = "";
                cDetalle[0, 17] = Environment.MachineName;
                cDetalle[0, 18] = Environment.UserName;
                cDetalle[0, 19] = retencion;

                // =========================
                // 📦 DETALLE
                // =========================
                object[,] cDetCompra = new object[listaFiltrada.Count, 19];

                int i = 0;

                foreach (var item in listaFiltrada)
                {
                    cDetCompra[i, 0] = item.Producto;
                    cDetCompra[i, 1] = item.Marca;
                    cDetCompra[i, 2] = iAlmacenId;
                    cDetCompra[i, 3] = iProveedorId;
                    cDetCompra[i, 4] = iCompraId;
                    cDetCompra[i, 5] = item.CantidadSurtida;
                    cDetCompra[i, 6] = item.Precio;
                    cDetCompra[i, 7] = item.Importe;
                    cDetCompra[i, 8] = item.Importe;
                    cDetCompra[i, 9] = item.Descuento;
                    cDetCompra[i, 10] = 0;
                    cDetCompra[i, 11] = 0;
                    cDetCompra[i, 12] = item.TotalSinIVA;
                    cDetCompra[i, 13] = item.IVA;
                    cDetCompra[i, 14] = item.TotalSinIVA + item.IVA - item.Retencion;
                    cDetCompra[i, 15] = i;
                    cDetCompra[i, 16] = item.Loc;
                    cDetCompra[i, 17] = item.Descripcion;
                    cDetCompra[i, 18] = item.Retencion;

                    i++;
                }

                // =========================
                // 💰 COSTOS
                // =========================
                object[,] cCosto = new object[listaFiltrada.Count, 6];

                i = 0;

                foreach (var item in listaFiltrada)
                {
                    cCosto[i, 0] = iAlmacenId;
                    cCosto[i, 1] = iProveedorId;
                    cCosto[i, 2] = item.Marca;
                    cCosto[i, 3] = item.Producto;
                    cCosto[i, 4] = item.Precio;
                    cCosto[i, 5] = item.Precio;

                    i++;
                }

                // =========================
                // 💾 GUARDAR EN BD
                // =========================
                clsBD bd = new clsBD();

                bd.Procedimiento = "compraInserta_IP_RETENCION_DESCUENTO";
                bd.cDetalle = cDetalle;
                bd.fnInsertaDetalle();

                bd.Procedimiento = "compraDetalleInserta_RETENCION_DESCUENTO";
                bd.cDetalle = cDetCompra;
                bd.fnInsertaDetalle();

                bd.Procedimiento = "CostoCompraInserta";
                bd.cDetalle = cCosto;
                bd.fnInsertaDetalle();

                MessageBox.Show("Compra guardada correctamente");

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
    // =========================
    // 🧩 CLASE BD (SIMPLIFICADA)
    // =========================
    public class clsBD
    {
        public string Procedimiento { get; set; }
        public object[,] cDetalle { get; set; }

        public void fnInsertaDetalle()
        {
        }
    }
    // =========================
    // 🧾 MODELO DEL GRID
    // =========================
    public class DetalleCompra
    {
        public string Producto { get; set; }
        public int Marca { get; set; }
        public string Descripcion { get; set; }
        public int Inventario { get; set; }
        public int PendienteOC { get; set; }
        public int CantidadSurtida { get; set; }
        public decimal Precio { get; set; }
        public decimal Importe { get; set; }
        public string Loc { get; set; }
        public decimal Descuento { get; set; }
        public decimal TotalSinIVA { get; set; }
        public decimal IVA { get; set; }
        public decimal Retencion { get; set; }
    }
}
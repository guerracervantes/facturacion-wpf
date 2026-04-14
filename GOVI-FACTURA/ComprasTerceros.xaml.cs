using GOVI_FACTURA.Services;
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

namespace GOVI_FACTURA
{
    public partial class ComprasTerceros : Window
    {


        private string connectionString = "Data Source=192.168.1.195;Initial Catalog=chapultepec;User ID=servicios;Password=Pa$$w0rd; Encrypt=False; TrustServerCertificate=True;";


        private enum ModoPantalla
        {
            Busqueda,
            Captura
        }
        private ModoPantalla modoActual = ModoPantalla.Busqueda;


        public ObservableCollection<DetalleOrden> Detalles { get; set; } = new ObservableCollection<DetalleOrden>();
        public ObservableCollection<dynamic> Proveedores { get; set; } = new ObservableCollection<dynamic>();


        ObservableCollection<ProductoOM> listaOM = new ObservableCollection<ProductoOM>();
        ObservableCollection<dynamic> Unidades = new ObservableCollection<dynamic>();
        ObservableCollection<dynamic> Familias = new ObservableCollection<dynamic>();
        ObservableCollection<dynamic> Grupos = new ObservableCollection<dynamic>();
        bool cargando = false;
        private bool insertOk = false;


        public ComprasTerceros()
        {
            InitializeComponent();

            DataContext = this;
            CargarProveedores();
            cmbIVA.ItemsSource = new List<int> { 8, 16 };
            cmbIVA.SelectedItem = 16;
            cmbDescuento.ItemsSource = new List<int> { 0, 5, 10, 15, 20, 25, 30 , 35 , 40};
            cmbDescuento.SelectedItem = 0;
            dgDetalle.ItemsSource = Detalles;

            dgOM.ItemsSource = listaOM;
            CargarUnidades();
            CargarFamilias();
            CargarGrupos();

            AplicarModo();
        }


        private void CargarProveedores()
        {
            try
            {
                Proveedores.Clear();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand("SELECT iProveedorId, strNombre FROM PROVEEDOR WHERE bBaja = 0", conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        Proveedores.Add(new
                        {
                            Id = reader["iProveedorId"],
                            Nombre = reader["strNombre"]
                        });
                    }
                }

                cmbProveedor.ItemsSource = Proveedores;
                cmbProveedor.DisplayMemberPath = "Nombre";
                cmbProveedor.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando proveedores: " + ex.Message);
            }
        }
        private void BuscarPorProveedor(int proveedorId)
        {
            try
            {
                Detalles.Clear();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                SELECT TOP 200 
                    d.strProductoId,
                    d.iMarcaId,
                    d.descripcion_producto,
                    d.iCantidad,
                    d.fPrecio,
                    d.fImporte,
                    d.strlocalizacionid,
                    d.iVenta,
                    p.iServicio,  
                    o.strAutorizacion,
                    o.dFechaOrden,
                    o.dfechallegada,
                    o.iOrdenId
                FROM DETALLE_ORDEN_COMPRA d
                INNER JOIN ORDEN_COMPRA o ON o.iOrdenId = d.iOrdenId
                INNER JOIN PRODUCTO p ON p.strProductoId = d.strProductoId 
                WHERE 1=1
            ";

                    if (!string.IsNullOrWhiteSpace(txtFactura.Text))
                    {
                        query += " AND o.iOrdenId = @OrdenId";
                    }
                    else
                    {
                        if (proveedorId > 0)
                            query += " AND o.iProveedorId = @ProveedorId";

                        if (dpFecha.SelectedDate != null)
                            query += " AND MONTH(o.dFechaOrden) = @Mes AND YEAR(o.dFechaOrden) = @Anio";
                    }

                    query += " ORDER BY o.dFechaOrden DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(txtFactura.Text))
                            cmd.Parameters.AddWithValue("@OrdenId", txtFactura.Text);
                        else
                        {
                            if (proveedorId > 0)
                                cmd.Parameters.AddWithValue("@ProveedorId", proveedorId);

                            if (dpFecha.SelectedDate != null)
                            {
                                cmd.Parameters.AddWithValue("@Mes", dpFecha.SelectedDate.Value.Month);
                                cmd.Parameters.AddWithValue("@Anio", dpFecha.SelectedDate.Value.Year);
                            }
                        }

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {

                            while (reader.Read())
                            {
                                var item = new DetalleOrden
                                {
                                    Producto = reader["strProductoId"]?.ToString() ?? "",
                                    Marca = Convert.ToInt32(reader["iMarcaId"]),
                                    Descripcion = reader["descripcion_producto"]?.ToString() ?? "",
                                    Cantidad = Convert.ToInt32(reader["iCantidad"]),
                                    Costo = Convert.ToDecimal(reader["fPrecio"]),
                                    Importe = Convert.ToDecimal(reader["fImporte"]),
                                    Localizacion = reader["strlocalizacionid"]?.ToString() ?? "",
                                    Venta = Convert.ToInt32(reader["iVenta"]),
                                    Servicio = reader["iServicio"] != DBNull.Value ? Convert.ToInt32(reader["iServicio"]) : 0,
                                    Autorizacion = reader["strAutorizacion"]?.ToString() ?? "",
                                    FechaOrden = reader["dFechaOrden"] != DBNull.Value
                                                 ? Convert.ToDateTime(reader["dFechaOrden"])
                                                 : (DateTime?)null,
                                    FechaLlegada = reader["dfechallegada"] != DBNull.Value
                                                 ? Convert.ToDateTime(reader["dfechallegada"])
                                                 : (DateTime?)null,
                                    OrdenId = Convert.ToInt32(reader["iOrdenId"])
                                };
                                Detalles.Add(item);
                            }
                        }
                    }
                }

                dgDetalle.ItemsSource = null;
                dgDetalle.ItemsSource = Detalles;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
        private void txtFactura_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = txtFactura.Text.Trim();

            if (string.IsNullOrEmpty(texto))
                return;

            foreach (var item in dgDetalle.Items)
            {
                // Usamos el tipo correcto
                var fila = item as DetalleOrden;
                if (fila == null) continue;

                // Comparamos con el OrdenId, no con Producto
                if (fila.OrdenId.ToString() == texto)
                {
                    dgDetalle.SelectedItem = fila;
                    dgDetalle.ScrollIntoView(fila);
                    break;
                }
            }
        }
        private void btnCalcular_Click(object sender, RoutedEventArgs e)
        {
            CalcularTotales();
        }
        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            GuardarOrden();
        }
        private void btnBusqueda_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                dpFecha.SelectedDate = null;
                dpFechaLlegada.SelectedDate = null;

                int proveedorId = 0;

                // 🔹 Prioridad ComboBox
                if (cmbProveedor.SelectedValue != null)
                {
                    proveedorId = Convert.ToInt32(cmbProveedor.SelectedValue);
                }
                // 🔹 Prioridad TextBox
                else if (!string.IsNullOrWhiteSpace(txtProveedorId.Text))
                {
                    int.TryParse(txtProveedorId.Text, out proveedorId);
                }

                if (proveedorId == 0)
                {
                    MessageBox.Show("Selecciona o escribe un proveedor válido");
                    return;
                }

                BuscarPorProveedor(proveedorId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }


        private void cmbProveedor_DropDownOpened(object sender, EventArgs e)
        {
            CargarProveedores();
        }
        private void cmbProveedor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cargando) return;

            if (cmbProveedor.SelectedValue != null)
            {
                cargando = true;

                int proveedorId = Convert.ToInt32(cmbProveedor.SelectedValue);

                txtProveedorId.Text = proveedorId.ToString();

                cargando = false;
            }
        }
        private void cmbProducto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
        private void txtProveedorId_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (cargando) return;

            if (int.TryParse(txtProveedorId.Text, out int proveedorId))
            {
                cargando = true;

                cmbProveedor.SelectedValue = proveedorId;

                cargando = false;
            }
        }
        private void dgDetalle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgDetalle.SelectedItem != null)
            {
                var fila = dgDetalle.SelectedItem as DetalleOrden;
                if (fila == null) return;

                // Llenar los TextBox solo cuando el usuario seleccione la fila
                txtAutorizacion.Text = fila.Autorizacion;
                dpFecha.SelectedDate = fila.FechaOrden;
                dpFechaLlegada.SelectedDate = fila.FechaLlegada;
                txtFactura.Text = fila.OrdenId.ToString(); 
            }
        }
        private DetalleOrden ObtenerDetalleProducto(string productoId, int marcaId, int cantidad = 1)
        {
            decimal factor = 1m; // Por defecto 1
            decimal precioAnterior = 0m;
            decimal costo = 0m; // ahora lo captura el usuario
            string localizacion = "";
            int venta = 0;
            string descripcion = "";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // 1️⃣ Leer datos del producto
                string queryProducto = @"
            SELECT 
                p.strDescripcion,
                pr.fPrecio AS PrecioAnterior,
                (SELECT TOP 1 pl.iLocalizacionId 
                 FROM PRODUCTO_LOCALIZACION pl
                 WHERE pl.strProductoId = p.strProductoId
                   AND pl.iMarcaId = @MarcaId
                   AND pl.iAlmacenId = @AlmacenId
                 ORDER BY pl.iLocalizacionId) AS Localizacion,
                p.iVenta
            FROM PRODUCTO p
            INNER JOIN PRECIO pr 
                ON pr.strProductoId = p.strProductoId AND pr.iMarcaId = @MarcaId
            WHERE p.strProductoId = @ProductoId
        ";

                using (SqlCommand cmd = new SqlCommand(queryProducto, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductoId", productoId);
                    cmd.Parameters.AddWithValue("@MarcaId", marcaId);
                    cmd.Parameters.AddWithValue("@AlmacenId", 4); // Cambia al almacén que uses

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            descripcion = reader["strDescripcion"]?.ToString() ?? "";
                            precioAnterior = reader["PrecioAnterior"] != DBNull.Value ? Convert.ToDecimal(reader["PrecioAnterior"]) : 0;
                            localizacion = reader["Localizacion"]?.ToString() ?? "";
                            venta = reader["iVenta"] != DBNull.Value ? Convert.ToInt32(reader["iVenta"]) : 0;
                        }
                    }
                }

                // 2️⃣ Leer factor de PARAMETRO / ALMACEN_PARAMETRO
                string queryFactor = @"
            SELECT strValor
            FROM ALMACEN_PARAMETRO
            WHERE iParametroId = 39 AND iAlmacenId = @AlmacenId
        ";

                using (SqlCommand cmdFactor = new SqlCommand(queryFactor, conn))
                {
                    cmdFactor.Parameters.AddWithValue("@AlmacenId", 4); // Cambia según tu almacén
                    object result = cmdFactor.ExecuteScalar();
                    if (result != null)
                    {
                        string str = result.ToString();
                        // Parse seguro usando punto decimal
                        if (!decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out factor))
                            factor = 1m; // si falla, factor = 1
                    }
                }
            }

            // 3️⃣ Construir objeto DetalleOrden
            return new DetalleOrden
            {
                Producto = productoId,
                Marca = marcaId,
                Descripcion = descripcion,
                PrecioAnterior = precioAnterior,
                Costo = 0, 
                Importe = 0,
                PrecioNuevo = 0,
                Localizacion = localizacion,
                Venta = venta,
                Cantidad = cantidad,
                Autorizacion = txtAutorizacion.Text,
                FechaOrden = dpFecha.SelectedDate,
                FechaLlegada = dpFechaLlegada.SelectedDate
            };
        }
        private void dgDetalle_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            var detalle = e.Row.Item as DetalleOrden;
            if (detalle == null) return;

            // 🔹 CUANDO EDITAS PRODUCTO
            if (e.Column.Header.ToString() == "Producto" && !string.IsNullOrEmpty(detalle.Producto) && detalle.Marca > 0)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var datos = ObtenerDetalleProducto(detalle.Producto, detalle.Marca, detalle.Cantidad);
                    if (datos != null)
                    {
                        detalle.Descripcion = datos.Descripcion;
                        detalle.PrecioAnterior = datos.PrecioAnterior; // 🔥 IMPORTANTE
                        detalle.Costo = 0; // usuario lo captura
                        detalle.Importe = 0;
                        detalle.Localizacion = datos.Localizacion;
                        detalle.Venta = datos.Venta;
                        detalle.Cantidad = datos.Cantidad;
                        detalle.PrecioNuevo = 0;
                    }

                    dgDetalle.Items.Refresh();
                    CalcularTotales();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }

            // 🔹 CUANDO EDITAS COSTO (🔥 ESTE VA FUERA)
            if (e.Column.Header.ToString() == "Costo")
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    detalle.PrecioNuevo = detalle.Costo * 1.35m;
                    detalle.Importe = detalle.Costo * detalle.Cantidad;

                    dgDetalle.Items.Refresh();
                    CalcularTotales();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
        private void dgDetalle_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            var detalle = e.Row.Item as DetalleOrden;
            if (detalle == null) return;

            // Si no se puso cantidad, la ponemos en 1
            if (detalle.Cantidad <= 0)
                detalle.Cantidad = 1;

            // Verifica que Producto y Marca tengan valor
            if (!string.IsNullOrEmpty(detalle.Producto) && detalle.Marca > 0)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var datos = ObtenerDetalleProducto(detalle.Producto, detalle.Marca, detalle.Cantidad);
                    if (datos != null)
                    {
                        detalle.Descripcion = datos.Descripcion;
                        detalle.PrecioAnterior = datos.PrecioAnterior;
                        detalle.Importe = detalle.Costo * detalle.Cantidad;
                        detalle.PrecioNuevo = detalle.Costo * 1.35m;
                        detalle.Localizacion = datos.Localizacion;
                        detalle.Venta = datos.Venta;
                        detalle.Cantidad = datos.Cantidad;
                    }

                    dgDetalle.Items.Refresh();
                    CalcularTotales();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
        private void dgDetalle_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var grid = sender as DataGrid;
            if (grid == null) return;

            e.Handled = true;

            // 🔥 FORZAR GUARDADO REAL
            grid.CommitEdit(DataGridEditingUnit.Cell, true);
            grid.CommitEdit(DataGridEditingUnit.Row, true);

            // 🔥 DEJAR QUE WPF TERMINE BIEN
            Dispatcher.BeginInvoke(new Action(() =>
            {
                int col = grid.CurrentCell.Column.DisplayIndex;
                int nextCol = -1;

                switch (col)
                {
                    case 0: nextCol = 2; break;
                    case 2: nextCol = 5; break;
                    case 5: nextCol = 6; break;
                    case 6: nextCol = 15; break;
                    case 15:
                        grid.SelectedIndex++;

                        if (grid.SelectedIndex >= grid.Items.Count)
                            return;

                        grid.CurrentCell = new DataGridCellInfo(
                            grid.Items[grid.SelectedIndex],
                            grid.Columns[0]);

                        grid.BeginEdit();
                        return;
                }

                if (nextCol >= 0)
                {
                    grid.CurrentCell = new DataGridCellInfo(
                        grid.SelectedItem,
                        grid.Columns[nextCol]);

                    grid.BeginEdit();
                }

            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        private void CalcularTotales()
        {
            foreach (var d in Detalles)
            {
                d.SubTotalSinDescuento = d.Importe;

                decimal descuentoPorc = cmbDescuento.SelectedItem != null
                    ? Convert.ToDecimal(cmbDescuento.SelectedItem)
                    : 0;

                d.Descuento = d.SubTotalSinDescuento * descuentoPorc / 100;

                decimal baseCalculo = d.SubTotalSinDescuento - d.Descuento;

                decimal ivaPorc = cmbIVA.SelectedItem != null
                    ? Convert.ToDecimal(cmbIVA.SelectedItem)
                    : 0;

                d.Impuesto = baseCalculo * ivaPorc / 100;

                // 🔴 IMPORTANTE: NO modificar Retencion aquí
            }

            decimal subTotal = Detalles.Sum(d => d.Importe);

            decimal descuento = cmbDescuento.SelectedItem != null
                ? subTotal * Convert.ToDecimal(cmbDescuento.SelectedItem) / 100
                : 0;

            decimal subTotalFinal = subTotal - descuento;

            decimal iva = cmbIVA.SelectedItem != null
                ? subTotalFinal * Convert.ToDecimal(cmbIVA.SelectedItem) / 100
                : 0;

            // 🔥 SUMAR RETENCIÓN DESDE EL GRID
            decimal retencion = Detalles.Sum(d => d.Retencion);

            decimal total = subTotalFinal + iva - retencion;

            txtSubTotal.Text = subTotal.ToString("N2");
            txtDescuento.Text = descuento.ToString("N2");
            txtSubTotalFinal.Text = subTotalFinal.ToString("N2");
            txtIVAImporte.Text = iva.ToString("N2");
            txtRetencion.Text = retencion.ToString("N2");
            txtTotal.Text = total.ToString("N2");

            dgDetalle.Items.Refresh();
        }
        private void GuardarOrden()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {
                    // 🔥 VALIDAR PROVEEDOR
                    if (!int.TryParse(txtProveedorId.Text, out int proveedorId))
                    {
                        MessageBox.Show("Proveedor inválido");
                        return;
                    }

                    // 🔥 VALIDAR TOTALES
                    decimal subTotal = Convert.ToDecimal(txtSubTotal.Text);
                    decimal iva = Convert.ToDecimal(txtIVAImporte.Text);
                    decimal total = Convert.ToDecimal(txtTotal.Text);
                    decimal retencion = Convert.ToDecimal(txtRetencion.Text);
                    decimal descuento = Convert.ToDecimal(txtDescuento.Text);

                    // 🔥 GENERAR NUEVO ID (CON BLOQUEO PARA EVITAR DUPLICADOS)
                    SqlCommand cmdNextId = new SqlCommand(@"
                SELECT ISNULL(MAX(iOrdenId),0) + 1 
                FROM ORDEN_COMPRA WITH (UPDLOCK, HOLDLOCK)", conn, tran);

                    int nuevoOrdenId = Convert.ToInt32(cmdNextId.ExecuteScalar());

                    // 1️⃣ INSERTAR ENCABEZADO
                    SqlCommand cmdEnc = new SqlCommand("OrdencompraInsertaAUTORIZACION_FACPROV_RETENCION_DESCUENTO", conn, tran);
                    cmdEnc.CommandType = CommandType.StoredProcedure;

                    cmdEnc.Parameters.AddWithValue("@iAlmacenId", 4);
                    cmdEnc.Parameters.AddWithValue("@iOrdenId", nuevoOrdenId);
                    cmdEnc.Parameters.AddWithValue("@iProveedorId", proveedorId);
                    cmdEnc.Parameters.AddWithValue("@iRequisicion", 0);
                    cmdEnc.Parameters.AddWithValue("@fSubTotal", subTotal);
                    cmdEnc.Parameters.AddWithValue("@fImpuesto", iva);
                    cmdEnc.Parameters.AddWithValue("@fTotal", total);
                    cmdEnc.Parameters.AddWithValue("@dfechallegada", dpFechaLlegada.SelectedDate ?? DateTime.Now);
                    cmdEnc.Parameters.AddWithValue("@strAutorizacion", txtAutorizacion.Text ?? "");
                    cmdEnc.Parameters.AddWithValue("@iFacturaProveedorid", 0);
                    cmdEnc.Parameters.AddWithValue("@fRetencion", retencion);
                    cmdEnc.Parameters.AddWithValue("@iStatus", "");
                    cmdEnc.Parameters.AddWithValue("@fDescuento", descuento);
                    cmdEnc.Parameters.AddWithValue("@fsubtotalsinDescuento", subTotal);

                    cmdEnc.ExecuteNonQuery();

                    // 2️⃣ INSERTAR DETALLE
                    foreach (var detalle in Detalles)
                    {
                        SqlCommand cmdDet = new SqlCommand("OrdencompraDetalleInserta_Servicio_Cod_Prov_RETENCION_DESCUENTO", conn, tran);
                        cmdDet.CommandType = CommandType.StoredProcedure;

                        cmdDet.Parameters.AddWithValue("@iAlmacenId", 4);
                        cmdDet.Parameters.AddWithValue("@strProductoId", detalle.Producto);
                        cmdDet.Parameters.AddWithValue("@iProveedorId", proveedorId);
                        cmdDet.Parameters.AddWithValue("@iMarcaId", detalle.Marca);
                        cmdDet.Parameters.AddWithValue("@iOrdenId", nuevoOrdenId);
                        cmdDet.Parameters.AddWithValue("@iCantidad", detalle.Cantidad);

                        // 🔥 PARÁMETROS OBLIGATORIOS
                        cmdDet.Parameters.AddWithValue("@iCantidadSurtida", 0);
                        cmdDet.Parameters.AddWithValue("@iOrdenCaptura", 0);
                        cmdDet.Parameters.AddWithValue("@iventaglobal", 0);
                        cmdDet.Parameters.AddWithValue("@iVenta", detalle.Venta);
                        cmdDet.Parameters.AddWithValue("@iInventario", 0);

                        cmdDet.Parameters.AddWithValue("@iMES1", 0);
                        cmdDet.Parameters.AddWithValue("@iMES2", 0);
                        cmdDet.Parameters.AddWithValue("@iMES3", 0);
                        cmdDet.Parameters.AddWithValue("@iMES4", 0);
                        cmdDet.Parameters.AddWithValue("@iMES5", 0);
                        cmdDet.Parameters.AddWithValue("@iMES6", 0);
                        cmdDet.Parameters.AddWithValue("@iMES7", 0);
                        cmdDet.Parameters.AddWithValue("@iMES8", 0);
                        cmdDet.Parameters.AddWithValue("@iMES9", 0);
                        cmdDet.Parameters.AddWithValue("@iMES10", 0);
                        cmdDet.Parameters.AddWithValue("@iMES11", 0);
                        cmdDet.Parameters.AddWithValue("@iMES12", 0);

                        cmdDet.Parameters.AddWithValue("@iMESANT1", 0);
                        cmdDet.Parameters.AddWithValue("@iMESANT2", 0);
                        cmdDet.Parameters.AddWithValue("@iMESANT3", 0);
                        cmdDet.Parameters.AddWithValue("@iMESANT4", 0);
                        cmdDet.Parameters.AddWithValue("@iMESANT5", 0);
                        cmdDet.Parameters.AddWithValue("@iMESANT6", 0);
                        cmdDet.Parameters.AddWithValue("@iMESANT7", 0);
                        cmdDet.Parameters.AddWithValue("@iMESANT8", 0);
                        cmdDet.Parameters.AddWithValue("@iMESANT9", 0);
                        cmdDet.Parameters.AddWithValue("@iMESANT10", 0);
                        cmdDet.Parameters.AddWithValue("@iMESANT11", 0);
                        cmdDet.Parameters.AddWithValue("@iMESANT12", 0);

                        cmdDet.Parameters.AddWithValue("@fPrecio", detalle.PrecioAnterior);
                        cmdDet.Parameters.AddWithValue("@fImporte", detalle.Importe);
                        cmdDet.Parameters.AddWithValue("@strlocalizacionId", detalle.Localizacion ?? "");

                        cmdDet.Parameters.AddWithValue("@descripcion_producto", detalle.Producto ?? "");
                        cmdDet.Parameters.AddWithValue("@fprecionuevo", detalle.PrecioNuevo);
                        cmdDet.Parameters.AddWithValue("@iServicio", 0);
                        cmdDet.Parameters.AddWithValue("@iFamilia", 0);
                        cmdDet.Parameters.AddWithValue("@iMarcaVehiculo", 0);
                        cmdDet.Parameters.AddWithValue("@iModeloVehiculo", 0);
                        cmdDet.Parameters.AddWithValue("@iAñoVehiculo", 0);
                        cmdDet.Parameters.AddWithValue("@iconceptoCompra", 0);
                        cmdDet.Parameters.AddWithValue("@TiposGarantias", 0);
                        cmdDet.Parameters.AddWithValue("@strObservaciones", "");
                        cmdDet.Parameters.AddWithValue("@iInventarioCedis", 0);
                        cmdDet.Parameters.AddWithValue("@strProductoProveedorid", "");

                        cmdDet.Parameters.AddWithValue("@fRetencion", 0);
                        cmdDet.Parameters.AddWithValue("@iStatus", "");
                        cmdDet.Parameters.AddWithValue("@iFacturaProveedorid", 0);
                        cmdDet.Parameters.AddWithValue("@fDescuento", 0);
                        cmdDet.Parameters.AddWithValue("@fsubtotalsinDescuento", detalle.Importe);
                        cmdDet.Parameters.AddWithValue("@fImpuesto", 0);

                        cmdDet.ExecuteNonQuery();
                    }

                    tran.Commit();
                    MessageBox.Show($"Orden {nuevoOrdenId} guardada correctamente.");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    MessageBox.Show("Error al guardar la orden: " + ex.Message);
                }
            }
        }


        private bool ProductoYaExiste(string productoId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
            SELECT COUNT(1)
            FROM PRODUCTO
            WHERE strProductoId = @ProductoId
        ", conn);

                cmd.Parameters.AddWithValue("@ProductoId", productoId);

                return (int)cmd.ExecuteScalar() > 0;
            }
        }
        private ProductoOM GetCurrentItem()
        {
            return panelOMConfig.DataContext as ProductoOM;
        }
        private void cmbUnidadOM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = GetCurrentItem();
            if (item == null || cmbUnidadOM.SelectedValue == null) return;

            item.UnidadId = Convert.ToInt32(cmbUnidadOM.SelectedValue);
        }
        private void cmbFamiliaOM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = GetCurrentItem();
            if (item == null || cmbFamiliaOM.SelectedValue == null) return;

            item.FamiliaId = Convert.ToInt32(cmbFamiliaOM.SelectedValue);
        }
        private void cmbGrupoOM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = GetCurrentItem();
            if (item == null || cmbGrupoOM.SelectedValue == null) return;

            item.GrupoId = Convert.ToInt32(cmbGrupoOM.SelectedValue);
        }
        private void SyncComboFromItem(ProductoOM item)
        {
            cmbUnidadOM.SelectedValue = item.UnidadId;
            cmbFamiliaOM.SelectedValue = item.FamiliaId;
            cmbGrupoOM.SelectedValue = item.GrupoId;
        }
        private void dgOM_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var grid = sender as DataGrid;
            if (grid == null) return;

            e.Handled = true;

            grid.CommitEdit(DataGridEditingUnit.Cell, true);

            int col = grid.CurrentCell.Column.DisplayIndex;
            var item = grid.CurrentItem as ProductoOM;

            // 🔥 VALIDAR SOLO CUANDO ESTÁ EN COLUMNA PRODUCTO (0)
            if (col == 0)
            {
                if (item != null && !string.IsNullOrWhiteSpace(item.Producto))
                {
                    if (ProductoYaExiste(item.Producto))
                    {
                        MessageBox.Show("⚠ Este producto ya existe en la base de datos");

                        // 🔥 LIMPIAR LA FILA
                        item.Producto = "";
                        item.Descripcion = "";
                        item.Marca = "";
                        item.Precio = 0;

                        grid.CommitEdit(DataGridEditingUnit.Cell, true);
                        grid.BeginEdit();

                        return; // 🔥 se detiene aquí
                    }
                }
            }

            // 🔥 SI ESTÁ EN ÚLTIMA COLUMNA (Precio antes de Acción)
            if (col >= grid.Columns.Count - 2)
            {
                grid.CommitEdit(DataGridEditingUnit.Row, true);

                grid.SelectedIndex += 1;

                grid.CurrentCell = new DataGridCellInfo(
                    grid.Items[grid.SelectedIndex],
                    grid.Columns[0]);

                grid.BeginEdit();
                return;
            }

            // 👉 mover a siguiente columna
            int nextCol = col + 1;

            var column = grid.Columns[nextCol];

            grid.CurrentCell = new DataGridCellInfo(grid.SelectedItem, column);
            grid.BeginEdit();
        }
        private void AgregarFila_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.DataContext as ProductoOM;

            if (item == null) return;

            // 🔥 FORZAR LECTURA DIRECTA DE LOS COMBOBOX
            if (cmbUnidadOM.SelectedValue != null)
                item.UnidadId = Convert.ToInt32(cmbUnidadOM.SelectedValue);

            if (cmbFamiliaOM.SelectedValue != null)
                item.FamiliaId = Convert.ToInt32(cmbFamiliaOM.SelectedValue);

            if (cmbGrupoOM.SelectedValue != null)
                item.GrupoId = Convert.ToInt32(cmbGrupoOM.SelectedValue);

            // 🔥 DEBUG (IMPORTANTE)
            MessageBox.Show(
                $"Unidad: {item.UnidadId}\nFamilia: {item.FamiliaId}\nGrupo: {item.GrupoId}");

            if (!ValidarProducto(item))
                return;

            InsertarProductoBD(item);

            if (insertOk)
            {
                var lista = dgOM.ItemsSource as ObservableCollection<ProductoOM>;
                lista?.Remove(item);
            }
        }
        private void EliminarFila_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.DataContext as ProductoOM;

            if (item == null) return;

            var result = MessageBox.Show(
                "¿Eliminar este producto?",
                "Confirmar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var lista = dgOM.ItemsSource as ObservableCollection<ProductoOM>;
                lista?.Remove(item);
            }
        }
        private void dgOM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = dgOM.SelectedItem as ProductoOM;
            if (item == null) return;

            panelOMConfig.DataContext = item;

            SyncComboFromItem(item); // 🔥 IMPORTANTE
        }
        private void InsertarProductoBD(ProductoOM item)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {
                    SqlCommand cmdProd = new SqlCommand(@"
                        INSERT INTO PRODUCTO
                        (strProductoId, strDescripcion, iUnidadId, iGrupoId, iFamiliaId, dFechaAlta, iServicio, iVenta)
                        VALUES
                        (@ProductoId, @Descripcion, @UnidadId, @GrupoId, @FamiliaId, GETDATE(), 1, 0)
                    ", conn, tran);

                    cmdProd.Parameters.AddWithValue("@ProductoId", item.Producto);
                    cmdProd.Parameters.AddWithValue("@Descripcion", item.Descripcion);
                    cmdProd.Parameters.AddWithValue("@UnidadId", item.UnidadId);
                    cmdProd.Parameters.AddWithValue("@GrupoId", item.GrupoId);
                    cmdProd.Parameters.AddWithValue("@FamiliaId", item.FamiliaId);
                    cmdProd.ExecuteNonQuery();

                    // 🔹 PRECIO (expandido)
                    SqlCommand cmdPrecio = new SqlCommand(@"
                        INSERT INTO PRECIO
                        (iMarcaId, strProductoId, fPrecio, fCosto, bActivo, iAlmacenid,
                         fCostoMayoreo, fCostoMexico, fPrecioMayoreo, fPrecioDollar)
                        VALUES
                        (@Marca, @ProductoId, @Precio, @Costo, @Activo, 4,
                         1, 1, 1, 1)
                    ", conn, tran);

                    // 🔥 FORZADO OM
                    cmdPrecio.Parameters.AddWithValue("@ProductoId", item.Producto);
                    cmdPrecio.Parameters.AddWithValue("@Marca", 88);
                    cmdPrecio.Parameters.AddWithValue("@Precio", 1);
                    cmdPrecio.Parameters.AddWithValue("@Costo", 1);
                    cmdPrecio.Parameters.AddWithValue("@Activo", 1);
                    cmdPrecio.Parameters.AddWithValue("@CostoMayoreo", item.CostoMayoreo);
                    cmdPrecio.Parameters.AddWithValue("@CostoMexico", item.CostoMexico);
                    cmdPrecio.Parameters.AddWithValue("@PrecioMayoreo", item.PrecioMayoreo);
                    cmdPrecio.Parameters.AddWithValue("@Dolar", item.Dolar);

                    cmdPrecio.ExecuteNonQuery();

                    tran.Commit();
                    insertOk = true;
                    MessageBox.Show("Producto OM completo agregado 🔥");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }
        private bool ValidarProducto(ProductoOM item)
        {
            if (item == null)
            {
                MessageBox.Show("No hay producto seleccionado");
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.Producto))
            {
                MessageBox.Show("Falta el código del producto");
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.Descripcion))
            {
                MessageBox.Show("Falta la descripción");
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.Marca))
            {
                MessageBox.Show("Falta la marca");
                return false;
            }

            if (item.Precio <= 0)
            {
                MessageBox.Show("El precio debe ser mayor a 0");
                return false;
            }

            if (item.UnidadId <= 0)
            {
                MessageBox.Show("Selecciona una unidad");
                return false;
            }

            if (item.FamiliaId <= 0)
            {
                MessageBox.Show("Selecciona una familia");
                return false;
            }

            if (item.GrupoId <= 0)
            {
                MessageBox.Show("Selecciona un grupo");
                return false;
            }

            return true;
        }
        private void CargarUnidades()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand("SELECT iUnidadId, strDescripcion FROM UNIDAD", conn);
                SqlDataReader dr = cmd.ExecuteReader();

                Unidades.Clear();

                while (dr.Read())
                {
                    Unidades.Add(new
                    {
                        Id = dr["iUnidadId"],
                        Descripcion = dr["strDescripcion"].ToString()
                    });
                }

                cmbUnidadOM.ItemsSource = Unidades;
            }
        }
        private void CargarFamilias()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand("SELECT iFamiliaId, strDescripcion FROM FAMILIA", conn);
                SqlDataReader dr = cmd.ExecuteReader();

                Familias.Clear();

                while (dr.Read())
                {
                    Familias.Add(new
                    {
                        Id = dr["iFamiliaId"],
                        Descripcion = dr["strDescripcion"].ToString()
                    });
                }

                cmbFamiliaOM.ItemsSource = Familias;
            }
        }
        private void CargarGrupos()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand("SELECT iGrupoId, strDescripcion FROM GRUPO", conn);
                SqlDataReader dr = cmd.ExecuteReader();

                Grupos.Clear();

                while (dr.Read())
                {
                    Grupos.Add(new
                    {
                        Id = dr["iGrupoId"],
                        Descripcion = dr["strDescripcion"].ToString()
                    });
                }

                cmbGrupoOM.ItemsSource = Grupos;
            }
        }


        private void ModoBusqueda_Checked(object sender, RoutedEventArgs e)
        {
            modoActual = ModoPantalla.Busqueda;
            AplicarModo();
            LimpiarTodo(); 
        }
        private void ModoCaptura_Checked(object sender, RoutedEventArgs e)
        {
            modoActual = ModoPantalla.Captura;
            AplicarModo();
            LimpiarTodo(); 
        }
        private void AplicarModo()
        {
            if (gridOMPanel == null)
                return;

            if (modoActual == ModoPantalla.Busqueda)
            {
                borderTop.BorderBrush = new SolidColorBrush(Color.FromRgb(250, 162, 25));
                borderMiddleTop.BorderBrush = new SolidColorBrush(Color.FromRgb(250, 162, 25));
                borderMiddleLow.BorderBrush = new SolidColorBrush(Color.FromRgb(250, 162, 25));
                borderLow1.BorderBrush = new SolidColorBrush(Color.FromRgb(250, 162, 25));
                borderLow2.BorderBrush = new SolidColorBrush(Color.FromRgb(250, 162, 25));
                borderLow3.BorderBrush = new SolidColorBrush(Color.FromRgb(250, 162, 25));
                this.Background = new SolidColorBrush(Color.FromRgb(255, 243, 200));

                gridOMPanel.Visibility = Visibility.Collapsed;

                dgDetalle.IsReadOnly = true;

                btnBusqueda.Visibility = Visibility.Visible;
                btnGuardar.Visibility = Visibility.Collapsed;
                btnCalcular.Visibility = Visibility.Collapsed;

                panelDescuento.Visibility = Visibility.Collapsed;
                panelIVA.Visibility = Visibility.Collapsed;
            }
            else
            {

                borderTop.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                borderMiddleTop.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                borderMiddleLow.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                borderLow1.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                borderLow2.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                borderLow3.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                this.Background = new SolidColorBrush(Color.FromRgb(210, 255, 210));

                gridOMPanel.Visibility = Visibility.Visible;

                dgDetalle.IsReadOnly = false;

                btnBusqueda.Visibility = Visibility.Collapsed;
                btnGuardar.Visibility = Visibility.Visible;
                btnCalcular.Visibility = Visibility.Visible;

                panelDescuento.Visibility = Visibility.Visible;
                panelIVA.Visibility = Visibility.Visible;
            }
        }
        private void LimpiarTodo()
        {
            // 🔹 Validar que la ventana ya esté cargada
            if (!IsLoaded) return;

            // 🔹 DataGrid principal
            if (Detalles != null)
                Detalles.Clear();

            // 🔹 DataGrid OM
            if (listaOM != null)
                listaOM.Clear();

            // 🔹 TextBox
            if (txtProveedorId != null) txtProveedorId.Text = "";
            if (txtFactura != null) txtFactura.Text = "";
            if (txtAutorizacion != null) txtAutorizacion.Text = "";

            // 🔹 ComboBox
            if (cmbProveedor != null) cmbProveedor.SelectedIndex = -1;

            if (cmbDescuento != null) cmbDescuento.SelectedItem = 0;
            if (cmbIVA != null) cmbIVA.SelectedItem = 16;

            // 🔹 DatePicker
            if (dpFecha != null) dpFecha.SelectedDate = null;
            if (dpFechaLlegada != null) dpFechaLlegada.SelectedDate = null;

            // 🔹 Totales
            if (txtSubTotal != null) txtSubTotal.Text = "0.00";
            if (txtDescuento != null) txtDescuento.Text = "0.00";
            if (txtSubTotalFinal != null) txtSubTotalFinal.Text = "0.00";
            if (txtIVAImporte != null) txtIVAImporte.Text = "0.00";
            if (txtRetencion != null) txtRetencion.Text = "0.00";
            if (txtTotal != null) txtTotal.Text = "0.00";

            dgDetalle.SelectedItem = null;
            dgOM.SelectedItem = null;
            // 🔹 Limpiar configuración OM
            panelOMConfig.DataContext = null;

            cmbUnidadOM.SelectedIndex = -1;
            cmbFamiliaOM.SelectedIndex = -1;
            cmbGrupoOM.SelectedIndex = -1;
        }

    }


    public class DetalleOrden
    {
        public string Producto { get; set; } = "";
        public int Marca { get; set; }
        public string Descripcion { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal Costo { get; set; }
        public decimal Importe { get; set; }
        public string Localizacion { get; set; } = "";
        public int Venta { get; set; }
        public int Servicio { get; set; } = 0;  // 0 = desmarcado, 1 = marcado
        public decimal Descuento { get; set; }
        public decimal SubTotalSinDescuento { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Retencion { get; set; }
        public decimal PrecioAnterior { get; set; }
        public decimal PrecioNuevo { get; set; }
        public string Autorizacion { get; set; } = "";
        public DateTime? FechaOrden { get; set; }
        public DateTime? FechaLlegada { get; set; }
        public int OrdenId { get; set; }
    }
    public class ProductoOM
    {
        public string Producto { get; set; } = "";
        public string Descripcion { get; set; } ="";

        public string Marca { get; set; } = "";

        public decimal Costo { get; set; }
        public decimal CostoMayoreo { get; set; }
        public decimal CostoMexico { get; set; }

        public decimal Precio { get; set; }
        public decimal PrecioMayoreo { get; set; }

        public decimal Dolar { get; set; }

        public bool Activo { get; set; }
        public int UnidadId { get; set; }
        public int FamiliaId { get; set; }
        public int GrupoId { get; set; }
    }
}
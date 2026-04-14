using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GOVI_FACTURA.Models;
using GOVI_FACTURA.Services;
using Microsoft.Data.SqlClient;

namespace GOVI_FACTURA
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // variables globales
        private int _clienteBuenFin = 0;
        private int _buenFinActivo = 0;
        private string _condicionVenta = ""; // ✅ AQUÍ
        private List<Partida> partidas = new List<Partida>();

        // para cliente 
        public int CondicionVentaId { get; set; }
        public int FormaPagoId { get; set; }
        public int VendedorId { get; set; }
        public decimal LimiteCredito { get; set; }
        public decimal ImporteAutorizado { get; set; }
        public bool Bloqueado { get; set; }

        // para el producto 
      

        public MainWindow()
        {
            InitializeComponent();
            lblFecha.Text = DateTime.Now.ToString("dd/MM/yyyy");
            lblFolio.Text = "1928";
            CargarClientesEspeciales();
            CargarCombos();

            var service = new ClienteEspecialService();

            _clienteBuenFin = service.ObtenerClienteBuenFinBase();
            _buenFinActivo = service.ObtenerBuenFin();

           

        // para cliente 
        
           
    }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void BtnBuscarCliente_Click(object sender, RoutedEventArgs e)
        {
            BuscarClienteWindow ventana = new BuscarClienteWindow();

            if (ventana.ShowDialog() == true)
            {
                var clienteBasico = ventana.ClienteSeleccionado;

                if (clienteBasico != null)
                {
                    var service = new ClienteService();

                    // 🔥 TRAER CLIENTE COMPLETO (AQUI ESTABA EL PEDO)
                    var cliente = service.ObtenerCliente(clienteBasico.Id);

                    if (cliente == null)
                    {
                        MessageBox.Show("No se pudo cargar el cliente completo");
                        return;
                    }

                    // 🔥 VALIDAR BUEN FIN
                    if (cliente.Id == _clienteBuenFin && _buenFinActivo == 0)
                    {
                        MessageBox.Show("Cliente Buen Fin no está activo");
                        return;
                    }

                    txtCliente.Text = cliente.Id.ToString();

                    // 🔥 usar tu mismo método (no repetir código)
                    AplicarCliente(cliente);
                }
            }
        }

        private void txtCliente_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        private void txtCliente_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (string.IsNullOrWhiteSpace(txtCliente.Text))
                {
                    // 🔍 Abrir buscador
                    BtnBuscarCliente_Click(null, null);
                }
                else
                {
                    // 🔢 Buscar directo
                    BuscarClientePorId(txtCliente.Text);
                }
            }
        }

        private void BuscarClientePorId(string idTexto)
        {
            if (!int.TryParse(idTexto, out int idCliente))
            {
                MessageBox.Show("Cliente inválido");
                return;
            }

            var clienteService = new ClienteService();
            var cliente = clienteService.ObtenerCliente(idCliente);
            


            if (cliente == null)
            {
                MessageBox.Show("Cliente no encontrado");
                txtCliente.SelectAll(); // 🔥 aquí
                txtCliente.Focus();     // 🔥 aquí
                return;
               // txtCliente.Clear();
               // return;
            }
            // 🔥 VALIDAR BUEN FIN
            if (cliente.Id == _clienteBuenFin && _buenFinActivo == 0)
            {
                MessageBox.Show("Cliente Buen Fin no está activo");
                txtCliente.SelectAll();
                txtCliente.Focus();
                return;
            }

            // 🔥 llenar UI
            txtCliente.Text = cliente.Id.ToString();
            AplicarCliente(cliente);
            // lblClienteNombre.Text = cliente.Nombre;
            // lblClienteRFC.Text = cliente.RFC;
            // lblClienteDireccion.Text = cliente.Direccion;

            txtCodigoProducto.Focus();
        }
        // carga clienes especiales
        private void CargarClientesEspeciales()
        {
            var service = new ClienteEspecialService();

            int buenFin = service.ObtenerBuenFin();

            if (buenFin == 0)
            {
                lblBuenFin.Text = "NO ACTIVO";
            }
            else
            {
                lblBuenFin.Text = buenFin.ToString();
            }

            lblMSI.Text = service.ObtenerMSI().ToString();
            lblConfort.Text = service.ObtenerConfort().ToString();
            lblCDO2.Text = service.ObtenerContado2().ToString();
        }
        // METODO PARA CARGAR LOS COMBOS DE LOS CATALOGOS
        private void CargarCombos()
        {
            var service = new ClienteEspecialService();

            //-   cmbFormaPago.ItemsSource = service.ObtenerFormaPago();
            //-   cmbUsoCFDI.ItemsSource = service.ObtenerUsoCFDI();

            //-   cmbFormaPago.DisplayMemberPath = "Descripcion";
            //-   cmbFormaPago.SelectedValuePath = "Id";

            //-   cmbUsoCFDI.DisplayMemberPath = "Descripcion";
            //-   cmbUsoCFDI.SelectedValuePath = "Id";
        }

        private void BtnSalir_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // no haces nada, solo dejas que cierre
        }
        //
        // clientes
        private void AplicarCliente(Cliente cliente)
        {
            // =========================
            // 1. DATOS BÁSICOS
            // =========================
            lblClienteNombre.Text = cliente.Nombre;
            lblClienteRFC.Text = cliente.RFC;
            lblClienteDireccion.Text = cliente.Direccion;

            lblTelefono.Text = cliente.Telefono ?? "";

            var service = new ClienteService();

            lblEstado.Text = cliente.Estado;
            lblCiudad.Text = cliente.Ciudad;
            lblCondicion.Text = service.ObtenerCondicionVenta(cliente.Id);
            lblTelefono.Text = cliente.Telefono;

            lblLimite.Text = (cliente.LimiteCredito + cliente.ImporteAutorizado).ToString("C");

            var saldo = service.ObtenerSaldo(cliente.Id);
            lblSaldo.Text = saldo.ToString("C");

            // segunda pestaña
            lblUsoCFDI.Text = cliente.UsoCFDI2;
            lblRegimenFiscal.Text = cliente.RegimenFiscal;

            lblVendedorId.Text = cliente.VendedorId.ToString();
            lblVendedorNombre.Text = cliente.VendedorNombre;

            lblRazonSocial.Text = cliente.RazonSocial;

            lblConsignado1.Text = cliente.Consignado1;
            lblConsignado2.Text = cliente.Consignado2;
            // =========================
            // TAB 3 - MONEDERO
            // =========================

            //var service = new ClienteService();

            // 🔥 traer tarjeta aparte
            int tarjeta = service.ObtenerTarjeta(cliente.Id);

            if (tarjeta == 0)
            {
                lblTarjeta.Text = "SIN TARJETA";
                lblSaldoMonedero.Text = "0.00";
                lblSaldoDisponible.Text = "0.00";
                return;
            }

            lblTarjeta.Text = tarjeta.ToString();

            // 🔥 usarla
            var saldoMonedero = service.ObtenerSaldoMonedero(cliente.Id, tarjeta);
            var saldoDisponible = service.ObtenerSaldoDisponible(cliente.Id, tarjeta);

            lblSaldoMonedero.Text = saldoMonedero.ToString("N2");
            lblSaldoDisponible.Text = saldoDisponible.ToString("N2");

            // =========================
            // 2. TIPO VENTA
            // =========================
            var clienteService = new ClienteService();

            bool esClienteContado = clienteService.EsClienteContado(cliente.Id);

            string tipoVenta = esClienteContado ? "CONTADO" : "CREDITO";

            //-   txtTipoVenta.Text = tipoVenta;

            // =========================
            // 4. COMPORTAMIENTO UI
            // =========================

            if (esClienteContado)
            {
                var ventana = new CapturaClienteWindow();

                if (ventana.ShowDialog() == true)
                {
                    var datos = ventana.ClienteCapturado;

                    // 👉 aquí reemplazas lo que vino del cliente
                    lblClienteNombre.Text = datos.Nombre;
                    lblClienteDireccion.Text = datos.Direccion;
                    lblRazonSocial.Text = datos.RazonSocial;
                    lblUsoCFDI.Text = datos.UsoCFDI;
                    lblRegimenFiscal.Text = datos.RegimenFiscal;
                }
            }
            else
            {
               
            }


        }

        //
        // para las partidas
        private void BtnAgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            AgregarProducto();
        }

        private void txtCodigoProducto_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AgregarProducto();
            }
        }
        //
        private void AgregarProducto()
        {
            var service = new ProductoService();
            var producto = service.BuscarProducto(txtCodigoProducto.Text);

            if (producto == null)
            {
                MessageBox.Show("Producto no existe");
                return;
            }

            // 🔥 BUSCAR SI YA EXISTE
            var existente = partidas.FirstOrDefault(p => p.Codigo == producto.Id);

            if (existente != null)
            {
                existente.Cantidad += 1;
            }
            else
            {
                partidas.Add(new Partida
                {
                    Codigo = producto.Id,
                    Descripcion = producto.Descripcion,
                    Cantidad = 1,
                    Precio = 100 // luego le metes precio real
                });
            }

            dgProductos.ItemsSource = null;
            dgProductos.ItemsSource = partidas;

            CalcularTotales();

            txtCodigoProducto.Clear();
            txtCodigoProducto.Focus();
        }
        private void CalcularTotales()
        {
            decimal subtotal = partidas.Sum(p => p.Importe);
            decimal iva = subtotal * 0.16m;
            decimal total = subtotal + iva;

            lblSubtotal.Text = subtotal.ToString("C");
            lblIVA.Text = iva.ToString("C");
            lblTotal.Text = total.ToString("C");
        }

        // elimina partida
        private void dgProductos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgProductos.SelectedItem is Partida p)
            {
                partidas.Remove(p);
                dgProductos.ItemsSource = null;
                dgProductos.ItemsSource = partidas;
                CalcularTotales();
            }
        }

        //siempre deja estos dos 
    }
}
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
                var cliente = ventana.ClienteSeleccionado;

                if (cliente != null)
                {
                    // 🔥 VALIDAR BUEN FIN
                    if (cliente.Id == _clienteBuenFin && _buenFinActivo == 0)
                    {
                        MessageBox.Show("Cliente Buen Fin no está activo");
                        return;
                    }

                    txtCliente.Text = cliente.Id.ToString();
                    lblClienteNombre.Text = cliente.Nombre;
                    lblClienteRFC.Text = cliente.RFC;
                    lblClienteDireccion.Text = cliente.Direccion;
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
            lblClienteNombre.Text = cliente.Nombre;
            lblClienteRFC.Text = cliente.RFC;
            lblClienteDireccion.Text = cliente.Direccion;

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

            cmbFormaPago.ItemsSource = service.ObtenerFormaPago();
            cmbUsoCFDI.ItemsSource = service.ObtenerUsoCFDI();

            cmbFormaPago.DisplayMemberPath = "Descripcion";
            cmbFormaPago.SelectedValuePath = "Id";

            cmbUsoCFDI.DisplayMemberPath = "Descripcion";
            cmbUsoCFDI.SelectedValuePath = "Id";
        }
        //

        //siempre deja estos dos 
    }
}
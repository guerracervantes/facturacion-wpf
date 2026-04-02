using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GOVI_FACTURA.Models;
using GOVI_FACTURA.Services;

namespace GOVI_FACTURA
{
    public partial class BuscarClienteWindow : Window
    {
        public Cliente ClienteSeleccionado { get; set; }

        private List<Cliente> listaClientes = new List<Cliente>();

        private ClienteService service = new ClienteService(); // 🔥 usamos el service

        public BuscarClienteWindow()
        {
            InitializeComponent();
        }

        // 🔍 BUSQUEDA DINAMICA
        private void TxtBuscar_KeyUp(object sender, KeyEventArgs e)
        {
            listaClientes = service.BuscarClientes(txtBuscar.Text);
            dgClientes.ItemsSource = listaClientes;
        }

        // ✅ BOTON SELECCIONAR
        private void BtnSeleccionar_Click(object sender, RoutedEventArgs e)
        {
            ClienteSeleccionado = dgClientes.SelectedItem as Cliente;

            if (ClienteSeleccionado != null)
            {
                this.DialogResult = true;
            }
        }

        // 🖱️ DOBLE CLICK
        private void dgClientes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BtnSeleccionar_Click(sender, e);
        }
    }
}
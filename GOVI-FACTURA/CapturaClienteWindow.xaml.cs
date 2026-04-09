using System;
using System.Collections.Generic;
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
using GOVI_FACTURA.Models;

namespace GOVI_FACTURA
{
    /// <summary>
    /// Lógica de interacción para CapturaClienteWindow.xaml
    /// </summary>
    public partial class CapturaClienteWindow : Window
    {
        public Cliente ClienteCapturado { get; set; }

        public CapturaClienteWindow()
        {
            InitializeComponent();
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            ClienteCapturado = new Cliente
            {
                Nombre = txtNombre.Text,
                Direccion = txtDireccion.Text,
                Colonia = txtColonia.Text,
                Estado = txtEstado.Text,
                CodigoPostal = txtCP.Text,
                Telefono = txtTelefono.Text,
                Email = txtEmail.Text,
                RazonSocial = txtRazonSocial.Text,
                Consignado1 = txtConsignado.Text,
                UsoCFDI = cmbUsoCFDI.Text,
                RegimenFiscal = cmbRegimen.Text
            };

            this.DialogResult = true;
            this.Close();
        }
    }
}

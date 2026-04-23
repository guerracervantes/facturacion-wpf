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

namespace GOVI_FACTURA
{

    public partial class MenuWindow : Window
    {

        private MainWindow ventanaFacturacion;
        private ComprasTerceros ventanaComprasTerceros;
        private ComprasFiliales ventanaComprasFiliales;
        private ComprasEntradaAlmacen ventanaComprasEntradaAlamacen;

        public MenuWindow()
        {
            InitializeComponent();
        }

        private void Salir_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


        private void BtnCompras_Click(object sender, RoutedEventArgs e)
        {

        }
        private void MenuComprasTerceros_Click(object sender, RoutedEventArgs e)
        {
            if (ventanaComprasTerceros == null || !ventanaComprasTerceros.IsLoaded)
            {
                ventanaComprasTerceros = new ComprasTerceros();
                ventanaComprasTerceros.Closed += (s, args) => ventanaComprasTerceros = null;
                ventanaComprasTerceros.Show();
            }
            else
            {
                ventanaComprasTerceros.Activate();
            }
        }
        private void MenuComprasFiliales_Click(object sender, RoutedEventArgs e)
        {
            if (ventanaComprasFiliales == null || !ventanaComprasFiliales.IsLoaded)
            {
                ventanaComprasFiliales = new ComprasFiliales();
                ventanaComprasFiliales.Closed += (s, args) => ventanaComprasFiliales = null;
                ventanaComprasFiliales.Show();
            }
            else
            {
                ventanaComprasFiliales.Activate();
            }
        }
        private void MenuComprasEntradaAlmacen_Click(object sender, RoutedEventArgs e)
        {
            if (ventanaComprasEntradaAlamacen == null || !ventanaComprasEntradaAlamacen .IsLoaded)
            {
                ventanaComprasEntradaAlamacen = new ComprasEntradaAlmacen();
                ventanaComprasEntradaAlamacen.Closed += (s, args) => ventanaComprasEntradaAlamacen = null;
                ventanaComprasEntradaAlamacen.Show();
            }
            else
            {
                ventanaComprasEntradaAlamacen.Activate();
            }
        }

        private void BtnFacturacion_Click(object sender, RoutedEventArgs e)
        {
            if (ventanaFacturacion == null || !ventanaFacturacion.IsLoaded)
            {
                ventanaFacturacion = new MainWindow();
                ventanaFacturacion.Show();
            }
            else
            {
                ventanaFacturacion.Activate(); // la trae al frente
            }
        }
    
    }
}

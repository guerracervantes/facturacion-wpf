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
    /// <summary>
    /// Lógica de interacción para MenuWindow.xaml
    /// </summary>
    public partial class MenuWindow : Window
    {
        private MainWindow ventanaFacturacion;
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

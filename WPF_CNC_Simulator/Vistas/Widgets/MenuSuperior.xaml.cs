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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPF_CNC_Simulator.Vistas.Widgets
{
    /// <summary>
    /// Lógica de interacción para MenuSuperior.xaml
    /// </summary>
    public partial class MenuSuperior : UserControl
    {
        public MenuSuperior()
        {
            InitializeComponent();
        }

        private void BtnImportar_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.ImportarModeloSTL();
        }

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.ExportarCodigoG();
        }

        private void BtnConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidad de configuración en desarrollo",
                "Configuración", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

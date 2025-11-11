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


        private void BotonImportar_Click(object sender, RoutedEventArgs e)
        {
            // El evento viene del BotonConIcono, podemos ejecutar nuestra lógica
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.ImportarModeloSTL();
        }

        private void BotonExportar_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.ExportarCodigoG();
        }

        private void BotonConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidad de configuración en desarrollo",
                "Configuración", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

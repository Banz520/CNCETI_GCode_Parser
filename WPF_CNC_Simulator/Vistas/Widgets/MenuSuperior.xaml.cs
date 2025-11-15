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
            try
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    var ventanaConfiguracion = new VentanaConfiguracion(
                        mainWindow.Simulador3d,
                        mainWindow.EditorGCode
                    );
                    ventanaConfiguracion.Owner = mainWindow;
                    ventanaConfiguracion.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir configuración:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

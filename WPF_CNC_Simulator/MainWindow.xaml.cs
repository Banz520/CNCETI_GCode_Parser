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
using WPF_CNC_Simulator.Views;
using WPF_CNC_Simulator.Vistas.Widgets;

namespace WPF_CNC_Simulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void OpenSlicerWindow_Click(object sender, RoutedEventArgs e)
        {
            var slicerWindow = new SlicerWindow();
            slicerWindow.ShowDialog(); // O .Show() si quieres que no sea modal
        }
        // Ejemplo de cómo controlar los modelos desde otra parte de tu código
        private void MoverMaquinaCNC()
        {
            // Mover la máquina CNC base
            Simulador3d.MoverCNC(x: 2.0, y: 1.5, z: 0.3, rotacionZ: 30);
        }

        private void MoverPiezaImportada()
        {
            // Mover el modelo importado
            Simulador3d.MoverModeloImportado(x: 1.0, y: -2.0, z: 0.5, rotacionX: 15);
        }
    }
}
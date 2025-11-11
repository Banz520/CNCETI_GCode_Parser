using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using WPF_CNC_Simulator.Services;
using WPF_CNC_Simulator.Views;
using WPF_CNC_Simulator.Vistas.Widgets;

namespace WPF_CNC_Simulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SlicerService _slicerService;

        public MainWindow()
        {
            InitializeComponent();
            InicializarSlicerService();
        }

        private void InicializarSlicerService()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var slic3rPath = Path.Combine(baseDir, "Slic3r", "Slic3r-console.exe");

                if (File.Exists(slic3rPath))
                {
                    _slicerService = new SlicerService(slic3rPath);
                }
                else
                {
                    // Intentar buscar en otras ubicaciones comunes
                    var alternativePaths = new[]
                    {
                        Path.Combine(baseDir, "Slic3r", "slic3r-console.exe"),
                        Path.Combine(baseDir, "Slic3r", "slic3r.exe"),
                        @"C:\Program Files\Slic3r\slic3r-console.exe",
                        @"C:\Program Files (x86)\Slic3r\slic3r-console.exe"
                    };

                    foreach (var path in alternativePaths)
                    {
                        if (File.Exists(path))
                        {
                            _slicerService = new SlicerService(path);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo inicializar Slic3r: {ex.Message}\n\n" +
                    "La funcionalidad de conversión STL→G-code no estará disponible.",
                    "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OpenSlicerWindow_Click(object sender, RoutedEventArgs e)
        {
            var slicerWindow = new SlicerWindow();
            slicerWindow.ShowDialog();
        }

        /// <summary>
        /// Importar modelo STL desde el menú superior
        /// </summary>
        public void ImportarModeloSTL()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Archivos STL (*.stl)|*.stl|Todos los archivos (*.*)|*.*",
                Title = "Importar modelo STL"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Cargar en el simulador 3D
                    Simulador3d.CargarModeloImportado(openFileDialog.FileName);

                    // Preguntar si desea generar G-code
                    var resultado = MessageBox.Show(
                        "Modelo STL cargado correctamente.\n\n¿Desea generar el código G ahora?",
                        "Modelo Importado",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        GenerarGCodeDesdeSTL(openFileDialog.FileName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al importar modelo:\n{ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Exportar código G desde el menú superior
        /// </summary>
        public void ExportarCodigoG()
        {
            try
            {
                var codigoG = EditorGCode.GCodeTexto;

                if (string.IsNullOrWhiteSpace(codigoG))
                {
                    MessageBox.Show("No hay código G para exportar.\n\nPrimero genera o carga un archivo G-code.",
                        "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Archivos G-code (*.gcode)|*.gcode|Archivos NC (*.nc)|*.nc|Todos los archivos (*.*)|*.*",
                    Title = "Exportar Código G",
                    FileName = "codigo_g.gcode"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveDialog.FileName, codigoG, System.Text.Encoding.UTF8);
                    MessageBox.Show($"Código G exportado correctamente a:\n{saveDialog.FileName}",
                        "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar código G:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Generar G-code desde un archivo STL
        /// </summary>
        private async void GenerarGCodeDesdeSTL(string stlPath)
        {
            if (_slicerService == null)
            {
                MessageBox.Show("El servicio Slic3r no está disponible.\n\n" +
                    "Asegúrate de tener Slic3r instalado en la carpeta Slic3r del proyecto.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Generar ruta de salida
                var outputPath = Path.ChangeExtension(stlPath, ".gcode");

                // Crear ventana de progreso
                var progressWindow = new Window
                {
                    Title = "Generando G-code",
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize
                };

                var progressText = new System.Windows.Controls.TextBlock
                {
                    Text = "Iniciando conversión...",
                    Margin = new Thickness(20),
                    TextWrapping = TextWrapping.Wrap
                };

                progressWindow.Content = progressText;
                progressWindow.Show();

                // Configuración personalizada
                var settings = new SlicerSettings
                {
                    LayerHeight = 0.2,
                    FillDensity = 20,
                    Perimeters = 3,
                    PrintSpeed = 60,
                    ExtruderTemperature = 200,
                    BedTemperature = 60
                };

                // Ejecutar slicing
                var result = await _slicerService.SliceWithCustomSettings(
                    stlPath,
                    outputPath,
                    settings,
                    progress => Dispatcher.Invoke(() => progressText.Text += "\n" + progress)
                );

                progressWindow.Close();

                if (result.Success)
                {
                    // Cargar el G-code generado en el editor
                    EditorGCode.CargarArchivo(result.OutputFilePath);

                    MessageBox.Show(
                        $"✅ G-code generado exitosamente\n\n" +
                        $"Archivo: {Path.GetFileName(result.OutputFilePath)}\n" +
                        $"Tamaño: {result.FileSize / 1024:F2} KB\n" +
                        $"Tiempo: {result.ProcessingTime.TotalSeconds:F2} segundos",
                        "Conversión Exitosa",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        $"❌ Error al generar G-code:\n\n{result.ErrorMessage}",
                        "Error de Conversión",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Métodos de ejemplo para controlar los modelos
        private void MoverMaquinaCNC()
        {
            Simulador3d.MoverCNC(x: 2.0, y: 1.5, z: 0.3, rotacionZ: 30);
        }

        private void MoverPiezaImportada()
        {
            Simulador3d.MoverModeloImportado(x: 1.0, y: -2.0, z: 0.5, rotacionX: 15);
        }
    }
}

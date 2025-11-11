using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using WPF_CNC_Simulator.Services;


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


        /// <summary>
        /// Importar modelo STL desde el menú superior
        /// </summary>
        public void ImportarModeloSTL()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Archivos STL (*.stl)|*.stl|Todos los archivos (*.*)|*.*",
                Title = "Importar modelo STL para fresado CNC"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Cargar en el simulador 3D
                    Simulador3d.CargarModeloImportado(openFileDialog.FileName);

                    // Preguntar si desea generar G-code
                    var resultado = MessageBox.Show(
                        "Modelo STL cargado correctamente.\n\n" +
                        "¿Desea generar el código G-code para fresadora CNC?\n\n" +
                        "Configuración:\n" +
                        "• Área: 300x300x150mm\n" +
                        "• Fresa: Ø5mm\n" +
                        "• Husillo: 8000 RPM\n" +
                        "• Material: Madera",
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
                    Title = "Exportar Código G para Fresadora CNC",
                    FileName = "fresado_cnc.gcode"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveDialog.FileName, codigoG, System.Text.Encoding.UTF8);
                    MessageBox.Show($"Código G exportado correctamente a:\n{saveDialog.FileName}\n\n" +
                        "El código está optimizado para:\n" +
                        "• Fresadora CNC de 3 ejes\n" +
                        "• Fresa Ø5mm\n" +
                        "• Corte de madera",
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
        /// Generar G-code desde un archivo STL con configuración para fresadora CNC
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
                var outputPath = Path.ChangeExtension(stlPath, ".gcode");

                // Crear ventana de progreso
                var progressWindow = new Window
                {
                    Title = "Generando G-code para Fresadora CNC",
                    Width = 600,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize
                };

                var scrollViewer = new System.Windows.Controls.ScrollViewer
                {
                    VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto
                };

                var progressText = new System.Windows.Controls.TextBlock
                {
                    Text = "Iniciando conversión STL → G-code...\n",
                    Margin = new Thickness(20),
                    TextWrapping = TextWrapping.Wrap,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    FontSize = 11
                };

                scrollViewer.Content = progressText;
                progressWindow.Content = scrollViewer;
                progressWindow.Show();

                // Configuración específica para fresadora CNC
                var settings = new CNCMillingSettings
                {
                    // Parámetros de la máquina
                    WorkAreaX = 300.0,
                    WorkAreaY = 300.0,
                    WorkAreaZ = 150.0,
                    StepResolution = 0.1,

                    // Herramienta
                    ToolDiameter = 5.0,
                    MaxSpindleSpeed = 10000,
                    WorkingSpindleSpeed = 8000,

                    // Parámetros de corte para madera
                    CuttingDepth = 0.5,            // 0.5mm por pasada
                    CuttingFeedrate = 1500.0,      // 1500 mm/min velocidad de corte
                    PlungeFeedrate = 500.0,        // 500 mm/min penetración
                    RapidFeedrate = 3000.0,        // 3000 mm/min movimientos rápidos

                    // Mecanizado
                    Perimeters = 1,                // Un solo contorno
                    FillDensity = 10,              // 10% relleno
                    FillPattern = "rectilinear",   // Patrón rectilíneo
                    SafeHeight = 50.0,             // 50mm altura segura

                    Material = "Madera"
                };

                // Ejecutar slicing con configuración CNC
                var result = await _slicerService.SliceWithCustomSettings(
                    stlPath,
                    outputPath,
                    settings,
                    progress => Dispatcher.Invoke(() =>
                    {
                        progressText.Text += progress + "\n";
                        scrollViewer.ScrollToEnd();
                    })
                );

                progressWindow.Close();

                if (result.Success)
                {
                    // Cargar el G-code generado en el editor
                    EditorGCode.CargarArchivo(result.OutputFilePath);

                    MessageBox.Show(
                        $"✅ G-code generado exitosamente para fresadora CNC\n\n" +
                        $"═══════════════════════════════════════\n" +
                        $"ESPECIFICACIONES:\n" +
                        $"═══════════════════════════════════════\n" +
                        $"Archivo: {Path.GetFileName(result.OutputFilePath)}\n" +
                        $"Tamaño: {result.FileSize / 1024:F2} KB\n" +
                        $"Tiempo: {result.ProcessingTime.TotalSeconds:F2} segundos\n\n" +
                        $"CONFIGURACIÓN CNC:\n" +
                        $"═══════════════════════════════════════\n" +
                        $"• Área de trabajo: 300x300x150mm\n" +
                        $"• Herramienta: Fresa Ø5mm\n" +
                        $"• Velocidad husillo: 8000 RPM\n" +
                        $"• Profundidad corte: 0.5mm/pasada\n" +
                        $"• Velocidad corte: 1500 mm/min\n" +
                        $"• Velocidad rápida: 3000 mm/min\n" +
                        $"• Material: Madera\n" +
                        $"• Precisión: 0.1mm/paso",
                        "Conversión Exitosa - Fresadora CNC",
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

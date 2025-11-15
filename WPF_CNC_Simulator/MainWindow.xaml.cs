using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using WPF_CNC_Simulator.Services;
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
            ConfigurarControles();
            InicializarConexionesEntreWidgets();
        }

        private void ConfigurarControles()
        {
            try
            {
                if (Simulador3d != null)
                {
                    Simulador3d.ConfigurarControlPropiedades();
                    Simulador3d.PropiedadCambiada += OnPropiedadPiezaCambiada;

                    // Conectar el editor con el simulador
                    Simulador3d.SetEditorGCode(EditorGCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configurando controles: {ex.Message}");
            }
        }

        private void OnPropiedadPiezaCambiada(string propiedad, double valor)
        {
            Console.WriteLine($"Propiedad {propiedad} cambiada a: {valor}");
        }

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
                    Simulador3d.CargarModeloImportado(openFileDialog.FileName);
                    Simulador3d.ActualizarControlPropiedades();

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

        private void InicializarSlicerService()
        {
            try
            {
                _slicerService = new SlicerService();
                Console.WriteLine("SlicerService (interno) inicializado correctamente.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo inicializar el servicio de conversión:\n{ex.Message}\n\n" +
                    "La funcionalidad de conversión STL→G-code no estará disponible.",
                    "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                _slicerService = null;
            }
        }

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

        private async void GenerarGCodeDesdeSTL(string stlPath)
        {
            if (_slicerService == null)
            {
                MessageBox.Show("El servicio de conversión no está disponible.\n\n" +
                    "Asegúrate de que el servicio esté inicializado.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var outputPath = Path.ChangeExtension(stlPath, ".gcode");

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

                var settings = new CNCMillingSettings
                {
                    WorkAreaX = 300.0,
                    WorkAreaY = 300.0,
                    WorkAreaZ = 150.0,
                    StepResolution = 0.1,
                    ToolDiameter = 5.0,
                    MaxSpindleSpeed = 10000,
                    WorkingSpindleSpeed = 8000,
                    CuttingDepth = 0.5,
                    CuttingFeedrate = 1500.0,
                    PlungeFeedrate = 500.0,
                    RapidFeedrate = 3000.0,
                    Perimeters = 1,
                    FillDensity = 10,
                    FillPattern = "rectilinear",
                    SafeHeight = 50.0,
                    Material = "Madera"
                };

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

        private void InicializarConexionesEntreWidgets()
        {
            try
            {
                Simulador3d.ConfigurarControlPropiedades();

                Simulador3d.ControlReproduccion.ReproducirClick += ControlReproduccion_ReproducirClick;
                Simulador3d.ControlReproduccion.PausarClick += ControlReproduccion_PausarClick;
                Simulador3d.ControlReproduccion.SiguienteClick += ControlReproduccion_SiguienteClick;
                Simulador3d.ControlReproduccion.AnteriorClick += ControlReproduccion_AnteriorClick;

                EditorGCode.GCodeAplicado += (sender, ruta) =>
                {
                    Console.WriteLine($"Código G guardado en: {ruta}");
                };

                Console.WriteLine("Widgets conectados exitosamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inicializando conexiones: {ex.Message}");
                MessageBox.Show($"Error al inicializar la aplicación: {ex.Message}",
                    "Error de Inicialización", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ControlReproduccion_ReproducirClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string codigoG = EditorGCode.GCodeTexto;

                if (string.IsNullOrWhiteSpace(codigoG))
                {
                    MessageBox.Show("No hay código G para reproducir.\nPor favor, carga o genera un archivo G-code primero.",
                        "Sin código G", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Simulador3d.IniciarAnimacionGCode(codigoG);
                Console.WriteLine("Animación iniciada desde MainWindow");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al iniciar reproducción: {ex.Message}");
                MessageBox.Show($"Error al iniciar la reproducción: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ControlReproduccion_PausarClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Simulador3d.PausarAnimacionGCode();
                Console.WriteLine("Animación pausada desde MainWindow");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al pausar: {ex.Message}");
            }
        }

        private void ControlReproduccion_SiguienteClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Simulador3d.AvanzarSiguienteLinea();
                Console.WriteLine("Avanzar a siguiente línea");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al avanzar: {ex.Message}");
            }
        }

        private void ControlReproduccion_AnteriorClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Simulador3d.RetrocederLineaAnterior();
                Console.WriteLine("Retroceder a línea anterior");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al retroceder: {ex.Message}");
            }
        }
    }
}

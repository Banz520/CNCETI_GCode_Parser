
//using Microsoft.Win32;
//using System;
//using System.IO;
//using System.Windows;
//using WPF_CNC_Simulator.Services;
//using WPF_CNC_Simulator.Vistas.Widgets;


//namespace WPF_CNC_Simulator
//{
//    /// <summary>
//    /// Interaction logic for MainWindow.xaml
//    /// </summary>
//    public partial class MainWindow : Window
//    {
//        private SlicerService _slicerService;

//        public MainWindow()
//        {
//            InitializeComponent();
//            InicializarSlicerService();
//            ConfigurarControles();
//            InicializarConexionesEntreWidgets();
//        }


//        private void ConfigurarControles()
//        {
//            try
//            {
//                // Configurar la referencia del simulador en el control de propiedades
//                if (Simulador3d != null)
//                {
//                    // Asignar la referencia del simulador al control de propiedades interno
//                    Simulador3d.ConfigurarControlPropiedades();

//                    // Suscribirse a eventos si es necesario
//                    Simulador3d.PropiedadCambiada += OnPropiedadPiezaCambiada;
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error configurando controles: {ex.Message}");
//            }
//        }

//        private void OnPropiedadPiezaCambiada(string propiedad, double valor)
//        {
//            // Opcional: Puedes agregar lógica adicional aquí cuando cambien las propiedades
//            Console.WriteLine($"Propiedad {propiedad} cambiada a: {valor}");
//        }
//        public void ImportarModeloSTL()
//        {
//            var openDialog = new OpenFileDialog
//            {
//                Filter = "Archivos STL (*.stl)|*.stl",
//                Title = "Importar modelo STL"
//            };

//            if (openDialog.ShowDialog() == true)
//            {
//                try
//                {
//                    // Cargar en simulador 3D
//                    Simulador3d.CargarModeloImportado(openDialog.FileName);
//                    Simulador3d.ActualizarControlPropiedades();

//                    // Preguntar si generar G-code
//                    if (MessageBox.Show(
//                        "Modelo cargado.\n¿Deseas generar el G-code ahora?",
//                        "STL Importado",
//                        MessageBoxButton.YesNo,
//                        MessageBoxImage.Question
//                    ) == MessageBoxResult.Yes)
//                    {
//                        GenerarGCodeDesdeSTL(openDialog.FileName);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    MessageBox.Show($"Error al cargar STL:\n{ex.Message}",
//                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//                }
//            }
//        }

//        /*
//        /// <summary>
//        /// Importar modelo STL desde el menú superior
//        /// </summary>
//        public void ImportarModeloSTL()
//        {
//            var openFileDialog = new OpenFileDialog
//            {
//                Filter = "Archivos STL (*.stl)|*.stl|Todos los archivos (*.*)|*.*",
//                Title = "Importar modelo STL para fresado CNC"
//            };

//            if (openFileDialog.ShowDialog() == true)
//            {
//                try
//                {
//                    // Cargar en el simulador 3D
//                    Simulador3d.CargarModeloImportado(openFileDialog.FileName);

//                    // Actualizar el control de propiedades después de cargar el modelo
//                    Simulador3d.ActualizarControlPropiedades();

//                    // Preguntar si desea generar G-code
//                    var resultado = MessageBox.Show(
//                        "Modelo STL cargado correctamente.\n\n" +
//                        "¿Desea generar el código G-code para fresadora CNC?\n\n" +
//                        "Configuración:\n" +
//                        "• Área: 300x300x150mm\n" +
//                        "• Fresa: Ø5mm\n" +
//                        "• Husillo: 8000 RPM\n" +
//                        "• Material: Madera",
//                        "Modelo Importado",
//                        MessageBoxButton.YesNo,
//                        MessageBoxImage.Question);

//                    if (resultado == MessageBoxResult.Yes)
//                    {
//                        GenerarGCodeDesdeSTL(openFileDialog.FileName);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    MessageBox.Show($"Error al importar modelo:\n{ex.Message}",
//                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//                }
//            }
//        }
//        */
//        private void InicializarSlicerService()
//        {
//            try
//            {
//                var config = new CNCConfig
//                {
//                    WorkAreaX = 300,
//                    WorkAreaY = 300,
//                    WorkAreaZ = 100,
//                    MaxSpindleRPM = 10000,
//                    MinStepResolution = 0.1
//                };

//                _slicerService = new SlicerService(config);
//                Console.WriteLine("SlicerService (CAM interno) inicializado correctamente.");
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"No se pudo inicializar el slicer interno:\n{ex.Message}",
//                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        /*
//        private void InicializarSlicerService()
//        {
//            try
//            {
//                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
//                var slic3rPath = Path.Combine(baseDir, "Slic3r", "Slic3r-console.exe");

//                if (File.Exists(slic3rPath))
//                {
//                    _slicerService = new SlicerService(slic3rPath);
//                }
//                else
//                {
//                    var alternativePaths = new[]
//                    {
//                        Path.Combine(baseDir, "Slic3r", "slic3r-console.exe"),
//                        Path.Combine(baseDir, "Slic3r", "slic3r.exe"),
//                        @"C:\Program Files\Slic3r\slic3r-console.exe",
//                        @"C:\Program Files (x86)\Slic3r\slic3r-console.exe"
//                    };

//                    foreach (var path in alternativePaths)
//                    {
//                        if (File.Exists(path))
//                        {
//                            _slicerService = new SlicerService(path);
//                            break;
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"No se pudo inicializar Slic3r: {ex.Message}\n\n" +
//                    "La funcionalidad de conversión STL→G-code no estará disponible.",
//                    "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
//            }
//        }
//        */
//        /*
//        /// <summary>
//        /// Importar modelo STL desde el menú superior
//        /// </summary>
//        public void ImportarModeloSTL()
//        {
//            var openFileDialog = new OpenFileDialog
//            {
//                Filter = "Archivos STL (*.stl)|*.stl|Todos los archivos (*.*)|*.*",
//                Title = "Importar modelo STL para fresado CNC"
//            };

//            if (openFileDialog.ShowDialog() == true)
//            {
//                try
//                {
//                    // Cargar en el simulador 3D
//                    Simulador3d.CargarModeloImportado(openFileDialog.FileName);

//                    // Preguntar si desea generar G-code
//                    var resultado = MessageBox.Show(
//                        "Modelo STL cargado correctamente.\n\n" +
//                        "¿Desea generar el código G-code para fresadora CNC?\n\n" +
//                        "Configuración:\n" +
//                        "• Área: 300x300x150mm\n" +
//                        "• Fresa: Ø5mm\n" +
//                        "• Husillo: 8000 RPM\n" +
//                        "• Material: Madera",
//                        "Modelo Importado",
//                        MessageBoxButton.YesNo,
//                        MessageBoxImage.Question);

//                    if (resultado == MessageBoxResult.Yes)
//                    {
//                        GenerarGCodeDesdeSTL(openFileDialog.FileName);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    MessageBox.Show($"Error al importar modelo:\n{ex.Message}",
//                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//                }
//            }
//        }
//        */

//        /// <summary>
//        /// Exportar código G desde el menú superior
//        /// </summary>
//        public void ExportarCodigoG()
//        {
//            try
//            {
//                var codigoG = EditorGCode.GCodeTexto;

//                if (string.IsNullOrWhiteSpace(codigoG))
//                {
//                    MessageBox.Show("No hay código G para exportar.\n\nPrimero genera o carga un archivo G-code.",
//                        "Información", MessageBoxButton.OK, MessageBoxImage.Information);
//                    return;
//                }

//                var saveDialog = new SaveFileDialog
//                {
//                    Filter = "Archivos G-code (*.gcode)|*.gcode|Archivos NC (*.nc)|*.nc|Todos los archivos (*.*)|*.*",
//                    Title = "Exportar Código G para Fresadora CNC",
//                    FileName = "fresado_cnc.gcode"
//                };

//                if (saveDialog.ShowDialog() == true)
//                {
//                    File.WriteAllText(saveDialog.FileName, codigoG, System.Text.Encoding.UTF8);
//                    MessageBox.Show($"Código G exportado correctamente a:\n{saveDialog.FileName}\n\n" +
//                        "El código está optimizado para:\n" +
//                        "• Fresadora CNC de 3 ejes\n" +
//                        "• Fresa Ø5mm\n" +
//                        "• Corte de madera",
//                        "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error al exportar código G:\n{ex.Message}",
//                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        /*
//        /// <summary>
//        /// Generar G-code desde un archivo STL con configuración para fresadora CNC
//        /// </summary>
//        private async void GenerarGCodeDesdeSTL(string stlPath)
//        {
//            if (_slicerService == null)
//            {
//                MessageBox.Show("El servicio Slic3r no está disponible.\n\n" +
//                    "Asegúrate de tener Slic3r instalado en la carpeta Slic3r del proyecto.",
//                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//                return;
//            }

//            try
//            {
//                var outputPath = Path.ChangeExtension(stlPath, ".gcode");

//                // Crear ventana de progreso
//                var progressWindow = new Window
//                {
//                    Title = "Generando G-code para Fresadora CNC",
//                    Width = 600,
//                    Height = 400,
//                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
//                    Owner = this,
//                    ResizeMode = ResizeMode.NoResize
//                };

//                var scrollViewer = new System.Windows.Controls.ScrollViewer
//                {
//                    VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto
//                };

//                var progressText = new System.Windows.Controls.TextBlock
//                {
//                    Text = "Iniciando conversión STL → G-code...\n",
//                    Margin = new Thickness(20),
//                    TextWrapping = TextWrapping.Wrap,
//                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
//                    FontSize = 11
//                };

//                scrollViewer.Content = progressText;
//                progressWindow.Content = scrollViewer;
//                progressWindow.Show();

//                // Configuración específica para fresadora CNC
//                var settings = new CNCMillingSettings
//                {
//                    // Parámetros de la máquina
//                    WorkAreaX = 300.0,
//                    WorkAreaY = 300.0,
//                    WorkAreaZ = 150.0,
//                    StepResolution = 0.1,

//                    // Herramienta
//                    ToolDiameter = 5.0,
//                    MaxSpindleSpeed = 10000,
//                    WorkingSpindleSpeed = 8000,

//                    // Parámetros de corte para madera
//                    CuttingDepth = 0.5,            // 0.5mm por pasada
//                    CuttingFeedrate = 1500.0,      // 1500 mm/min velocidad de corte
//                    PlungeFeedrate = 500.0,        // 500 mm/min penetración
//                    RapidFeedrate = 3000.0,        // 3000 mm/min movimientos rápidos

//                    // Mecanizado
//                    Perimeters = 1,                // Un solo contorno
//                    FillDensity = 10,              // 10% relleno
//                    FillPattern = "rectilinear",   // Patrón rectilíneo
//                    SafeHeight = 50.0,             // 50mm altura segura

//                    Material = "Madera"
//                };

//                // Ejecutar slicing con configuración CNC
//                var result = await _slicerService.SliceWithCustomSettings(
//                    stlPath,
//                    outputPath,
//                    settings,
//                    progress => Dispatcher.Invoke(() =>
//                    {
//                        progressText.Text += progress + "\n";
//                        scrollViewer.ScrollToEnd();
//                    })
//                );

//                progressWindow.Close();

//                if (result.Success)
//                {
//                    // Cargar el G-code generado en el editor
//                    EditorGCode.CargarArchivo(result.OutputFilePath);

//                    MessageBox.Show(
//                        $"✅ G-code generado exitosamente para fresadora CNC\n\n" +
//                        $"═══════════════════════════════════════\n" +
//                        $"ESPECIFICACIONES:\n" +
//                        $"═══════════════════════════════════════\n" +
//                        $"Archivo: {Path.GetFileName(result.OutputFilePath)}\n" +
//                        $"Tamaño: {result.FileSize / 1024:F2} KB\n" +
//                        $"Tiempo: {result.ProcessingTime.TotalSeconds:F2} segundos\n\n" +
//                        $"CONFIGURACIÓN CNC:\n" +
//                        $"═══════════════════════════════════════\n" +
//                        $"• Área de trabajo: 300x300x150mm\n" +
//                        $"• Herramienta: Fresa Ø5mm\n" +
//                        $"• Velocidad husillo: 8000 RPM\n" +
//                        $"• Profundidad corte: 0.5mm/pasada\n" +
//                        $"• Velocidad corte: 1500 mm/min\n" +
//                        $"• Velocidad rápida: 3000 mm/min\n" +
//                        $"• Material: Madera\n" +
//                        $"• Precisión: 0.1mm/paso",
//                        "Conversión Exitosa - Fresadora CNC",
//                        MessageBoxButton.OK,
//                        MessageBoxImage.Information);
//                }
//                else
//                {
//                    MessageBox.Show(
//                        $"❌ Error al generar G-code:\n\n{result.ErrorMessage}",
//                        "Error de Conversión",
//                        MessageBoxButton.OK,
//                        MessageBoxImage.Error);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error inesperado:\n{ex.Message}",
//                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }
//        */
//        private async void GenerarGCodeDesdeSTL(string stlPath)
//        {
//            if (_slicerService == null)
//            {
//                MessageBox.Show("El servicio de slicing no está inicializado.",
//                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//                return;
//            }

//            try
//            {
//                // Mostrar progreso
//                var progressWindow = new Window
//                {
//                    Title = "Generando G-code",
//                    Width = 550,
//                    Height = 350,
//                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
//                    Owner = this
//                };

//                var progressText = new System.Windows.Controls.TextBlock
//                {
//                    Text = "Cargando STL...\n",
//                    Margin = new Thickness(15),
//                    TextWrapping = TextWrapping.Wrap,
//                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
//                    FontSize = 12
//                };

//                var scroll = new System.Windows.Controls.ScrollViewer
//                {
//                    Content = progressText
//                };

//                progressWindow.Content = scroll;
//                progressWindow.Show();

//                // Cargar STL en el slicer interno
//                _slicerService.LoadStl(stlPath);
//                progressText.Text += "STL cargado.\nProcesando...\n";

//                // Ejecutar slicing interno
//                var gcodeLines = await _slicerService.SliceAsync(
//                    stepOver: 1.0,
//                    layerHeight: 0.5,
//                    feedRate: 800,
//                    plungeRate: 300,
//                    spindleRPM: 9000
//                );

//                progressText.Text += "G-code generado.\n";
//                progressWindow.Close();

//                // Mostrar en el editor
//                EditorGCode.CargarTexto(string.Join("\n", gcodeLines));

//                MessageBox.Show("G-code generado correctamente.",
//                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error en el procesamiento:\n{ex.Message}",
//                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }


//        /// <summary>
//        /// Configura las conexiones y eventos entre los diferentes widgets
//        /// </summary>
//        private void InicializarConexionesEntreWidgets()
//        {
//            try
//            {
//                // Configurar el simulador 3D
//                Simulador3d.ConfigurarControlPropiedades();

//                // Conectar eventos del control de reproducción al simulador
//                Simulador3d.ControlReproduccion.ReproducirClick += ControlReproduccion_ReproducirClick;
//                Simulador3d.ControlReproduccion.PausarClick += ControlReproduccion_PausarClick;
//                Simulador3d.ControlReproduccion.SiguienteClick += ControlReproduccion_SiguienteClick;
//                Simulador3d.ControlReproduccion.AnteriorClick += ControlReproduccion_AnteriorClick;

//                // Conectar evento de código G aplicado
//                EditorGCode.GCodeAplicado += (sender, ruta) =>
//                {
//                    Console.WriteLine($"Código G guardado en: {ruta}");
//                };

//                Console.WriteLine("Widgets conectados exitosamente");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error inicializando conexiones: {ex.Message}");
//                MessageBox.Show($"Error al inicializar la aplicación: {ex.Message}",
//                    "Error de Inicialización", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        /// <summary>
//        /// Maneja el evento de reproducción desde el control
//        /// </summary>
//        private void ControlReproduccion_ReproducirClick(object sender, RoutedEventArgs e)
//        {
//            try
//            {
//                string codigoG = EditorGCode.GCodeTexto;

//                if (string.IsNullOrWhiteSpace(codigoG))
//                {
//                    MessageBox.Show("No hay código G para reproducir.\nPor favor, carga o genera un archivo G-code primero.",
//                        "Sin código G", MessageBoxButton.OK, MessageBoxImage.Warning);
//                    return;
//                }

//                Simulador3d.IniciarAnimacionGCode(codigoG);
//                Console.WriteLine("Animación iniciada desde MainWindow");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error al iniciar reproducción: {ex.Message}");
//                MessageBox.Show($"Error al iniciar la reproducción: {ex.Message}",
//                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        /// <summary>
//        /// Maneja el evento de pausa desde el control
//        /// </summary>
//        private void ControlReproduccion_PausarClick(object sender, RoutedEventArgs e)
//        {
//            try
//            {
//                Simulador3d.PausarAnimacionGCode();
//                Console.WriteLine("Animación pausada desde MainWindow");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error al pausar: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Maneja el evento de siguiente línea
//        /// </summary>
//        private void ControlReproduccion_SiguienteClick(object sender, RoutedEventArgs e)
//        {
//            try
//            {
//                Simulador3d.AvanzarSiguienteLinea();
//                Console.WriteLine("Avanzar a siguiente línea");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error al avanzar: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Maneja el evento de línea anterior
//        /// </summary>
//        private void ControlReproduccion_AnteriorClick(object sender, RoutedEventArgs e)
//        {
//            try
//            {
//                Simulador3d.RetrocederLineaAnterior();
//                Console.WriteLine("Retroceder a línea anterior");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error al retroceder: {ex.Message}");
//            }
//        }
//    }

//}
//*/
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
                // Configurar la referencia del simulador en el control de propiedades
                if (Simulador3d != null)
                {
                    // Asignar la referencia del simulador al control de propiedades interno
                    Simulador3d.ConfigurarControlPropiedades();

                    // Suscribirse a eventos si es necesario
                    Simulador3d.PropiedadCambiada += OnPropiedadPiezaCambiada;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configurando controles: {ex.Message}");
            }
        }

        private void OnPropiedadPiezaCambiada(string propiedad, double valor)
        {
            // Opcional: Puedes agregar lógica adicional aquí cuando cambien las propiedades
            Console.WriteLine($"Propiedad {propiedad} cambiada a: {valor}");
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

                    // Actualizar el control de propiedades después de cargar el modelo
                    Simulador3d.ActualizarControlPropiedades();

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
        /// Inicializa el SlicerService.
        /// Ahora crea la implementación interna (motor CAM híbrido) si no hay ejecutable externo.
        /// Esta versión evita buscar slic3r en disco y usa la implementación que entregué.
        /// </summary>
        private void InicializarSlicerService()
        {
            try
            {
                // Simple: inicializa el SlicerService interno (CAM híbrido).
                // Si deseas usar un ejecutable externo en el futuro, puedes usar:
                // _slicerService = new SlicerService("ruta\\a\\slic3r.exe");
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
                MessageBox.Show("El servicio de conversión no está disponible.\n\n" +
                    "Asegúrate de que el servicio esté inicializado.",
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

                // Ejecutar slicing con configuración CNC (usa SliceWithCustomSettings, que sí existe en el nuevo SlicerService)
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
                    // Cargar el G-code generado en el editor (usa la API existente EditorGCode.CargarArchivo)
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

        /// <summary>
        /// Configura las conexiones y eventos entre los diferentes widgets
        /// </summary>
        private void InicializarConexionesEntreWidgets()
        {
            try
            {
                // Configurar el simulador 3D
                Simulador3d.ConfigurarControlPropiedades();

                // Conectar eventos del control de reproducción al simulador
                Simulador3d.ControlReproduccion.ReproducirClick += ControlReproduccion_ReproducirClick;
                Simulador3d.ControlReproduccion.PausarClick += ControlReproduccion_PausarClick;
                Simulador3d.ControlReproduccion.SiguienteClick += ControlReproduccion_SiguienteClick;
                Simulador3d.ControlReproduccion.AnteriorClick += ControlReproduccion_AnteriorClick;

                // Conectar evento de código G aplicado
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

        /// <summary>
        /// Maneja el evento de reproducción desde el control
        /// </summary>
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

        /// <summary>
        /// Maneja el evento de pausa desde el control
        /// </summary>
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

        /// <summary>
        /// Maneja el evento de siguiente línea
        /// </summary>
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

        /// <summary>
        /// Maneja el evento de línea anterior
        /// </summary>
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

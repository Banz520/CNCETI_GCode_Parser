using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using WPF_CNC_Simulator.Services;

namespace WPF_CNC_Simulator.ViewModels
{
    /// <summary>
    /// ViewModel para la funcionalidad de Slicing STL a G-code
    /// </summary>
    public class SlicerViewModel : INotifyPropertyChanged
    {
        private readonly SlicerService _slicerService;
        private string _stlFilePath;
        private string _gcodeOutputPath;
        private string _progressText;
        private bool _isProcessing;
        private SlicerSettings _settings;

        public SlicerViewModel()
        {
            // Configurar ruta al ejecutable de Slic3r
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var slic3rPath = Path.Combine(baseDir, "Slic3r", "Slic3r-console.exe");

            // Verificar que existe
            if (!File.Exists(slic3rPath))
            {
                ProgressText = $"⚠️ ERROR: No se encontró Slic3r en:\n{slic3rPath}\n\n" +
                              "Asegúrate de haber copiado los archivos correctamente.";
            }
            else
            {
                _slicerService = new SlicerService(slic3rPath);
                ProgressText = "✓ Slic3r cargado correctamente. Listo para convertir archivos STL a G-code";
            }

            _settings = new SlicerSettings();

            // Inicializar comandos
            SelectSTLCommand = new RelayCommand(SelectSTLFile);
            SelectOutputCommand = new RelayCommand(SelectOutputFile);
            SliceCommand = new RelayCommand(async () => await SliceFile(), () => CanSlice);
        }

        #region Propiedades

        public string STLFilePath
        {
            get => _stlFilePath;
            set
            {
                _stlFilePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSlice));
            }
        }

        public string GCodeOutputPath
        {
            get => _gcodeOutputPath;
            set
            {
                _gcodeOutputPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSlice));
            }
        }

        public string ProgressText
        {
            get => _progressText;
            set
            {
                _progressText = value;
                OnPropertyChanged();
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSlice));
            }
        }

        public SlicerSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                OnPropertyChanged();
            }
        }

        public bool CanSlice => !string.IsNullOrEmpty(STLFilePath) &&
                                !string.IsNullOrEmpty(GCodeOutputPath) &&
                                !IsProcessing &&
                                _slicerService != null;

        #endregion

        #region Comandos

        public ICommand SelectSTLCommand { get; }
        public ICommand SelectOutputCommand { get; }
        public ICommand SliceCommand { get; }

        private void SelectSTLFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Archivos STL (*.stl)|*.stl|Todos los archivos (*.*)|*.*",
                Title = "Seleccionar archivo STL"
            };

            if (dialog.ShowDialog() == true)
            {
                STLFilePath = dialog.FileName;

                // Auto-sugerir ruta de salida
                if (string.IsNullOrEmpty(GCodeOutputPath))
                {
                    GCodeOutputPath = Path.ChangeExtension(STLFilePath, ".gcode");
                }

                ProgressText = $"Archivo STL seleccionado: {Path.GetFileName(STLFilePath)}";
            }
        }

        private void SelectOutputFile()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Archivos G-code (*.gcode)|*.gcode|Todos los archivos (*.*)|*.*",
                Title = "Guardar G-code como",
                FileName = string.IsNullOrEmpty(STLFilePath)
                    ? "output.gcode"
                    : Path.GetFileNameWithoutExtension(STLFilePath) + ".gcode"
            };

            if (dialog.ShowDialog() == true)
            {
                GCodeOutputPath = dialog.FileName;
                ProgressText = $"Archivo de salida configurado: {Path.GetFileName(GCodeOutputPath)}";
            }
        }

        private async Task SliceFile()
        {
            if (_slicerService == null)
            {
                MessageBox.Show(
                    "El servicio Slic3r no está disponible. Verifica que los archivos estén copiados correctamente.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            try
            {
                IsProcessing = true;
                ProgressText = "🔄 Iniciando conversión STL → G-code...\n";

                var result = await _slicerService.SliceWithCustomSettings(
                    STLFilePath,
                    GCodeOutputPath,
                    Settings,
                    progress => Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProgressText += progress + "\n";
                    })
                );

                if (result.Success)
                {
                    ProgressText = $"✅ CONVERSIÓN COMPLETADA\n" +
                                  $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                                  $"⏱️  Tiempo: {result.ProcessingTime.TotalSeconds:F2} segundos\n" +
                                  $"📁 Archivo: {result.OutputFilePath}\n" +
                                  $"📊 Tamaño: {result.FileSize / 1024:F2} KB\n" +
                                  $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";

                    MessageBox.Show(
                        $"✅ G-code generado exitosamente\n\n" +
                        $"Archivo: {Path.GetFileName(result.OutputFilePath)}\n" +
                        $"Ubicación: {Path.GetDirectoryName(result.OutputFilePath)}\n\n" +
                        $"Tiempo de conversión: {result.ProcessingTime.TotalSeconds:F2} segundos\n" +
                        $"Tamaño del archivo: {result.FileSize / 1024:F2} KB",
                        "Conversión Exitosa",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    ProgressText = $"❌ ERROR EN LA CONVERSIÓN\n" +
                                  $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                                  $"{result.ErrorMessage}\n\n" +
                                  $"Detalles:\n{result.ErrorOutput}";

                    MessageBox.Show(
                        $"❌ Error al generar G-code:\n\n{result.ErrorMessage}\n\n" +
                        $"Revisa el panel de progreso para más detalles.",
                        "Error de Conversión",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                ProgressText = $"💥 EXCEPCIÓN: {ex.Message}\n\n{ex.StackTrace}";
                MessageBox.Show(
                    $"Error inesperado:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// Implementación simple de ICommand para WPF
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
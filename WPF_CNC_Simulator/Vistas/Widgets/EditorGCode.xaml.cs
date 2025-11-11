using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace WPF_CNC_Simulator.Vistas.Widgets
{
    /// <summary>
    /// Lógica de interacción para EditorGCode.xaml
    /// </summary>
    public partial class EditorGCode : UserControl
    {
        // Evento para notificar cuando se aplican cambios
        public event EventHandler<string> GCodeAplicado;

        public string GCodeTexto
        {
            get => txtGCode.Text;
            set => txtGCode.Text = value ?? string.Empty;
        }

        public EditorGCode()
        {
            InitializeComponent();
            ConfigurarResaltadoGCode();
            txtGCode.Text = "; Editor de Código G\n; Carga o genera un archivo G-code para comenzar\n";
        }

        private void ConfigurarResaltadoGCode()
        {
            try
            {
                // Definición personalizada del resaltado G-code
                string xshd = @"<?xml version=""1.0""?>
<SyntaxDefinition name=""GCode"" extensions="".gcode;.nc"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">
  <Color name=""Command"" foreground=""#7FFF00"" fontWeight=""bold""/>
  <Color name=""Axis"" foreground=""#00BFFF"" fontWeight=""bold""/>
  <Color name=""Value"" foreground=""#FFA500""/>
  <Color name=""Comment"" foreground=""#808080"" fontStyle=""italic""/>
  
  <RuleSet ignoreCase=""true"">
    <Span color=""Comment"" begin="";"" />
    <Span color=""Comment"" begin=""("" end="")""/>
    
    <Keywords color=""Command"">
      <Word>G0</Word>
      <Word>G1</Word>
      <Word>G2</Word>
      <Word>G3</Word>
      <Word>G4</Word>
      <Word>G17</Word>
      <Word>G18</Word>
      <Word>G19</Word>
      <Word>G20</Word>
      <Word>G21</Word>
      <Word>G28</Word>
      <Word>G90</Word>
      <Word>G91</Word>
      <Word>G92</Word>
      <Word>M0</Word>
      <Word>M1</Word>
      <Word>M2</Word>
      <Word>M3</Word>
      <Word>M4</Word>
      <Word>M5</Word>
      <Word>M6</Word>
      <Word>M30</Word>
      <Word>M104</Word>
      <Word>M109</Word>
      <Word>M140</Word>
      <Word>M190</Word>
    </Keywords>
    
    <Rule color=""Command"">
      \b[GM]\d+\b
    </Rule>
    
    <Rule color=""Axis"">
      \b[XYZIJKEF]\b
    </Rule>
    
    <Rule color=""Value"">
      [+-]?\d+\.?\d*
    </Rule>
  </RuleSet>
</SyntaxDefinition>";

                using (var reader = new StringReader(xshd))
                using (var xmlReader = XmlReader.Create(reader))
                {
                    txtGCode.SyntaxHighlighting = HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error configurando resaltado de sintaxis: {ex.Message}",
                    "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void txtGCode_TextChanged(object sender, EventArgs e)
        {
            // Evento que se dispara cuando cambia el texto
            // Puedes agregar validación o procesamiento aquí si lo necesitas
        }

        private void BotonAplicar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(GCodeTexto))
                {
                    MessageBox.Show("No hay código G para guardar.",
                        "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Archivos G-code (*.gcode)|*.gcode|Archivos NC (*.nc)|*.nc|Todos los archivos (*.*)|*.*",
                    Title = "Guardar Código G",
                    FileName = "codigo_g.gcode"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveDialog.FileName, GCodeTexto, Encoding.UTF8);
                    MessageBox.Show($"Código G guardado correctamente en:\n{saveDialog.FileName}",
                        "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Disparar evento
                    GCodeAplicado?.Invoke(this, saveDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar el código G:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Carga un archivo G-code en el editor
        /// </summary>
        public void CargarArchivo(string rutaArchivo)
        {
            try
            {
                if (File.Exists(rutaArchivo))
                {
                    GCodeTexto = File.ReadAllText(rutaArchivo, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar archivo:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Limpia el contenido del editor
        /// </summary>
        public void Limpiar()
        {
            GCodeTexto = string.Empty;
        }
    }
}

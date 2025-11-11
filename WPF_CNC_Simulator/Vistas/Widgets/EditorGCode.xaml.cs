using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Xml;

namespace WPF_CNC_Simulator.Vistas.Widgets
{
    /// <summary>
    /// Lógica de interacción para EditorGCode.xaml
    /// </summary>
    public partial class EditorGCode : UserControl
    {
        public string GCodeTexto
        {
            get => txtGCode.Text;
            set => txtGCode.Text = value;
        }

        public EditorGCode()
        {
            InitializeComponent();
            ConfigurarResaltadoGCode();
        }

        private void ConfigurarResaltadoGCode()
        {
            // Definición personalizada del resaltado G-code
            string xshd = @"
<SyntaxDefinition name='GCode' extensions='.gcode'>
  <Color name='Command' foreground='Lime'/>
  <Color name='Axis' foreground='DeepSkyBlue'/>
  <Color name='Value' foreground='Orange'/>
  <Color name='Comment' foreground='Gray'/>

  <RuleSet ignoreCase='false'>
    <Span color='Comment' begin=';' end='$'/>
    <Rule color='Command' regex='\bG\d+\b'/>
    <Rule color='Command' regex='\bM\d+\b'/>
    <Rule color='Axis' regex='\b[XYZEF]\b'/>
    <Rule color='Value' regex='[+-]?[0-9]+(\.[0-9]+)?'/>
  </RuleSet>
</SyntaxDefinition>";

            /**
             * 
             * using (var reader = new StringReader(xshd))
            using (var xmlReader = new XmlReader(reader))
            {
                txtGCode.SyntaxHighlighting = HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
            }
             * 
             */

        }

        private void txtGCode_TextChanged(object sender, EventArgs e)
        {
            // Aquí podrías disparar eventos si quieres detectar cambios del usuario
        }

        private void BotonAplicar_Click(object sender, RoutedEventArgs e)
        {
            // Guardar el G-code para uso posterior (por ejemplo en archivo o memoria)
            File.WriteAllText("gcode_generado.gcode", GCodeTexto);
            MessageBox.Show("Código G guardado correctamente.");
        }
    }

}

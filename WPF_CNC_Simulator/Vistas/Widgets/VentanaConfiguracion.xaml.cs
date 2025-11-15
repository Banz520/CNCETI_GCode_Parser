using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WPF_CNC_Simulator.Vistas.Widgets
{
    public partial class VentanaConfiguracion : Window
    {
        private Simulador3d _simulador3d;
        private EditorGCode _editorGCode;

        // Paleta de colores disponibles
        private Dictionary<string, string> _paletaColores = new Dictionary<string, string>
        {
            { "Verde Oscuro", "#1b7147" },
            { "Azul Claro", "#25aef4" },
            { "Naranja Dorado", "#ca8c1b" },
            { "Morado", "#9b4aa0" },
            { "Azul Índigo", "#4c57db" },
            { "Verde Brillante", "#22ac51" },
            { "Rojo", "#de1b29" },
            { "Naranja", "#fb7c23" },
            { "Rosa", "#eb6869" },
            { "Verde Lima", "#c7f236" },
            { "Morado Oscuro", "#524798" },
            { "Verde Azulado Oscuro", "#10423d" },
            { "Verde Bosque", "#015108" },
            { "Rojo Oscuro", "#980d14" },
            { "Verde Oliva", "#326b23" },
            { "Verde Azulado", "#238782" },
            { "Naranja Quemado", "#c1631c" },
            { "Coral", "#fd5e3e" }
        };

        public VentanaConfiguracion(Simulador3d simulador3d, EditorGCode editorGCode)
        {
            InitializeComponent();
            _simulador3d = simulador3d;
            _editorGCode = editorGCode;

            // Cargar valores actuales
            txtResolucionGrid.Text = "10"; // Valor por defecto

            // Llenar ComboBoxes con la paleta de colores
            foreach (var color in _paletaColores)
            {
                cmbColorComando.Items.Add(new ComboBoxItem { Content = color.Key, Tag = color.Value });
                cmbColorEje.Items.Add(new ComboBoxItem { Content = color.Key, Tag = color.Value });
                cmbColorValor.Items.Add(new ComboBoxItem { Content = color.Key, Tag = color.Value });
                cmbColorComentario.Items.Add(new ComboBoxItem { Content = color.Key, Tag = color.Value });
            }

            // Establecer valores por defecto
            cmbColorComando.SelectedIndex = 11; // Verde Bosque
            cmbColorEje.SelectedIndex = 1;      // Azul Claro
            cmbColorValor.SelectedIndex = 2;    // Naranja Dorado
            cmbColorComentario.SelectedIndex = 10; // Morado Oscuro
        }

        private void BtnAplicarGrid_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(txtResolucionGrid.Text, out double resolucion))
            {
                if (resolucion < 1 || resolucion > 100)
                {
                    MessageBox.Show("La resolución del grid debe estar entre 1mm y 100mm.",
                        "Valor no válido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _simulador3d.ActualizarResolucionGrid(resolucion);
                MessageBox.Show($"Resolución del grid actualizada a {resolucion}mm",
                    "Configuración aplicada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Por favor ingrese un valor numérico válido.",
                    "Valor inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnAplicarColores_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string colorComando = ((ComboBoxItem)cmbColorComando.SelectedItem)?.Tag?.ToString() ?? "#7FFF00";
                string colorEje = ((ComboBoxItem)cmbColorEje.SelectedItem)?.Tag?.ToString() ?? "#00BFFF";
                string colorValor = ((ComboBoxItem)cmbColorValor.SelectedItem)?.Tag?.ToString() ?? "#FFA500";
                string colorComentario = ((ComboBoxItem)cmbColorComentario.SelectedItem)?.Tag?.ToString() ?? "#808080";

                _editorGCode.ActualizarColoresResaltado(colorComando, colorEje, colorValor, colorComentario);

                MessageBox.Show("Colores del editor actualizados correctamente.",
                    "Configuración aplicada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al aplicar colores:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

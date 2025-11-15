using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WPF_CNC_Simulator.Vistas.Widgets
{
    /// <summary>
    /// Lógica de interacción para PropiedadesPieza.xaml
    /// </summary>
    public partial class PropiedadesPieza : UserControl
    {
        /// <summary>
        /// Referencia al simulador 3D
        /// </summary>
        public Simulador3d Simulador3D { get; set; }

        /// <summary>
        /// Evento que se dispara cuando cambia alguna propiedad
        /// </summary>
        public event Action<string, double> PropiedadCambiada;

        public PropiedadesPieza()
        {
            InitializeComponent();

            txtEscala.PreviewMouseDown += ValidarAnimacionEnProgreso;
            txtPosicionX.PreviewMouseDown += ValidarAnimacionEnProgreso;
            txtPosicionY.PreviewMouseDown += ValidarAnimacionEnProgreso;
            txtRotacionZ.PreviewMouseDown += ValidarAnimacionEnProgreso;
        }

        private void ValidarAnimacionEnProgreso(object sender, MouseButtonEventArgs e)
        {
            if (Simulador3D != null && Simulador3D.AnimacionEnProgreso)
            {
                MessageBox.Show("Debe pausar la animación antes de modificar las propiedades.",
                    "Animación en progreso", MessageBoxButton.OK, MessageBoxImage.Information);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Actualiza los valores mostrados en los TextBox
        /// </summary>
        public void ActualizarValoresVisuales()
        {
            if (Simulador3D == null) return;

            // Actualizar TextBoxes con los valores actuales
            txtEscala.Text = Simulador3D.ObtenerEscalaImportado().ToString("F2");
            txtPosicionX.Text = Simulador3D.ObtenerPosicionXImportado().ToString("F2");
            txtPosicionY.Text = Simulador3D.ObtenerPosicionYImportado().ToString("F2");
            txtRotacionZ.Text = Simulador3D.ObtenerRotacionZImportado().ToString("F2");
        }

        /// <summary>
        /// Habilita o deshabilita los controles
        /// </summary>
        public void SetHabilitado(bool habilitado)
        {
            txtEscala.IsEnabled = habilitado;
            txtPosicionX.IsEnabled = habilitado;
            txtPosicionY.IsEnabled = habilitado;
            txtRotacionZ.IsEnabled = habilitado;
        }

        /// <summary>
        /// Procesa el cambio de valor en los TextBox
        /// </summary>
        private void ProcesarCambioValor(TextBox textBox, string propiedad)
        {
            if (Simulador3D == null) return;

            if (double.TryParse(textBox.Text, out double valor))
            {
                try
                {
                    bool cambioAplicado = false;

                    switch (propiedad)
                    {
                        case "Escala":
                            cambioAplicado = Simulador3D.EstablecerEscalaImportado(valor);
                            break;
                        case "PosicionX":
                            cambioAplicado = Simulador3D.EstablecerPosicionXImportado(valor);
                            break;
                        case "PosicionY":
                            cambioAplicado = Simulador3D.EstablecerPosicionYImportado(valor);
                            break;
                        case "RotacionZ":
                            cambioAplicado = Simulador3D.EstablecerRotacionZImportado(valor);
                            break;
                    }

                    if (cambioAplicado)
                    {
                        // Disparar evento
                        PropiedadCambiada?.Invoke(propiedad, valor);
                    }
                    else
                    {
                        // Restaurar valor anterior si la validación falló
                        ActualizarValoresVisuales();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al aplicar {propiedad}: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ActualizarValoresVisuales();
                }
            }
            else
            {
                // Restaurar valor anterior si no es válido
                ActualizarValoresVisuales();
                MessageBox.Show("Por favor ingrese un valor numérico válido",
                    "Valor inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Event Handlers
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string propiedad = textBox.Name switch
                {
                    "txtEscala" => "Escala",
                    "txtPosicionX" => "PosicionX",
                    "txtPosicionY" => "PosicionY",
                    "txtRotacionZ" => "RotacionZ",
                    _ => "Desconocida"
                };

                ProcesarCambioValor(textBox, propiedad);
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox textBox)
            {
                textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            if (Simulador3D != null)
            {
                if (Simulador3D.AnimacionEnProgreso)
                {
                    MessageBox.Show("Debe pausar la animación antes de resetear las propiedades.",
                        "Animación en progreso", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Simulador3D.ResetearTransformacionesImportado();
                ActualizarValoresVisuales();
                PropiedadCambiada?.Invoke("Reset", 0);
            }
        }

        /// <summary>
        /// Valida que solo se ingresen números y punto decimal
        /// </summary>
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Permitir números, punto decimal y signo negativo
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '.' && c != '-')
                {
                    e.Handled = true;
                    return;
                }
            }

            // Validar que no haya múltiples puntos decimales
            if (e.Text == ".")
            {
                TextBox textBox = (TextBox)sender;
                if (textBox.Text.Contains("."))
                {
                    e.Handled = true;
                }
            }

            // Validar que el signo negativo solo esté al inicio
            if (e.Text == "-")
            {
                TextBox textBox = (TextBox)sender;
                if (textBox.Text.Length > 0)
                {
                    e.Handled = true;
                }
            }
        }
    }
}

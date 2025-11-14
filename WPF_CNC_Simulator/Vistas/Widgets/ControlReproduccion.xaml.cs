using System;
using System.Windows;
using System.Windows.Controls;

namespace WPF_CNC_Simulator.Vistas.Widgets
{
    /// <summary>
    /// Lógica de interacción para ControlReproduccion.xaml
    /// </summary>
    public partial class ControlReproduccion : UserControl
    {
        // Definir los Routed Events
        public static readonly RoutedEvent ReproducirClickEvent = EventManager.RegisterRoutedEvent(
            "ReproducirClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ControlReproduccion));

        public static readonly RoutedEvent PausarClickEvent = EventManager.RegisterRoutedEvent(
            "PausarClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ControlReproduccion));

        public static readonly RoutedEvent SiguienteClickEvent = EventManager.RegisterRoutedEvent(
            "SiguienteClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ControlReproduccion));

        public static readonly RoutedEvent AnteriorClickEvent = EventManager.RegisterRoutedEvent(
            "AnteriorClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ControlReproduccion));

        // Propiedades CLR para suscribirse a los eventos
        public event RoutedEventHandler ReproducirClick
        {
            add { AddHandler(ReproducirClickEvent, value); }
            remove { RemoveHandler(ReproducirClickEvent, value); }
        }

        public event RoutedEventHandler PausarClick
        {
            add { AddHandler(PausarClickEvent, value); }
            remove { RemoveHandler(PausarClickEvent, value); }
        }

        public event RoutedEventHandler SiguienteClick
        {
            add { AddHandler(SiguienteClickEvent, value); }
            remove { RemoveHandler(SiguienteClickEvent, value); }
        }

        public event RoutedEventHandler AnteriorClick
        {
            add { AddHandler(AnteriorClickEvent, value); }
            remove { RemoveHandler(AnteriorClickEvent, value); }
        }

        public ControlReproduccion()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Maneja el clic en el botón Reproducir (Play)
        /// </summary>
        private void boton_reproducir_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Botón Play presionado");
            RaiseEvent(new RoutedEventArgs(ReproducirClickEvent));
        }

        /// <summary>
        /// Maneja el clic en el botón Pausar
        /// </summary>
        private void boton_pausa_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Botón Pause presionado");
            RaiseEvent(new RoutedEventArgs(PausarClickEvent));
        }

        /// <summary>
        /// Maneja el clic en el botón Siguiente (Next)
        /// </summary>
        private void boton_sig_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Botón Next presionado");
            RaiseEvent(new RoutedEventArgs(SiguienteClickEvent));
        }

        /// <summary>
        /// Maneja el clic en el botón Anterior (Prev)
        /// </summary>
        private void boton_prev_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Botón Prev presionado");
            RaiseEvent(new RoutedEventArgs(AnteriorClickEvent));
        }

        /// <summary>
        /// Habilita o deshabilita los controles de reproducción
        /// </summary>
        public void SetControlesHabilitados(bool habilitado)
        {
            boton_reproducir.IsEnabled = habilitado;
            boton_pausa.IsEnabled = habilitado;
            boton_sig.IsEnabled = habilitado;
            boton_prev.IsEnabled = habilitado;
        }

        /// <summary>
        /// Actualiza el estado visual según el estado de reproducción
        /// </summary>
        public void ActualizarEstadoReproduccion(bool reproduciendo)
        {
            if (reproduciendo)
            {
                boton_reproducir.Content = "⏸ Playing";
                boton_reproducir.IsEnabled = false;
                boton_pausa.IsEnabled = true;
            }
            else
            {
                boton_reproducir.Content = "▶ Play";
                boton_reproducir.IsEnabled = true;
                boton_pausa.IsEnabled = false;
            }
        }
    }
}

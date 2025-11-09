using System;
using System.Collections.Generic;
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

namespace WPF_CNC_Simulator.Vistas.Widgets
{
    /// <summary>
    /// Lógica de interacción para BotonConIcono.xaml
    /// </summary>
    public partial class BotonConIcono : UserControl
    {


        public string Titulo
        {
            get { return (string)GetValue(TituloProperty); }
            set { SetValue(TituloProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Titulo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TituloProperty =
            DependencyProperty.Register("Titulo", typeof(string), typeof(BotonConIcono), new PropertyMetadata("Texto Titulo"));



        // Propiedad Icono (tipo ImageSource)
        public static readonly DependencyProperty IconoProperty =
            DependencyProperty.Register(
                nameof(Icono),
                typeof(ImageSource),
                typeof(BotonConIcono),
                new PropertyMetadata(null));

        public ImageSource Icono
        {
            get => (ImageSource)GetValue(IconoProperty);
            set => SetValue(IconoProperty, value);
        }



        public BotonConIcono()
        {
            InitializeComponent();
        }
    }
}

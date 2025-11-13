using HelixToolkit.Wpf;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace WPF_CNC_Simulator.Vistas.Widgets
{
    /// <summary>
    /// Lógica de interacción para Simulador3d.xaml
    /// </summary>
    public partial class Simulador3d : UserControl
    {
         

        private ModelVisual3D modeloBase;
        private ModelVisual3D modeloEjeY;
        private ModelVisual3D modeloEjeX;

        private Material materialBase = MaterialHelper.CreateMaterial(Colors.DimGray);
        private Material materialEjeY = MaterialHelper.CreateMaterial(Colors.DarkGray);
        private Material materialEjeX = MaterialHelper.CreateMaterial(Colors.LightSlateGray);

        private double posicionEjeX = 0;
        private double posicionEjeY = 0;

        private const string RUTA_MODELO_BASE = @"Vistas\Modelos3D\cnc_base.stl";
        private const string RUTA_MODELO_EJE_Y = @"Vistas\Modelos3D\cnc_eje_y.stl";
        private const string RUTA_MODELO_EJE_X = @"Vistas\Modelos3D\cnc_eje_x.stl";

        
        /// <summary>
        /// Constructor de la clase Simulador3d
        /// </summary>
        public Simulador3d()
        {
            InitializeComponent();
            CargarModelosCNC();
            ConfigurarViewport();

        }

        /// <summary>
        /// Configura la cámara y viewport 3D
        /// </summary>
        private void ConfigurarViewport()
        {
            viewport.Camera = new PerspectiveCamera
            {
                Position = new Point3D(800, 250, 400),
                LookDirection = new Vector3D(-50, -5, -5),
                UpDirection = new Vector3D(0, 0, 1),
                FieldOfView = 60
            };
        }

        /// <summary>
        /// Carga los tres modelos STL que componen la máquina CNC
        /// </summary>
        private void CargarModelosCNC()
        {
            try
            {
                string directorio_base = AppDomain.CurrentDomain.BaseDirectory;

                // Cargar modelo base con material gris
                CargarModeloIndividual(RUTA_MODELO_BASE, ref modeloBase, "base", materialBase);

                // Cargar modelo eje Y con material azul
                CargarModeloIndividual(RUTA_MODELO_EJE_Y, ref modeloEjeY, "eje_y", materialEjeY);

                // Cargar modelo eje X con material rojo
                CargarModeloIndividual(RUTA_MODELO_EJE_X, ref modeloEjeX, "eje_x", materialEjeX);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando modelos CNC: {ex.Message}");
                CrearModelosEjemplo();
            }
        }

        /// <summary>
        /// Carga un modelo STL individual y lo agrega al viewport
        /// </summary>
        /// <param name="ruta_relativa">Ruta relativa del archivo STL</param>
        /// <param name="modelo_visual">Referencia al ModelVisual3D donde se almacenará</param>
        /// <param name="nombre_modelo">Nombre identificador del modelo</param>
        private void CargarModeloIndividual(string ruta_relativa, ref ModelVisual3D modelo_visual, string nombre_modelo, Material material)
        {
            try
            {
                string directorio_base = AppDomain.CurrentDomain.BaseDirectory;
                string ruta_completa = Path.Combine(directorio_base, ruta_relativa);

                Console.WriteLine($"Buscando modelo {nombre_modelo} en: {ruta_completa}");
                Console.WriteLine($"Existe archivo: {File.Exists(ruta_completa)}");

                if (File.Exists(ruta_completa))
                {
                    var importador = new StLReader();
                    var modelo_3d = importador.Read(ruta_completa);

                    if (modelo_3d != null)
                    {
                        // Aplicar material a todos los GeometryModel3D del grupo
                        AplicarMaterialAModelo(modelo_3d, material);

                        modelo_visual = new ModelVisual3D();
                        modelo_visual.Content = modelo_3d;
                        viewport.Children.Add(modelo_visual);
                        Console.WriteLine($"Modelo {nombre_modelo} cargado exitosamente");
                        return;
                    }
                }

                Console.WriteLine($"Modelo {nombre_modelo} no encontrado en: {ruta_completa}");
                throw new FileNotFoundException($"No se pudo cargar el modelo {nombre_modelo}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando modelo {nombre_modelo}: {ex.Message}");
                throw;
            }
        }

        private void AplicarMaterialAModelo(Model3D modelo, Material material)
        {
            if (modelo is Model3DGroup grupo)
            {
                foreach (var modelo_hijo in grupo.Children)
                {
                    AplicarMaterialAModelo(modelo_hijo, material);
                }
            }
            else if (modelo is GeometryModel3D modelo_geometrico)
            {
                modelo_geometrico.Material = material;
                modelo_geometrico.BackMaterial = material; // Opcional: material para la parte trasera
            }
        }

        /// <summary>
        /// Crea modelos de ejemplo en caso de error al cargar los STL
        /// </summary>
        private void CrearModelosEjemplo()
        {
            // Modelo base (plataforma principal)
            var mesh_builder_base = new MeshBuilder();
            mesh_builder_base.AddBox(new Point3D(0, 0, 0), 4, 3, 0.5);
            var geometria_base = mesh_builder_base.ToMesh();
            var material_base = MaterialHelper.CreateMaterial(Colors.DarkGray);

            modeloBase = new ModelVisual3D();
            modeloBase.Content = new GeometryModel3D(geometria_base, material_base);
            viewport.Children.Add(modeloBase);

            // Modelo eje Y (se mueve en Y)
            var mesh_builder_eje_y = new MeshBuilder();
            mesh_builder_eje_y.AddBox(new Point3D(0, 0, 0.5), 3, 0.3, 0.3);
            var geometria_eje_y = mesh_builder_eje_y.ToMesh();
            var material_eje_y = MaterialHelper.CreateMaterial(Colors.Blue);

            modeloEjeY = new ModelVisual3D();
            modeloEjeY.Content = new GeometryModel3D(geometria_eje_y, material_eje_y);
            viewport.Children.Add(modeloEjeY);

            // Modelo eje X (se mueve en X)
            var mesh_builder_eje_x = new MeshBuilder();
            mesh_builder_eje_x.AddBox(new Point3D(0, 0, 0.8), 0.3, 0.3, 0.3);
            var geometria_eje_x = mesh_builder_eje_x.ToMesh();
            var material_eje_x = MaterialHelper.CreateMaterial(Colors.Red);

            modeloEjeX = new ModelVisual3D();
            modeloEjeX.Content = new GeometryModel3D(geometria_eje_x, material_eje_x);
            viewport.Children.Add(modeloEjeX);
        }

        /// <summary>
        /// Mueve el modelo del eje X en su eje correspondiente
        /// </summary>
        /// <param name="desplazamiento">Cantidad de desplazamiento en el eje X</param>
        public void MoverEjeX(double desplazamiento)
        {
            if (modeloEjeX != null)
            {
                var transformacion = new TranslateTransform3D(desplazamiento, 0, 0);
                modeloEjeX.Transform = transformacion;
                Console.WriteLine($"Eje X movido a posición: {desplazamiento}");
            }
        }

        /// <summary>
        /// Mueve el modelo del eje Y en su eje correspondiente
        /// </summary>
        /// <param name="desplazamiento">Cantidad de desplazamiento en el eje Y</param>
        public void MoverEjeY(double desplazamiento)
        {
            if (modeloEjeY != null)
            {
                var transformacion = new TranslateTransform3D(0, desplazamiento, 0);
                modeloEjeY.Transform = transformacion;
                Console.WriteLine($"Eje Y movido a posición: {desplazamiento}");
            }
        }

        /// <summary>
        /// Mueve ambos ejes simultáneamente
        /// </summary>
        /// <param name="desplazamiento_x">Desplazamiento en el eje X</param>
        /// <param name="desplazamiento_y">Desplazamiento en el eje Y</param>
        public void MoverEjes(double desplazamiento_x, double desplazamiento_y)
        {
            MoverEjeX(desplazamiento_x);
            MoverEjeY(desplazamiento_y);
        }

        /// <summary>
        /// Importa y carga un modelo STL adicional
        /// </summary>
        /// <param name="ruta_archivo">Ruta completa del archivo STL</param>
        public void CargarModeloImportado(string ruta_archivo)
        {
            try
            {
                if (!File.Exists(ruta_archivo))
                {
                    MessageBox.Show($"Archivo no encontrado: {ruta_archivo}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var importador = new ModelImporter();
                var modelo = importador.Load(ruta_archivo);

                var modelo_importado = new ModelVisual3D();
                modelo_importado.Content = modelo;

                // Posición inicial del modelo importado
                var transformacion = new TranslateTransform3D(2, 2, 0);
                modelo_importado.Transform = transformacion;

                viewport.Children.Add(modelo_importado);

                MessageBox.Show($"Modelo STL cargado correctamente:\n{Path.GetFileName(ruta_archivo)}",
                    "Importación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando modelo STL:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Métodos para los botones del control
        private void ImportarSTLClick(object sender, RoutedEventArgs e)
        {
            var dialogo_abrir_archivo = new OpenFileDialog
            {
                Filter = "Archivos STL (*.stl)|*.stl|Todos los archivos (*.*)|*.*",
                Title = "Importar modelo STL"
            };

            if (dialogo_abrir_archivo.ShowDialog() == true)
            {
                CargarModeloImportado(dialogo_abrir_archivo.FileName);
            }
        }

        private void ResetVistaClick(object sender, RoutedEventArgs e)
        {
            ConfigurarViewport();
        }

        //MEOTOD DE PRUEBA PARA MOVER EJES
        public void MoverEjesClick(object sender, RoutedEventArgs e)
        {
            var timer = new DispatcherTimer(); // creating a new timer
            timer.Interval = TimeSpan.FromMilliseconds(1); // this timer will trigger every 10 milliseconds
            timer.Start(); // starting the timer
            timer.Tick += MoverPruebaTiempo; // with each tick it will trigger this function

            //MoverEjeX(1.5);
            //MoverEjeY(0.8);
            //bool avanzarX = false;
        }

        void MoverPruebaTiempo(object sender, EventArgs e)
        {
            
            if (posicionEjeX < 300)
            {
                MoverEjeX(posicionEjeX++);
               
            }
            

            if(posicionEjeY < 300)
            {
                MoverEjeY(posicionEjeY++);
            }
           
           
        }

    }
}
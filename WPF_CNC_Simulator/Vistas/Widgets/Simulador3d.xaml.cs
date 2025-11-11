using HelixToolkit.Wpf;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WPF_CNC_Simulator.Vistas.Widgets
{
    /// <summary>
    /// Lógica de interacción para Simulador3d.xaml
    /// </summary>
    public partial class Simulador3d : UserControl
    {
        private Model3DGroup modeloCNC;
        private ModelVisual3D modeloImportado;
        private string rutaModeloCNC = @"Vistas\Modelos3D\cnceti.stl";

        public Simulador3d()
        {
            InitializeComponent();
            CargarModeloCNC();
            ConfigurarViewport();
        }

        private void ConfigurarViewport()
        {
            // Configurar cámara
            viewport.Camera = new PerspectiveCamera
            {
                Position = new Point3D(5, 5, 5),
                LookDirection = new Vector3D(-5, -5, -5),
                UpDirection = new Vector3D(0, 0, 1),
                FieldOfView = 60
            };
        }

        private void CargarModeloCNC()
        {
            try
            {
                string directorioBase = AppDomain.CurrentDomain.BaseDirectory;
                string rutaCompleta = Path.Combine(directorioBase, rutaModeloCNC);

                Console.WriteLine($"Buscando modelo en: {rutaCompleta}");
                Console.WriteLine($"Existe archivo: {File.Exists(rutaCompleta)}");

                if (File.Exists(rutaCompleta))
                {
                    var importador = new StLReader();
                    var modelo = importador.Read(rutaCompleta);

                    if (modelo != null)
                    {
                        modeloCNC = modelo;
                        var visual = new ModelVisual3D();
                        visual.Content = modeloCNC;
                        viewport.Children.Add(visual);
                        return;
                    }
                }

                Console.WriteLine($"Modelo CNC no encontrado en: {rutaCompleta}");
                CrearModeloEjemplo();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando modelo CNC: {ex.Message}");
                CrearModeloEjemplo();
            }
        }

        private void CrearModeloEjemplo()
        {
            // Cubo de ejemplo como máquina CNC
            var meshBuilder = new MeshBuilder();
            meshBuilder.AddBox(new Point3D(0, 0, 0), 2, 1, 0.5);
            meshBuilder.AddBox(new Point3D(0, 0, 0.5), 0.5, 0.5, 1); // Eje Z

            var geometry = meshBuilder.ToMesh();
            var material = MaterialHelper.CreateMaterial(Colors.SteelBlue);

            modeloCNC = new Model3DGroup();
            modeloCNC.Children.Add(new GeometryModel3D(geometry, material));

            var visual = new ModelVisual3D();
            visual.Content = modeloCNC;
            viewport.Children.Add(visual);
        }

        /// <summary>
        /// Mover el modelo CNC base
        /// </summary>
        public void MoverCNC(double x, double y, double z, double rotacionX = 0, double rotacionY = 0, double rotacionZ = 0)
        {
            if (modeloCNC != null)
            {
                var transformGroup = new Transform3DGroup();

                // Rotaciones
                transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), rotacionX)));
                transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), rotacionY)));
                transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), rotacionZ)));

                // Traslación
                transformGroup.Children.Add(new TranslateTransform3D(x, y, z));

                modeloCNC.Transform = transformGroup;
            }
        }

        /// <summary>
        /// Importar modelos STL adicionales (método público para llamar desde MainWindow)
        /// </summary>
        public void CargarModeloImportado(string rutaArchivo)
        {
            try
            {
                if (!File.Exists(rutaArchivo))
                {
                    MessageBox.Show($"Archivo no encontrado: {rutaArchivo}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Eliminar modelo anterior si existe
                if (modeloImportado != null)
                {
                    viewport.Children.Remove(modeloImportado);
                }

                var importador = new ModelImporter();
                var modelo = importador.Load(rutaArchivo);

                modeloImportado = new ModelVisual3D();
                modeloImportado.Content = modelo;

                // Posición inicial del modelo importado (a la derecha del CNC)
                var transform = new TranslateTransform3D(3, 0, 0);
                modeloImportado.Transform = transform;

                viewport.Children.Add(modeloImportado);

                MessageBox.Show($"Modelo STL cargado correctamente:\n{Path.GetFileName(rutaArchivo)}",
                    "Importación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando modelo STL:\n{ex.Message}\n\n{ex.StackTrace}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Alterar posición y rotación del modelo importado
        /// </summary>
        public void MoverModeloImportado(double x, double y, double z, double rotacionX = 0, double rotacionY = 0, double rotacionZ = 0)
        {
            if (modeloImportado != null)
            {
                var transformGroup = new Transform3DGroup();

                // Rotaciones
                transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), rotacionX)));
                transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), rotacionY)));
                transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), rotacionZ)));

                // Traslación
                transformGroup.Children.Add(new TranslateTransform3D(x, y, z));

                modeloImportado.Transform = transformGroup;
            }
        }

        // Métodos para los botones del control
        private void ImportarSTL_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Archivos STL (*.stl)|*.stl|Todos los archivos (*.*)|*.*",
                Title = "Importar modelo STL"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                CargarModeloImportado(openFileDialog.FileName);
            }
        }

        private void ResetVista_Click(object sender, RoutedEventArgs e)
        {
            ConfigurarViewport();
        }

        private void MoverCNC_Click(object sender, RoutedEventArgs e)
        {
            // Ejemplo: mover el CNC a una nueva posición
            MoverCNC(1, 0.5, 0.2, 0, 45, 0);
        }
    }
}

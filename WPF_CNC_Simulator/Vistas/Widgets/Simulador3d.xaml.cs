using HelixToolkit.Wpf;
using Microsoft.Win32;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using IO = System.IO;

namespace WPF_CNC_Simulator.Vistas.Widgets
{
    /// <summary>
    /// Lógica de interacción para Simulador3d.xaml
    /// </summary>
    public partial class Simulador3d : UserControl
    {
        private Model3DGroup modeloCNC;
        private ModelVisual3D modeloImportado;
        //private string rutaModeloCNC = @"..\..\Vistas\Modelos3D\cncceti.stl";//@"Modelos3D\cnceti.stl"; // Ruta relativa

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

        // 1. Cargar modelo CNC inicial
        private string rutaModeloCNC = @"Vistas\Modelos3D\cnceti.stl";

        private void CargarModeloCNC()
        {
            try
            {
                string directorioBase = AppDomain.CurrentDomain.BaseDirectory;
                string rutaCompleta = IO.Path.Combine(directorioBase, rutaModeloCNC);

                // Debug: Verificar ruta
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

                MessageBox.Show($"Modelo CNC no encontrado en:\n{rutaCompleta}");
                CrearModeloEjemplo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando modelo CNC: {ex.Message}");
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

        // 2. Mover el modelo CNC base
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

        // 3. Importar modelos STL adicionales
        private void ImportarSTL_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Archivos STL (*.stl)|*.stl",
                Title = "Importar modelo STL"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                CargarModeloImportado(openFileDialog.FileName);
            }
        }

        private void CargarModeloImportado(string rutaArchivo)
        {
            try
            {
                var importador = new ModelImporter();
                var modelo = importador.Load(rutaArchivo);

                modeloImportado = new ModelVisual3D();
                modeloImportado.Content = modelo;

                // Posición inicial del modelo importado
                var transform = new TranslateTransform3D(3, 0, 0); // A la derecha del CNC
                modeloImportado.Transform = transform;

                viewport.Children.Add(modeloImportado);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando modelo STL: {ex.Message}");
            }
        }

        // 4. Alterar posición y rotación del modelo importado
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

        // Métodos para los botones
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

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

        // Agrega estas variables privadas
        private ModelVisual3D mallaMetrica;
        private const double TAMANO_MALLA = 600;
        private const double ESPACIO_LINEAS = 5.0;

        private ModelVisual3D modeloImportado;
        private double posicionXImportado = 0;
        private double posicionYImportado = 0;
        private double escalaImportado = 1.0;
        private double rotacionZImportado = 0;

        public event Action<string, double> PropiedadCambiada;
        /// <summary>
        /// Constructor de la clase Simulador3d
        /// </summary>
        public Simulador3d()
        {
            InitializeComponent();
            CargarModelosCNC();
            ConfigurarViewport();
            DibujarMallaMetrica();

        }

        /// <summary>
        /// Configura el control de propiedades interno
        /// </summary>
        public void ConfigurarControlPropiedades()
        {
            try
            {
                if (PropiedadesPieza != null)
                {
                    // Asignar referencia de este simulador al control de propiedades
                    PropiedadesPieza.Simulador3D = this;

                    // Suscribirse a eventos del control de propiedades
                    PropiedadesPieza.PropiedadCambiada += (propiedad, valor) =>
                    {
                        // Disparar el evento hacia MainWindow
                        PropiedadCambiada?.Invoke(propiedad, valor);

                        // Debug
                        Console.WriteLine($"Propiedad cambiada desde control: {propiedad} = {valor}");
                    };

                    // Actualizar estado inicial
                    ActualizarControlPropiedades();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configurando control de propiedades: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza los valores visuales del control de propiedades
        /// </summary>
        public void ActualizarControlPropiedades()
        {
            try
            {
                if (PropiedadesPieza != null)
                {
                    // Habilitar/deshabilitar controles según si hay modelo cargado
                    PropiedadesPieza.SetHabilitado(TieneModeloImportado());

                    // Actualizar valores numéricos
                    PropiedadesPieza.ActualizarValoresVisuales();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando control de propiedades: {ex.Message}");
            }
        }
        /// <summary>
        /// Obtiene si hay un modelo importado cargado
        /// </summary>
        /// <returns>True si hay modelo importado cargado</returns>
        public bool TieneModeloImportado()
        {
            return modeloImportado != null;
        }

        /// <summary>
        /// Actualiza las propiedades del control de propiedades cuando se carga un modelo
        /// </summary>
        public void NotificarModeloCargado()
        {
            // Este método puede ser llamado desde MainWindow para actualizar la UI
            Console.WriteLine("Modelo importado cargado - propiedades actualizadas");
        }
        /// <summary>
        /// Establece la posicion en el eje X del modelo importado
        /// </summary>
        /// <param name="posicionX">Posicion en el eje X</param>
        public void EstablecerPosicionXImportado(double posicionX)
        {
            if (modeloImportado != null)
            {
                posicionXImportado = posicionX;
                AplicarTransformacionesImportado();
                Console.WriteLine($"Posicion X del modelo importado: {posicionX}");
            }
        }

        /// <summary>
        /// Establece la posicion en el eje Y del modelo importado
        /// </summary>
        /// <param name="posicionY">Posicion en el eje Y</param>
        public void EstablecerPosicionYImportado(double posicionY)
        {
            if (modeloImportado != null)
            {
                posicionYImportado = posicionY;
                AplicarTransformacionesImportado();
                Console.WriteLine($"Posicion Y del modelo importado: {posicionY}");
            }
        }

        /// <summary>
        /// Establece la escala del modelo importado en todos los ejes
        /// </summary>
        /// <param name="escala">Factor de escala (1.0 = tamaño original)</param>
        public void EstablecerEscalaImportado(double escala)
        {
            if (modeloImportado != null && escala > 0)
            {
                escalaImportado = escala;
                AplicarTransformacionesImportado();
                Console.WriteLine($"Escala del modelo importado: {escala}");
            }
        }

        /// <summary>
        /// Establece la rotacion del modelo importado sobre el eje Z
        /// </summary>
        /// <param name="anguloGrados">Angulo de rotacion en grados</param>
        public void EstablecerRotacionZImportado(double anguloGrados)
        {
            if (modeloImportado != null)
            {
                rotacionZImportado = anguloGrados;
                AplicarTransformacionesImportado();
                Console.WriteLine($"Rotacion Z del modelo importado: {anguloGrados}°");
            }
        }

        /// <summary>
        /// Aplica todas las transformaciones acumuladas al modelo importado
        /// </summary>
        private void AplicarTransformacionesImportado()
        {
            if (modeloImportado != null)
            {
                var grupoTransformaciones = new Transform3DGroup();

                // Escala
                grupoTransformaciones.Children.Add(new ScaleTransform3D(escalaImportado, escalaImportado, escalaImportado));

                // Rotacion en Z
                grupoTransformaciones.Children.Add(new RotateTransform3D(
                    new AxisAngleRotation3D(new Vector3D(0, 0, 1), rotacionZImportado)));

                // Traslacion
                grupoTransformaciones.Children.Add(new TranslateTransform3D(
                    posicionXImportado, posicionYImportado, 0));

                modeloImportado.Transform = grupoTransformaciones;
            }
        }

        /// <summary>
        /// Obtiene la posicion actual en X del modelo importado
        /// </summary>
        /// <returns>Posicion actual en el eje X</returns>
        public double ObtenerPosicionXImportado()
        {
            return posicionXImportado;
        }

        /// <summary>
        /// Obtiene la posicion actual en Y del modelo importado
        /// </summary>
        /// <returns>Posicion actual en el eje Y</returns>
        public double ObtenerPosicionYImportado()
        {
            return posicionYImportado;
        }

        /// <summary>
        /// Obtiene la escala actual del modelo importado
        /// </summary>
        /// <returns>Factor de escala actual</returns>
        public double ObtenerEscalaImportado()
        {
            return escalaImportado;
        }

        /// <summary>
        /// Obtiene la rotacion actual en Z del modelo importado
        /// </summary>
        /// <returns>Angulo de rotacion en grados</returns>
        public double ObtenerRotacionZImportado()
        {
            return rotacionZImportado;
        }

        /// <summary>
        /// Resetea todas las transformaciones del modelo importado a sus valores por defecto
        /// </summary>
        public void ResetearTransformacionesImportado()
        {
            posicionXImportado = 0;
            posicionYImportado = 0;
            escalaImportado = 1.0;
            rotacionZImportado = 0;
            AplicarTransformacionesImportado();
            Console.WriteLine("Transformaciones del modelo importado reseteadas");
        }
        /// <summary>
        /// Dibuja una malla metrica en el plano Z=0
        /// </summary>
        private void DibujarMallaMetrica()
        {
            mallaMetrica = new ModelVisual3D();
            var grupoMalla = new Model3DGroup();

            var materialLineas = new DiffuseMaterial(new SolidColorBrush(Colors.WhiteSmoke));
            var geometriaLineas = new MeshGeometry3D();

            // Lineas en direccion X (horizontales)
            for (double y = -TAMANO_MALLA; y <= TAMANO_MALLA; y += ESPACIO_LINEAS)
            {
                geometriaLineas.Positions.Add(new Point3D(-TAMANO_MALLA, y, 0));
                geometriaLineas.Positions.Add(new Point3D(TAMANO_MALLA, y, 0));
            }

            // Lineas en direccion Y (verticales)
            for (double x = -TAMANO_MALLA; x <= TAMANO_MALLA; x += ESPACIO_LINEAS)
            {
                geometriaLineas.Positions.Add(new Point3D(x, -TAMANO_MALLA, 0));
                geometriaLineas.Positions.Add(new Point3D(x, TAMANO_MALLA, 0));
            }

            // Crear indices para las lineas
            for (int i = 0; i < geometriaLineas.Positions.Count; i += 2)
            {
                
                geometriaLineas.TriangleIndices.Add(i);
                geometriaLineas.TriangleIndices.Add(i + 1);
            }

            var modeloLineas = new GeometryModel3D(geometriaLineas, materialLineas);
            grupoMalla.Children.Add(modeloLineas);

            mallaMetrica.Content = grupoMalla;
            viewport.Children.Add(mallaMetrica);
        }

       
        

        /// <summary>
        /// Configura la cámara y viewport 3D
        /// </summary>
        private void ConfigurarViewport()
        {
            viewport.Camera = new PerspectiveCamera
            {
                Position = new Point3D(700, 750, 550),
                LookDirection = new Vector3D(-5, -5, -5),
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

        // Modifica el método CargarModeloImportado para actualizar el control
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

                // Eliminar modelo anterior si existe
                if (modeloImportado != null)
                {
                    viewport.Children.Remove(modeloImportado);
                }

                var importador = new ModelImporter();
                var modelo = importador.Load(ruta_archivo);

                modeloImportado = new ModelVisual3D();
                modeloImportado.Content = modelo;

                // Resetear transformaciones
                ResetearTransformacionesImportado();

                viewport.Children.Add(modeloImportado);

                // Actualizar control de propiedades después de cargar
                ActualizarControlPropiedades();

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
        // Agrega estos metodos al final de la clase para facilitar el acceso desde botones
        private void DebugCamaraClick(object sender, RoutedEventArgs e)
        {
            MostrarInfoCamara();
        }
        public void MostrarInfoCamara()
        {
            if (viewport?.Camera is PerspectiveCamera camara)
            {
                Console.WriteLine("=== DEBUG CAMARA ===");
                Console.WriteLine($"Posicion: X={camara.Position.X:F2}, Y={camara.Position.Y:F2}, Z={camara.Position.Z:F2}");
                Console.WriteLine($"Direccion Mirada: X={camara.LookDirection.X:F2}, Y={camara.LookDirection.Y:F2}, Z={camara.LookDirection.Z:F2}");
                Console.WriteLine($"Direccion Arriba: X={camara.UpDirection.X:F2}, Y={camara.UpDirection.Y:F2}, Z={camara.UpDirection.Z:F2}");
                Console.WriteLine($"Campo de Vision: {camara.FieldOfView:F2}°");

                // Calcular punto al que esta mirando
                Point3D puntoMirada = camara.Position + camara.LookDirection;
                Console.WriteLine($"Punto de Mirada: X={puntoMirada.X:F2}, Y={puntoMirada.Y:F2}, Z={puntoMirada.Z:F2}");

                // Calcular distancia al punto de mirada
                double distancia = camara.LookDirection.Length;
                Console.WriteLine($"Distancia al objetivo: {distancia:F2}");
                Console.WriteLine("=====================");
                MessageBox.Show(
                    $"Posicion: X={camara.Position.X:F2}, Y={camara.Position.Y:F2}, Z={camara.Position.Z:F2} " +
                    $"\nDireccion Mirada: X={camara.LookDirection.X:F2}, Y={camara.LookDirection.Y:F2}, Z={camara.LookDirection.Z:F2}" +
                    $"\nDireccion Arriba: X={camara.UpDirection.X:F2}, Y={camara.UpDirection.Y:F2}, Z={camara.UpDirection.Z:F2}" +
                    $"\nPunto de Mirada: X={puntoMirada.X:F2}, Y={puntoMirada.Y:F2}, Z={puntoMirada.Z:F2}",
                    "Debug", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                Console.WriteLine("Error: No hay camara PerspectiveCamera activa");
            }
        }

        //MEOTOD DE PRUEBA PARA MOVER EJES
        /**
         * 
         * 
         * 
         *  public void MoverEjesClick(object sender, RoutedEventArgs e)
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
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         */






    }
}
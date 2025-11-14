using HelixToolkit.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using WPF_CNC_Simulator.Services;

namespace WPF_CNC_Simulator.Vistas.Widgets
{
    /// <summary>
    /// Lógica de interacción para Simulador3d.xaml
    /// </summary>
    public partial class Simulador3d : UserControl
    {
        // Campos y constantes
        private ModelVisual3D modeloBase;
        private ModelVisual3D modeloEjeY;
        private ModelVisual3D modeloEjeX;
        
        private ModelVisual3D modeloImportado;

        private Material materialBase = MaterialHelper.CreateMaterial(Colors.DimGray);
        private Material materialEjeY = MaterialHelper.CreateMaterial(Colors.DarkGray);
        private Material materialEjeX = MaterialHelper.CreateMaterial(Colors.LightSlateGray);

        private double posicionEjeX = 0;
        private double posicionEjeY = 0;
        private double posicionXImportado = 0;
        private double posicionYImportado = 0;
        private double escalaImportado = 1.0;
        private double rotacionZImportado = 0;

        private const string RUTA_MODELO_BASE = @"Vistas\Modelos3D\cnc_base.stl";
        private const string RUTA_MODELO_EJE_Y = @"Vistas\Modelos3D\cnc_eje_y.stl";
        private const string RUTA_MODELO_EJE_X = @"Vistas\Modelos3D\cnc_eje_x.stl";
        
        private const int TASA_ACTUALIZACION_MS = 25;

        // Variables para animación G-code
        private DispatcherTimer timerAnimacion;
        private InterpretadorGCode interpretador;
        private List<ComandoGCode> comandosGCode;
        private List<ResultadoEjecucion> resultadosEjecucion;
        private int indiceComandoActual = 0;
        private DateTime tiempoInicioMovimiento;
        private bool animacionEnProgreso = false;
        private double posicionInicialX;
        private double posicionInicialY;
        private double posicionObjetivoX;
        private double posicionObjetivoY;
        private double duracionMovimientoActual;

        // Variables para zoom
        private double zoomSpeed = 0.1;
        private double minZoomDistance = 200;
        private double maxZoomDistance = 1500;

        // Eventos
        public event Action<string, double> PropiedadCambiada;

        // ===== CONSTRUCTOR E INICIALIZACIÓN =====
        /// <summary>
        /// Constructor de la clase Simulador3d
        /// </summary>
        public Simulador3d()
        {
            InitializeComponent();
            CargarModelosCNC();
            ConfigurarViewport();
            InicializarSistemaAnimacion();
        }

        // ===== CONFIGURACIÓN INICIAL =====
        private void ConfigurarViewport()
        {
            viewport.Camera = new PerspectiveCamera
            {
                Position = new Point3D(700, 750, 550),
                LookDirection = new Vector3D(-700, -750, -550),
                UpDirection = new Vector3D(0, 0, 1),
                FieldOfView = 60
            };

            viewport.IsZoomEnabled = false;
            viewport.MouseWheel += Viewport_MouseWheel;
            viewport.Focusable = true;
        }

        private void InicializarSistemaAnimacion()
        {
            interpretador = new InterpretadorGCode();
            comandosGCode = new List<ComandoGCode>();
            resultadosEjecucion = new List<ResultadoEjecucion>();

            timerAnimacion = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(TASA_ACTUALIZACION_MS)
            };
            timerAnimacion.Tick += TimerAnimacion_Tick;

            Console.WriteLine($"Sistema de animación inicializado (tasa: {TASA_ACTUALIZACION_MS}ms)");
        }

        // ===== CARGA DE MODELOS 3D =====
        private void CargarModelosCNC()
        {
            try
            {
                CargarModeloIndividual(RUTA_MODELO_BASE, ref modeloBase, "base", materialBase);
                CargarModeloIndividual(RUTA_MODELO_EJE_Y, ref modeloEjeY, "eje_y", materialEjeY);
                CargarModeloIndividual(RUTA_MODELO_EJE_X, ref modeloEjeX, "eje_x", materialEjeX);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando modelos CNC: {ex.Message}");
                CrearModelosEjemplo();
            }
        }

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
                modelo_geometrico.BackMaterial = material;
            }
        }

        private void CrearModelosEjemplo()
        {
            var mesh_builder_base = new MeshBuilder();
            mesh_builder_base.AddBox(new Point3D(0, 0, 0), 4, 3, 0.5);
            var geometria_base = mesh_builder_base.ToMesh();
            var material_base = MaterialHelper.CreateMaterial(Colors.DarkGray);

            modeloBase = new ModelVisual3D();
            modeloBase.Content = new GeometryModel3D(geometria_base, material_base);
            viewport.Children.Add(modeloBase);

            var mesh_builder_eje_y = new MeshBuilder();
            mesh_builder_eje_y.AddBox(new Point3D(0, 0, 0.5), 3, 0.3, 0.3);
            var geometria_eje_y = mesh_builder_eje_y.ToMesh();
            var material_eje_y = MaterialHelper.CreateMaterial(Colors.Blue);

            modeloEjeY = new ModelVisual3D();
            modeloEjeY.Content = new GeometryModel3D(geometria_eje_y, material_eje_y);
            viewport.Children.Add(modeloEjeY);

            var mesh_builder_eje_x = new MeshBuilder();
            mesh_builder_eje_x.AddBox(new Point3D(0, 0, 0.8), 0.3, 0.3, 0.3);
            var geometria_eje_x = mesh_builder_eje_x.ToMesh();
            var material_eje_x = MaterialHelper.CreateMaterial(Colors.Red);

            modeloEjeX = new ModelVisual3D();
            modeloEjeX.Content = new GeometryModel3D(geometria_eje_x, material_eje_x);
            viewport.Children.Add(modeloEjeX);
        }

        // ===== CONTROL DE MOVIMIENTO DE EJES =====
        public void MoverEjeX(double desplazamiento)
        {
            if (modeloEjeX != null)
            {
                var transformacion = new TranslateTransform3D(desplazamiento, 0, 0);
                modeloEjeX.Transform = transformacion;
            }
        }

        public void MoverEjeY(double desplazamiento)
        {
            if (modeloEjeY != null)
            {
                var transformacion = new TranslateTransform3D(0, desplazamiento, 0);
                modeloEjeY.Transform = transformacion;
            }
        }

        public void MoverEjes(double desplazamiento_x, double desplazamiento_y)
        {
            MoverEjeX(desplazamiento_x);
            MoverEjeY(desplazamiento_y);
            MoverModeloImportadoY(desplazamiento_y);
        }

        // ===== GESTIÓN DE MODELO IMPORTADO =====
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

                if (modeloImportado != null)
                {
                    viewport.Children.Remove(modeloImportado);
                }

                var importador = new ModelImporter();
                var modelo = importador.Load(ruta_archivo);

                modeloImportado = new ModelVisual3D();
                modeloImportado.Content = modelo;

                ResetearTransformacionesImportado();

                viewport.Children.Add(modeloImportado);

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

        private void MoverModeloImportadoY(double desplazamientoY)
        {
            if (modeloImportado != null)
            {
                posicionYImportado = desplazamientoY;
                AplicarTransformacionesImportado();
                Console.WriteLine($"Modelo importado movido a Y: {desplazamientoY:F2}");
            }
        }

        private void AplicarTransformacionesImportado()
        {
            if (modeloImportado != null)
            {
                var grupoTransformaciones = new Transform3DGroup();

                grupoTransformaciones.Children.Add(new ScaleTransform3D(escalaImportado, escalaImportado, escalaImportado));
                grupoTransformaciones.Children.Add(new RotateTransform3D(
                    new AxisAngleRotation3D(new Vector3D(0, 0, 1), rotacionZImportado)));
                grupoTransformaciones.Children.Add(new TranslateTransform3D(
                    posicionXImportado, posicionYImportado, 0));

                modeloImportado.Transform = grupoTransformaciones;
            }
        }

        // ===== PROPIEDADES DEL MODELO IMPORTADO =====
        public void EstablecerPosicionXImportado(double posicionX)
        {
            if (modeloImportado != null)
            {
                posicionXImportado = posicionX;
                AplicarTransformacionesImportado();
                Console.WriteLine($"Posicion X del modelo importado: {posicionX}");
            }
        }

        public void EstablecerPosicionYImportado(double posicionY)
        {
            if (modeloImportado != null && !animacionEnProgreso)
            {
                posicionYImportado = posicionY;
                AplicarTransformacionesImportado();
                Console.WriteLine($"Posicion Y del modelo importado: {posicionY}");
            }
        }

        public void EstablecerEscalaImportado(double escala)
        {
            if (modeloImportado != null && escala > 0)
            {
                escalaImportado = escala;
                AplicarTransformacionesImportado();
                Console.WriteLine($"Escala del modelo importado: {escala}");
            }
        }

        public void EstablecerRotacionZImportado(double anguloGrados)
        {
            if (modeloImportado != null)
            {
                rotacionZImportado = anguloGrados;
                AplicarTransformacionesImportado();
                Console.WriteLine($"Rotacion Z del modelo importado: {anguloGrados}°");
            }
        }

        public void ResetearTransformacionesImportado()
        {
            posicionXImportado = 0;
            posicionYImportado = 0;
            escalaImportado = 1.0;
            rotacionZImportado = 0;
            AplicarTransformacionesImportado();
            Console.WriteLine("Transformaciones del modelo importado reseteadas");
        }

        public double ObtenerPosicionXImportado()
        {
            return posicionXImportado;
        }

        public double ObtenerPosicionYImportado()
        {
            return posicionYImportado;
        }

        public double ObtenerEscalaImportado()
        {
            return escalaImportado;
        }

        public double ObtenerRotacionZImportado()
        {
            return rotacionZImportado;
        }

        public Point3D ObtenerPosicionModeloImportado()
        {
            return new Point3D(posicionXImportado, posicionYImportado, 0);
        }

        public bool TieneModeloImportado()
        {
            return modeloImportado != null;
        }

        public void NotificarModeloCargado()
        {
            Console.WriteLine("Modelo importado cargado - propiedades actualizadas");
        }

        // ===== CONTROL DE PROPIEDADES =====
        public void ConfigurarControlPropiedades()
        {
            try
            {
                if (PropiedadesPieza != null)
                {
                    PropiedadesPieza.Simulador3D = this;

                    PropiedadesPieza.PropiedadCambiada += (propiedad, valor) =>
                    {
                        PropiedadCambiada?.Invoke(propiedad, valor);
                        Console.WriteLine($"Propiedad cambiada desde control: {propiedad} = {valor}");
                    };

                    ActualizarControlPropiedades();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configurando control de propiedades: {ex.Message}");
            }
        }

        public void ActualizarControlPropiedades()
        {
            try
            {
                if (PropiedadesPieza != null)
                {
                    PropiedadesPieza.SetHabilitado(TieneModeloImportado());
                    PropiedadesPieza.ActualizarValoresVisuales();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando control de propiedades: {ex.Message}");
            }
        }

        // ===== ANIMACIÓN G-CODE =====
        public void IniciarAnimacionGCode(string codigoG)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigoG))
                {
                    MessageBox.Show("No hay código G para reproducir.",
                        "Sin código", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (animacionEnProgreso)
                {
                    PausarAnimacionGCode();
                }

                interpretador.Resetear();
                comandosGCode = interpretador.ParsearCodigoCompleto(codigoG);

                if (comandosGCode.Count == 0)
                {
                    MessageBox.Show("No se encontraron comandos válidos en el código G.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                resultadosEjecucion.Clear();
                interpretador.Resetear();

                foreach (var comando in comandosGCode)
                {
                    var resultado = interpretador.EjecutarComando(comando);
                    if (resultado != null)
                    {
                        resultadosEjecucion.Add(resultado);
                    }
                }

                Console.WriteLine($"Código G parseado: {comandosGCode.Count} comandos, {resultadosEjecucion.Count} movimientos");

                interpretador.Resetear();
                indiceComandoActual = 0;
                animacionEnProgreso = true;

                posicionEjeX = 0;
                posicionEjeY = 0;

                if (modeloImportado != null)
                {
                    posicionYImportado = 0;
                }

                MoverEjes(0, 0);
                timerAnimacion.Start();

                Console.WriteLine("Animación G-code iniciada");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al iniciar animación: {ex.Message}");
                MessageBox.Show($"Error al iniciar la animación:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void PausarAnimacionGCode()
        {
            if (timerAnimacion != null && timerAnimacion.IsEnabled)
            {
                timerAnimacion.Stop();
                animacionEnProgreso = false;
                Console.WriteLine("Animación pausada");
            }
        }

        public void ReanudarAnimacionGCode()
        {
            if (!animacionEnProgreso && indiceComandoActual < resultadosEjecucion.Count)
            {
                animacionEnProgreso = true;
                timerAnimacion.Start();
                Console.WriteLine("Animación reanudada");
            }
        }

        public void AvanzarSiguienteLinea()
        {
            if (indiceComandoActual < resultadosEjecucion.Count)
            {
                EjecutarComandoActual();
                indiceComandoActual++;
                Console.WriteLine($"Avanzado a línea {indiceComandoActual}/{resultadosEjecucion.Count}");
            }
        }

        public void RetrocederLineaAnterior()
        {
            if (indiceComandoActual > 0)
            {
                indiceComandoActual--;

                for (int i = indiceComandoActual; i >= 0; i--)
                {
                    var resultado = resultadosEjecucion[i];
                    if (resultado.RequiereMovimiento)
                    {
                        posicionEjeX = resultado.PosicionInicialX;
                        posicionEjeY = resultado.PosicionInicialY;
                        MoverEjes(posicionEjeX, posicionEjeY);
                        break;
                    }
                }

                Console.WriteLine($"Retrocedido a línea {indiceComandoActual}/{resultadosEjecucion.Count}");
            }
        }

        private void TimerAnimacion_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!animacionEnProgreso || indiceComandoActual >= resultadosEjecucion.Count)
                {
                    FinalizarAnimacion();
                    return;
                }

                var resultadoActual = resultadosEjecucion[indiceComandoActual];

                if (!resultadoActual.RequiereMovimiento)
                {
                    indiceComandoActual++;
                    return;
                }

                if (duracionMovimientoActual == 0)
                {
                    IniciarNuevoMovimiento(resultadoActual);
                }

                var tiempoTranscurrido = (DateTime.Now - tiempoInicioMovimiento).TotalSeconds;
                var progreso = Math.Min(tiempoTranscurrido / duracionMovimientoActual, 1.0);

                posicionEjeX = posicionInicialX + (posicionObjetivoX - posicionInicialX) * progreso;
                posicionEjeY = posicionInicialY + (posicionObjetivoY - posicionInicialY) * progreso;

                MoverEjes(posicionEjeX, posicionEjeY);

                if (progreso >= 1.0)
                {
                    posicionEjeX = posicionObjetivoX;
                    posicionEjeY = posicionObjetivoY;
                    MoverEjes(posicionEjeX, posicionEjeY);

                    indiceComandoActual++;
                    duracionMovimientoActual = 0;

                    Console.WriteLine($"Comando {indiceComandoActual}/{resultadosEjecucion.Count} completado - X:{posicionEjeX:F2} Y:{posicionEjeY:F2}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en timer de animación: {ex.Message}");
                FinalizarAnimacion();
            }
        }

        private void IniciarNuevoMovimiento(ResultadoEjecucion resultado)
        {
            posicionInicialX = resultado.PosicionInicialX;
            posicionInicialY = resultado.PosicionInicialY;
            posicionObjetivoX = resultado.PosicionFinalX;
            posicionObjetivoY = resultado.PosicionFinalY;

            duracionMovimientoActual = Math.Max(resultado.DuracionSegundos, 0.05);

            if (resultado.EsMovimientoRapido)
            {
                duracionMovimientoActual *= 0.5;
            }

            tiempoInicioMovimiento = DateTime.Now;

            Console.WriteLine($"Nuevo movimiento: ({posicionInicialX:F2},{posicionInicialY:F2}) → ({posicionObjetivoX:F2},{posicionObjetivoY:F2}) en {duracionMovimientoActual:F2}s");
        }

        private void EjecutarComandoActual()
        {
            if (indiceComandoActual < resultadosEjecucion.Count)
            {
                var resultado = resultadosEjecucion[indiceComandoActual];

                if (resultado.RequiereMovimiento)
                {
                    posicionEjeX = resultado.PosicionFinalX;
                    posicionEjeY = resultado.PosicionFinalY;
                    MoverEjes(posicionEjeX, posicionEjeY);
                }
            }
        }

        private void FinalizarAnimacion()
        {
            timerAnimacion.Stop();
            animacionEnProgreso = false;
            duracionMovimientoActual = 0;

            Console.WriteLine("Animación G-code finalizada");
            MessageBox.Show("Simulación de mecanizado completada.",
                "Finalizado", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ===== CONTROL DE CÁMARA Y ZOOM =====
        private void Viewport_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (viewport.Camera is PerspectiveCamera camara)
            {
                try
                {
                    Vector3D lookDirection = camara.LookDirection;
                    lookDirection.Normalize();

                    double distanciaActual = camara.LookDirection.Length;
                    double zoomFactor = e.Delta > 0 ? (1 - zoomSpeed) : (1 + zoomSpeed);

                    double nuevaDistancia = distanciaActual * zoomFactor;

                    if (nuevaDistancia < minZoomDistance)
                    {
                        nuevaDistancia = minZoomDistance;
                        zoomFactor = nuevaDistancia / distanciaActual;
                    }
                    else if (nuevaDistancia > maxZoomDistance)
                    {
                        nuevaDistancia = maxZoomDistance;
                        zoomFactor = nuevaDistancia / distanciaActual;
                    }

                    Vector3D nuevoLookDirection = camara.LookDirection * zoomFactor;
                    Point3D puntoMira = camara.Position + camara.LookDirection;
                    Point3D nuevaPosicion = puntoMira - nuevoLookDirection;

                    camara.Position = nuevaPosicion;
                    camara.LookDirection = nuevoLookDirection;

                    Console.WriteLine($"Zoom aplicado - Posición: {camara.Position}, Distancia: {nuevaDistancia:F2}");

                    e.Handled = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en zoom: {ex.Message}");
                }
            }
        }

        // ===== MÉTODOS DE DEBUG Y EVENTOS UI =====
        
        

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

       
    }
}
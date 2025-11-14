/*
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WPF_CNC_Simulator.Services
{
    /// <summary>
    /// Servicio para convertir archivos STL a G-code para fresadora CNC de 3 ejes
    /// Configuración: 300x300x150mm, fresa 5mm, madera, 0.1mm precisión
    /// </summary>
    public class SlicerServiceOld
    {
        private readonly string _slic3rPath;
        private readonly string _configPath;

        public SlicerServiceOld(string slic3rExecutablePath, string configPath = null)
        {
            _slic3rPath = slic3rExecutablePath;
            _configPath = configPath;

            if (!File.Exists(_slic3rPath))
                throw new FileNotFoundException($"No se encontró Slic3r en: {_slic3rPath}");
        }

        public async Task<SlicingResult> SliceSTLAsync(
            string stlFilePath,
            string outputGcodePath,
            Action<string> progressCallback = null)
        {
            var result = new SlicingResult();

            try
            {
                if (!File.Exists(stlFilePath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Archivo STL no encontrado: {stlFilePath}";
                    return result;
                }

                var outputDir = Path.GetDirectoryName(outputGcodePath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                var stlDirectory = Path.GetDirectoryName(stlFilePath);
                var stlFileName = Path.GetFileNameWithoutExtension(stlFilePath);
                var expectedOutputPath = Path.Combine(stlDirectory, stlFileName + ".gcode");

                var arguments = BuildArguments(stlFilePath, outputGcodePath);

                progressCallback?.Invoke($"═══════════════════════════════════════");
                progressCallback?.Invoke($"CONFIGURACIÓN FRESADORA CNC 3 EJES");
                progressCallback?.Invoke($"═══════════════════════════════════════");
                progressCallback?.Invoke($"Área de trabajo: 300x300x150mm");
                progressCallback?.Invoke($"Herramienta: Fresa Ø5mm");
                progressCallback?.Invoke($"Precisión: 0.1mm por paso");
                progressCallback?.Invoke($"Velocidad máx: 10,000 RPM");
                progressCallback?.Invoke($"Material: Madera");
                progressCallback?.Invoke($"═══════════════════════════════════════");
                progressCallback?.Invoke($"Ejecutando: {Path.GetFileName(_slic3rPath)}");
                progressCallback?.Invoke($"Entrada: {Path.GetFileName(stlFilePath)}");
                progressCallback?.Invoke($"Salida esperada: {Path.GetFileName(expectedOutputPath)}");
                progressCallback?.Invoke($"═══════════════════════════════════════");

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _slic3rPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(stlFilePath)
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    var outputBuilder = new System.Text.StringBuilder();
                    var errorBuilder = new System.Text.StringBuilder();

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            progressCallback?.Invoke($"[OUT] {e.Data}");
                            outputBuilder.AppendLine(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            progressCallback?.Invoke($"[INFO] {e.Data}");
                            errorBuilder.AppendLine(e.Data);
                        }
                    };

                    var startTime = DateTime.Now;
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    var timeout = TimeSpan.FromMinutes(3);
                    var completed = await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds));

                    if (!completed)
                    {
                        process.Kill();
                        result.Success = false;
                        result.ErrorMessage = "El proceso excedió el tiempo límite de 5 minutos";
                        return result;
                    }

                    result.ProcessingTime = DateTime.Now - startTime;
                    result.ExitCode = process.ExitCode;
                    result.Output = outputBuilder.ToString();
                    result.ErrorOutput = errorBuilder.ToString();

                    progressCallback?.Invoke($"═══════════════════════════════════════");
                    progressCallback?.Invoke($"Proceso terminado con código: {process.ExitCode}");
                    progressCallback?.Invoke($"═══════════════════════════════════════");

                    if (process.ExitCode == 0)
                    {
                        string foundFile = null;

                        if (File.Exists(expectedOutputPath))
                        {
                            foundFile = expectedOutputPath;
                            progressCallback?.Invoke($"✓ Archivo encontrado en: {expectedOutputPath}");
                        }
                        else if (File.Exists(outputGcodePath))
                        {
                            foundFile = outputGcodePath;
                            progressCallback?.Invoke($"✓ Archivo encontrado en: {outputGcodePath}");
                        }
                        else
                        {
                            var gcodeFiles = Directory.GetFiles(stlDirectory, "*.gcode");
                            if (gcodeFiles.Length > 0)
                            {
                                Array.Sort(gcodeFiles, (a, b) =>
                                    File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                                foundFile = gcodeFiles[0];
                                progressCallback?.Invoke($"✓ Archivo encontrado (búsqueda): {foundFile}");
                            }
                        }

                        if (foundFile != null)
                        {
                            if (foundFile != outputGcodePath)
                            {
                                try
                                {
                                    if (File.Exists(outputGcodePath))
                                        File.Delete(outputGcodePath);
                                    File.Move(foundFile, outputGcodePath);
                                    progressCallback?.Invoke($"→ Archivo movido a: {outputGcodePath}");
                                }
                                catch (Exception ex)
                                {
                                    progressCallback?.Invoke($"⚠ No se pudo mover el archivo: {ex.Message}");
                                    outputGcodePath = foundFile;
                                }
                            }

                            progressCallback?.Invoke($"═══════════════════════════════════════");
                            progressCallback?.Invoke($"Post-procesando G-code para fresadora...");
                            PostProcessGCodeForCNC(outputGcodePath, progressCallback);
                            progressCallback?.Invoke($"✓ Post-procesamiento completado");
                            progressCallback?.Invoke($"═══════════════════════════════════════");

                            result.Success = true;
                            result.OutputFilePath = outputGcodePath;
                            result.FileSize = new FileInfo(outputGcodePath).Length;
                            progressCallback?.Invoke($"✓ Conversión exitosa. Tamaño: {result.FileSize / 1024:F2} KB");
                        }
                        else
                        {
                            result.Success = false;
                            result.ErrorMessage = "El proceso terminó exitosamente pero no se generó el archivo G-code.";
                        }
                    }
                    else
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Slic3r falló con código de salida: {process.ExitCode}";

                        if (!string.IsNullOrEmpty(result.ErrorOutput))
                            result.ErrorMessage += $"\n\nDetalles:\n{result.ErrorOutput}";
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Excepción: {ex.Message}";
                result.ErrorOutput = ex.StackTrace;
                progressCallback?.Invoke($"[EXCEPCIÓN] {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Post-procesa el G-code para adaptarlo a fresadora CNC con comentarios descriptivos
        /// </summary>
        private void PostProcessGCodeForCNC(string gcodeFilePath, Action<string> progressCallback)
        {
            try
            {
                var lines = File.ReadAllLines(gcodeFilePath);
                var processedLines = new System.Collections.Generic.List<string>();

                // Agregar encabezado para fresadora CNC
                processedLines.Add("; ═══════════════════════════════════════════════════════════");
                processedLines.Add("; G-CODE PARA FRESADORA CNC DE 3 EJES");
                processedLines.Add("; ═══════════════════════════════════════════════════════════");
                processedLines.Add("; Especificaciones de la máquina:");
                processedLines.Add(";   • Área de trabajo: 300mm x 300mm x 150mm");
                processedLines.Add(";   • Herramienta: Fresa de corte Ø5mm");
                processedLines.Add(";   • Precisión: 0.1mm por paso");
                processedLines.Add(";   • Velocidad máxima husillo: 10,000 RPM");
                processedLines.Add(";   • Velocidad de trabajo: 8,000 RPM");
                processedLines.Add(";   • Material: Madera");
                processedLines.Add($";   • Generado: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                processedLines.Add("; ═══════════════════════════════════════════════════════════");
                processedLines.Add("");
                processedLines.Add("; ───────────────────────────────────────────────────────────");
                processedLines.Add("; INICIALIZACIÓN DEL SISTEMA");
                processedLines.Add("; ───────────────────────────────────────────────────────────");
                processedLines.Add("G21                     ; Establecer unidades en milímetros");
                processedLines.Add("G90                     ; Modo de posicionamiento absoluto");
                processedLines.Add("G17                     ; Seleccionar plano de trabajo XY");
                processedLines.Add("G94                     ; Velocidad de avance en unidades por minuto");
                processedLines.Add("");
                processedLines.Add("; ───────────────────────────────────────────────────────────");
                processedLines.Add("; POSICIONAMIENTO INICIAL Y SEGURIDAD");
                processedLines.Add("; ───────────────────────────────────────────────────────────");
                processedLines.Add("G28                     ; Ejecutar home en todos los ejes (X, Y, Z)");
                processedLines.Add("G0 Z50.0 F3000.0        ; Subir fresa a altura de seguridad (50mm)");
                processedLines.Add("");
                processedLines.Add("; ───────────────────────────────────────────────────────────");
                processedLines.Add("; ACTIVACIÓN DEL HUSILLO");
                processedLines.Add("; ───────────────────────────────────────────────────────────");
                processedLines.Add("M3 S8000                ; Activar husillo en sentido horario a 8000 RPM");
                processedLines.Add("G4 P2                   ; Pausa de 2 segundos para estabilización del husillo");
                processedLines.Add("");
                processedLines.Add("; ═══════════════════════════════════════════════════════════");
                processedLines.Add("; INICIO DEL CÓDIGO DE MECANIZADO");
                processedLines.Add("; ═══════════════════════════════════════════════════════════");
                processedLines.Add("");

                bool inCode = false;
                int lineCounter = 0;
                int removedLines = 0;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    // Omitir líneas vacías y comentarios del encabezado original
                    if (!inCode && (trimmedLine.StartsWith(";") || string.IsNullOrWhiteSpace(trimmedLine)))
                        continue;

                    if (!string.IsNullOrWhiteSpace(trimmedLine) && !trimmedLine.StartsWith(";"))
                        inCode = true;

                    if (inCode)
                    {
                        // ELIMINAR COMPLETAMENTE (no comentar) líneas con comandos de impresora 3D
                        if (trimmedLine.StartsWith("M104") || trimmedLine.StartsWith("M109") ||
                            trimmedLine.StartsWith("M140") || trimmedLine.StartsWith("M190") ||
                            trimmedLine.StartsWith("M106") || trimmedLine.StartsWith("M107"))
                        {
                            removedLines++;
                            continue; // SALTAR esta línea completamente
                        }

                        // ELIMINAR COMPLETAMENTE líneas que contienen parámetro E (extrusión)
                        if (Regex.IsMatch(trimmedLine, @"\bE[-+]?\d+\.?\d*"))
                        {
                            removedLines++;
                            continue; // SALTAR esta línea completamente
                        }

                        // Procesar líneas válidas
                        if (!string.IsNullOrWhiteSpace(trimmedLine))
                        {
                            // Si ya es un comentario, mantenerlo
                            if (trimmedLine.StartsWith(";"))
                            {
                                processedLines.Add(trimmedLine);
                            }
                            else
                            {
                                // Agregar comentario descriptivo al comando
                                var processedLine = AgregarComentarioDescriptivo(trimmedLine);

                                // Ajustar velocidades si es necesario
                                processedLine = AjustarVelocidades(processedLine);

                                processedLines.Add(processedLine);
                                lineCounter++;
                            }
                        }
                    }
                }

                // Agregar finalización
                processedLines.Add("");
                processedLines.Add("; ═══════════════════════════════════════════════════════════");
                processedLines.Add("; FINALIZACIÓN DEL MECANIZADO");
                processedLines.Add("; ═══════════════════════════════════════════════════════════");
                processedLines.Add("G0 Z50.0 F3000.0        ; Subir fresa a altura de seguridad");
                processedLines.Add("M5                      ; Apagar husillo");
                processedLines.Add("G0 X0 Y0 F3000.0        ; Retornar al punto de origen (home)");
                processedLines.Add("M2                      ; Fin del programa");
                processedLines.Add("");
                processedLines.Add("; ═══════════════════════════════════════════════════════════");
                processedLines.Add($"; Fin del G-code - {lineCounter} líneas de mecanizado");
                processedLines.Add($"; Líneas eliminadas (impresora 3D): {removedLines}");
                processedLines.Add("; ═══════════════════════════════════════════════════════════");

                // Guardar archivo procesado
                File.WriteAllLines(gcodeFilePath, processedLines);

                progressCallback?.Invoke($"  → {lineCounter} líneas de mecanizado procesadas");
                progressCallback?.Invoke($"  → {removedLines} líneas de impresora 3D eliminadas");
            }
            catch (Exception ex)
            {
                progressCallback?.Invoke($"⚠ Error en post-procesamiento: {ex.Message}");
            }
        }

        /// <summary>
        /// Agrega comentario descriptivo a cada comando G-code
        /// </summary>
        private string AgregarComentarioDescriptivo(string linea)
        {
            // Separar comando de posibles comentarios existentes
            var partes = linea.Split(new[] { ';' }, 2);
            var comando = partes[0].Trim();

            if (string.IsNullOrWhiteSpace(comando))
                return linea;

            // Extraer el código del comando (G0, G1, M3, etc.)
            var match = Regex.Match(comando, @"^([GM]\d+)");
            if (!match.Success)
                return linea; // Si no es un comando reconocido, devolver sin cambios

            var codigo = match.Groups[1].Value;
            var comentario = ObtenerComentarioParaComando(codigo, comando);

            // Formatear: alinear comando a 24 caracteres y agregar comentario
            return $"{comando,-24}; {comentario}";
        }

        /// <summary>
        /// Obtiene el comentario descriptivo para cada comando
        /// </summary>
        private string ObtenerComentarioParaComando(string codigo, string lineaCompleta)
        {
            switch (codigo)
            {
                // Comandos de movimiento
                case "G0":
                    return "Movimiento rápido sin cortar (posicionamiento)";
                case "G1":
                    if (lineaCompleta.Contains("Z"))
                        return "Movimiento lineal de corte (eje Z - profundidad)";
                    else
                        return "Movimiento lineal de corte";
                case "G2":
                    return "Movimiento circular horario (arco CW)";
                case "G3":
                    return "Movimiento circular antihorario (arco CCW)";
                case "G4":
                    return "Pausa temporizada";

                // Comandos de configuración
                case "G17":
                    return "Seleccionar plano de trabajo XY";
                case "G18":
                    return "Seleccionar plano de trabajo ZX";
                case "G19":
                    return "Seleccionar plano de trabajo YZ";
                case "G20":
                    return "Unidades en pulgadas";
                case "G21":
                    return "Unidades en milímetros";
                case "G28":
                    return "Retorno a home (punto de referencia)";
                case "G90":
                    return "Modo de coordenadas absolutas";
                case "G91":
                    return "Modo de coordenadas relativas";
                case "G92":
                    return "Establecer posición actual como origen";
                case "G94":
                    return "Velocidad de avance en unidades/minuto";

                // Comandos del husillo
                case "M0":
                    return "Parada del programa (requiere intervención manual)";
                case "M1":
                    return "Parada opcional del programa";
                case "M2":
                    return "Fin del programa";
                case "M3":
                    return "Activar husillo en sentido horario (CW)";
                case "M4":
                    return "Activar husillo en sentido antihorario (CCW)";
                case "M5":
                    return "Detener husillo";
                case "M6":
                    return "Cambio de herramienta";
                case "M30":
                    return "Fin del programa y reinicio";

                default:
                    return "Comando G-code";
            }
        }

        /// <summary>
        /// Ajusta velocidades de avance para fresado seguro
        /// </summary>
        private string AjustarVelocidades(string linea)
        {
            var partes = linea.Split(new[] { ';' }, 2);
            var comando = partes[0].Trim();
            var comentario = partes.Length > 1 ? partes[1] : "";

            // Buscar y ajustar parámetro F (velocidad de avance)
            var match = Regex.Match(comando, @"F([-+]?\d+\.?\d*)");
            if (match.Success)
            {
                double velocidad = double.Parse(match.Groups[1].Value);
                double nuevaVelocidad = velocidad;

                // Limitar velocidades según tipo de movimiento
                if (comando.StartsWith("G1")) // Movimiento de corte
                {
                    if (velocidad > 2000)
                        nuevaVelocidad = 2000.0;
                }
                else if (comando.StartsWith("G0")) // Movimiento rápido
                {
                    if (velocidad > 3000)
                        nuevaVelocidad = 3000.0;
                }

                if (Math.Abs(velocidad - nuevaVelocidad) > 0.1)
                {
                    comando = Regex.Replace(comando, @"F[-+]?\d+\.?\d*", $"F{nuevaVelocidad:F1}");
                }
            }
            else
            {
                // Agregar velocidad por defecto si no tiene
                if (comando.StartsWith("G1") && !comando.Contains("F"))
                    comando += " F1500.0";
                else if (comando.StartsWith("G0") && !comando.Contains("F"))
                    comando += " F3000.0";
            }

            return string.IsNullOrWhiteSpace(comentario) ? comando : $"{comando} ;{comentario}";
        }

        public async Task<SlicingResult> SliceWithCustomSettings(
            string stlFilePath,
            string outputGcodePath,
            CNCMillingSettings settings,
            Action<string> progressCallback = null)
        {
            var tempConfigPath = Path.Combine(Path.GetTempPath(), $"slic3r_config_{Guid.NewGuid()}.ini");

            try
            {
                File.WriteAllText(tempConfigPath, settings.ToIniFormat());
                progressCallback?.Invoke($"Configuración CNC temporal creada: {tempConfigPath}");

                var tempService = new SlicerService(_slic3rPath, tempConfigPath);
                return await tempService.SliceSTLAsync(stlFilePath, outputGcodePath, progressCallback);
            }
            finally
            {
                if (File.Exists(tempConfigPath))
                {
                    try { File.Delete(tempConfigPath); }
                    catch { }
                }
            }
        }

        private string BuildArguments(string stlFilePath, string outputGcodePath)
        {
            var args = "";

            if (!string.IsNullOrEmpty(_configPath) && File.Exists(_configPath))
            {
                args += $"--load \"{_configPath}\" ";
            }
            else
            {
                args += "--layer-height 1.0 ";
                args += "--first-layer-height 1.0 ";
                args += "--perimeters 2 ";
                args += "--top-solid-layers 0 ";
                args += "--bottom-solid-layers 1 ";
                args += "--fill-density 50% ";
                args += "--fill-pattern concentric ";
                args += "--solid-infill-every-layers 1" ;
                args += "--fill-angle 45 ";
                
                args += "--dont-arrange ";
                args += "--center 0,0 ";
                args += "--align-xy 0,0 ";
            }

            args += $"\"{stlFilePath}\"";
            return args;
        }
    }

    public class SlicingResult
    {
        public bool Success { get; set; }
        public string OutputFilePath { get; set; }
        public long FileSize { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public int ExitCode { get; set; }
        public string Output { get; set; }
        public string ErrorOutput { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class CNCMillingSettings
    {
        public double WorkAreaX { get; set; } = 300.0;
        public double WorkAreaY { get; set; } = 300.0;
        public double WorkAreaZ { get; set; } = 150.0;
        public double StepResolution { get; set; } = 0.5;

        public double ToolDiameter { get; set; } = 5.0;
        public int MaxSpindleSpeed { get; set; } = 10000;
        public int WorkingSpindleSpeed { get; set; } = 8000;

        public double CuttingDepth { get; set; } = 1.0;
        public double CuttingFeedrate { get; set; } = 1500.0;
        public double PlungeFeedrate { get; set; } = 500.0;
        public double RapidFeedrate { get; set; } = 3000.0;

        public int Perimeters { get; set; } = 2;
        public int FillDensity { get; set; } = 50;
        public string FillPattern { get; set; } = "concentric";
        public double SafeHeight { get; set; } = 50.0;

        public string Material { get; set; } = "Madera";

        public string ToIniFormat()
        {
            return $@"; ═══════════════════════════════════════════════════════════
; Configuración para Fresadora CNC de 3 ejes
; ═══════════════════════════════════════════════════════════

[print_settings]
layer_height = {CuttingDepth}
first_layer_height = {CuttingDepth}
perimeters = {Perimeters}
top_solid_layers = 0
bottom_solid_layers = 1
fill_density = {FillDensity}%
fill_pattern = {FillPattern}
solid_infill_every_layers = 1
fill_angle = 45
infill_speed = {CuttingFeedrate}
perimeter_speed = {CuttingFeedrate}
travel_speed = {RapidFeedrate}
first_layer_speed = {PlungeFeedrate}

[printer_settings]
temperature = 0
bed_temperature = 0
nozzle_diameter = {ToolDiameter}
--dont-arrange 
--center 0,0 
--align-xy 0,0 
";
        }
    }

    public class SlicerSettings : CNCMillingSettings
    {
        public double LayerHeight
        {
            get => CuttingDepth;
            set => CuttingDepth = value;
        }

        public int TopSolidLayers { get; set; } = 0;
        public int BottomSolidLayers { get; set; } = 1;
        public double PrintSpeed
        {
            get => CuttingFeedrate;
            set => CuttingFeedrate = value;
        }
        public double TravelSpeed
        {
            get => RapidFeedrate;
            set => RapidFeedrate = value;
        }
        public int ExtruderTemperature { get; set; } = 0;
        public int BedTemperature { get; set; } = 0;
        public bool SupportMaterial { get; set; } = false;
    }
}
*/
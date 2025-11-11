using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace WPF_CNC_Simulator.Services
{
    /// <summary>
    /// Servicio para convertir archivos STL a G-code usando Slic3r
    /// </summary>
    public class SlicerService
    {
        private readonly string _slic3rPath;
        private readonly string _configPath;

        /// <summary>
        /// Constructor del servicio
        /// </summary>
        public SlicerService(string slic3rExecutablePath, string configPath = null)
        {
            _slic3rPath = slic3rExecutablePath;
            _configPath = configPath;

            if (!File.Exists(_slic3rPath))
                throw new FileNotFoundException($"No se encontró Slic3r en: {_slic3rPath}");
        }

        /// <summary>
        /// Convierte un archivo STL a G-code de forma asíncrona
        /// </summary>
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

                var arguments = BuildArguments(stlFilePath, outputGcodePath);

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _slic3rPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(_slic3rPath)
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            progressCallback?.Invoke(e.Data);
                            result.Output += e.Data + Environment.NewLine;
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            result.ErrorOutput += e.Data + Environment.NewLine;
                        }
                    };

                    var startTime = DateTime.Now;
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    await Task.Run(() => process.WaitForExit());

                    result.ProcessingTime = DateTime.Now - startTime;
                    result.ExitCode = process.ExitCode;

                    if (process.ExitCode == 0 && File.Exists(outputGcodePath))
                    {
                        result.Success = true;
                        result.OutputFilePath = outputGcodePath;
                        result.FileSize = new FileInfo(outputGcodePath).Length;
                    }
                    else
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Slic3r falló con código de salida: {process.ExitCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Excepción: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Convierte con configuración personalizada
        /// </summary>
        public async Task<SlicingResult> SliceWithCustomSettings(
            string stlFilePath,
            string outputGcodePath,
            SlicerSettings settings,
            Action<string> progressCallback = null)
        {
            var tempConfigPath = Path.Combine(Path.GetTempPath(), $"slic3r_config_{Guid.NewGuid()}.ini");

            try
            {
                File.WriteAllText(tempConfigPath, settings.ToIniFormat());

                var tempService = new SlicerService(_slic3rPath, tempConfigPath);
                return await tempService.SliceSTLAsync(stlFilePath, outputGcodePath, progressCallback);
            }
            finally
            {
                if (File.Exists(tempConfigPath))
                    File.Delete(tempConfigPath);
            }
        }

        private string BuildArguments(string stlFilePath, string outputGcodePath)
        {
            var args = $"--export-gcode \"{stlFilePath}\" --output \"{outputGcodePath}\"";

            if (!string.IsNullOrEmpty(_configPath) && File.Exists(_configPath))
            {
                args += $" --load \"{_configPath}\"";
            }

            args += " --layer-height 0.2";
            args += " --fill-density 20%";
            args += " --perimeters 3";
            args += " --top-solid-layers 3";
            args += " --bottom-solid-layers 3";

            return args;
        }
    }

    /// <summary>
    /// Resultado del proceso de slicing
    /// </summary>
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

    /// <summary>
    /// Configuración personalizada para el slicer
    /// </summary>
    public class SlicerSettings
    {
        public double LayerHeight { get; set; } = 0.2;
        public int FillDensity { get; set; } = 20;
        public int Perimeters { get; set; } = 3;
        public int TopSolidLayers { get; set; } = 3;
        public int BottomSolidLayers { get; set; } = 3;
        public double PrintSpeed { get; set; } = 60;
        public double TravelSpeed { get; set; } = 130;
        public int ExtruderTemperature { get; set; } = 200;
        public int BedTemperature { get; set; } = 60;
        public string FillPattern { get; set; } = "honeycomb";
        public bool SupportMaterial { get; set; } = false;

        public string ToIniFormat()
        {
            return $@"# Configuración generada automáticamente
[print_settings]
layer_height = {LayerHeight}
fill_density = {FillDensity}%
perimeters = {Perimeters}
top_solid_layers = {TopSolidLayers}
bottom_solid_layers = {BottomSolidLayers}
print_speed = {PrintSpeed}
travel_speed = {TravelSpeed}
fill_pattern = {FillPattern}
support_material = {(SupportMaterial ? "1" : "0")}

[printer_settings]
bed_temperature = {BedTemperature}
temperature = {ExtruderTemperature}
";
        }
    }
}
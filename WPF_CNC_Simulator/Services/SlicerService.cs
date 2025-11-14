using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WPF_CNC_Simulator.Services
{
    /// <summary>
    /// Servicio Slicer / CAM híbrido:
    /// - Si se pasa ruta a ejecutable externo y existe, puede intentar usarlo (fallback).
    /// - Por defecto usa motor CAM interno (waterline + parallel finishing) 100% C# (opción C).
    ///
    /// Mantiene compatibilidad con la interfaz previa (SliceSTLAsync, SliceWithCustomSettings).
    /// </summary>
    public class SlicerService
    {
        private readonly string _externalSlicerPath;
        private readonly string _configPath;
        private readonly bool _useExternalSlicer;
        private readonly CamEngine _cam;

        #region Constructors (compatibility)

        // Parameterless: use internal CAM engine
        public SlicerService()
        {
            _useExternalSlicer = false;
            _externalSlicerPath = null;
            _configPath = null;
            _cam = new CamEngine();
        }

        // Old signature compatibility: accepts slic3r path (but will use internal CAM if path is null/invalid)
        public SlicerService(string slic3rExecutablePath, string configPath = null)
        {
            if (!string.IsNullOrEmpty(slic3rExecutablePath) && File.Exists(slic3rExecutablePath))
            {
                _externalSlicerPath = slic3rExecutablePath;
                _configPath = configPath;
                _useExternalSlicer = true;
            }
            else
            {
                _useExternalSlicer = false;
            }

            _cam = new CamEngine();
        }

        #endregion

        #region Public API (compatible signatures)

        /// <summary>
        /// Converts STL -> G-code. If external slicer is available, it may be used (fallback).
        /// Otherwise uses internal CAM engine (waterline + parallel finishing).
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

                var dir = Path.GetDirectoryName(outputGcodePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (_useExternalSlicer)
                {
                    progressCallback?.Invoke($"Usando ejecutable externo: {_externalSlicerPath}");
                    var start = DateTime.Now;
                    var procResult = await RunExternalSlicer(stlFilePath, outputGcodePath, progressCallback);
                    result.ProcessingTime = DateTime.Now - start;
                    result.ExitCode = procResult.exitCode;
                    result.Output = procResult.stdout;
                    result.ErrorOutput = procResult.stderr;

                    if (procResult.exitCode == 0 && File.Exists(outputGcodePath))
                    {
                        result.Success = true;
                        result.OutputFilePath = outputGcodePath;
                        result.FileSize = new FileInfo(outputGcodePath).Length;
                        progressCallback?.Invoke("Archivo G-code generado por ejecutable externo.");
                    }
                    else
                    {
                        result.Success = false;
                        result.ErrorMessage = "Fallo en ejecutable externo o no se generó archivo G-code.";
                    }

                    return result;
                }
                else
                {
                    progressCallback?.Invoke("Usando motor CAM interno (Waterline + Parallel Finishing)");
                    var start = DateTime.Now;

                    // default settings for CAM if none provided later
                    var defaultSettings = new CNCMillingSettings();
                    // We'll create waterline (rough) and parallel finishing (finish) and merge
                    var waterlineLayer = Math.Max(defaultSettings.CuttingDepth, 0.5); // roughing layer
                    var waterlinePaths = await _cam.GenerateWaterlineRoughingAsync(stlFilePath, waterlineLayer, progressCallback);
                    var finishingPaths = await _cam.GenerateParallelFinishingAsync(stlFilePath, Math.Max(0.4 * defaultSettings.ToolDiameter, defaultSettings.StepResolution), 'X', null, progressCallback);

                    // Merge paths: rough first then finish
                    var allPaths = new List<Toolpath>();
                    allPaths.AddRange(waterlinePaths);
                    allPaths.AddRange(finishingPaths);

                    var gcode = _cam.GenerateGcode(allPaths, defaultSettings);
                    File.WriteAllText(outputGcodePath, gcode, Encoding.UTF8);

                    result.ProcessingTime = DateTime.Now - start;
                    result.Success = true;
                    result.OutputFilePath = outputGcodePath;
                    result.FileSize = new FileInfo(outputGcodePath).Length;
                    result.ExitCode = 0;
                    progressCallback?.Invoke($"G-code interno generado: {outputGcodePath}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.ErrorOutput = ex.StackTrace;
                progressCallback?.Invoke("[EXCEPCIÓN] " + ex.Message);
                return result;
            }
        }

        /// <summary>
        /// Compatible with previous MainWindow usage: creates a temporary config and calls slicer.
        /// For internal CAM this simply uses settings passed to generate tailored G-code.
        /// </summary>
        public async Task<SlicingResult> SliceWithCustomSettings(
            string stlFilePath,
            string outputGcodePath,
            CNCMillingSettings settings,
            Action<string> progressCallback = null)
        {
            var result = new SlicingResult();
            try
            {
                if (!File.Exists(stlFilePath))
                {
                    result.Success = false;
                    result.ErrorMessage = "Archivo STL no encontrado.";
                    return result;
                }

                var dir = Path.GetDirectoryName(outputGcodePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (_useExternalSlicer)
                {
                    progressCallback?.Invoke("Usando ejecutable externo con configuración personalizada (fallback).");
                    // create temp config if needed
                    string tempCfg = null;
                    try
                    {
                        if (settings != null)
                        {
                            tempCfg = Path.Combine(Path.GetTempPath(), $"slic3r_config_{Guid.NewGuid()}.ini");
                            File.WriteAllText(tempCfg, settings.ToIniFormat());
                            progressCallback?.Invoke($"Configuración temporal creada: {tempCfg}");
                        }

                        var start = DateTime.Now;
                        var procResult = await RunExternalSlicer(stlFilePath, outputGcodePath, progressCallback, tempCfg);
                        result.ProcessingTime = DateTime.Now - start;
                        result.ExitCode = procResult.exitCode;
                        result.Output = procResult.stdout;
                        result.ErrorOutput = procResult.stderr;

                        if (procResult.exitCode == 0 && File.Exists(outputGcodePath))
                        {
                            // Post-process to convert to CNC-friendly (remove extruder commands...)
                            PostProcessGCodeForCNC(outputGcodePath, progressCallback);
                            result.Success = true;
                            result.OutputFilePath = outputGcodePath;
                            result.FileSize = new FileInfo(outputGcodePath).Length;
                        }
                        else
                        {
                            result.Success = false;
                            result.ErrorMessage = "Fallo en ejecutable externo o no se generó archivo G-code.";
                        }
                    }
                    finally
                    {
                        // cleanup temp cfg
                        try { if (!string.IsNullOrEmpty(tempCfg) && File.Exists(tempCfg)) File.Delete(tempCfg); } catch { }
                    }

                    return result;
                }
                else
                {
                    progressCallback?.Invoke("Usando motor CAM interno con configuración personalizada.");
                    // Use settings to generate waterline + parallel finishing with chosen params
                    var layer = Math.Max(settings.CuttingDepth, settings.StepResolution);
                    var waterlinePaths = await _cam.GenerateWaterlineRoughingAsync(stlFilePath, layer, progressCallback);

                    // stepover recommended = 0.4*toolDiameter, but allow override by settings.StepResolution
                    double stepover = Math.Max(0.4 * settings.ToolDiameter, settings.StepResolution);

                    var finishingPaths = await _cam.GenerateParallelFinishingAsync(stlFilePath, stepover, 'X', null, progressCallback);

                    var allPaths = new List<Toolpath>();
                    allPaths.AddRange(waterlinePaths);
                    allPaths.AddRange(finishingPaths);

                    var gcode = _cam.GenerateGcode(allPaths, settings);
                    File.WriteAllText(outputGcodePath, gcode, Encoding.UTF8);

                    result.Success = true;
                    result.OutputFilePath = outputGcodePath;
                    result.FileSize = new FileInfo(outputGcodePath).Length;
                    result.ExitCode = 0;
                    result.ProcessingTime = TimeSpan.Zero;
                    progressCallback?.Invoke($"G-code CAM interno guardado: {outputGcodePath}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.ErrorOutput = ex.StackTrace;
                progressCallback?.Invoke("[EXCEPCIÓN] " + ex.Message);
                return result;
            }
        }

        #endregion

        #region External slicer runner (fallback)

        private Task<(int exitCode, string stdout, string stderr)> RunExternalSlicer(string stl, string output, Action<string> progress = null, string configPath = null)
        {
            return Task.Run(() =>
            {
                var sbOut = new StringBuilder();
                var sbErr = new StringBuilder();

                try
                {
                    var args = new StringBuilder();
                    if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
                        args.Append($"--load \"{configPath}\" ");
                    args.Append($"\"{stl}\" --output \"{output}\"");

                    var psi = new ProcessStartInfo
                    {
                        FileName = _externalSlicerPath,
                        Arguments = args.ToString(),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetDirectoryName(stl)
                    };

                    using (var p = Process.Start(psi))
                    {
                        if (p == null)
                            return (-1, "", "No se pudo iniciar proceso.");

                        p.OutputDataReceived += (s, e) => { if (e.Data != null) { sbOut.AppendLine(e.Data); progress?.Invoke("[OUT] " + e.Data); } };
                        p.ErrorDataReceived += (s, e) => { if (e.Data != null) { sbErr.AppendLine(e.Data); progress?.Invoke("[ERR] " + e.Data); } };
                        p.BeginOutputReadLine();
                        p.BeginErrorReadLine();
                        p.WaitForExit(1000 * 60 * 5); // 5 min
                        return (p.ExitCode, sbOut.ToString(), sbErr.ToString());
                    }
                }
                catch (Exception ex)
                {
                    return (-1, "", ex.Message + "\n" + ex.StackTrace);
                }
            });
        }

        #endregion

        #region PostProcess (compatibility helper)

        /// <summary>
        /// Post-procesa el g-code (elimina comandos de impresión 3D y ajusta velocidades).
        /// Compatible con la versión anterior.
        /// </summary>
        public void PostProcessGCodeForCNC(string gcodeFilePath, Action<string> progressCallback)
        {
            try
            {
                var lines = File.Exists(gcodeFilePath) ? File.ReadAllLines(gcodeFilePath).ToList() : new List<string>();
                var processed = new List<string>();

                // Header
                processed.Add("; Post-procesado por SlicerService (CAM interno)");
                processed.Add($" ; Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                processed.Add("G21");
                processed.Add("G90");
                processed.Add("G17");
                processed.Add("");

                int removed = 0;
                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Ignore common 3D-print-only commands
                    if (line.StartsWith("M104") || line.StartsWith("M109") || line.StartsWith("M140")
                        || line.StartsWith("M190") || line.StartsWith("M106") || line.StartsWith("M107")
                        || line.StartsWith("M82") || line.StartsWith("; Filament"))
                    {
                        removed++;
                        continue;
                    }

                    // Remove extruder E commands
                    if (Regex.IsMatch(line, @"\bE[-+]?\d+(\.\d+)?"))
                    {
                        // remove E parameter
                        var cleaned = Regex.Replace(line, @"\sE[-+]?\d+(\.\d+)?", "");
                        processed.Add(cleaned);
                        removed++;
                        continue;
                    }

                    // Keep others
                    processed.Add(raw);
                }

                processed.Add($"; Líneas eliminadas específicas de impresión 3D: {removed}");
                File.WriteAllLines(gcodeFilePath, processed, Encoding.UTF8);
                progressCallback?.Invoke($"Post-procesado completado. {removed} líneas eliminadas.");
            }
            catch (Exception ex)
            {
                progressCallback?.Invoke("Error en PostProcessGCodeForCNC: " + ex.Message);
            }
        }

        #endregion
    }

    #region Support classes: SlicingResult, CNCMillingSettings (compatible)

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
        public double WorkAreaZ { get; set; } = 100.0;
        public double StepResolution { get; set; } = 0.1;

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
            return $@"; Temporary config generated by SlicerService
[print_settings]
layer_height = {CuttingDepth}
first_layer_height = {CuttingDepth}
perimeters = {Perimeters}
top_solid_layers = 0
bottom_solid_layers = 1
fill_density = {FillDensity}%
fill_pattern = {FillPattern}
solid_infill_every_layers = 1
infill_speed = {CuttingFeedrate}
perimeter_speed = {CuttingFeedrate}
travel_speed = {RapidFeedrate}
first_layer_speed = {PlungeFeedrate}

[printer_settings]
temperature = 0
bed_temperature = 0
nozzle_diameter = {ToolDiameter}
";
        }
    }

    #endregion

    #region CAM Engine (internal): Mesh loader, waterline, parallel finishing, gcode writer

    // Toolpath and PathPoint used by generator
    public class Toolpath
    {
        public List<PathPoint> Segments { get; set; } = new List<PathPoint>();
        public double FeedRate { get; set; } = 1500.0;
        public bool IsRapid { get; set; } = false;
        public string Comment { get; set; } = "";
    }

    public class PathPoint
    {
        public double X;
        public double Y;
        public double Z;
        public PathPoint(double x, double y, double z) { X = x; Y = y; Z = z; }
    }

    internal class CamEngine
    {
        private readonly CNCMillingSettings _settings;
        public CamEngine(CNCMillingSettings settings = null)
        {
            _settings = settings ?? new CNCMillingSettings();
        }

        #region Public CAM methods

        public async Task<List<Toolpath>> GenerateWaterlineRoughingAsync(string stlPath, double layerHeight, Action<string> progress = null)
        {
            return await Task.Run(() =>
            {
                var result = new List<Toolpath>();
                var mesh = StlLoader.Load(stlPath);
                var bounds = mesh.GetBounds();

                layerHeight = Math.Max(layerHeight, _settings.StepResolution);
                double topZ = Math.Min(bounds.MaxZ, _settings.WorkAreaZ);
                double bottomZ = Math.Max(bounds.MinZ, 0.0);

                for (double z = topZ; z >= bottomZ; z -= layerHeight)
                {
                    var segs = mesh.IntersectPlaneZ(z);
                    var polylines = Geometry.ChainSegmentsToPolylines(segs, 1e-3);
                    foreach (var poly in polylines)
                    {
                        var tp = new Toolpath
                        {
                            Segments = poly.Select(p => new PathPoint(p.X, p.Y, z)).ToList(),
                            FeedRate = _settings.CuttingFeedrate,
                            IsRapid = false,
                            Comment = $"Waterline Z={z:F3}"
                        };
                        result.Add(tp);
                    }
                    progress?.Invoke($"Waterline: Z={z:F3} -> {polylines.Count} contours");
                }

                progress?.Invoke($"Waterline completado: {result.Count} toolpaths");
                return result;
            });
        }

        public async Task<List<Toolpath>> GenerateParallelFinishingAsync(string stlPath, double stepover, char direction = 'X', double? finishZ = null, Action<string> progress = null)
        {
            return await Task.Run(() =>
            {
                var result = new List<Toolpath>();
                var mesh = StlLoader.Load(stlPath);
                var bounds = mesh.GetBounds();

                stepover = Math.Max(stepover, _settings.StepResolution);
                double zTarget = finishZ ?? bounds.MaxZ;
                zTarget = Math.Min(Math.Max(zTarget, bounds.MinZ), bounds.MaxZ);

                if (char.ToUpper(direction) == 'X')
                {
                    for (double y = bounds.MinY; y <= bounds.MaxY; y += stepover)
                    {
                        var intervals = mesh.IntersectHorizontalLineY(y);
                        if (intervals.Count == 0) continue;

                        var pts = new List<PathPoint>();
                        bool leftToRight = ((int)Math.Round((y - bounds.MinY) / stepover) % 2 == 0);

                        var sampleXs = new List<double>();
                        foreach (var iv in intervals)
                        {
                            double sampleStep = Math.Max(_settings.StepResolution, Math.Min(stepover / 6.0, 0.5));
                            for (double x = iv.Item1; x <= iv.Item2; x += sampleStep)
                                sampleXs.Add(x);
                        }
                        if (!leftToRight) sampleXs.Reverse();

                        foreach (var x in sampleXs)
                        {
                            var z = mesh.SampleZAtXY(x, y);
                            if (double.IsNaN(z)) continue;
                            pts.Add(new PathPoint(x, y, z));
                        }

                        if (pts.Count > 1)
                        {
                            result.Add(new Toolpath
                            {
                                Segments = pts,
                                FeedRate = _settings.CuttingFeedrate,
                                IsRapid = false,
                                Comment = $"Parallel pass Y={y:F3}"
                            });
                        }
                    }
                }
                else
                {
                    for (double x = bounds.MinX; x <= bounds.MaxX; x += stepover)
                    {
                        var intervals = mesh.IntersectVerticalLineX(x);
                        if (intervals.Count == 0) continue;

                        var pts = new List<PathPoint>();
                        bool bottomToTop = ((int)Math.Round((x - bounds.MinX) / stepover) % 2 == 0);

                        var sampleYs = new List<double>();
                        foreach (var iv in intervals)
                        {
                            double sampleStep = Math.Max(_settings.StepResolution, Math.Min(stepover / 6.0, 0.5));
                            for (double y = iv.Item1; y <= iv.Item2; y += sampleStep)
                                sampleYs.Add(y);
                        }
                        if (!bottomToTop) sampleYs.Reverse();

                        foreach (var y in sampleYs)
                        {
                            var z = mesh.SampleZAtXY(x, y);
                            if (double.IsNaN(z)) continue;
                            pts.Add(new PathPoint(x, y, z));
                        }

                        if (pts.Count > 1)
                        {
                            result.Add(new Toolpath
                            {
                                Segments = pts,
                                FeedRate = _settings.CuttingFeedrate,
                                IsRapid = false,
                                Comment = $"Parallel pass X={x:F3}"
                            });
                        }
                    }
                }

                progress?.Invoke($"Parallel finishing completado: {result.Count} toolpaths");
                return result;
            });
        }

        public string GenerateGcode(IEnumerable<Toolpath> toolpaths, CNCMillingSettings settings = null)
        {
            var cfg = settings ?? _settings;

            var sb = new StringBuilder();

            sb.AppendLine($"; G-code generado por SlicerService (Motor CAM interno)");
            sb.AppendLine($"; Area: {cfg.WorkAreaX}x{cfg.WorkAreaY}x{cfg.WorkAreaZ} mm | Tool: Ø{cfg.ToolDiameter} mm");
            sb.AppendLine($"; Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("G21");
            sb.AppendLine("G90");
            sb.AppendLine("G17");
            sb.AppendLine("G94");
            sb.AppendLine();
            sb.AppendLine("G28");
            sb.AppendLine($"G0 Z{cfg.SafeHeight:F3} F{cfg.RapidFeedrate:F1}");
            sb.AppendLine();
            sb.AppendLine($"M3 S{Math.Min(cfg.WorkingSpindleSpeed, cfg.MaxSpindleSpeed)}");
            sb.AppendLine("G4 P1");
            sb.AppendLine();

            foreach (var tp in toolpaths)
            {
                if (tp?.Segments == null || tp.Segments.Count == 0) continue;

                var first = tp.Segments.First();
                sb.AppendLine($"; {tp.Comment}");
                double y0 = -first.Y;
                sb.AppendLine($"G0 X{first.X:F3} Y{y0:F3} F{cfg.RapidFeedrate:F1}");

                sb.AppendLine($"G0 Z{cfg.SafeHeight:F3} F{cfg.RapidFeedrate:F1}");
                sb.AppendLine($"G1 Z{first.Z:F3} F{cfg.PlungeFeedrate:F1}");

                foreach (var p in tp.Segments)
                {
                    double yInv = -p.Y;   // invertir eje Y
                    sb.AppendLine($"G1 X{p.X:F3} Y{yInv:F3} Z{p.Z:F3} F{tp.FeedRate:F1}");
                }


                sb.AppendLine($"G0 Z{cfg.SafeHeight:F3} F{cfg.RapidFeedrate:F1}");
                sb.AppendLine();
            }

            sb.AppendLine("M5");
            sb.AppendLine($"G0 Z{_settings.SafeHeight:F3} F{_settings.RapidFeedrate:F1}");
            sb.AppendLine($"G0 X0 Y0 F{_settings.RapidFeedrate:F1}");
            sb.AppendLine("M2");

            return sb.ToString();
        }

        #endregion
    }

    #endregion

    #region Geometry, Mesh, STL Loader (internal lightweight)

    // Basic triangle representation
    internal class Triangle
    {
        public Vector3 A, B, C;
    }

    internal class Mesh
    {
        public List<Triangle> Triangles { get; } = new List<Triangle>();

        public (double MinX, double MaxX, double MinY, double MaxY, double MinZ, double MaxZ) GetBounds()
        {
            if (Triangles.Count == 0) return (0, 0, 0, 0, 0, 0);
            var xs = new List<double>(); var ys = new List<double>(); var zs = new List<double>();
            foreach (var t in Triangles)
            {
                xs.Add(t.A.X); xs.Add(t.B.X); xs.Add(t.C.X);
                ys.Add(t.A.Y); ys.Add(t.B.Y); ys.Add(t.C.Y);
                zs.Add(t.A.Z); zs.Add(t.B.Z); zs.Add(t.C.Z);
            }
            return (xs.Min(), xs.Max(), ys.Min(), ys.Max(), zs.Min(), zs.Max());
        }

        // Intersect with plane Z=z0 -> list of 2D segments
        public List<(Vector2, Vector2)> IntersectPlaneZ(double z0)
        {
            var segs = new List<(Vector2, Vector2)>();
            foreach (var tri in Triangles)
            {
                double da = tri.A.Z - z0, db = tri.B.Z - z0, dc = tri.C.Z - z0;
                // if all same sign and not near zero -> skip
                if ((da > 1e-12 && db > 1e-12 && dc > 1e-12) || (da < -1e-12 && db < -1e-12 && dc < -1e-12))
                    continue;

                var pts = new List<Vector2>();
                TryEdgeIntersect(tri.A, tri.B, z0, pts);
                TryEdgeIntersect(tri.B, tri.C, z0, pts);
                TryEdgeIntersect(tri.C, tri.A, z0, pts);

                if (pts.Count >= 2)
                {
                    segs.Add((pts[0], pts[1]));
                }
            }
            return segs;
        }

        private static void TryEdgeIntersect(Vector3 p1, Vector3 p2, double z0, List<Vector2> outPts)
        {
            double z1 = p1.Z, z2 = p2.Z;
            if (Math.Abs(z1 - z2) < 1e-12) return;
            if ((z1 < z0 && z2 < z0) || (z1 > z0 && z2 > z0)) return;
            double t = (z0 - z1) / (z2 - z1);
            if (t < -1e-8 || t > 1 + 1e-8) return;
            double x = p1.X + t * (p2.X - p1.X);
            double y = p1.Y + t * (p2.Y - p1.Y);
            outPts.Add(new Vector2(x, y));
        }

        // Projected intersections for horizontal/vertical scanlines
        public List<(double, double)> IntersectHorizontalLineY(double y0)
        {
            var xs = new List<double>();
            foreach (var t in Triangles)
            {
                TryEdgeIntersectXY(t.A, t.B, y0, xs);
                TryEdgeIntersectXY(t.B, t.C, y0, xs);
                TryEdgeIntersectXY(t.C, t.A, y0, xs);
            }
            xs.Sort();
            var intervals = new List<(double, double)>();
            for (int i = 0; i + 1 < xs.Count; i += 2)
                intervals.Add((xs[i], xs[i + 1]));
            return intervals;
        }

        private static void TryEdgeIntersectXY(Vector3 p1, Vector3 p2, double y0, List<double> outX)
        {
            double y1 = p1.Y, y2 = p2.Y;
            if (Math.Abs(y1 - y2) < 1e-12) return;
            if ((y1 < y0 && y2 < y0) || (y1 > y0 && y2 > y0)) return;
            double t = (y0 - y1) / (y2 - y1);
            double x = p1.X + t * (p2.X - p1.X);
            outX.Add(x);
        }

        public List<(double, double)> IntersectVerticalLineX(double x0)
        {
            var ys = new List<double>();
            foreach (var t in Triangles)
            {
                TryEdgeIntersectYX(t.A, t.B, x0, ys);
                TryEdgeIntersectYX(t.B, t.C, x0, ys);
                TryEdgeIntersectYX(t.C, t.A, x0, ys);
            }
            ys.Sort();
            var intervals = new List<(double, double)>();
            for (int i = 0; i + 1 < ys.Count; i += 2)
                intervals.Add((ys[i], ys[i + 1]));
            return intervals;
        }

        private static void TryEdgeIntersectYX(Vector3 p1, Vector3 p2, double x0, List<double> outY)
        {
            double x1 = p1.X, x2 = p2.X;
            if (Math.Abs(x1 - x2) < 1e-12) return;
            if ((x1 < x0 && x2 < x0) || (x1 > x0 && x2 > x0)) return;
            double t = (x0 - x1) / (x2 - x1);
            double y = p1.Y + t * (p2.Y - p1.Y);
            outY.Add(y);
        }

        // Sample topmost Z at given (x,y): cast vertical ray
        public double SampleZAtXY(double x, double y)
        {
            double bestZ = double.NaN;
            foreach (var t in Triangles)
            {
                if (RayIntersectsTriangleVertical(x, y, t, out double zHit))
                {
                    if (double.IsNaN(bestZ) || zHit > bestZ)
                        bestZ = zHit;
                }
            }
            return bestZ;
        }

        private static bool RayIntersectsTriangleVertical(double x, double y, Triangle tri, out double z)
        {
            z = double.NaN;
            var v0 = tri.B - tri.A;
            var v1 = tri.C - tri.A;
            var n = Vector3.Cross(v0, v1);
            if (Math.Abs(n.Z) < 1e-12) return false; // near vertical plane -> ignore
            z = tri.A.Z - (n.X * (x - tri.A.X) + n.Y * (y - tri.A.Y)) / n.Z;
            var p = new Vector3(x, y, z);
            if (PointInTriangle(p, tri.A, tri.B, tri.C)) return true;
            return false;
        }

        private static bool PointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            var normal = Vector3.Cross(b - a, c - a);
            double absX = Math.Abs(normal.X), absY = Math.Abs(normal.Y), absZ = Math.Abs(normal.Z);
            int u = 0, v = 1;
            if (absX > absY && absX > absZ) { u = 1; v = 2; }
            else if (absY > absZ && absY > absX) { u = 0; v = 2; }
            else { u = 0; v = 1; }

            double ax = a[u], ay = a[v];
            double bx_ = b[u], by_ = b[v];
            double cx_ = c[u], cy_ = c[v];
            double px = p[u], py = p[v];

            double denom = (by_ - cy_) * (ax - cx_) + (cx_ - bx_) * (ay - cy_);
            if (Math.Abs(denom) < 1e-12) return false;
            double w1 = ((by_ - cy_) * (px - cx_) + (cx_ - bx_) * (py - cy_)) / denom;
            double w2 = ((cy_ - ay) * (px - cx_) + (ax - cx_) * (py - cy_)) / denom;
            double w3 = 1 - w1 - w2;
            return (w1 >= -1e-9 && w2 >= -1e-9 && w3 >= -1e-9);
        }
    }

    internal struct Vector3
    {
        public double X, Y, Z;
        public Vector3(double x, double y, double z) { X = x; Y = y; Z = z; }
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        public double this[int idx] => idx == 0 ? X : idx == 1 ? Y : Z;
    }

    internal struct Vector2
    {
        public double X, Y;
        public Vector2(double x, double y) { X = x; Y = y; }
    }

    internal static class Geometry
    {
        public static List<List<Vector2>> ChainSegmentsToPolylines(List<(Vector2, Vector2)> segments, double tolerance = 1e-3)
        {
            var polylines = new List<List<Vector2>>();
            var used = new bool[segments.Count];

            for (int i = 0; i < segments.Count; i++)
            {
                if (used[i]) continue;
                used[i] = true;
                var cur = segments[i];
                var poly = new List<Vector2> { cur.Item1, cur.Item2 };
                bool extended = true;
                while (extended)
                {
                    extended = false;
                    for (int j = 0; j < segments.Count; j++)
                    {
                        if (used[j]) continue;
                        var s = segments[j];
                        if (Distance(poly.Last(), s.Item1) < tolerance)
                        {
                            poly.Add(s.Item2); used[j] = true; extended = true;
                        }
                        else if (Distance(poly.Last(), s.Item2) < tolerance)
                        {
                            poly.Add(s.Item1); used[j] = true; extended = true;
                        }
                        else if (Distance(poly.First(), s.Item2) < tolerance)
                        {
                            poly.Insert(0, s.Item1); used[j] = true; extended = true;
                        }
                        else if (Distance(poly.First(), s.Item1) < tolerance)
                        {
                            poly.Insert(0, s.Item2); used[j] = true; extended = true;
                        }
                    }
                }
                polylines.Add(poly);
            }
            return polylines;
        }

        private static double Distance(Vector2 a, Vector2 b)
        {
            var dx = a.X - b.X; var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }

    internal static class StlLoader
    {
        public static Mesh Load(string path)
        {
            var bytes = File.ReadAllBytes(path);
            if (IsBinary(bytes)) return LoadBinary(bytes);
            return LoadAscii(File.ReadAllText(path));
        }

        private static bool IsBinary(byte[] bytes)
        {
            if (bytes.Length < 84) return false;
            // header heuristic
            var header = Encoding.ASCII.GetString(bytes, 0, Math.Min(80, bytes.Length));
            if (!header.StartsWith("solid", StringComparison.InvariantCultureIgnoreCase)) return true;
            // check expected size
            uint triCount = BitConverter.ToUInt32(bytes, 80);
            long expected = 84 + triCount * 50;
            if (expected == bytes.Length) return true;
            // else assume ASCII
            return false;
        }

        private static Mesh LoadBinary(byte[] bytes)
        {
            var mesh = new Mesh();
            if (bytes.Length < 84) return mesh;
            uint triCount = BitConverter.ToUInt32(bytes, 80);
            int offset = 84;
            for (uint i = 0; i < triCount && offset + 50 <= bytes.Length; i++)
            {
                // skip normal
                offset += 12;
                float ax = BitConverter.ToSingle(bytes, offset); offset += 4;
                float ay = BitConverter.ToSingle(bytes, offset); offset += 4;
                float az = BitConverter.ToSingle(bytes, offset); offset += 4;

                float bx = BitConverter.ToSingle(bytes, offset); offset += 4;
                float by = BitConverter.ToSingle(bytes, offset); offset += 4;
                float bz = BitConverter.ToSingle(bytes, offset); offset += 4;

                float cx = BitConverter.ToSingle(bytes, offset); offset += 4;
                float cy = BitConverter.ToSingle(bytes, offset); offset += 4;
                float cz = BitConverter.ToSingle(bytes, offset); offset += 4;

                offset += 2; // attribute bytes

                mesh.Triangles.Add(new Triangle
                {
                    A = new Vector3(ax, ay, az),
                    B = new Vector3(bx, by, bz),
                    C = new Vector3(cx, cy, cz)
                });
            }
            return mesh;
        }

        private static Mesh LoadAscii(string text)
        {
            var mesh = new Mesh();
            using (var sr = new StringReader(text))
            {
                string line;
                Vector3 a = default, b = default, c = default;
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("vertex", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 4)
                        {
                            double x = double.Parse(parts[1], CultureInfo.InvariantCulture);
                            double y = double.Parse(parts[2], CultureInfo.InvariantCulture);
                            double z = double.Parse(parts[3], CultureInfo.InvariantCulture);
                            if (a.Equals(default(Vector3))) a = new Vector3(x, y, z);
                            else if (b.Equals(default(Vector3))) b = new Vector3(x, y, z);
                            else c = new Vector3(x, y, z);
                        }

                        if (!a.Equals(default(Vector3)) && !b.Equals(default(Vector3)) && !c.Equals(default(Vector3)))
                        {
                            mesh.Triangles.Add(new Triangle { A = a, B = b, C = c });
                            a = default; b = default; c = default;
                        }
                    }
                }
            }
            return mesh;
        }
    }

    #endregion
}

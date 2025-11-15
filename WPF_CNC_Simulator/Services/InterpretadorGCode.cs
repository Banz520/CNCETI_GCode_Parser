using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WPF_CNC_Simulator.Services
{
    /// <summary>
    /// Interpreta y ejecuta comandos G-code para animación CNC
    /// </summary>
    public class InterpretadorGCode
    {
        // Posición actual de la máquina
        public double PosicionX { get; private set; }
        public double PosicionY { get; private set; }
        public double PosicionZ { get; private set; }

        // Modo de posicionamiento (true = absoluto, false = relativo)
        private bool modoAbsoluto = true;

        // Velocidad de avance actual (mm/min)
        private double velocidadAvance = 1500.0;

        public InterpretadorGCode()
        {
            PosicionX = 0;
            PosicionY = 0;
            PosicionZ = 0;
        }

        /// <summary>
        /// Parsea una línea de G-code y devuelve el comando interpretado
        /// </summary>
        public ComandoGCode ParsearLinea(string linea)
        {
            if (string.IsNullOrWhiteSpace(linea))
                return null;

            // Eliminar espacios y convertir a mayúsculas
            linea = linea.Trim().ToUpper();

            // Ignorar comentarios
            if (linea.StartsWith(";") || linea.StartsWith("("))
                return null;

            // Eliminar comentarios al final de la línea
            int indiceComentario = linea.IndexOf(';');
            if (indiceComentario >= 0)
                linea = linea.Substring(0, indiceComentario).Trim();

            if (string.IsNullOrWhiteSpace(linea))
                return null;

            var comando = new ComandoGCode
            {
                LineaOriginal = linea
            };

            // Extraer comando principal (G0, G1, M3, etc.)
            var matchComando = Regex.Match(linea, @"^([GM])(\d+)");
            if (matchComando.Success)
            {
                comando.TipoComando = matchComando.Groups[1].Value;
                comando.NumeroComando = int.Parse(matchComando.Groups[2].Value);
            }

            // Extraer parámetros X, Y, Z, F
            comando.X = ExtraerParametro(linea, 'X');
            comando.Y = ExtraerParametro(linea, 'Y');
            comando.Z = ExtraerParametro(linea, 'Z');
            comando.F = ExtraerParametro(linea, 'F');

            return comando;
        }

        /// <summary>
        /// Extrae el valor de un parámetro de la línea G-code
        /// </summary>
        private double? ExtraerParametro(string linea, char parametro)
        {
            var pattern = $@"{parametro}([-+]?\d+\.?\d*)";
            var match = Regex.Match(linea, pattern);

            if (match.Success)
            {
                return double.Parse(match.Groups[1].Value);
            }

            return null;
        }

        /// <summary>
        /// Ejecuta un comando y actualiza el estado interno
        /// </summary>
        public ResultadoEjecucion EjecutarComando(ComandoGCode comando)
        {
            if (comando == null)
                return null;

            var resultado = new ResultadoEjecucion
            {
                Comando = comando,
                NumeroLinea = comando.NumeroLinea // Asignar número de línea al resultado
            };

            // Procesar según el tipo de comando
            if (comando.TipoComando == "G")
            {
                switch (comando.NumeroComando)
                {
                    case 0: // Movimiento rápido
                    case 1: // Movimiento lineal
                        resultado = ProcesarMovimiento(comando);
                        break;

                    case 28: // Home
                        PosicionX = 0;
                        PosicionY = 0;
                        PosicionZ = 0;
                        resultado.RequiereMovimiento = true;
                        resultado.PosicionFinalX = 0;
                        resultado.PosicionFinalY = 0;
                        resultado.PosicionFinalZ = 0;
                        break;

                    case 90: // Modo absoluto
                        modoAbsoluto = true;
                        resultado.RequiereMovimiento = false;
                        break;

                    case 91: // Modo relativo
                        modoAbsoluto = false;
                        resultado.RequiereMovimiento = false;
                        break;

                    default:
                        resultado.RequiereMovimiento = false;
                        break;
                }
            }
            else if (comando.TipoComando == "M")
            {
                // Comandos M (husillo, etc.) no requieren movimiento
                resultado.RequiereMovimiento = false;
            }

            return resultado;
        }

        /// <summary>
        /// Procesa comandos de movimiento G0/G1
        /// </summary>
        private ResultadoEjecucion ProcesarMovimiento(ComandoGCode comando)
        {
            var resultado = new ResultadoEjecucion
            {
                Comando = comando,
                NumeroLinea = comando.NumeroLinea, // Asignar número de línea
                RequiereMovimiento = false
            };

            double nuevaX = PosicionX;
            double nuevaY = PosicionY;
            double nuevaZ = PosicionZ;

            // Calcular nuevas posiciones
            if (comando.X.HasValue)
            {
                nuevaX = modoAbsoluto ? comando.X.Value : PosicionX + comando.X.Value;
                resultado.RequiereMovimiento = true;
            }

            if (comando.Y.HasValue)
            {
                nuevaY = modoAbsoluto ? comando.Y.Value : PosicionY + comando.Y.Value;
                resultado.RequiereMovimiento = true;
            }

            if (comando.Z.HasValue)
            {
                nuevaZ = modoAbsoluto ? comando.Z.Value : PosicionZ + comando.Z.Value;
                resultado.RequiereMovimiento = true;
            }

            if (comando.F.HasValue)
            {
                velocidadAvance = comando.F.Value;
            }

            // Si hay movimiento, calcular la duración
            if (resultado.RequiereMovimiento)
            {
                double distancia = Math.Sqrt(
                    Math.Pow(nuevaX - PosicionX, 2) +
                    Math.Pow(nuevaY - PosicionY, 2) +
                    Math.Pow(nuevaZ - PosicionZ, 2)
                );

                // Duración en segundos = distancia (mm) / velocidad (mm/min) * 60
                resultado.DuracionSegundos = (distancia / velocidadAvance) * 60.0;

                resultado.PosicionInicialX = PosicionX;
                resultado.PosicionInicialY = PosicionY;
                resultado.PosicionInicialZ = PosicionZ;

                resultado.PosicionFinalX = nuevaX;
                resultado.PosicionFinalY = nuevaY;
                resultado.PosicionFinalZ = nuevaZ;

                // Actualizar posición actual
                PosicionX = nuevaX;
                PosicionY = nuevaY;
                PosicionZ = nuevaZ;

                resultado.EsMovimientoRapido = (comando.NumeroComando == 0);
            }

            return resultado;
        }

        /// <summary>
        /// Parsea todo el código G y devuelve lista de comandos
        /// </summary>
        public List<ComandoGCode> ParsearCodigoCompleto(string codigoG)
        {
            var comandos = new List<ComandoGCode>();
            var lineas = codigoG.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lineas.Length; i++)
            {
                var linea = lineas[i];
                var comando = ParsearLinea(linea);
                if (comando != null)
                {
                    comando.NumeroLinea = i + 1; // Asignar número de línea (1-based)
                    comandos.Add(comando);
                }
            }

            return comandos;
        }

        /// <summary>
        /// Resetea el interpretador a su estado inicial
        /// </summary>
        public void Resetear()
        {
            PosicionX = 0;
            PosicionY = 0;
            PosicionZ = 0;
            modoAbsoluto = true;
            velocidadAvance = 1500.0;
        }
    }

    /// <summary>
    /// Representa un comando G-code parseado
    /// </summary>
    public class ComandoGCode
    {
        public string LineaOriginal { get; set; }
        public string TipoComando { get; set; } // "G" o "M"
        public int NumeroComando { get; set; } // 0, 1, 28, etc.
        public double? X { get; set; }
        public double? Y { get; set; }
        public double? Z { get; set; }
        public double? F { get; set; } // Velocidad de avance

        // Nueva propiedad para el número de línea
        public int NumeroLinea { get; set; }

        public override string ToString()
        {
            return $"{TipoComando}{NumeroComando} X:{X} Y:{Y} Z:{Z} F:{F} (Línea:{NumeroLinea})";
        }
    }

    /// <summary>
    /// Resultado de la ejecución de un comando
    /// </summary>
    public class ResultadoEjecucion
    {
        public ComandoGCode Comando { get; set; }
        public bool RequiereMovimiento { get; set; }
        public bool EsMovimientoRapido { get; set; }

        public double PosicionInicialX { get; set; }
        public double PosicionInicialY { get; set; }
        public double PosicionInicialZ { get; set; }

        public double PosicionFinalX { get; set; }
        public double PosicionFinalY { get; set; }
        public double PosicionFinalZ { get; set; }

        public double DuracionSegundos { get; set; }

        // Nueva propiedad para el número de línea
        public int NumeroLinea { get; set; }
    }
}

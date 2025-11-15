using ICSharpCode.AvalonEdit.Rendering;
using System.Windows;
using System.Windows.Media;

namespace WPF_CNC_Simulator.Vistas.Widgets
{
    public class BackgroundRenderer : IBackgroundRenderer
    {
        public int LineaActual { get; set; } = -1;

        public KnownLayer Layer => KnownLayer.Background;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (LineaActual < 1 || textView.Document == null)
                return;

            try
            {
                if (LineaActual > textView.Document.LineCount)
                    return;

                var linea = textView.Document.GetLineByNumber(LineaActual);
                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, linea))
                {
                    var brush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 0)); // Amarillo semi-transparente
                    drawingContext.DrawRectangle(brush, null, 
                        new Rect(0, rect.Top, textView.ActualWidth, rect.Height));
                }
            }
            catch
            {
                // Ignorar errores de renderizado
            }
        }
    }
}

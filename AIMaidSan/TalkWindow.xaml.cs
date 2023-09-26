using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
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
using System.Windows.Shapes;

namespace AIMaidSan
{
    /// <summary>
    /// TalkWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TalkWindow : Window
    {
        public TalkWindow()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (sender, e) => { this.DragMove(); };
        }

        public void SetText(string text)
        {
            int dpi = 96;
            int margin = 30;
            int fontSize = 20;
            int round = 80;
            var lines = WakachiConverter.ConvertAutoLineBreak(text);

            double maxWidth = 0;
            double maxHeight = 0;
            double totalHeight = 0;

            var formatLines = new List<FormattedText>();

            foreach (var line in lines)
            {
                FormattedText formattedTextLine1 = new FormattedText(
                    line,
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Verdana"),
                    fontSize, // フォントサイズ
                    Brushes.Black, dpi);

                Console.WriteLine(line);

                formatLines.Add(formattedTextLine1);

                // 両方のテキストの最大の幅と、2行のテキストの合計の高さを計算
                maxWidth = Math.Max(formattedTextLine1.Width, maxWidth);
                maxHeight = Math.Max(formattedTextLine1.Height, maxHeight);
                totalHeight = formattedTextLine1.Height + totalHeight;
            }

            // 描画を行う
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                // 背景の丸みを帯びた矩形を描画
                drawingContext.DrawRoundedRectangle(
                    Brushes.LightGray,
                    new Pen(Brushes.DarkGray, 1),
                    new Rect(new Point(0, 0), new Size(maxWidth + (margin * 2), totalHeight + (margin * 2))), // テキストの周りにパディングを追加
                    round, round); // 丸みの半径

                // 2行のテキストを描画
                for (int i = 0; i < formatLines.Count; i++)
                {
                    drawingContext.DrawText(formatLines[i], new Point(margin, margin + (maxHeight * i))); // 1行目のテキストの高さ分を足して2行目を描画
                }
            }

            var renderTargetBitmap2 = new RenderTargetBitmap(500, 500, dpi, dpi, PixelFormats.Pbgra32);
            renderTargetBitmap2.Render(drawingVisual);
            image.Source = renderTargetBitmap2;

            this.Width = maxWidth + (margin * 2);
            this.Height = totalHeight + (margin * 2);
        }

        private void image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void SaveRenderTargetBitmapToFile(RenderTargetBitmap rtb, string filePath)
        {
            // PngBitmapEncoderを使用してRenderTargetBitmapをエンコード
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (FileStream file = File.Create(filePath))
            {
                encoder.Save(file);
            }
        }

        public RenderTargetBitmap TrimBitmap(RenderTargetBitmap sourceBitmap, int x, int y, int width, int height)
        {
            // トリミング対象の領域を定義します
            var rect = new Int32Rect(x, y, width, height);

            // ピクセルバッファを取得します
            var pixelBuffer = new byte[rect.Width * rect.Height * (sourceBitmap.Format.BitsPerPixel / 8)];
            sourceBitmap.CopyPixels(rect, pixelBuffer, rect.Width * (sourceBitmap.Format.BitsPerPixel / 8), 0);

            // 新しいビットマップを作成して、ピクセルデータを設定します
            var targetBitmap = new WriteableBitmap(rect.Width, rect.Height, sourceBitmap.DpiX, sourceBitmap.DpiY, sourceBitmap.Format, null);
            targetBitmap.WritePixels(new Int32Rect(0, 0, rect.Width, rect.Height), pixelBuffer, rect.Width * (sourceBitmap.Format.BitsPerPixel / 8), 0);

            // 新しい RenderTargetBitmap を作成して、WriteableBitmap からのデータを転送します
            var renderTarget = new RenderTargetBitmap(rect.Width, rect.Height, sourceBitmap.DpiX, sourceBitmap.DpiY, PixelFormats.Pbgra32);
            renderTarget.Render(new Image { Source = targetBitmap });

            return renderTarget;
        }

    }
}

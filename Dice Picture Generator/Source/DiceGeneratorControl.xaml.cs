using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;
using Rectangle = System.Drawing.Rectangle;
using Image = System.Drawing.Image;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Dice_Picture_Generator.Source
{
    /// <summary>
    /// Interaction logic for DiceGeneratorControl.xaml
    /// </summary>
    public partial class DiceGeneratorControl : UserControl
    {

        private Bitmap[] whiteDice = new Bitmap[6];
        private Bitmap[] blackDice = new Bitmap[6];
        private Bitmap[] fullColorDice = new Bitmap[12];

        private Dictionary<string, Bitmap[]> diceList;
        private Dictionary<string, string[]> diceNamesList;

        public string Text { get; private set; }
        public string NumDice { get { return DiceTextBlock.Text;  } }

        public DiceGeneratorControl()
        {
            InitializeComponent();
            InitDiceArrays();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (NumDice == "")
                return;

            string topText = this.Text;
            int maxDice = Int32.Parse(NumDice);

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.RestoreDirectory = true;
            string fileNameNoExt = "";
            string filePath = "";
            if (openFileDialog1.ShowDialog() is bool result && result)
            {
                var path = openFileDialog1.FileName;
                fileNameNoExt = System.IO.Path.GetFileNameWithoutExtension(path);
                filePath = System.IO.Path.GetDirectoryName(path);
            }
            else
            {
                return;
            }
            await Task.Run(() =>
            {
                try
                {
                    Bitmap bm = (Bitmap)Bitmap.FromFile(openFileDialog1.FileName);
                    this.Dispatcher.Invoke(() =>
                    {
                        InputImage.Source = BitmapToImageSource(bm);
                    });

                    if (maxDice != 0)
                    {
                        System.Drawing.Size original = new System.Drawing.Size(bm.Width, bm.Height);
                        int maxSize = (int)Math.Floor(Math.Sqrt(maxDice));
                        float percent = (new List<float> { (float)maxSize / (float)original.Width, (float)maxSize / (float)original.Height }).Min();
                        System.Drawing.Size resultSize = new System.Drawing.Size((int)Math.Floor(original.Width * percent), (int)Math.Floor(original.Height * percent));
                        if (resultSize.Width <= bm.Width && resultSize.Height <= bm.Height)
                            bm = ResizeImage((Image)bm, resultSize.Width, resultSize.Height);
                    }

                    for (int i = 0; i < diceList.Keys.Count; i++)
                    {
                        var curName = diceList.Keys.ToArray()[i];
                        CreateDiceImage(diceList[curName], curName, bm, filePath, fileNameNoExt);
                    }
                }
                catch (ArgumentException err)
                {
                    this.Text = topText;
                    MessageBox.Show($"{err.ToString()}");
                }
                catch (Exception err)
                {
                    this.Text = topText;
                    MessageBox.Show(err.ToString());
                }
            });
        }

        private void CreateDiceImage(Bitmap[] dice, string curName, Bitmap bm, string filePath, string fileNameNoExt)
        {
            Bitmap grayscaleImage = MakeGrayscale3(bm);
            double brightnessIncrement = 1.0 / dice.Length;

            string diceMap = "";
            using (StreamWriter file = new StreamWriter(GetFileName(filePath, fileNameNoExt, curName, ".txt")))
            {
                for (int x = 0; x < grayscaleImage.Height; x++)
                {
                    for (int y = 0; y < grayscaleImage.Width; y++)
                    {
                        var curPixel = grayscaleImage.GetPixel(y, x);
                        double pixelBrightness = curPixel.GetBrightness();
                        double brightness = Math.Round(pixelBrightness, 2);

                        int writtenDice = dice.Length - 1;
                        int curBrightness = 1;

                        while (curBrightness * brightnessIncrement < brightness)
                        {
                            curBrightness++;
                            writtenDice--;
                        }

                        diceMap += writtenDice.ToString() + " ";
                    }

                    file.WriteLine(diceMap);
                    diceMap = "";
                }
            }

            FinalizeGeneratedImage(dice, curName, filePath, fileNameNoExt, grayscaleImage);
        }

        private void FinalizeGeneratedImage(Bitmap[] dice, string curDicePackName, string filePath, string fileNameNoExt, Bitmap grayscaleImage)
        {
            var lines = File.ReadAllLines(GetFileName(filePath, fileNameNoExt, curDicePackName, ".txt"));
            Bitmap preview = new Bitmap(grayscaleImage.Width * 40, grayscaleImage.Height * 40);
            int dieWidth = 40;
            int curLine = 0;
            using (Graphics g = Graphics.FromImage(preview))
            {
                foreach (var line in lines)
                {
                    var splitLine = line.Split(null);

                    for (var i = 0; i < splitLine.Length; i++)
                    {
                        if (splitLine[i] == "")
                            continue;

                        var diceNum = int.Parse(splitLine[i]);
                        var curImage = dice[diceNum];
                        g.DrawImage(curImage, dieWidth * i, curLine);
                    }
                    curLine += dieWidth;
                }
            }

            preview.Save(GetFileName(filePath, fileNameNoExt, curDicePackName, "_DiceImage.png"), ImageFormat.Png);

            this.Dispatcher.Invoke(() =>
            {
                (FindName(curDicePackName) as System.Windows.Controls.Image).Source = BitmapToImageSource(preview);
            });
        }

        private void WriteInstructions(string curDicePackName, string filePath, string fileNameNoExt)
        {
            string line;
            using (StreamReader file = new StreamReader(GetFileName(filePath, fileNameNoExt, curDicePackName, ".txt")))
            {
                line = file.ReadLine();
                while (line != null)
                {
                    var splitLine = line.Split(null);
                    for (int i = 0; i < splitLine.Length; i++)
                    {
                        var curDiceNum = int.Parse(splitLine[i]);
                        var dice = diceList[curDicePackName];
                    }
                }
            }
        }

        private string GetFileName(string filePath, string fileNameNoExt, string curName, string ext)
        {
            return filePath + "\\" + fileNameNoExt + curName + ext;
        }

        private static Bitmap MakeGrayscale3(Bitmap original)
        {
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);
            Graphics g = Graphics.FromImage(newBitmap);


            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
               {
                 new float[] {.3f, .3f, .3f, 0, 0},
                 new float[] {.59f, .59f, .59f, 0, 0},
                 new float[] {.11f, .11f, .11f, 0, 0},
                 new float[] {0, 0, 0, 1, 0},
                 new float[] {0, 0, 0, 0, 1}
               });
            ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            g.Dispose();
            return newBitmap;
        }

        private static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);
            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return destImage;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private Bitmap GetBitmap(string name)
        {
            return new Bitmap($"..\\..\\Resources\\{name}.png");
        }

        private void InitDiceArrays()
        {
            fullColorDice[0] = GetBitmap("dice1");
            fullColorDice[1] = GetBitmap("dice2");
            fullColorDice[2] = GetBitmap("dice3");
            fullColorDice[3] = GetBitmap("dice4");
            fullColorDice[4] = GetBitmap("dice5");
            fullColorDice[5] = GetBitmap("black_dice6");
            fullColorDice[6] = GetBitmap("black_dice6");
            fullColorDice[7] = GetBitmap("black_dice5");
            fullColorDice[8] = GetBitmap("black_dice4");
            fullColorDice[9] = GetBitmap("black_dice3");
            fullColorDice[10] = GetBitmap("black_dice2");
            fullColorDice[11] = GetBitmap("black_dice1");

            whiteDice[0] = GetBitmap("dice1");
            whiteDice[1] = GetBitmap("dice2");
            whiteDice[2] = GetBitmap("dice3");
            whiteDice[3] = GetBitmap("dice4");
            whiteDice[4] = GetBitmap("dice5");
            whiteDice[5] = GetBitmap("dice6");

            blackDice[0] = GetBitmap("black_dice6");
            blackDice[1] = GetBitmap("black_dice5");
            blackDice[2] = GetBitmap("black_dice4");
            blackDice[3] = GetBitmap("black_dice3");
            blackDice[4] = GetBitmap("black_dice2");
            blackDice[5] = GetBitmap("black_dice1");

            diceList = new Dictionary<string, Bitmap[]>();
            diceNamesList = new Dictionary<string, string[]>();

            diceList["Full_Color"] = fullColorDice;
            diceList["White"] = whiteDice;
            diceList["Black"] = blackDice;

            string[] whiteDiceNames = { "White One", "White Two", "White Three", "White Four", "White Five", "White Six"};
            string[] blackDiceNames = { "Black One", "Black Two", "Black Three", "Black Four", "Black Five", "Black Six" };
            string[] allnames = whiteDiceNames.Concat(blackDiceNames).ToArray();

            diceNamesList["Full_Color"] = allnames;
            diceNamesList["White"] = whiteDiceNames;
            diceNamesList["Black"] = blackDiceNames;
        }

        private static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }
    }
}

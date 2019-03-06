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

        private void InitDiceArrays()
        {
            fullColorDice[0] = GetBitmap("dice1");
            fullColorDice[1] = GetBitmap("dice2");
            fullColorDice[2] = GetBitmap("dice3");
            fullColorDice[3] = GetBitmap("dice4");
            fullColorDice[4] = GetBitmap("dice5");
            fullColorDice[5] = GetBitmap("dice6");
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
            //diceList["White"] = whiteDice;
            //diceList["Black"] = blackDice;

            string[] whiteDiceNames = { "White One", "White Two", "White Three", "White Four", "White Five", "White Six" };
            string[] blackDiceNames = { "Black Six", "Black Five", "Black Four", "Black Three", "Black Two", "Black One" };
            string[] allnames = whiteDiceNames.Concat(blackDiceNames).ToArray();
            allnames[6] = "Black Six";

            diceNamesList["Full_Color"] = allnames;
            //diceNamesList["White"] = whiteDiceNames;
            //diceNamesList["Black"] = blackDiceNames;
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
                Directory.CreateDirectory(filePath + "_Output");
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

                        float width = resultSize.Width;
                        float height = resultSize.Height;
                        float aspectRatio = width / height;
                        int diceBuffer = 100;
                        var curDiceRequired = width * height;

                        while (curDiceRequired < maxDice - diceBuffer)
                        {
                            width += aspectRatio;
                            height += 1 / aspectRatio;
                            curDiceRequired = width * height;
                        }

                        if (resultSize.Width <= bm.Width && resultSize.Height <= bm.Height)
                            bm = ResizeImage(bm, (int) width, (int) height);
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
            using (StreamWriter file = new StreamWriter(GetFullFilePath(filePath, fileNameNoExt, curName, ".txt")))
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
            var lines = File.ReadAllLines(GetFullFilePath(filePath, fileNameNoExt, curDicePackName, ".txt"));
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

            preview.Save(GetFullFilePath(filePath, fileNameNoExt, curDicePackName, "_DiceImage.png"), ImageFormat.Png);
            WriteInstructions(curDicePackName, filePath, fileNameNoExt);

            this.Dispatcher.Invoke(() =>
            {
                (FindName(curDicePackName) as System.Windows.Controls.Image).Source = BitmapToImageSource(preview);
            });
        }

        private void WriteInstructions(string curDicePackName, string filePath, string fileNameNoExt)
        {
            string line;
            var lineCount = 1;
            int width = 0, height = 0;
            var diceInstruction = diceNamesList[curDicePackName];
            var diceCount = new Dictionary<string, int>();
            var diceColorCount = new Dictionary<string, int>();

            var instructionsFilePath = GetFullFilePath(filePath, fileNameNoExt, curDicePackName, "_Instructions.txt");
            var diceIntFilePath = GetFullFilePath(filePath, fileNameNoExt, curDicePackName, ".txt");

            using (StreamReader file = new StreamReader(diceIntFilePath))
            using (StreamWriter instructions = new StreamWriter(instructionsFilePath))
            {
                line = file.ReadLine();
                
                while (line != null)
                {
                    height++;
                    instructions.WriteLine($"{Environment.NewLine}Instructions for row number {height} below:");

                    var splitLine = line.Split(null).Where(c => c != " " && c != "" && c != null)
                                    .Select(c => int.Parse(c))
                                    .Select(c => diceInstruction[c]);

                    var splitLineLinked = new LinkedList<string>(splitLine);

                    while (splitLineLinked.Count() > 0)
                    {
                        var curDiceName = splitLineLinked.First.Value;
                        var curDiceList = splitLineLinked.TakeWhile(c => c == curDiceName);
                        var curSequenceCount = curDiceList.Count();

                        instructions.WriteLine($"#{lineCount}: Put ({curSequenceCount}) {curDiceName} in a row");

                        IncrementKey(diceCount, curDiceName, curSequenceCount);
                        lineCount++;

                        for (var i = 0; i < curSequenceCount; i++)
                            splitLineLinked.RemoveFirst();
                    }

                    if (width == 0)
                        width = splitLine.Count();                    

                    instructions.WriteLine($"#{lineCount}: Start new line on board");
                    lineCount++;

                    line = file.ReadLine();
                }
            }

            foreach (var curDiceName in diceCount.Keys)
            {
                var diceColor = curDiceName.Split(null)[0];
                var countForDiceNum = diceCount[curDiceName];

                IncrementKey(diceColorCount, diceColor, countForDiceNum);
            }

            string diceTotals = GetTotalDiceString(diceCount, diceColorCount, width, height);
            PrependStringToFile(instructionsFilePath, diceTotals);
        }

        private string GetTotalDiceString(Dictionary<string, int> diceCount, Dictionary<string, int> diceColorCount, int width, int height)
        {
            int totalDice = 0;

            foreach (var curDiceColor in diceColorCount.Keys)
                totalDice += diceColorCount[curDiceColor];

            string diceTotals = $"-- Total Dice required: {totalDice} -- {Environment.NewLine}";

            diceTotals += $"-- Width: {width} | Height: {height} | WxH: {width * height} -- {Environment.NewLine}";

            foreach (var curDiceColor in diceColorCount.Keys)
                diceTotals += $"-- Total {curDiceColor} required: {diceColorCount[curDiceColor]} --{Environment.NewLine}";

            diceTotals += Environment.NewLine;

            foreach (var curDice in diceCount.Keys)
                diceTotals += $"-- Total {curDice} required: {diceCount[curDice]} -- {Environment.NewLine}";

            diceTotals += Environment.NewLine;

            return diceTotals;
        }

        private void PrependStringToFile(string filePath, string newContent)
        {
            string currentContent = String.Empty;
            if (File.Exists(filePath))
            {
                currentContent = File.ReadAllText(filePath);
            }
            File.WriteAllText(filePath, newContent + currentContent);
        }

        private string GetFullFilePath(string filePath, string fileNameNoExt, string curName, string ext)
        {
            return filePath + "_Output\\" + fileNameNoExt + "_" + curName + ext;
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

        public static void IncrementKey<TKey>(Dictionary<TKey, int> dictionary, TKey key, int value)
        {
            dictionary.TryGetValue(key, out var currentCount);
            dictionary[key] = currentCount += value;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ImageQuantization
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        Stopwatch timer = new Stopwatch();

        //View Console
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();        

        //Global Declaration
        Dictionary<int, RGBPixel>[] Red = new Dictionary<int, RGBPixel>[256];
        Dictionary<int, RGBPixel>[,] Green = new Dictionary<int, RGBPixel>[256, 256];

        Dictionary<int, int> distinctRed = new Dictionary<int, int>();
        Dictionary<RGBPixel, int> distinctGreen = new Dictionary<RGBPixel, int>();

        Dictionary<int, RGBPixel> Colors = new Dictionary<int, RGBPixel>();

        Dictionary<int, Edges> edges = new Dictionary<int, Edges>();

        Dictionary<int, int> max_Indicies = new Dictionary<int, int>();

        bool lastColor = false, check = false;
        int ImageWidth, ImageHeight, indicies_Count = 0, k;
        float Total_Cost = 0;

        struct Edges
        {
            public float distance;

            public RGBPixel firstColor;
            public RGBPixel secondColor;
        };
        Edges e;


        RGBPixel[,] ImageMatrix;
        private void btnOpen_Click(object sender, EventArgs e)
        {

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Open the browsed image and display it
                string OpenedFilePath = openFileDialog1.FileName;
                ImageMatrix = ImageOperations.OpenImage(OpenedFilePath);
                ImageOperations.DisplayImage(ImageMatrix, pictureBox1);
            }
            txtWidth.Text = ImageOperations.GetWidth(ImageMatrix).ToString();
            //Width
            ImageWidth = ImageOperations.GetWidth(ImageMatrix);

            txtHeight.Text = ImageOperations.GetHeight(ImageMatrix).ToString();
            //Height
            ImageHeight = ImageOperations.GetHeight(ImageMatrix);
        }

        private void btnQuantize_Click(object sender, EventArgs e)
        {
            timer.Start();

            k = int.Parse(txtKValue.Text);

            Distinct_Colors();
            Console.WriteLine("Colors = {0}", Colors.Count);

            Calculate_Distance();
            Console.WriteLine("Total Cost = " + Total_Cost);

            Console.WriteLine("Representative colors");
            Clusters();
            Console.WriteLine("Clusters");

            Mapping();
            Console.WriteLine("Mapping");

            ImageOperations.DisplayImage(ImageMatrix, pictureBox2);

            timer.Stop();
            Console.WriteLine("\nTotal time: " + timer.Elapsed);
        }

        void Distinct_Colors()
        {
            for (int i = 0; i < ImageHeight; i++)
            {
                for (int j = 0; j < ImageWidth; j++)
                {
                    if (distinctRed.ContainsKey(ImageMatrix[i, j].red))
                    {
                        //Distinct green values
                        if (!Red[ImageMatrix[i, j].red].ContainsKey(ImageMatrix[i, j].green))
                        {
                            Red[ImageMatrix[i, j].red].Add(ImageMatrix[i, j].green, ImageMatrix[i, j]);

                            Green[ImageMatrix[i, j].red, ImageMatrix[i, j].green] = new Dictionary<int, RGBPixel>();
                            Green[ImageMatrix[i, j].red, ImageMatrix[i, j].green].Add(ImageMatrix[i, j].blue, ImageMatrix[i, j]);
                            distinctGreen.Add(ImageMatrix[i, j], 1);
                        }
                        //Distinct blue values
                        else if (!Green[ImageMatrix[i, j].red, ImageMatrix[i, j].green].ContainsKey(ImageMatrix[i, j].blue))
                        {
                            Green[ImageMatrix[i, j].red, ImageMatrix[i, j].green].Add(ImageMatrix[i, j].blue, ImageMatrix[i, j]);
                        }
                    }
                    else
                    {
                        Red[ImageMatrix[i, j].red] = new Dictionary<int, RGBPixel>();
                        Red[ImageMatrix[i, j].red].Add(ImageMatrix[i, j].green, ImageMatrix[i, j]);
                        distinctRed.Add(ImageMatrix[i, j].red, 1);

                        Green[ImageMatrix[i, j].red, ImageMatrix[i, j].green] = new Dictionary<int, RGBPixel>();
                        Green[ImageMatrix[i, j].red, ImageMatrix[i, j].green].Add(ImageMatrix[i, j].blue, ImageMatrix[i, j]);
                        distinctGreen.Add(ImageMatrix[i, j], 1);
                    }
                }
            }

            int counter = 0;
            foreach (KeyValuePair<RGBPixel, int> i in distinctGreen)
            {
                foreach (KeyValuePair<int, RGBPixel> j in Green[i.Key.red, i.Key.green])
                {
                    Colors.Add(counter++, j.Value);
                }
            }
        }

        void Calculate_Distance()
        {
            edges = new Dictionary<int, Edges>();
            Dictionary<int, float> Key = new Dictionary<int, float>();

            bool[] inMST = new bool[Colors.Count];
            int leastIndex = 0, counter = 0;

            Key.Add(0, 0);
            for (int vertix = 0; vertix < Colors.Count - 1; vertix++)
            {
                float least = 10000000;
                int newVertix = leastIndex;
                inMST[leastIndex] = true;

                for (int adjecent = 0; adjecent < Colors.Count; adjecent++)
                {
                    //Prim's
                    if (inMST[adjecent] == false)
                    {
                        float red = Colors[newVertix].red - Colors[adjecent].red,
                                green = Colors[newVertix].green - Colors[adjecent].green,
                                blue = Colors[newVertix].blue - Colors[adjecent].blue;

                        float sum = (float)Math.Sqrt(red * red + green * green + blue * blue);
                        if (vertix == 0)
                        {
                            Key.Add(adjecent, sum);
                        }
                        else if (sum < Key[adjecent])
                        {
                            Key[adjecent] = sum;
                        }
                        //Calculate least distance
                        if (Key[adjecent] < least)
                        {
                            least = Key[adjecent];
                            leastIndex = adjecent;
                        }
                    }
                    //Add least distance and update entire cost
                    if (adjecent == Colors.Count - 1)
                    {
                        e = new Edges
                        {
                            distance = least,
                            firstColor = Colors[newVertix],
                            secondColor = Colors[leastIndex]
                        };
                        edges.Add(counter++, e);
                        Total_Cost += least;
                    }
                }
            }
        }

        void Clusters()
        {
            //Get max distances in the tree
            float max_Distance = -1;
            int max_Index = 0;
            for (int i = 0; i < k - 1; i++)
            {
                for (int j = 0; j < edges.Count; j++)
                {
                    if (edges[j].distance > max_Distance)
                    {
                        max_Distance = edges[j].distance;
                        max_Index = j;
                    }
                }
                max_Indicies.Add(i, max_Index);
                max_Distance = -1;

                //remove taken distance
                e = new Edges
                {
                    distance = -1,
                    firstColor = edges[max_Index].firstColor,
                    secondColor = edges[max_Index].secondColor
                };
                edges[max_Index] = e;
            }

            int last_min = 0, min = 100000, delete_index = 0;
            for (int i = 0; i < k; i++)
            {
                //search for distance with min index (near to zero)
                foreach (KeyValuePair<int, int> j in max_Indicies)
                {
                    if (j.Value < min)
                    {
                        min = j.Value;
                        delete_index = j.Key;
                    }
                }
                //calculating representative color 
                Calculate_representative_color(last_min, min);

                last_min = min + 1;
                max_Indicies.Remove(delete_index);

                //One before last iteration
                if (max_Indicies.Count == 1 && !lastColor)
                {
                    foreach (KeyValuePair<int, int> x in max_Indicies)
                    {
                        indicies_Count = max_Indicies[x.Key];
                    }
                }

                //not to enter the below condition in the last iteration
                if (check)
                    lastColor = false;
                
                //last color is in separate cluster
                if (indicies_Count == edges.Count - 1 && !check)
                {
                    lastColor = true;
                    check = true;
                }

                min = 100000;
            }
        }

        void Calculate_representative_color(int last_min, int min)
        {
            //calculate last cluster)
            if (min == 100000)
                min = edges.Count - 1;

            RGBPixel tmp = new RGBPixel();
            float r = 0, g = 0, b = 0;
            int counter = 0;
            for (int i = last_min; i <= min; i++)
            {
                r += edges[i].firstColor.red;
                g += edges[i].firstColor.green;
                b += edges[i].firstColor.blue;
                counter++;
            }
            if (min == edges.Count - 1 && !lastColor)
            {
                r += edges[min].secondColor.red;
                g += edges[min].secondColor.green;
                b += edges[min].secondColor.blue;

                tmp.red = (byte)Math.Round(r / ++counter);
                tmp.green = (byte)Math.Round(g / counter);
                tmp.blue = (byte)Math.Round(b / counter);
                Green[edges[min].secondColor.red, edges[min].secondColor.green][edges[min].secondColor.blue] = tmp;
            }
            else
            {
                tmp.red = (byte)Math.Round(r / counter);
                tmp.green = (byte)Math.Round(g / counter);
                tmp.blue = (byte)Math.Round(b / counter);
            }

            for (int i = last_min; i <= min; i++)
            {
                Green[edges[i].firstColor.red, edges[i].firstColor.green][edges[i].firstColor.blue] = tmp;
            }

            //Console.WriteLine("Red:" + "(" + tmp.red + ")" + "   Green:" + "(" + tmp.green + ")" + "   Blue:" + "(" + tmp.blue + ")");
        }

        void Mapping()
        {
            for (int i = 0; i < ImageHeight; i++)
            {
                for (int j = 0; j < ImageWidth; j++)
                {
                    ImageMatrix[i, j] = Green[ImageMatrix[i, j].red, ImageMatrix[i, j].green][ImageMatrix[i, j].blue];
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            AllocConsole();
        }
    }
}
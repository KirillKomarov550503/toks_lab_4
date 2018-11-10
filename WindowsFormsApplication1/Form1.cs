using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows.Forms;
namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void UpdatePortsInfo()
        {
            while (true)
            {
                string[] portNames = SerialPort.GetPortNames();
                foreach (string name in portNames)
                {
                    this.Invoke((MethodInvoker)(delegate
                    {
                        if (!comboBox1.Items.Contains(name))
                            comboBox1.Items.Add(name);

                    }));
                }
                Thread.Sleep(5000);
            }
        }
        private SerialPort serialPort;
        private Thread thread;
        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.ReadOnly = true;
            textBox2.ReadOnly = true;
            textBox1.ScrollBars = ScrollBars.Vertical;
            textBox2.ScrollBars = ScrollBars.Vertical;
            textBox3.ScrollBars = ScrollBars.Vertical;
            button1.Text = "Connect";
            button2.Text = "Send";
            thread = new Thread(UpdatePortsInfo);
            thread.Start();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Connect")
            {
                try
                {
                    serialPort = new SerialPort(comboBox1.SelectedItem.ToString(), 9600, Parity.None, 8);
                    serialPort.Open();
                    if (serialPort.IsOpen)
                    {
                        textBox2.Text += "Connected to port " + comboBox1.SelectedItem.ToString() + "\r\n";
                        serialPort.DataReceived += DataReceived;
                        button1.Text = "Disconnect";
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                catch (Exception ex)
                {
                    textBox2.Text += "Problems with connection to port\r\n";
                }
            }
            else
            {
                try
                {
                    serialPort.Close();
                    if (serialPort.IsOpen)
                    {
                        throw new Exception();
                    }
                    else
                    {
                        button1.Text = "Connect";
                    }
                }
                catch (Exception ex)
                {
                    textBox2.Text += "Problems with disconnection\r\n";
                }
            }
        }

        private static string receivedMessage = "";
        private static int collisionCount = 0;
        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int size = serialPort.BytesToRead;
            byte[] bytes = new byte[size];
            serialPort.Read(bytes, 0, bytes.Length);
            byte receiveByte  =0;
            foreach (byte bt in bytes)
            {
                receiveByte = bt;
            }
            Console.Write(Encoding.ASCII.GetString(new byte[] { receiveByte }) + "(" + receiveByte + ")" + "|");
            if (receiveByte == 0x08)
            {
                collisionCount++;
                this.Invoke((MethodInvoker)(delegate
                {
                    if (collisionCount == 1)
                        textBox2.Text += "\r\n";
                    textBox2.Text += "X";
                }));
                if (collisionCount == 10)
                {
                    receivedMessage = "";
                    this.Invoke((MethodInvoker)(delegate
                    {
                        textBox2.Text += "Program received message with too much amount of collision\r\n";
                        textBox2.Text += "_______________________________\r\n";
                        Console.WriteLine();
                    }));
                }

            }
            else if (receiveByte == 0x0A)
            {
                collisionCount = 0;
                this.Invoke((MethodInvoker)(delegate
                {
                    textBox1.Text += receivedMessage + "\r\n";
                    textBox2.Text += "___________________________\r\n";
                    receivedMessage = "";
                    Console.WriteLine();
                }));
            }
            else
            {
                collisionCount = 0;
                string temp = Encoding.ASCII.GetString(new byte[] { receiveByte });
                this.Invoke((MethodInvoker)(delegate
                {
                    receivedMessage += temp;
                    textBox2.Text += "\r\n" + temp;
                }));
            }

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            thread.Abort();
        }

        private static bool Random()
        {
            return new Random().Next(11) > 4;
        }

        private static void Delay(int tryNumber)
        {
            int times = tryNumber < 5 ? 30 : 10;
            Thread.Sleep(new Random().Next((int)Math.Pow(2.0, (double)tryNumber)) * times);

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox3.Text.Length > 0)
            {
                if (serialPort.IsOpen)
                {
                    if (Random())
                    {
                        textBox2.Text += "Channel is free" + "\r\n";
                    }
                    else
                    {
                        textBox2.Text += "Channel is busy" + "\r\n";
                        Thread.Sleep(1000);
                        textBox2.Text += "Channel is free" + "\r\n";
                    }
                    serialPort.RtsEnable = true;
                    byte[] bytes = Encoding.ASCII.GetBytes(textBox3.Text);
                    bool limit = false;
                    foreach (byte bt in bytes)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            if (Random())
                            {
                                serialPort.Write(new byte[] { 0x08 }, 0, 1);
                                textBox2.Text += "X";
                                Delay(i);
                            }
                            else
                            {
                                if (i != 0)
                                    textBox2.Text += "\r\n";
                                break;
                            }
                            limit = i == 9;
                        }
                        if (!limit)
                        {
                            byte[] temp = new byte[] { bt };
                            textBox2.Text += Encoding.ASCII.GetString(temp) + "\r\n";
                            serialPort.Write(temp, 0, 1);
                            Thread.Sleep(50);
                        }
                        else
                        {
                            textBox2.Text += " Program try to send symbol \"" + Encoding.ASCII.GetString(new byte[] { bt }) + "\" 10 times\r\n";
                            limit = false;
                            return;
                        }
                    }
                    serialPort.Write(new byte[] { 0x0A }, 0, 1);
                    Thread.Sleep(50);
                    serialPort.RtsEnable = false;
                }
                else
                    textBox2.Text += "You are not connected to any ports\r\n";
            }
            else
                textBox2.Text += "You are not input message\r\n";
        }

    }
}

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

        private SerialPort portWriter;
        private SerialPort portReader;

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.ReadOnly = true;
            textBox2.ReadOnly = true;
            textBox1.ScrollBars = ScrollBars.Vertical;
            textBox2.ScrollBars = ScrollBars.Vertical;
            textBox3.ScrollBars = ScrollBars.Vertical;
            button1.Text = "Connect";
            button2.Text = "Send";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Connect")
            {
                try
                {
                    String[] portNames = SerialPort.GetPortNames();
                    if (portNames.Length > 0)
                    {
                        portWriter = new SerialPort(portNames[0], 9600, Parity.None, 8);
                        portReader = new SerialPort(portNames[1], 9600, Parity.None, 8);
                        portWriter.Open();
                        portReader.Open();
                        if (portWriter.IsOpen && portReader.IsOpen)
                        {
                            textBox2.Text += "Successfull connection to COM ports" + "\r\n";
                            button1.Text = "Disconnect";
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                }
                catch (Exception ex)
                {
                    textBox2.Text += "Problems with connection to ports" + "\r\n";
                }
            }
            else
            {
                try
                {
                    portWriter.Close();
                    portReader.Close();
                    if (portWriter.IsOpen || portReader.IsOpen)
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

        private static bool Random()
        {
            return new Random().Next(11) > 4; //генератор значений true или false с вероятностью 50%
        }

        private static void Delay(int tryNumber)
        {
            Thread.Sleep(new Random().Next((int)Math.Pow(2.0, (double)tryNumber))); //задержка после возникновения коллизии. Формула взята из методы
        }

        private void ReceiveData()//    чтение данных из порта
        {
            int size = portReader.BytesToRead;
            byte[] bytes = new byte[size];
            portReader.Read(bytes, 0, bytes.Length);
            byte receiveByte = 0;
            foreach (byte bt in bytes)
            {
                receiveByte = bt;
            }
            if (receiveByte != 0x08) // если не символ коллизии, то добавляем его к строке, которая будет выведена в поле Output 
            {
                receiveMessage += Encoding.ASCII.GetString(new byte[] { receiveByte });
               
            }

        }

        private string receiveMessage = "";
       
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBox3.Text.Length > 0)
                {
                    if (portWriter.IsOpen)
                    {
                        textBox2.Text += "New message" + "\r\n";
                        portWriter.RtsEnable = true;
                        byte[] bytes = Encoding.ASCII.GetBytes(textBox3.Text);
                        bool limit = false;
                        foreach (byte bt in bytes)
                        {
                            while (Random()) // проверяем доступен ли канал. Если Random() равен true, значит канала занят и делаем задержку. И так до тех пор пока Random() не станет равен false, что значит, что канал свободен
                            {
                                Thread.Sleep(50);
                            }

                            byte[] temp = new byte[] { bt };
                            textBox2.Text += "-"; // в окно Debug вместо символа выводим символ "-"
                            portWriter.Write(temp, 0, 1); // сам же байт сообщения пишем в канал
                            Thread.Sleep(10);
                            ReceiveData(); // Читаем записанный из канала символ. Очень важно сначала записать символ в канал, а потом генерировать коллизии
                            int collisionCount = 0; // счетчик коллизий
                            for (; collisionCount < 10; collisionCount++)
                            {
                                if (Random()) // если true, значит возникла коллизия и нужно делать задержку.
                                {
                                    portWriter.Write(new byte[] { 0x08 }, 0, 1); //пишем коллизию в канал
                                    textBox2.Text += "X"; //выводим X окно Debug
                                    Delay(collisionCount);// делаем задержку и в функцию передаем номер попытки. Так требует формула в методе
                                    ReceiveData();
                                }
                                else
                                {
                                    break;// если коллизии не возникло, то прерываем цикл
                                }
                                limit = collisionCount == 9; // если количество коллизий равно 10
                            }
                            textBox2.Text += "\r\n";
                            if (limit)// если колличество коллизий стало равно 10, то значит сообщение испорчено и мы прекращаем пересылку сообщения
                            {
                                textBox2.Text += " Program try to send symbol \"" + Encoding.ASCII.GetString(new byte[] { bt }) + "\" 10 times\r\n";
                                limit = false;
                                receiveMessage = "";//очищаем полученное сообщение
                                return;
                            }

                        }
                        portWriter.RtsEnable = false;
                        textBox1.Text += receiveMessage + "\r\n"; // если сообщение не было испорчено 10 коллизиями подряд для одного символа, то выводим в окно Output полученное сообщение
                        receiveMessage = "";
                    }
                }
                else
                    textBox2.Text += "You are input empty message\r\n";
            }
            catch (Exception ex)
            {

                textBox2.Text += "You are not connected to any ports\r\n";
            }
        }

    }
}

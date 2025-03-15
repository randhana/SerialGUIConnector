using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Timers;

namespace RxTx_with_Arduino
{
    public partial class SerialCom : Form
    {
        string serialDataIn;  // To get serial data 
        bool waitForEnd = false;
        private System.Timers.Timer repeatTimer;
        private string filePath; // Store file path instead of content
        private bool isRepeating = false;

        public SerialCom()
        {
            InitializeComponent();
            repeatTimer = new System.Timers.Timer();
            repeatTimer.Elapsed += RepeatTimerElapsed;
        }

        private void SerialCom_Load(object sender, EventArgs e)
        {
            buttonOPEN.Enabled = true;
            button_CLOSE.Enabled = false;
            button_SEND.Enabled = false;
            button_start_repeat.Enabled = false;
            button_browsefile.Enabled = false;
            progressBar_STATUSbar.Value = 0;
            labelSTATUS.Text = "Disconnected";
            button_start_repeat.Text = "Start Repeating";
            labelSTATUS.ForeColor = Color.Red;

            comboBox_lineEnding.Items.AddRange(new string[] { "None", "Line Feed (LF)", "Carriage Return (CR)", "CR + LF (CRLF)" });
            LoadSettings(); // Load saved settings

            string[] portLists = SerialPort.GetPortNames();
            comboBox_COM_PORT.Items.AddRange(portLists);

            // Auto-open port if it’s available and was saved
            if (!string.IsNullOrEmpty(comboBox_COM_PORT.Text) && portLists.Contains(comboBox_COM_PORT.Text))
            {
                buttonOPEN_Click(null, null);
            }
        }

        private void buttonOPEN_Click(object sender, EventArgs e)
        {
            try
            {
                serialPort1.PortName = comboBox_COM_PORT.Text;
                serialPort1.BaudRate = Convert.ToInt32(comboBox_BAUD_RATE.Text);
                serialPort1.Open();

                buttonOPEN.Enabled = false;
                button_CLOSE.Enabled = true;
                button_SEND.Enabled = true;
                button_browsefile.Enabled = true;
                button_start_repeat.Enabled = true;
                progressBar_STATUSbar.Value = 100;
                labelSTATUS.Text = "CONNECTED";
                labelSTATUS.ForeColor = Color.Green;

                SaveSettings(); // Save settings after opening port
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private void button_CLOSE_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                try
                {
                    serialPort1.Close();

                    buttonOPEN.Enabled = true;
                    button_CLOSE.Enabled = false;
                    button_SEND.Enabled = false;
                    button_browsefile.Enabled = false;
                    button_start_repeat.Enabled = false;
                    progressBar_STATUSbar.Value = 0;
                    labelSTATUS.Text = "Disconnected";
                    richTextBox_Txt_Receiver.Text = "";
                    labelSTATUS.ForeColor = Color.Red;

                    sending_Status.Text = "Ready";
                    sending_Status.ForeColor = Color.Gray;
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message);
                }
            }
        }

        private void SerialCom_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                try
                {
                    repeatTimer.Stop();
                    serialPort1.Close();
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message);
                }
            }
            SaveSettings(); // Save settings on close
        }

        private void button_SEND_Click(object sender, EventArgs e)
        {
            try
            {
                if (waitForEnd)
                {
                    button_SEND.Enabled = false;
                    MessageBox.Show("Please Wait!");
                    return;
                }

                string textToSend = textBox_Txt_Send.Text;
                if (string.IsNullOrEmpty(textToSend) && !string.IsNullOrEmpty(filePath))
                {
                    textToSend = File.ReadAllText(filePath); // Re-read file each time
                }

                if (!string.IsNullOrEmpty(textToSend))
                {
                    serialPort1.Write(textToSend + GetLineEnding());
                    richTextBox_Txt_Receiver.Text += "";
                    textBox_Txt_Send.Text = "";
                    sending_Status.Text = "Message Sent";
                    sending_Status.ForeColor = Color.Black;
                    button_SEND.Enabled = true;
                    waitForEnd = false;
                }
                else
                {
                    MessageBox.Show("Invalid input or no file loaded");
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            serialDataIn = serialPort1.ReadExisting();
            this.Invoke(new EventHandler(ShowData));
        }

        private void ShowData(object sender, EventArgs e)
        {
            if (sending_Status.Text == "Message is sending...")
            {
                sendingStatus();
            }
            if (sending_Status.Text == "Ready")
            {
                receivingStatusAsync();
            }
            richTextBox_Txt_Receiver.Text += serialDataIn;
        }

        private async void sendingStatus()
        {
            button_SEND.Enabled = true;
            button_browsefile.Enabled = true;
            sending_Status.Text = "Message Sent";
            sending_Status.ForeColor = Color.Black;
            await Task.Delay(2000);
            sending_Status.Text = "Ready";
            sending_Status.ForeColor = Color.Black;
            waitForEnd = false;
        }

        private async Task receivingStatusAsync()
        {
            button_SEND.Enabled = true;
            button_browsefile.Enabled = true;
            sending_Status.Text = "Message Received";
            sending_Status.ForeColor = Color.Black;
            await Task.Delay(2000);
            sending_Status.Text = "Ready";
            sending_Status.ForeColor = Color.Black;
            waitForEnd = false;
        }

        private void richTextBox_Txt_Receiver_TextChanged(object sender, EventArgs e)
        {
            richTextBox_Txt_Receiver.SelectionStart = richTextBox_Txt_Receiver.Text.Length;
            richTextBox_Txt_Receiver.ScrollToCaret();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox_Txt_Receiver.Text = "";
        }

        private void textBox_Txt_Send_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button_SEND_Click(sender, e);
            }
        }

        private void button_browsefile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Select a Text File"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filePath = openFileDialog.FileName;
                textbox_filepath.Text = filePath;
                SaveSettings(); // Save file path
            }
        }

        private void RepeatTimerElapsed(object sender, ElapsedEventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                if (serialPort1.IsOpen && !waitForEnd && !string.IsNullOrEmpty(filePath))
                {
                    string content = File.ReadAllText(filePath); // Re-read file each time
                    serialPort1.Write(content + GetLineEnding());
                    richTextBox_Txt_Receiver.Text += "";
                    sending_Status.Text = "Message sent (repeating)";
                    sending_Status.ForeColor = Color.Black;
                }
            }));
        }

        private string GetLineEnding()
        {
            switch (comboBox_lineEnding.SelectedIndex)
            {
                case 1: return "\n"; // Line Feed (LF)
                case 2: return "\r"; // Carriage Return (CR)
                case 3: return "\r\n"; // CR + LF (CRLF)
                case 0: // None
                default: return "";
            }
        }

        private void button_start_repeat_Click(object sender, EventArgs e)
        {
            if (!serialPort1.IsOpen)
            {
                MessageBox.Show("Please open the serial port first!");
                return;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("Please load a text file first!");
                return;
            }

            if (!int.TryParse(textBox_interval.Text, out int intervalSeconds) || intervalSeconds <= 0)
            {
                MessageBox.Show("Please enter a valid interval in seconds!");
                return;
            }

            if (!isRepeating)
            {
                repeatTimer.Interval = intervalSeconds * 1000;
                repeatTimer.Start();
                isRepeating = true;
                button_start_repeat.Text = "Stop Repeating";
                sending_Status.Text = "Repeating every " + intervalSeconds + " seconds";
            }
            else
            {
                repeatTimer.Stop();
                isRepeating = false;
                button_start_repeat.Text = "Start Repeating";
                sending_Status.Text = "Ready";
                sending_Status.ForeColor = Color.Gray;
            }
            SaveSettings(); // Save repeat state and interval
        }

        private void LoadSettings()
        {
            comboBox_COM_PORT.Text = Properties.Settings.Default.COMPort;
            comboBox_BAUD_RATE.Text = Properties.Settings.Default.BaudRate ?? "9600";
            textbox_filepath.Text = Properties.Settings.Default.FilePath;
            filePath = Properties.Settings.Default.FilePath;
            textBox_interval.Text = Properties.Settings.Default.IntervalSeconds ?? "1";
            comboBox_lineEnding.SelectedIndex = Properties.Settings.Default.LineEndingIndex;
            if (Properties.Settings.Default.IsRepeating && !string.IsNullOrEmpty(filePath) && serialPort1.IsOpen)
            {
                isRepeating = true;
                button_start_repeat.Text = "Stop Repeating";
                repeatTimer.Interval = int.Parse(textBox_interval.Text) * 1000;
                repeatTimer.Start();
            }
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.COMPort = comboBox_COM_PORT.Text;
            Properties.Settings.Default.BaudRate = comboBox_BAUD_RATE.Text;
            Properties.Settings.Default.FilePath = filePath;
            Properties.Settings.Default.IntervalSeconds = textBox_interval.Text;
            Properties.Settings.Default.LineEndingIndex = comboBox_lineEnding.SelectedIndex;
            Properties.Settings.Default.IsRepeating = isRepeating;
            Properties.Settings.Default.Save();
        }

        private void label1_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void groupBox1_Enter(object sender, EventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void label4_Click(object sender, EventArgs e) { }
        private void SerialCom_FormClosed(object sender, FormClosedEventArgs e) { }
        private void Title_Click(object sender, EventArgs e) { }
        private void button_browsefile_HelpRequest(object sender, EventArgs e) { }
        private void textbox_filepath_TextChanged(object sender, EventArgs e) { }
    }
}
using System.Linq;
using System.Windows;
using System.Speech.Synthesis;
using System;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Timers;

namespace TtsApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SpeechSynthesizer _reader;
        private int index = 0;
        private List<subData> data = new List<subData>();
        const string voiceName = "Microsoft Huihui Desktop";
        Timer t = new Timer();

        public MainWindow()
        {
            InitializeComponent();
            this.Time.Text = "00:00:00";
            t.AutoReset = false;
            t.Elapsed += t_Elapsed;
            _reader = new SpeechSynthesizer();
            //MessageBox.Show(_reader.GetInstalledVoices().First().VoiceInfo.Name);
            var voiceInstalled = _reader.GetInstalledVoices()
                                     .FirstOrDefault(x => x.VoiceInfo.Description.Contains(voiceName)) != null;

            if (!voiceInstalled)
            {
                MessageBox.Show("Voice $'{voiceName}' is not installed in system. Now we exit");
                Application.Current.Shutdown();
            }
            try
            {
                _reader.SelectVoice(voiceName);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, e.StackTrace);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _reader.Speak(TextToSpeakBox.Text);
            //Prompt p = new Prompt("");
            PromptBuilder pb = new PromptBuilder();
            pb.AppendText(TextToSpeakBox.Text, PromptRate.Fast);
            _reader.SpeakAsync(pb);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() ?? true)
            {
                String[] content = File.ReadAllLines(dialog.FileName);
                readFile(content);
            }
        }

        void t_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this.data.Count <= this.index)
            {
                return;
            }
            TimeSpan cur = this.data[this.index].Pos;
            this.index++;
            if (this.data.Count > this.index)
            {
                t.Interval = (this.data[this.index].Pos - cur).TotalMilliseconds;
                t.Start();
            }
            var c = this.data[this.index];
            System.Console.WriteLine(DateTime.Now.ToString() + "---" + c.Pos.ToString() + c.Content);
            //_reader.Speak(this.data[this.index].Content);
            PromptBuilder pb = new PromptBuilder();
            pb.AppendText(c.Content, c.Rate);
            _reader.SpeakAsync(pb);
        }

        private void readFile(String[] content)
        {
            /*
             * 
1
00:00:02,000 --> 00:00:07,000
Downloaded from
YTS.MX
             */
            int inline = 0;
            subData d = new subData();
            foreach (String line in content)
            {
                switch (inline)
                {
                    case 0:
                        inline++;
                        if (line.Trim().Length == 0)
                        {
                            return;
                        }
                        d.Index = int.Parse(line.Trim());
                        break;
                    case 1:
                        d.Pos = TimeSpan.Parse(line.Substring(0, 12).Replace(",", "."));
                        TimeSpan end = TimeSpan.Parse(line.Substring(17, 12).Replace(",", "."));
                        d.Session = (end - d.Pos).TotalMilliseconds;
                        //d.Pos = TimeSpan.ParseExact(line.Substring(0, 12), "hh:mm:ss,fff", null);
                        inline++;
                        break;
                    case 2:
                        String cur = line.Trim();
                        if (cur.Length == 0)
                        {
                            double word = (d.Content.Length * 1000 / d.Session);
                            if (word < 0.8)
                            {
                                d.Rate = PromptRate.Slow;
                            }
                            else if (word <= 4)
                            {
                                d.Rate = PromptRate.Medium;
                            }
                            else if (word < 9)
                            {
                                d.Rate = PromptRate.Fast;
                            }
                            else
                            {
                                d.Rate = PromptRate.ExtraFast;
                            }
                            this.data.Add(d);
                            d = new subData();
                            inline = 0;
                            break;
                        }
                        d.Content += line.Trim();
                        break;
                    default:
                        break;
                }
            }
        }

        class subData
        {
            public int Index;
            public String Content;
            public TimeSpan Pos;
            public double Session;
            public PromptRate Rate;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            TimeSpan start;
            if (!TimeSpan.TryParse(this.Time.Text, out start))
            {
                MessageBox.Show("时间格式错误");
                return;
            }
            for (int i = 0; i < data.Count; i++)
            {
                if (this.data[i].Pos >= start)
                {
                    this.index = i;
                    break;
                }
            }
            if (this.index >= data.Count)
            {
                return;
            }
            t.Interval = (data[this.index].Pos - start).TotalMilliseconds;
            t.Start();
        }
    }
}

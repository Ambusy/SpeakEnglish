using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SpeakEng
{
    public partial class Form1 : Form
    {
        List<Lemma> lemmas = new List<Lemma>();
        string[] wavFiles = new string[32]; // [0] unused
        int[] FirstSoundByte = new int[32];
        string lineToParse;
        bool changeMadeByProgram = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Lemma_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (changeMadeByProgram) return;
            Lemma lemma = lemmas[lbLemma.SelectedIndex];
            string fn = lemma.wavFileName + ".WAV";
            audio.LoadFile(fn, lemma.startPositionInWavfile, lemma.lengthOfSoundByte);
            audio.Play(-1);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Enabled = true;
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            Form1_Loaded();
            tbFind.Visible = true;
            lbLemma.Visible = true;
            label1.Visible = true;
            label2.Text = "Click on word to hear it.";
        }
        private void Form1_Loaded()
        {
            int nWavFiles = 0;
            System.IO.StreamReader wordIndexFile = new System.IO.StreamReader("WordIndex.txt");
            string lineRead;
            string AllWavfileNames = " ";
            char firstLetter = ' ';
            while ((lineRead = wordIndexFile.ReadLine()) != null)
            {
                if (lineRead.Length > 1)
                {
                    if (firstLetter != lineRead[1])
                    {
                        firstLetter = lineRead[1];
                        label2.Text = "Loading " + firstLetter;
                        label2.Refresh();
                    }
                    var NLemma = new Lemma();
                    lineToParse = lineRead;
                    string tempString = NextString();
                    lbLemma.Items.Add(tempString);
                    NLemma.lemmaText = tempString.ToUpper();
                    NLemma.wavFileName = NextString();
                    NLemma.startPositionInWavfile = NextInteger();
                    NLemma.lengthOfSoundByte = NextInteger();
                    lemmas.Add(NLemma);
                    if (AllWavfileNames.IndexOf(' ' + NLemma.wavFileName + ' ') == -1)
                    {
                        AllWavfileNames = AllWavfileNames + NLemma.wavFileName + ' ';
                        nWavFiles = nWavFiles + 1;
                        wavFiles[nWavFiles] = NLemma.wavFileName;
                        openWavFileAndReadHeaders(nWavFiles);
                        audio.CloseFile();
                    }
                }
            }
            label2.Text = "Loading dictionary";
            label2.Refresh();
            wordIndexFile.Close();
        }
        void openWavFileAndReadHeaders(int CurFile)
        {
            string fn = wavFiles[CurFile] + ".WAV";
            int dataOffset = audio.OpenFile(fn);
            // store initial and final limits of data chunk            
            FirstSoundByte[CurFile] = dataOffset;
        }
        private void tbFind_TextChanged(object sender, EventArgs e)
        {
            string s = tbFind.Text.Trim();
            if (s.Length != 0)
            {
                int i = Math.Abs(binSearchOnLemmas(s.ToUpper()));
                changeMadeByProgram = true;
                lbLemma.SelectedIndex = i + 10;
                lbLemma.SelectedIndex = i;
                changeMadeByProgram = false;
            }
        }
        string NextString()
        {   // Get next "string" from lineToParse , truncate lineToParse
            int i, st, en;
            string sr;
            st = 0;
            en = 0;
            for (i = 1; i <= lineToParse.Length; i++)
            {
                if (lineToParse[i - 1] == '"')
                {
                    if (st == 0)
                        st = i;
                    else if (en == 0)
                    {
                        en = i;
                    }
                }
                else if (en > 0 && lineToParse[i - 1] == ',')
                {
                    sr = lineToParse.Substring(st, en - st - 1);
                    lineToParse = lineToParse.Substring(i);
                    return sr;
                }
            }
            return lineToParse;
        }
        int NextInteger()
        {  // Get next integer from lineToParse , truncate lineToParse
            int ir;
            string sr;
            int i, l;
            l = lineToParse.Length + 1;
            for (i = 1; i < l; i++)
            {
                if (i == l - 1 || lineToParse[i - 1] == ',')
                {
                    if (i == l - 1)
                        sr = lineToParse.Substring(0, i);
                    else
                        sr = lineToParse.Substring(0, i - 1);
                    ir = int.Parse(sr);
                    lineToParse = lineToParse.Substring(i);
                    return ir;
                }
            }
            return 0; // never hits this line
        }
        int binSearchOnLemmas(string s)
        {  // binary search sorted list
            int i, i1, i2;
            Lemma NLemma;
            i = 0;
            i1 = -1;
            i2 = lemmas.Count;
            while (i2 - i1 > 1)
            {
                i = i1 + (i2 - i1) / 2;
                NLemma = lemmas[i];
                if (s.CompareTo(NLemma.lemmaText) < 0)
                    i2 = i;
                else
                    i1 = i;
            }
            NLemma = lemmas[i];
            if (s.CompareTo(NLemma.lemmaText) == 0)
                return i;
            if (i1 > -1)
            {
                NLemma = lemmas[i1];
                if (s.CompareTo(NLemma.lemmaText) == 0)
                    return i1;
            }
            if (i2 < lemmas.Count)
            {
                NLemma = lemmas[i2];
                if (s.CompareTo(NLemma.lemmaText) == 0)
                    return i2;
            }
            return -(i1 + 1); // not found:  next higher negative (to avoid -0!)
        }
    }
    class Lemma
    {
        public string lemmaText;
        public string wavFileName;
        public int startPositionInWavfile;
        public int lengthOfSoundByte;
    }
}

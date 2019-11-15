using Snoop;
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Snooper
{
    public partial class Form1 : Form
    {

        SteamSnooper program;

        public Form1()
        {
            InitializeComponent();
        }

        public void GetSettings()
        {
            textBox1.Text = Properties.Settings.Default.SteamWebAPIKey;
            textBox2.Text = Properties.Settings.Default.Client;
            textBox3.Text = Properties.Settings.Default.PrivateSteamID;
            checkBox1.Checked = Properties.Settings.Default.CheckedCredentials;
            checkBox2.Checked = Properties.Settings.Default.CheckedSteamID;
        }

        public void SaveCredentials()
        {
            Properties.Settings.Default.SteamWebAPIKey = textBox1.Text;
            Properties.Settings.Default.Client = textBox2.Text;
            Properties.Settings.Default.Save(); // important line.
        }

        public void SaveSteamID64()
        {
            Properties.Settings.Default.PrivateSteamID = textBox3.Text;
            Properties.Settings.Default.Save(); // important line.
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GetSettings();
        }

        private void Label1_Click(object sender, EventArgs e)
        {

        }
        
        private void Label2_Click(object sender, EventArgs e)
        {

        }

        private void Label3_Click(object sender, EventArgs e)
        {

        }

        private void Label4_Click(object sender, EventArgs e)
        {

        }

        private void Label5_Click(object sender, EventArgs e)
        {

        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void TextBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void TextBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked)
            {
                Properties.Settings.Default.SteamWebAPIKey = "";
                Properties.Settings.Default.Client = "";
            }
            else
            {
                if (this.textBox1.Text != "" && this.textBox2.Text != "")
                {
                    Properties.Settings.Default.SteamWebAPIKey = this.textBox1.Text;
                    Properties.Settings.Default.Client = this.textBox2.Text;
                }
            }
            Properties.Settings.Default.CheckedCredentials = checkBox1.Checked;
            Properties.Settings.Default.Save(); // important line.
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox2.Checked)
            {
                Properties.Settings.Default.PrivateSteamID = "";
            }
            else
            {
                if (this.textBox3.Text != "")
                {
                    Properties.Settings.Default.PrivateSteamID = this.textBox3.Text;
                }
            }
            Properties.Settings.Default.CheckedSteamID = checkBox2.Checked;
            Properties.Settings.Default.Save(); // important line.
        }

        private void RichTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void SubmitCredentials_Click(object sender, EventArgs e)
        {
            if (checkBox1.Checked) // if the checkbox is checked, save the creds. 
            {
                SaveCredentials();
            }
        }

        private void SubmitSteamID64_Click(object sender, EventArgs e)
        {
            if (checkBox2.Checked) // if the checkbox is checked, save the creds. 
            {
                SaveSteamID64();
            }
        }

        private async void Search_Click(object sender, EventArgs e)
        {
            timer1.Start();
            if (this.textBox1.Text != null && this.textBox2.Text != null && this.textBox3.Text != "")
            {
                ulong client = Convert.ToUInt64(this.textBox2.Text);

                string steamWebApi = this.textBox1.Text;
                ulong privatePerson = Convert.ToUInt64(this.textBox3.Text);


                this.program = new SteamSnooper(steamWebApi, privatePerson); // create a new steamsnooper!
                await program.LoadSavedFriendsAsync();
                Console.WriteLine("past 1");

                try
                {
                    await program.OpenFriendsOfAsync(Convert.ToUInt64(this.textBox2.Text));
                }
                catch (System.ArgumentException ex)
                {
                    ; // do notin nigga
                }

                Console.WriteLine("past 2");
                await program.OpenFriendsOfUncheckedUsersAsync();
                Console.WriteLine("past 3");
                await program.OpenFriendsOfUncheckedUsersAsync();
                Console.WriteLine("past 4");
                await program.OpenFriendsOfUncheckedUsersAsync();
                Console.WriteLine("past 5");
            }
        }

        private void DisplayFriends()
        {
            if (program != null)
            {
                this.richTextBox1.Clear();
                this.richTextBox1.AppendText(program.getFriendsOfPersonToSearch());
            }
        }

        private async void TextBox4_TextChangedAsync(object sender, EventArgs e)
        {

        }

        private async void Add_Click(object sender, EventArgs e)
        {
            string s = this.textBox4.Text.Trim();
            ulong i;
            if (ulong.TryParse(s, out i))
            {
                try
                {
                    await program.OpenFriendsOfAsync(i);
                }
                catch (System.ArgumentException ex)
                {

                }
            }
        }


        private void Save_Friends()
        {
            string dir = Environment.CurrentDirectory + @"\saved_friends\";
            if (!Directory.Exists(dir)) // if the directory doesn't exist, make it. 
            {
                Directory.CreateDirectory(dir);
            }
            string friends = program.getFriendsOfPersonToSearch();

            string file = dir + this.textBox3.Text + "_friends.txt";
            try
            {
                File.WriteAllText(file, friends);
            }
            catch (Exception ex)
            {

            }
        }
          
        private void Timer1_Tick(object sender, EventArgs e)
        {
            Save_Friends();
            DisplayFriends();
        }
    }
}
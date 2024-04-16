using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

struct Program
{
	[STAThread]
	static void Main()
	{
		Console.WriteLine("Piano_pls.cs Start");
		Application.Run(new FormPiano());
	}
}
class FormPiano : Form
{
	/*--------------------------------------------------*/
	/* 使用するAPIの宣言 */
	/*--------------------------------------------------*/
	[DllImport("Winmm.dll")]
	extern static uint midiOutOpen(ref long lphmo, uint uDeviceID, uint dwCallback, uint dwCallbackInstance, uint dwFlags);

	[DllImport("Winmm.dll")]
	extern static uint midiOutClose(long hmo);

	[DllImport("Winmm.dll")]
	extern static uint midiOutShortMsg(long hmo, uint dwMsg);

	[DllImport("kernel32.dll")]
	extern static IntPtr GetConsoleWindow();

	[DllImport("user32.dll")]
	extern static bool SetWindowPos(IntPtr hwnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

	[DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, CallingConvention = System.Runtime.InteropServices.CallingConvention.StdCall)]
	extern static short GetKeyState(int nVirtKey);

	private const uint MMSYSERR_NOERROR = 0;
	private const uint MIDI_MAPPER = 0xffffffff;

	/*--------------------------------------------------*/

	readonly public static string pianoVersion = "ver.1.40.21";
	public static string binreadVersion = "NoData";
	public static string musicDirectory =@"C:\hoge";
	public static string musicPath =@"Music.txt";
	readonly public static string settingPath =@"Settings.dat";
	readonly public static string batPath =@"compile.bat";
	readonly public static string iconPath =@"pinfl.ico";
	public static string binreadPath =@"binread_pls";
	public static string[] midFilter;
	public static int midFilterInd = 0;
	public static bool sleepPC = false;
	public static bool fpsFlag = true;
	public static bool skipFrame = false;
	public static int fasts = 0;
	public static bool binSecure = false;
	public static bool binNoiseCut = false;
	public static double binNoiseSize = 0.06;

	public static bool newbinreadflag = false;

	private long hMidi;
	private List<int> listI;
	private List<string> listS;
	private List<byte> listB;
	private Label[] lbl;
	private ComboBox[] cmb;
	private CheckBox[] chkb;
	private Panel pnl;
	private Button[] key;
	private Button[] btn;
	private Color[] col;
	private Color[] rain;
	private static Timer timer1 = new Timer();
	private bool[] KDown;
	private int pitch = 60;
	private int[] pitchs;
	private int chcou = -1;
	private int waits = 10;
	private int mode = 2;
	private bool playback = false;
	private bool callback = false;
	private bool callTmp = false;
	private bool overflows = false;
	private StringBuilder data = new StringBuilder("");
	private string[] tmpdata;
	private byte[] prog;
	private int[] syncs;
	private string[] musicNames;
	private int nowMusicInd = -1;
	private int syn;
	private int synWC;
	private bool syncsOK = true;
	private bool synceFlag = false;
	private char[] nppc;
	private int avgChar = 0;
	private bool onsettingflag = false;


	private System.Diagnostics.Stopwatch fsw = System.Diagnostics.Stopwatch.StartNew();
	private int throughF = 0;
	private int avgFrame = 0;

	private bool colorful = false;

	private FormPianoHelp pianoHelp = null;



	public FormPiano()
	{
		Console.WriteLine("# Piano_pls.exe " + pianoVersion);
		if(File.Exists(iconPath)){
			this.Icon = new System.Drawing.Icon(iconPath);
		}
		else
		{
			Console.WriteLine("! Warning:\""+ iconPath +"\" does not exist");
		}
		this.KeyPreview = true;
		this.MaximizeBox = false;
		this.ClientSize = new Size(840, 240);
		this.Text = "PianoPls "+ pianoVersion;

		this.Load += new EventHandler(this.FormPiano_Load);
		this.Closed += new EventHandler(this.FormPiano_Closed);
		
		midFilter = new string[4];
		midFilter[0] = "*.mid";
		midFilter[1] = "*.mid*";
		midFilter[2] = "*-f0.mid";
		midFilter[3] = "*.txt";

		if(fasts  == 2){
			mode = 3;
		}

		Console.WriteLine("* StartUp Set");


		if(!File.Exists(binreadPath+".exe")){
			Console.WriteLine("! Warning:\"binread_pls.exe\" does not exist");
			if(!File.Exists(binreadPath+".cs")){
				Console.WriteLine("! Warning:\"binread_pls.cs\" does not exist");
			}
			else
			{
				if(!File.Exists(batPath)){
					Console.WriteLine("@ Create \"compile.bat\"");
					using (StreamWriter dest = new StreamWriter(batPath,false))
					{
						dest.WriteLine("@echo off");
						dest.WriteLine("PATH=\"%WINDIR%\\Microsoft.NET\\Framework\\v1.0.3705\";%PATH%");
						dest.WriteLine("PATH=\"%WINDIR%\\Microsoft.NET\\Framework\\v1.1.4322\";%PATH%");
						dest.WriteLine("PATH=\"%WINDIR%\\Microsoft.NET\\Framework\\v2.0.50727\";%PATH%");
						dest.WriteLine("PATH=\"%WINDIR%\\Microsoft.NET\\Framework\\v3.0\";%PATH%");
						dest.WriteLine("PATH=\"%WINDIR%\\Microsoft.NET\\Framework\\v3.5\";%PATH%");
						dest.WriteLine("PATH=\"%WINDIR%\\Microsoft.NET\\Framework\\v4.0.30319\";%PATH%");
						dest.WriteLine("if not \"%1\" == \"\" (");
						dest.WriteLine("csc %1");
						dest.WriteLine(") else (");
						dest.WriteLine("echo. You can easily compile with \"csc\" command.");
						dest.WriteLine("echo.");
						dest.WriteLine("cmd");
						dest.WriteLine(")");
						dest.WriteLine("if not %errorlevel% == 0 (");
						dest.WriteLine("echo. Error!");
						dest.WriteLine("PAUSE");
						dest.WriteLine(")");
					}
					Console.WriteLine("@ Close \"compile.bat\"");
				}
				Console.WriteLine("@ binread compile start");
				Process proc = new Process();
				proc.StartInfo.FileName = batPath;
				proc.StartInfo.Arguments = binreadPath+".cs";

				proc.Start();
				proc.WaitForExit();
				Console.WriteLine("@ binread compile end");

			}
		}
		if(File.Exists(binreadPath+".exe"))
		{
			Console.WriteLine("@ binread Load");
			Process proc = new Process();
			proc.StartInfo.FileName =binreadPath+".exe";

			proc.Start();
			proc.WaitForExit();
			Console.WriteLine("@ binread Close");
			int exco = proc.ExitCode;
			if(exco>1000000)
			{
				char[] arr = exco.ToString().ToCharArray();
				binreadVersion = "ver."+ arr[0] +"."+ int.Parse(arr[1].ToString()+arr[2].ToString()+arr[3].ToString()) +"."+ int.Parse(arr[4].ToString()+arr[5].ToString()+arr[6].ToString());
				Console.WriteLine("# binread_pls.exe "+ binreadVersion);
			}
			else
			{
				Console.WriteLine("! Warning:binread_pls.exe Wrong ExitCode");
			}
		}

		if(File.Exists(settingPath))
		{
			onsettingflag = true;
			SettingFiles.Reading();
		}
		else
		{
			Console.WriteLine("! Warning:\"Setting.dat\" does not exist");
			Console.WriteLine("@ Create \"Setting.dat\"");
			SettingFiles.Update();
			Console.WriteLine("@ Close \"Setting.dat\"");
		}


		Console.WriteLine("* musicDirectory Load");
		if(Directory.Exists(musicDirectory))
		{
			musicNames = Directory.GetFiles(musicDirectory, midFilter[midFilterInd], SearchOption.AllDirectories);
		}
		else
		{
			Console.WriteLine("! Warning:musicDirectory does not exist");
			musicNames = new string[1];
			musicNames[0] = "notdata";
		}
		if(musicNames.Length <= 0){
			Console.WriteLine("! Warning:midi file does not exist");
			musicNames = new string[1];
			musicNames[0] = "notdata";
		}

		/*--------------------------------------------------*/
		/* コントロールの作成 */
		/*--------------------------------------------------*/
		timer1.Interval = waits;
		timer1.Enabled = true;
		timer1.Tick += new EventHandler(timer1_Tick);
		Console.WriteLine("* Timer Set");

		this.pitchs = new int[1];
		this.tmpdata = new string[1];
		this.prog = new byte[1];
		this.syncs = new int[1];
		this.syncs[0] = 2;

		this.KDown = new bool[28];
		for(int i = 0; i < this.KDown.Length; i++)
		{
			this.KDown[i] = false;
		}
		Console.WriteLine("* KeyDownFlag Set");

		this.lbl = new Label[4];

		for( int i = 0; i < this.lbl.Length; i++)
		{
			this.lbl[i] = new Label();
			this.Controls.Add(this.lbl[i]);
		}
		this.lbl[0].SetBounds(1, 3, 44, 18, BoundsSpecified.All);
		this.lbl[0].Text = "channel";
		this.lbl[1].SetBounds(241, 3, 34, 18, BoundsSpecified.All);
		this.lbl[1].Text = " wait";
		this.lbl[2].SetBounds(530, 0, 230, 38, BoundsSpecified.All);
		if(fasts == 0)
		{
			this.lbl[2].Text = "Char:0/0 0 n/s 0s\nNA:";
		}
		else
		{
			this.lbl[2].Text = "";
		}
		this.lbl[3].SetBounds(760, 0, 100, 38, BoundsSpecified.All);
		if(fpsFlag){
			this.lbl[3].Text = "0fps\nsyn: 0";
		}
		else
		{
			this.lbl[3].Text = "";
		}
		Console.WriteLine("* Label Set");

		this.cmb = new ComboBox[5];

		for( int i = 0; i < this.cmb.Length; i++)
		{
			this.cmb[i] = new ComboBox();
			this.cmb[i].DropDownStyle = ComboBoxStyle.DropDownList;
			this.Controls.Add(this.cmb[i]);
		}

		this.cmb[0].SetBounds(46, 0, 60, 40, BoundsSpecified.All);
		this.cmb[0].Items.Add("Piano");
		this.cmb[0].Items.Add("Dram");

		this.cmb[0].SelectedIndex = 0;

		this.cmb[1].SetBounds(106, 0, 135, 40, BoundsSpecified.All);
		//ピアノ系
		this.cmb[1].Items.Add("Acostic Grand Piano");
		this.cmb[1].Items.Add("Bright Acostic Piano");
		this.cmb[1].Items.Add("Electric Grand Piano");
		this.cmb[1].Items.Add("Honky-Tonk Piano");
		this.cmb[1].Items.Add("Electric Piano 1");
		this.cmb[1].Items.Add("Electric Piano 2");
		this.cmb[1].Items.Add("Harpsicord");
		this.cmb[1].Items.Add("Clavi");
		//クロマチック・パーカッション系
		this.cmb[1].Items.Add("Celesta");
		this.cmb[1].Items.Add("Glockenspiel");
		this.cmb[1].Items.Add("Music Box");
		this.cmb[1].Items.Add("Vibraphone");
		this.cmb[1].Items.Add("Marimba");
		this.cmb[1].Items.Add("Xylophone");
		this.cmb[1].Items.Add("Tubular Bells");
		this.cmb[1].Items.Add("Dulcimer");
		//オルガン系
		this.cmb[1].Items.Add("Drawber Organ");
		this.cmb[1].Items.Add("Percussive Organ");
		this.cmb[1].Items.Add("Rock Organ");
		this.cmb[1].Items.Add("Church Organ");
		this.cmb[1].Items.Add("Reed Organ");
		this.cmb[1].Items.Add("Accordion");
		this.cmb[1].Items.Add("Harmonica");
		this.cmb[1].Items.Add("Tango Accordion");
		//ギター系
		this.cmb[1].Items.Add("Acostic Guitar(nylon)");
		this.cmb[1].Items.Add("Acostic Guitar(steel)");
		this.cmb[1].Items.Add("Electric Guitar(jazz)");
		this.cmb[1].Items.Add("Electric Guitar(clean)");
		this.cmb[1].Items.Add("Electric Guitar(muted)");
		this.cmb[1].Items.Add("Overdriven Guitar");
		this.cmb[1].Items.Add("Distortion Guitar");
		this.cmb[1].Items.Add("Guitar Harmonics");
		//ベース系
		this.cmb[1].Items.Add("Acosic Bass");
		this.cmb[1].Items.Add("Electric Bass(finger)");
		this.cmb[1].Items.Add("Electric Bass(pick)");
		this.cmb[1].Items.Add("Fretless Bass");
		this.cmb[1].Items.Add("Slap Bass 1");
		this.cmb[1].Items.Add("Slap Bass 2");
		this.cmb[1].Items.Add("Synth Bass 1");
		this.cmb[1].Items.Add("Synth Bass 2");
		//ストリングス系
		this.cmb[1].Items.Add("Violin");
		this.cmb[1].Items.Add("Viola");
		this.cmb[1].Items.Add("Cello");
		this.cmb[1].Items.Add("Contrabass");
		this.cmb[1].Items.Add("Tremoro Strings");
		this.cmb[1].Items.Add("Pizzicato Strings");
		this.cmb[1].Items.Add("Orchestral Harp");
		this.cmb[1].Items.Add("Timpani");
		//アンサンブル系
		this.cmb[1].Items.Add("String Ensamble 1");
		this.cmb[1].Items.Add("String Ensamble 2");
		this.cmb[1].Items.Add("Synth Strings 1");
		this.cmb[1].Items.Add("Synth Strings 2");
		this.cmb[1].Items.Add("Choir Aahs");
		this.cmb[1].Items.Add("Voice Oohs");
		this.cmb[1].Items.Add("Synth Voice");
		this.cmb[1].Items.Add("Orchestra Hit");
		//ブラス系
		this.cmb[1].Items.Add("Trumpet");
		this.cmb[1].Items.Add("Trombone");
		this.cmb[1].Items.Add("Tuba");
		this.cmb[1].Items.Add("Muted Trumpet");
		this.cmb[1].Items.Add("French Horn");
		this.cmb[1].Items.Add("Brass Section");
		this.cmb[1].Items.Add("Synth Brass 1");
		this.cmb[1].Items.Add("Synth Brass 2");
		//リード系
		this.cmb[1].Items.Add("Soprano Sax");
		this.cmb[1].Items.Add("Alto Sax");
		this.cmb[1].Items.Add("Tenor Sax");
		this.cmb[1].Items.Add("Baritone Sax");
		this.cmb[1].Items.Add("Oboe");
		this.cmb[1].Items.Add("English Horn");
		this.cmb[1].Items.Add("Bassoon");
		this.cmb[1].Items.Add("Clarinet");
		//パイプ系
		this.cmb[1].Items.Add("Piccolo");
		this.cmb[1].Items.Add("Flute");
		this.cmb[1].Items.Add("Recorder");
		this.cmb[1].Items.Add("Pan Flute");
		this.cmb[1].Items.Add("Bottle Blow");
		this.cmb[1].Items.Add("Shakuhachi");
		this.cmb[1].Items.Add("Whistle");
		this.cmb[1].Items.Add("Ocarina");
		//シンセ・リード系
		this.cmb[1].Items.Add("Lead 1(square)");
		this.cmb[1].Items.Add("Lead 2(sawtooth)");
		this.cmb[1].Items.Add("Lead 3(caliope)");
		this.cmb[1].Items.Add("Lead 4(chiff)");
		this.cmb[1].Items.Add("Lead 5(charang)");
		this.cmb[1].Items.Add("Lead 6(voice)");
		this.cmb[1].Items.Add("Lead 7(fifth)");
		this.cmb[1].Items.Add("Lead 8(bass+lead)");
		//シンセ・パッド系
		this.cmb[1].Items.Add("Pad 1(new age)");
		this.cmb[1].Items.Add("Pad 2(warm)");
		this.cmb[1].Items.Add("Pad 3(polysynth)");
		this.cmb[1].Items.Add("Pad 4(choir)");
		this.cmb[1].Items.Add("Pad 5(bowed)");
		this.cmb[1].Items.Add("Pad 6(metalic)");
		this.cmb[1].Items.Add("Pad 7(halo)");
		this.cmb[1].Items.Add("Pad 8(sweep)");
		//シンセ・エフェクト系
		this.cmb[1].Items.Add("FX(rain)");
		this.cmb[1].Items.Add("FX(soundtrack)");
		this.cmb[1].Items.Add("FX(crystal)");
		this.cmb[1].Items.Add("FX(atmosphere)");
		this.cmb[1].Items.Add("FX(brightness)");
		this.cmb[1].Items.Add("FX(goblins)");
		this.cmb[1].Items.Add("FX(echoes)");
		this.cmb[1].Items.Add("FX(sci-fi)");
		//エスニック系
		this.cmb[1].Items.Add("Sitar");
		this.cmb[1].Items.Add("Banjo");
		this.cmb[1].Items.Add("Shamisen");
		this.cmb[1].Items.Add("Koto");
		this.cmb[1].Items.Add("Kalimba");
		this.cmb[1].Items.Add("Bagpipe");
		this.cmb[1].Items.Add("Fiddle");
		this.cmb[1].Items.Add("Shanai");
		//パーカッシヴ系
		this.cmb[1].Items.Add("Tinkle Bell");
		this.cmb[1].Items.Add("Agogo");
		this.cmb[1].Items.Add("Steel Drums");
		this.cmb[1].Items.Add("Woodblock");
		this.cmb[1].Items.Add("Taiko Drum");
		this.cmb[1].Items.Add("Melodic Tom");
		this.cmb[1].Items.Add("Synth Drum");
		this.cmb[1].Items.Add("Reverse Cymbal");
		//効果音
		this.cmb[1].Items.Add("Guitar Fret Noise");
		this.cmb[1].Items.Add("Breath Noise");
		this.cmb[1].Items.Add("Seashore");
		this.cmb[1].Items.Add("Bird Tweet");
		this.cmb[1].Items.Add("Telephone Ring");
		this.cmb[1].Items.Add("Helicopter");
		this.cmb[1].Items.Add("Applause");
		this.cmb[1].Items.Add("Gun Shot");

		this.cmb[1].SelectedIndex = 0;
		this.cmb[1].SelectedIndexChanged+=new EventHandler(this.cmb_SelectedIndexChanged);

		Console.WriteLine("* Sound Set");

		this.cmb[2].SetBounds(276, 0, 65, 40, BoundsSpecified.All);
		this.cmb[2].Items.Add("1000ms");
		this.cmb[2].Items.Add(" 500ms");
		this.cmb[2].Items.Add(" 300ms");
		this.cmb[2].Items.Add(" 200ms");
		this.cmb[2].Items.Add(" 100ms");
		this.cmb[2].Items.Add("   70ms");
		this.cmb[2].Items.Add("   50ms");
		this.cmb[2].Items.Add("   40ms");
		this.cmb[2].Items.Add("   30ms");
		this.cmb[2].Items.Add("   25ms");
		this.cmb[2].Items.Add("   20ms");
		this.cmb[2].Items.Add("   15ms");
		this.cmb[2].Items.Add("   10ms");
		this.cmb[2].Items.Add("    5ms");
		this.cmb[2].Items.Add("    1ms");

		this.cmb[2].SelectedIndex = 12;
		this.cmb[2].SelectedIndexChanged+=new EventHandler(this.cmb_SelectedIndexChanged);

		this.cmb[3].SetBounds(420, 0, 60, 40, BoundsSpecified.All);
		this.cmb[3].Items.Add("重量");
		this.cmb[3].Items.Add("通常");
		this.cmb[3].Items.Add("色だけ");
		this.cmb[3].Items.Add("軽量化");

		if(fasts == 2){
			this.cmb[3].SelectedIndex = 3;
		}
		else
		{
			this.cmb[3].SelectedIndex = 2;
		}
		this.cmb[3].SelectedIndexChanged+=new EventHandler(this.cmb_SelectedIndexChanged);

		this.cmb[4].SetBounds(340, 0, 80, 40, BoundsSpecified.All);
		this.cmb[4].DropDownWidth = 200;
		string Stmp;
		if(musicNames[0]!="notdata")
		{
			for( int i = 0; i < musicNames.Length; i++)
			{
				Stmp = musicNames[i].Substring(musicNames[i].LastIndexOf("\\") + 1);
				if(midFilterInd==2)
				{
					this.cmb[4].Items.Add(Stmp.Remove(Stmp.LastIndexOf("-")));
				}
				else
				{
					this.cmb[4].Items.Add(Stmp.Remove(Stmp.LastIndexOf(".")));
				}
			}
		}
		else
		{
			this.cmb[4].Items.Add("データが存在しません");
		}
		this.cmb[4].SelectedIndex = 0;
		this.cmb[4].SelectedIndexChanged+=new EventHandler(this.cmb_SelectedIndexChanged);

		Console.WriteLine("* ComboBox Set");

		this.chkb = new CheckBox[3];
		for(int i = 0; i < this.chkb.Length; i++)
		{
			this.chkb[i] = new CheckBox();
			this.Controls.Add(this.chkb[i]);
		}
		this.chkb[0].SetBounds(480, 0, 80, 18, BoundsSpecified.All);
		this.chkb[0].Text = "Key";
		this.chkb[0].Checked = true;

		this.chkb[1].SetBounds(480, 20, 80, 18, BoundsSpecified.All);
		this.chkb[1].Text = "Loop";
		this.chkb[1].Checked = false;

		this.chkb[2].SetBounds(340, 20, 80, 18, BoundsSpecified.All);
		this.chkb[2].Text = "AN/N";
		this.chkb[2].Checked = true;

		Console.WriteLine("* CheckBox Set");

		this.btn = new Button[1];
		for(int i = 0; i < this.btn.Length; i++)
		{
			this.btn[i] = new Button();
			this.btn[i].Name = i.ToString();
			this.Controls.Add(this.btn[i]);
			this.btn[i].MouseDown += new MouseEventHandler(this.clickButtons);
		}
		this.btn[0].SetBounds(0, 20, 20, 20, BoundsSpecified.All);
		this.btn[0].Text = "?";
		this.btn[0].BackColor = Color.LightGray;
		Console.WriteLine("* Button Set");

		rain = CreatePalette();
		Console.WriteLine("* Color Set");

		/*--------------------------------------------------*/
		/* 鍵盤(ボタン)の作成 */
		/*--------------------------------------------------*/
		this.pnl = new Panel();
		this.pnl.SetBounds(0, 40, 840, 200, BoundsSpecified.All);
		this.pnl.AutoScroll = true;
		this.pnl.Anchor = (AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom);
		this.Controls.Add(this.pnl);

		this.key = new Button[128];
		this.col = new Color[128];
		this.nppc = new Char[128];

		int count = 0;
	
		for( int i = 0; i < this.key.Length; i++)
		{
			this.key[i] = new Button();
			this.key[i].Name = i.ToString();
			this.key[i].Text = i.ToString();
			this.key[i].TextAlign = ContentAlignment.BottomCenter;
			this.pnl.Controls.Add(this.key[i]);
	
			this.key[i].MouseDown += new MouseEventHandler(this.key_MouseDown);
			this.key[i].MouseUp += new MouseEventHandler(this.key_MouseUp);
			this.key[i].MouseEnter += new EventHandler(this.key_MouseEnter);
			this.key[i].MouseLeave += new EventHandler(this.key_MouseLeave);

			if(count % 2 == 0)
			{
				this.key[i].SetBounds(20 * count, 0, 40, 160, BoundsSpecified.All);
				this.key[i].ForeColor = Color.Black;
				this.key[i].BackColor = Color.White; //白
				this.col[i] = Color.White;
			}
			else
			{
				this.key[i].SetBounds(20 * count + 5, 0, 30, 80, BoundsSpecified.All);
				this.key[i].ForeColor = Color.White;
				this.key[i].BackColor = Color.Black; //黒
				this.col[i] = Color.Black;
				this.key[i].BringToFront();
			}

			count += (i % 12 == 4 || i % 12 == 11) ? 2 : 1;
		}
		Console.WriteLine("* Keyboard Set");
	}

	/*--------------------------------------------------*/
	/* Buttonを押した際の実行
	/*--------------------------------------------------*/
	private void clickButtons(object sender, MouseEventArgs e)
	{
		int key = int.Parse(((Button)sender).Name);

		if(key==0){
			if(pianoHelp==null)
			{
				Console.WriteLine("-FormPianoHelp Start");
				pianoHelp = new FormPianoHelp();
				pianoHelp.Show();
			}
			if(pianoHelp.IsDisposed==true)
			{
				Console.WriteLine("-FormPianoHelp ReStart");
				pianoHelp = new FormPianoHelp();
				pianoHelp.Show();
			}
			else
			{
				//Console.WriteLine("-FormPianoHelp ReDisplay");
				pianoHelp.WindowState = FormWindowState.Normal;
				pianoHelp.Activate();
			}
		}
		else
		{
			Console.WriteLine("! 不明なbutton入力");
		}
	}
	/*--------------------------------------------------*/
	/* MIDIデバイスのOpenとClose
	/*--------------------------------------------------*/
	private void FormPiano_Load(object sender, EventArgs e)
	{
		SetWindowPos(GetConsoleWindow(), IntPtr.Zero, 0, 256, (int)(Screen.PrimaryScreen.Bounds.Width/3), Screen.PrimaryScreen.Bounds.Height-256, 0x0010);

		this.SetDesktopBounds(-5, 0, Screen.PrimaryScreen.Bounds.Width+10, 256);
		Console.WriteLine("* Window Set");

		if(sleepPC){
			ExecutionState.DisableSuspend();
			Console.WriteLine("* Display Sleep off");
		}

		Console.WriteLine("! PianoPls Load");
		if(binSecure)
		{
			Console.WriteLine("# Secure mode on!");
		}
		if(binNoiseCut)
		{
			Console.WriteLine("# Secure mode on!");
			Console.WriteLine("# Sounds with a length of "+binNoiseSize+" or less are truncated");
		}
		if(fpsFlag)
		{
			Console.WriteLine("# 'fps' display");
		}
		if(fasts == 1)
		{
			Console.WriteLine("# 'fasts' is true!");
		}
		else if(fasts == 2)
		{
			Console.WriteLine("# 'super fasts' is true!");
		}
	
		if( midiOutOpen(ref this.hMidi, MIDI_MAPPER, 0, 0, 0) != MMSYSERR_NOERROR)
		{
			Console.WriteLine("! midiOutOpen error");
			MessageBox.Show("midiOutOpen error");
			Application.Exit();
		}
		if(onsettingflag)
		{
			if(pianoVersion!=SettingFiles.settingData[0])
			{
				DialogResult result= MessageBox.Show("PianoPlsの設定ファイルとのバージョンが違います\nデータを削除し再起動しますか？","PianoPls", MessageBoxButtons.YesNo);
				if (result == DialogResult.Yes)
				{
					Console.WriteLine("@ Reset \"Setting.dat\"");
					SettingFiles.Update();
					Console.WriteLine("@ Close \"Setting.dat\"");
					ExecutionState.EnableSuspend();
					Console.WriteLine("* Display Sleep reset");
					Console.WriteLine("! PianoPls Restart");
					Application.Restart();
				}
				else
				{
					ExecutionState.EnableSuspend();
					Console.WriteLine("* Display Sleep reset");
					Console.WriteLine("! PianoPls Closed");
					Application.Exit();
				}
			}
			else if(newbinreadflag)
			{
				Console.WriteLine("@ Update \"Setting.dat\"");
				SettingFiles.Update();
				Console.WriteLine("@ Close \"Setting.dat\"");
			}
		}
		this.pnl.AutoScrollPosition = new Point(900, 0);
	}
	private void FormPiano_Closed(object sender, EventArgs e)
	{
		ExecutionState.EnableSuspend();
		Console.WriteLine("* Display Sleep reset");
		Console.WriteLine("! PianoPls Closed");
	
		midiOutClose(this.hMidi);
		System.Environment.Exit(0);
	}
	/*--------------------------------------------------*/
	/* Note On/Off */
	/*--------------------------------------------------*/
	private void NoteOn(byte key)
	{
		if((uint)key <= (uint)127)
		{
			byte ch = (byte)((this.cmb[0].SelectedIndex == 0) ? 0 : 9);
			byte velocity = 0x7f;

			uint msg;
			msg = (uint)((velocity << 16) + ( key << 8) + 0x90 + ch);
			midiOutShortMsg(this.hMidi, msg);
		}
	}
	private void NoteOff(byte key)
	{
		if((uint)key <= (uint)127)
		{
			byte ch = (byte)((this.cmb[0].SelectedIndex == 0) ? 0 : 9);
			byte velocity = 0x7f;

			uint msg;
			msg = (uint)((velocity << 16) + ( key << 8) + 0x80 + ch);
			midiOutShortMsg(this.hMidi, msg);
		}
	}
	/*--------------------------------------------------*/
	/* プログラム変更 */
	/*--------------------------------------------------*/
	private void ProgramChange(byte prg)
	{
		byte ch = (byte)0;

		uint msg;
		msg = (uint)((prg << 8) + 0xc0 + ch);
		midiOutShortMsg(this.hMidi, msg);
	}
	private void DisplayChange(int ch,int mo,int kcol = -1)
	{
		if(ch < 128)
		{
			if(mode==0 || mode==1)
			{
				if(mo==0)
				{
					Console.WriteLine("key_LockDown "+ch.ToString());
					if(mode==1){
						this.key[ch].Focus();
					}
				}
				else if(mo==1)
				{
					Console.WriteLine("key_LockUp "+ch.ToString().ToString());
				}
				else if(mo==2)
				{
					Console.WriteLine("key_SendDown "+ch.ToString());
					if(mode==1){
						this.key[ch].Focus();
					}
				}
				else
				{
					Console.WriteLine("key_SendUp "+ch.ToString());
				}
			}
			if(mo==0 || mo== 2)
			{
				this.NoteOn((byte)ch);
				if(mode==0 || mode==2)
				{
					if(colorful)
					{
						this.key[ch].BackColor = rain[ch*2];
					}
					else
					{
						if (kcol == -1)
						{
							this.key[ch].BackColor = Color.Red;
						}
						else
						{
							kcol = kcol * 36;
							if (kcol > 256)
							{
								kcol = kcol / 12;
							}
							this.key[ch].BackColor = rain[kcol];
						}
					}
				}
	
			}
			else
			{
				this.NoteOff((byte)ch);
				if(mode==0 || mode==2)
				{
					this.key[ch].BackColor = this.col[ch];
				}
			}
		}
	}
	public void MusicStart()
	{
		if(musicNames[0]=="notdata"){
			Console.WriteLine("! Warning:song does not exist");
			return;
		}
		if(nowMusicInd!=this.cmb[4].SelectedIndex)
		{
			if(midFilterInd!=3)
			{
				if(!File.Exists(binreadPath+".exe")){
					Console.WriteLine("! Warning:\"binread_pls.exe\" does not exist");
					return;
				}
				if(!File.Exists(musicNames[this.cmb[4].SelectedIndex])){
					Console.WriteLine("! Warning:file does not exist");
					return;
				}
				Console.WriteLine("@ Start song conversion");
				Process proc = new Process();
				proc.StartInfo.FileName =binreadPath+".exe";
				proc.StartInfo.Arguments = musicNames[this.cmb[4].SelectedIndex] +" "+ binSecure +" "+ binNoiseCut +" "+ binNoiseSize;

				proc.Start();
				proc.WaitForExit();
				Console.WriteLine("@ End   song conversion");
				if(proc.ExitCode!=0)
				{
					Console.WriteLine("! Warning:binread_pls.exe Wrong ExitCode");
					return;
				}
			}
			else
			{
				if(!File.Exists(musicNames[this.cmb[4].SelectedIndex])){
					Console.WriteLine("! Warning:file does not exist");
					return;
				}
				musicPath = musicNames[this.cmb[4].SelectedIndex];
				Console.WriteLine("@ song reading");
			}
		}
		nowMusicInd = this.cmb[4].SelectedIndex;

		chcou++;
		listI = new List<int>(pitchs);
		listI.Add(60);
		pitchs = listI.ToArray();
		listS = new List<string>(tmpdata);
		listS.Add("");
		tmpdata = listS.ToArray();
		listB = new List<byte>(prog);
		listB.Add((byte)0);
		prog = listB.ToArray();
		listI = new List<int>(syncs);
		listI.Add(1);
		syncs = listI.ToArray();
		playback=true;
		playMusic(chcou);
	}

	public static Color[] CreatePalette()
	{
		Color[] _ans = new Color[256];
		// 青→緑
		for (int i = 0; i < 64; i++)
		{
			int _green = i * 4;
			_ans[i] = Color.FromArgb(255, 0, _green, 255 - _green);
		}
		// 緑→黄
		for (int i = 0; i < 64; i++)
		{
			int _red = i * 4;
			_ans[i+64] = Color.FromArgb(255, _red, 255, 0);
		}
		// 黄→赤
		for (int i = 0; i < 128; i++)
		{
			int _green = 255 - i * 2;
			_ans[i + 128] = Color.FromArgb(255, 255, _green, 0);
		}

		return _ans;
	}
	/*--------------------------------------------------*/
	/* コンボボックスの処理 */
	/*--------------------------------------------------*/
	private void cmb_SelectedIndexChanged(object sender, System.EventArgs e)
	{
		byte prg1 = (byte)this.cmb[1].SelectedIndex;
		int prg2 = this.cmb[2].SelectedIndex;
		mode = this.cmb[3].SelectedIndex;
		if(mode<=1){
			Console.WriteLine("cmb_SelectedIndexChanged_program " + prg1.ToString());
			Console.WriteLine("cmb_SelectedIndexChanged_wait " + prg2.ToString());
			Console.WriteLine("cmb_SelectedIndexChanged_mode " + mode.ToString());
		}

		this.ProgramChange(prg1);

		switch(prg2)
		{
			case 0:
				waits=1000;
				break;
			case 1:
				waits=500;
				break;
			case 2:
				waits=300;
				break;
			case 3:
				waits=200;
				break;
			case 4:
				waits=100;
				break;
			case 5:
				waits=70;
				break;
			case 6:
				waits=50;
				break;
			case 7:
				waits=40;
				break;
			case 8:
				waits=30;
				break;
			case 9:
				waits=25;
				break;
			case 10:
				waits=20;
				break;
			case 11:
				waits=15;
				break;
			case 12:
				waits=10;
				break;
			case 13:
				waits=5;
				break;
			case 14:
				waits=1;
				break;
			default:
				waits=10;
				break;
		}
		timer1.Interval = waits;
		if(mode==1 || mode==3)
		{
			for (int i=0;i<128;i++)
			{
				this.key[i].BackColor = this.col[i];
			}
		}
	}
	/*--------------------------------------------------*/
	/* マウス処理 */
	/*--------------------------------------------------*/
	private void key_MouseDown(object sender, MouseEventArgs e)
	{
		((Button)sender).Capture = false;

		byte key = (byte) int.Parse(((Button)sender).Name);
		if(mode<=1){
			Console.WriteLine("key_MouseDown " + key.ToString());
		}
	
		this.NoteOn(key);
		if(mode==0 || mode==2)
		{
			if(colorful)
			{
				this.key[key].BackColor = rain[key*2];
			}
			else
			{
				this.key[key].BackColor = Color.Red;
			}
		}

	}
	private void key_MouseUp(object sender, MouseEventArgs e)
	{
		byte key = (byte)int.Parse(((Button)sender).Name);
		if(mode<=1){
			Console.WriteLine("key_MouseUp " + key.ToString());
		}

		if(!KDown[23])
		{
			this.NoteOff(key);
			if(mode==0 || mode==2)
			{
				this.key[key].BackColor = this.col[key];
			}
		}
	}
	private void key_MouseEnter(object sender, EventArgs e)
	{
		if ((Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left)
		{
			this.key_MouseDown(sender, new MouseEventArgs( MouseButtons.Left, 1, 0, 0, 0));
		}
	}
	private void key_MouseLeave(object sender, EventArgs e)
	{
		if ((Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left)
		{
			this.key_MouseUp(sender, new MouseEventArgs( MouseButtons.Left, 1, 0, 0, 0));
		}
	}
	/*--------------------------------------------------*/	
	private void timer1_Tick(object sender, EventArgs e)
	{
		if (IsKeyLocked(System.Windows.Forms.Keys.A))
		{
			if (!KDown[0])
			{
				DisplayChange(pitch,0);
				KDown[0]=true;
				if(callback)
				{
					data.Append("A");
				}
			}
		}
		else if (KDown[0])
		{
			KDown[0]=false;
			if(!KDown[23])
			{
				DisplayChange(pitch,1);
				if(callback)
				{
					data.Append("a");
				}
			}
		}
		if (IsKeyLocked(System.Windows.Forms.Keys.W))
		{
			if (!KDown[1])
			{
				DisplayChange(pitch+1,0);
				KDown[1]=true;
				if(callback)
				{
					data.Append("W");
				}
			}
		}
		else if (KDown[1])
		{
			KDown[1]=false;
			if(!KDown[23])
			{
				DisplayChange(pitch+1,1);
				if(callback)
				{
					data.Append("w");
				}
			}
		}
		if (IsKeyLocked(System.Windows.Forms.Keys.S))
		{
			if (!KDown[2])
			{
				DisplayChange(pitch+2,0);
				KDown[2]=true;
				if(callback)
				{
					data.Append("S");
				}
			}
		}
		else if (KDown[2])
		{
			KDown[2]=false;
			if(!KDown[23])
			{
				DisplayChange(pitch+2,1);
				if(callback)
				{
					data.Append("s");
				}
			}
		}
		if (IsKeyLocked(System.Windows.Forms.Keys.E))
		{
			if (!KDown[3])
			{
				DisplayChange(pitch+3,0);
				KDown[3]=true;
				if(callback)
				{
					data.Append("E");
				}
			}
		}
		else if (KDown[3])
		{
			KDown[3]=false;
			if(!KDown[23])
			{
				DisplayChange(pitch+3,1);
				if(callback)
				{
					data.Append("e");
				}
			}
		}
		if (IsKeyLocked(System.Windows.Forms.Keys.D))
		{
			if (!KDown[4])
			{
				DisplayChange(pitch+4,0);
				KDown[4]=true;
				if(callback)
				{
					data.Append("D");
				}
			}
		}
		else if (KDown[4])
		{
			KDown[4]=false;
			if(!KDown[23])
			{
				DisplayChange(pitch+4,1);
				if(callback)
				{
					data.Append("d");
				}
			}
		}
		if (IsKeyLocked(System.Windows.Forms.Keys.F))
		{
			if (!KDown[5])
			{
				DisplayChange(pitch+5,0);
				KDown[5]=true;
				if(callback)
				{
					data.Append("F");
				}
			}
		}
		else if (KDown[5])
		{
			KDown[5]=false;
			if(!KDown[23])
			{
				DisplayChange(pitch+5,1);
				if(callback)
				{
					data.Append("f");
					}
			}
		}
		if (IsKeyLocked(System.Windows.Forms.Keys.T))
		{
			if (!KDown[6])
			{
				DisplayChange(pitch+6,0);
				KDown[6]=true;
				if(callback)
				{
					data.Append("T");
				}
			}
		}
		else if (KDown[6])
		{
			KDown[6]=false;
			if(!KDown[23])
			{
				DisplayChange(pitch+6,1);
				if(callback)
				{
					data.Append("t");
				}
			}
		}
		if (IsKeyLocked(System.Windows.Forms.Keys.G))
		{
			if (!KDown[7])
			{
				DisplayChange(pitch+7,0);
				KDown[7]=true;
				if(callback)
				{
					data.Append("G");
				}
			}
		}
		else if (KDown[7])
		{
			KDown[7]=false;
			if(!KDown[23])
			{
				DisplayChange(pitch+7,1);
				if(callback)
				{
					data.Append("g");
				}
			}
		}
		if (IsKeyLocked(System.Windows.Forms.Keys.Y))
		{
			if (!KDown[8])
			{
				DisplayChange(pitch+8,0);
				KDown[8]=true;
				if(callback)
				{
					data.Append("Y");
				}
			}
		}
		else if (KDown[8])
		{
			KDown[8]=false;
			if(!KDown[23])
			{
				DisplayChange(pitch+8,1);
				if(callback)
				{
					data.Append("y");
				}
			}
		}
		if (IsKeyLocked(System.Windows.Forms.Keys.H))
		{
			if (!KDown[9])
			{
				DisplayChange(pitch+9,0);
				KDown[9]=true;
				if(callback)
				{
					data.Append("H");
				}
			}
		}
		else if (KDown[9])
		{
			KDown[9]=false;
			if(!KDown[23])
			{
				DisplayChange(pitch+9,1);
				if(callback)
				{
					data.Append("h");
				}
			}
		}
		if (IsKeyLocked(System.Windows.Forms.Keys.U))
		{
			if (!KDown[10])
			{
				DisplayChange(pitch+10,0);
				KDown[10]=true;
				if(callback)
				{
					data.Append("U");
				}
			}
		}
		else if (KDown[10])
		{
			KDown[10]=false;
			if(!KDown[23])
			{
				DisplayChange(pitch+10,1);
				if(callback)
				{
					data.Append("u");
				}
			}
		}
		if (IsKeyLocked(System.Windows.Forms.Keys.J))
		{
			if (!KDown[11])
			{
				DisplayChange(pitch+11,0);
				KDown[11]=true;
				if(callback)
				{
					data.Append("J");
				}
			}
		}
		else if (KDown[11])
		{
			KDown[11]=false;
			if(!KDown[23])
			{
				DisplayChange(pitch+11,1);
				if(callback)
				{
					data.Append("j");
				}
			}
		}
		if (IsKeyLocked(System.Windows.Forms.Keys.K))
		{
			if (!KDown[12])
			{
				DisplayChange(pitch+12,0);
				KDown[12]=true;
				if(callback)
				{
					data.Append("K");
				}
			}
		}
		else if (KDown[12])
		{
			KDown[12]=false;
			if(!KDown[23])
			{
				DisplayChange(pitch+12,1);
				if(callback)
				{
					data.Append("k");
				}
			}
		}
		if (IsKeyLocked(System.Windows.Forms.Keys.L))
		{
			if (!KDown[13])
			{
				DisplayChange(pitch+14,0);
				KDown[13]=true;
				if(callback)
				{
					data.Append("K");
				}
			}
		}
		else if (KDown[13])
		{
			KDown[13]=false;
			if(!KDown[23])
			{
				DisplayChange(pitch+14,1);
				if(callback)
				{
					data.Append("k");
				}
			}
		}

		if (IsKeyLocked(System.Windows.Forms.Keys.Left))
		{
			if (!KDown[14] && pitch>0)
			{
				if(mode==0 || mode==1 || mode==3)
				{
					Console.WriteLine("key_LockDown_LeftArrow "+(pitch/12).ToString());
				}
				pitch -= 12;
				KDown[14]=true;
				if(callTmp)
				{
					data.Append("Z");
				}
			}
		}
		else if (KDown[14])
		{
			if(mode==0 || mode==1 || mode==3)
			{
				Console.WriteLine("key_LockUp_LeftArrow "+(pitch/12).ToString());
			}
			KDown[14]=false;
		}
		if (IsKeyLocked(System.Windows.Forms.Keys.Right))
		{
			if (!KDown[15] && pitch<120)
			{
				if(mode==0 || mode==1 || mode==3)
				{
					Console.WriteLine("key_LockDown_RightArrow "+(pitch/12).ToString());
				}
				pitch += 12;
				KDown[15]=true;
				if(callTmp)
				{
					data.Append("X");
				}
			}
		}
		else if (KDown[15])
		{
			if(mode==0 || mode==1 || mode==3)
			{
				Console.WriteLine("key_LockUp_RightArrow "+(pitch/12).ToString());
			}
			KDown[15]=false;
		}

		if (IsKeyLocked(System.Windows.Forms.Keys.Up))
		{
			if (!KDown[16] && this.cmb[1].SelectedIndex>0)
			{
				if(mode==0 || mode==1 || mode==3)
				{
					Console.WriteLine("key_LockDown_UpArrow "+this.cmb[1].SelectedIndex.ToString());
				}
				this.cmb[1].SelectedIndex --;
				KDown[16]=true;
				if(callTmp)
				{
					data.Append("C");
				}
			}
		}
		else if (KDown[16])
		{
			if(mode==0 || mode==1 || mode==3)
			{
				Console.WriteLine("key_LockUp_UpArrow "+this.cmb[1].SelectedIndex.ToString());
			}
			KDown[16]=false;
		}
		if (IsKeyLocked(System.Windows.Forms.Keys.Down))
		{
			if (!KDown[17] && this.cmb[1].SelectedIndex<127)
			{
				if(mode==0 || mode==1 || mode==3)
				{
					Console.WriteLine("key_LockDown_DownArrow "+this.cmb[1].SelectedIndex.ToString());
				}
				this.cmb[1].SelectedIndex ++;
				KDown[17]=true;
				if(callTmp)
				{
					data.Append("V");
				}
			}
		}
		else if (KDown[17])
		{
			if(mode==0 || mode==1 || mode==3)
			{
				Console.WriteLine("key_LockUp_DownArrow "+this.cmb[1].SelectedIndex.ToString());
			}
			KDown[17]=false;
		}

		if (IsKeyLocked(System.Windows.Forms.Keys.Q))
		{
			if (!KDown[18])
			{
				if(mode==0 || mode==1 || mode==3)
				{
					Console.WriteLine("key_LockDown_Channel "+this.cmb[0].SelectedIndex.ToString());
				}
				if(this.cmb[0].SelectedIndex==0)
				{
					this.cmb[0].SelectedIndex=1;
				}
				else
				{
					this.cmb[0].SelectedIndex=0;
				}
				KDown[18]=true;
				if(callTmp)
				{
					data.Append("Q");
				}
			}
		}
		else if (KDown[18])
		{
			if(mode==0 || mode==1 || mode==3)
			{
				Console.WriteLine("key_LockUp_Channel "+this.cmb[0].SelectedIndex.ToString());
			}
			KDown[18]=false;
		}

		if (IsKeyLocked(System.Windows.Forms.Keys.D0))
		{
			if (!KDown[19])
			{
				Console.WriteLine("key_LockDown_PlayStart " + this.cmb[4].SelectedIndex);
				this.cmb[0].SelectedIndex=0;
				this.cmb[1].SelectedIndex=0;
				MusicStart();
				KDown[19]=true;
			}
		}
		else if (KDown[19])
		{
			KDown[19]=false;
		}

		if(fasts != 2){
			if (IsKeyLocked(System.Windows.Forms.Keys.D9))
			{
				if (!KDown[20])
				{
					if(callback && callTmp)
					{
						Console.WriteLine("key_LockDown_RecordTmpStop");
						callback=false;
					}
					else if(callTmp)
					{
						Console.WriteLine( "key_LockUp_RecordTmpStart ");
						callback=true;
					}
					KDown[20]=true;
				}
			}
			else if (KDown[20])
			{
				KDown[20]=false;
			}
			if (IsKeyLocked(System.Windows.Forms.Keys.D8))
			{
				if (!KDown[21])
				{
					if(callback && callTmp)
					{
						Console.WriteLine("key_LockUp_RecordEnd");
						callback=false;
						if(overflows)
						{
							File.AppendAllText(musicPath,data.ToString());
						}
						else
						{
							File.WriteAllText(musicPath,data.ToString());
						}
						callTmp=false;
					}
					else if(!callTmp)
					{
						data=new StringBuilder("");
						pitch=60;
						this.cmb[0].SelectedIndex=0;
						this.cmb[1].SelectedIndex=0;
						this.cmb[2].SelectedIndex=10;
						colorful=false;
						Console.WriteLine("key_LockDown_RecordStart");
						callback=true;
						callTmp=true;
						overflows=false;
					}
					KDown[21]=true;
				}
			}
			else if (KDown[21])
			{
				KDown[21]=false;
			}
			if (IsKeyLocked(System.Windows.Forms.Keys.D7))
			{
				if (!KDown[22])
				{
					if(callback && callTmp)
					{
						Console.WriteLine("key_LockDown_RecordNoSave");
						callback=false;
						callTmp=false;
						data=new StringBuilder("");
					}
					KDown[22]=true;
				}
			}
			else if (KDown[22])
			{
				KDown[22]=false;
			}
		}

		if (IsKeyLocked(System.Windows.Forms.Keys.ShiftKey))
		{
			if (!KDown[23])
			{
				if(mode==0 || mode==1)
				{
					Console.WriteLine("key_LockDown_ShiftKey");
				}
				KDown[23]=true;
				if(callback)
				{
					data.Append("B");
				}
			}
		}
		else if (KDown[23])
		{
			if(mode==0 || mode==1)
			{
				Console.WriteLine("key_LockUp_ShiftKey");
			}
			KDown[23]=false;
			for (int i=0;i<128;i++)
			{
				this.NoteOff((byte)i);
				if(mode==0 || mode==2)
				{
					this.key[i].BackColor = this.col[i];
				}
			}
			if(callback)
			{
				data.Append("b");
			}
		}

		if (IsKeyLocked(System.Windows.Forms.Keys.N))
		{
			if (!KDown[24] && this.cmb[2].SelectedIndex>0)
			{
				if(mode==0 || mode==1)
				{
					Console.WriteLine("key_LockDown_WaitUp "+this.cmb[2].SelectedIndex.ToString());
				}
				this.cmb[2].SelectedIndex --;
				KDown[24]=true;
				if(callTmp)
				{
					data.Append("M");
				}
			}
		}
		else if (KDown[24])
		{
			if(mode==0 || mode==1)
			{
				Console.WriteLine("key_LockUp_WaitUp "+this.cmb[2].SelectedIndex.ToString());
			}
			KDown[24]=false;
		}
		if (IsKeyLocked(System.Windows.Forms.Keys.M))
		{
			if (!KDown[25] && this.cmb[2].SelectedIndex<14)
			{
				if(mode==0 || mode==1)
				{
					Console.WriteLine("key_LockDown_WaitDown "+this.cmb[2].SelectedIndex.ToString());
				}
				this.cmb[2].SelectedIndex ++;
				KDown[25]=true;
				if(callTmp)
				{
					data.Append("m");
				}
			}
		}
		else if (KDown[25])
		{
			if(mode==0 || mode==1)
			{
				Console.WriteLine("key_LockUp_WaitDown "+this.cmb[2].SelectedIndex.ToString());
			}
			KDown[25]=false;
		}

		if (IsKeyLocked(System.Windows.Forms.Keys.P))
		{
			if (!KDown[26])
			{
				if(mode==0 || mode==1)
				{
					Console.WriteLine("key_LockDown_Color "+colorful.ToString());
				}
				if(colorful)
				{
					colorful=false;
				}
				else
				{
					colorful=true;
				}
				KDown[26]=true;
				if(callback)
				{
					data.Append("P");
				}
			}
		}
		else if (KDown[26])
		{
			if(mode==0 || mode==1)
			{
				Console.WriteLine("key_LockUp_Color "+colorful.ToString());
			}
			KDown[26]=false;
		}
		if(fasts == 0)
		{
			if (IsKeyLocked(System.Windows.Forms.Keys.B))
			{
				if (!KDown[27])
				{
					Console.WriteLine("key_LockDown_Synce "+synceFlag.ToString());
					if(synceFlag)
					{
						synceFlag=false;
					}
					else
					{
						synceFlag=true;
					}
					KDown[27]=true;
				}
			}
			else if (KDown[27])
			{
				KDown[27]=false;
			}

			if(synceFlag)
			{
				syncsOK = true;
				syn = Array.IndexOf(syncs,0);
				synWC++;
				if(syn == -1)
				{
					if(synWC > 3)
					{
						Console.WriteLine("! Warning:Sync is unstable |" + synWC);
					}
					synWC = 0;
					syncsOK = false;
				}
				if(fpsFlag){
					this.lbl[3].Text = avgFrame +"/"+ (1000/waits) +"fps\nsyn: " + synWC;
				}
			}
		}

		if(fpsFlag){
			if(skipFrame){
				throughF += 2;
			}
			else
			{
				throughF++;
			}
			if(fsw.Elapsed.ToString().CompareTo("00:00:01") != -1) {
				avgFrame = throughF;
				throughF = 0;
				fsw.Reset();
				fsw.Start();
				if(avgFrame<=0)
				{
					Console.WriteLine("! Warning:fps is unstable");
				}
				if(!synceFlag)
				{
					this.lbl[3].Text = avgFrame +"/"+ (1000/waits) +"fps\nsyn: " + synWC;
				}
			}
		}

		if(fasts != 2){
			if(callback)
			{
				data.Append("N");
				if(data.Length>=90000000)
				{
					Console.WriteLine("! 既定の最大文字数ですデータを上書きします");
					if(overflows)
					{
						File.AppendAllText(musicPath,data.ToString());
					}
					else
					{
						File.WriteAllText(musicPath,data.ToString());
						overflows=true;
					}
					data=new StringBuilder("");
				}
			}
		}
	}
	private async void playMusic(int chcous)
	{
		if (File.Exists(musicPath))
		{
			using (StreamReader reader = new StreamReader(musicPath))
			{
				tmpdata[chcous] = reader.ReadToEnd();
				reader.Close();
			}
			string tmp;
			StringBuilder Ntmp = new StringBuilder("");
			System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
			int throughN = 0;
			int secRemain = 0;
			int skipflag = 1;
			double averageSpeed = 1000/waits;

			pitchs[chcous] = 60;
			int tdl = tmpdata[chcous].Length;
			for(int i=0;i<tdl;i++)
			{
				throughN++;
				tmp = tmpdata[chcous].Substring(i, 1);
				if(!this.chkb[2].Checked)
				{
					if(tmp!="N" && tmp!="X" && tmp!="Z" && Char.IsUpper(tmp.ToCharArray()[0]))
					{
						Ntmp.Append(tmp);
					}
				}
				ProgramChange(prog[chcous]);
				syncs[chcous] = 0;
				switch(tmp)
				{
					case "A":
						DisplayChange(pitchs[chcous],2,chcous);
						nppc[pitchs[chcous]] = 'A';
						if(callback)
						{
							data.Append("A");
						}
						break;
					case "a":
						DisplayChange(pitchs[chcous],3);
						nppc[pitchs[chcous]] = 'a';
						if(!KDown[23])
						{
							if(callback)
							{
								data.Append("a");
							}
						}
						break;
					case "W":
						DisplayChange(pitchs[chcous]+1,2,chcous);
						nppc[pitchs[chcous]+1] = 'W';
						if(callback)
						{
							data.Append("W");
						}
						break;
					case "w":
						DisplayChange(pitchs[chcous]+1,3);
						nppc[pitchs[chcous]+1] = 'w';
						if(!KDown[23])
						{
							if(callback)
							{
								data.Append("w");
							}
						}
						break;
					case "S":
						DisplayChange(pitchs[chcous]+2,2,chcous);
						nppc[pitchs[chcous]+2] = 'S';
						if(callback)
						{
							data.Append("S");
						}
						break;
					case "s":
						DisplayChange(pitchs[chcous]+2,3);
						nppc[pitchs[chcous]+2] = 's';
						if(!KDown[23])
						{
							if(callback)
							{
								data.Append("s");
							}
						}
						break;
					case "E":
						DisplayChange(pitchs[chcous]+3,2,chcous);
						nppc[pitchs[chcous]+3] = 'E';
						if(callback)
						{
							data.Append("E");
						}
						break;
					case "e":
						DisplayChange(pitchs[chcous]+3,3);
						nppc[pitchs[chcous]+3] = 'e';
						if(!KDown[23])
						{
							if(callback)
							{
								data.Append("e");
							}
						}
						break;
					case "D":
						DisplayChange(pitchs[chcous]+4,2,chcous);
						nppc[pitchs[chcous]+4] = 'D';
						if(callback)
						{
							data.Append("D");
						}
						break;
					case "d":
						DisplayChange(pitchs[chcous]+4,3);
						nppc[pitchs[chcous]+4] = 'd';
						if(!KDown[23])
						{
							if(callback)
							{
								data.Append("d");
							}
						}
						break;
					case "F":
						DisplayChange(pitchs[chcous]+5,2,chcous);
						nppc[pitchs[chcous]+5] = 'F';
						if(callback)
						{
							data.Append("F");
						}
						break;
					case "f":
						DisplayChange(pitchs[chcous]+5,3);
						nppc[pitchs[chcous]+5] = 'f';
						if(!KDown[23])
						{
							if(callback)
							{
								data.Append("f");
							}
						}
						break;
					case "T":
						DisplayChange(pitchs[chcous]+6,2,chcous);
						nppc[pitchs[chcous]+6] = 'T';
						if(callback)
						{
							data.Append("T");
						}
						break;
					case "t":
						DisplayChange(pitchs[chcous]+6,3);
						nppc[pitchs[chcous]+6] = 't';
						if(!KDown[23])
						{
							if(callback)
							{
								data.Append("t");
							}
						}
						break;
					case "G":
						DisplayChange(pitchs[chcous]+7,2,chcous);
						nppc[pitchs[chcous]+7] = 'G';
						if(callback)
						{
							data.Append("G");
						}
						break;
					case "g":
						DisplayChange(pitchs[chcous]+7,3);
						nppc[pitchs[chcous]+7] = 'g';
						if(!KDown[23])
						{
							if(callback)
							{
								data.Append("g");
							}
						}
						break;
					case "Y":
						DisplayChange(pitchs[chcous]+8,2,chcous);
						nppc[pitchs[chcous]+8] = 'Y';
						if(callback)
						{
							data.Append("Y");
						}
						break;
					case "y":
						DisplayChange(pitchs[chcous]+8,3);
						nppc[pitchs[chcous]+8] = 'y';
						if(!KDown[23])
						{
							if(callback)
							{
								data.Append("y");
							}
						}
						break;
					case "H":
						DisplayChange(pitchs[chcous]+9,2,chcous);
						nppc[pitchs[chcous]+9] = 'H';
						if(callback)
						{
							data.Append("H");
						}
						break;
					case "h":
						DisplayChange(pitchs[chcous]+9,3);
						nppc[pitchs[chcous]+9] = 'h';
						if(!KDown[23])
						{
							if(callback)
							{
								data.Append("h");
							}
						}
						break;
					case "U":
						DisplayChange(pitchs[chcous]+10,2,chcous);
						nppc[pitchs[chcous]+10] = 'U';
						if(callback)
						{
							data.Append("U");
						}
						break;
					case "u":
						DisplayChange(pitchs[chcous]+10,3);
						nppc[pitchs[chcous]+10] = 'u';
						if(!KDown[23])
						{
							if(callback)
							{
								data.Append("u");
							}
						}
						break;
					case "J":
						DisplayChange(pitchs[chcous]+11,2,chcous);
						nppc[pitchs[chcous]+11] = 'J';
						if(callback)
						{
							data.Append("J");
						}
						break;
					case "j":
						DisplayChange(pitchs[chcous]+11,3);
						nppc[pitchs[chcous]+11] = 'j';
						if(!KDown[23])
						{
							if(callback)
							{
								data.Append("j");
							}
						}
						break;
					case "K":
						DisplayChange(pitchs[chcous]+12,2,chcous);
						nppc[pitchs[chcous]+12] = 'K';
						if(callback)
						{
							data.Append("K");
						}
						break;
					case "k":
						DisplayChange(pitchs[chcous]+12,3);
						nppc[pitchs[chcous]+12] = 'k';
						if(!KDown[23])
						{
							if(callback)
							{
								data.Append("k");
							}
						}
						break;
					case "Z":
						if (pitchs[chcous]>0)
						{
							if(mode==0 || mode==1)
							{
								Console.WriteLine("key_Send_LeftArrow "+(pitchs[chcous]/12).ToString());
							}
							pitchs[chcous] -= 12;
							if(callTmp)
							{
								data.Append("Z");
							}
						}
						break;
					case "X":
						if (pitchs[chcous]<120)
						{
							if(mode==0 || mode==1)
							{
								Console.WriteLine("key_Send_RightArrow "+(pitchs[chcous]/12).ToString());
							}
							pitchs[chcous] += 12;
							if(callTmp)
							{
								data.Append("X");
							}
						}
						break;
					case "C":
						if (this.cmb[1].SelectedIndex>0)
						{
							if(mode==0 || mode==1)
							{
								Console.WriteLine("key_Send_UpArrow "+this.cmb[1].SelectedIndex.ToString());
							}
							prog[chcous]--;
							if(callTmp)
							{
									data.Append("C");
							}
						}
						break;
					case "V":
						if (this.cmb[1].SelectedIndex<127)
						{
							if(mode==0 || mode==1)
							{
								Console.WriteLine("key_Send_UpArrow "+this.cmb[1].SelectedIndex.ToString());
							}
							prog[chcous]++;
							if(callTmp)
							{
								data.Append("V");
							}
						}
						break;
					case "Q":
						if(mode==0 || mode==1)
						{
							Console.WriteLine("key_Send_Channel "+this.cmb[0].SelectedIndex.ToString());
						}
						if(this.cmb[0].SelectedIndex==0)
						{
							this.cmb[0].SelectedIndex=1;
						}
						else
						{
							this.cmb[0].SelectedIndex=0;
						}
						if(callTmp)
						{
							data.Append("Q");
						}
						break;
					case "B":
						if(mode==0 || mode==1)
						{
							Console.WriteLine("key_SendDown_ShiftKey");
						}
						KDown[23]=true;
						if(callback)
						{
							data.Append("B");
						}
						break;
					case "b":
						if(mode==0 || mode==1)
						{
							Console.WriteLine("key_SendUp_ShiftKey");
						}
						KDown[23]=false;
						for (int j=0;j<128;j++)
						{
							this.NoteOff((byte)j);
							if(mode==0 || mode==2)
							{
								this.key[j].BackColor = this.col[j];
							}
						}
						if(callback)
						{
							data.Append("b");
						}
						break;
					case "M":
						if (this.cmb[2].SelectedIndex>0)
						{
							if(mode==0 || mode==1)
							{
								Console.WriteLine("key_Send_WaitUp "+this.cmb[2].SelectedIndex.ToString());
							}
							this.cmb[2].SelectedIndex --;
							if(callback)
							{
								data.Append("M");
							}
						}
						break;
					case "m":
						if (this.cmb[2].SelectedIndex<12)
						{
							if(mode==0 || mode==1)
							{
								Console.WriteLine("key_Send_WaitDown "+this.cmb[2].SelectedIndex.ToString());
							}
							this.cmb[2].SelectedIndex ++;
							if(callTmp)
							{
								data.Append("m");
							}
						}
						break;
					case "P":
						if(colorful)
						{
							colorful=false;
						}
						else
						{
							colorful=true;
						}
						if(callback)
						{
							data.Append("P");
						}
						break;
					case "0":
						Console.WriteLine("key_SendDown_PlayStart "+ this.cmb[4].SelectedIndex);
						MusicStart();
						break;
					case "N":
						if(skipFrame)
						{
							if(skipflag == 1)
							{
								await Task.Delay(waits);
							}
							skipflag *= -1;
						}
						else
						{
							await Task.Delay(waits);
						}
						if(fasts != 2){
							if(synceFlag)
							{
								syncs[chcous] = 1;
								while(syncsOK)
								{
									await Task.Delay(1);
								}
							}
							if(sw.Elapsed.ToString().CompareTo("00:00:01") != -1)
							{
								avgChar = throughN;
								throughN = 0;
								sw.Reset();
								sw.Start();
								averageSpeed = 0.05 * avgChar + (1-0.05) * averageSpeed;
								secRemain = (int)((tdl-i)/averageSpeed);
							}
							if(this.chkb[2].Checked)
							{
								for(int k=0;k<nppc.Length;k++)
								{
									if(Char.IsUpper(nppc[k]))
									{
										Ntmp.Append(nppc[k]);
									}
								}
								this.lbl[2].Text = "Char:"+ i +"/"+ tdl +" "+ avgChar +" c/s " + secRemain + "s\nAN:"+ Ntmp.Length +"/ "+ strCou(Ntmp.ToString());
							}
							else
							{
								this.lbl[2].Text = "Char:"+ i +"/"+ tdl +" "+ avgChar +" c/s " + secRemain + "s\nN:"+ Ntmp.Length +"/ "+ strCou(Ntmp.ToString());
							}
							Ntmp = new StringBuilder("");
						}
						break;
					default:
						Console.WriteLine("key_Send_Error "+tmp);
						break;
				}
			}

			if(fasts != 2){
				if(this.chkb[2].Checked)
				{
					this.lbl[2].Text = "Char:"+ tdl +"/"+ tdl + " 0 c/s 0s\nAN:";
				}
				else
				{
					this.lbl[2].Text = "Char:"+ tdl +"/"+ tdl + " 0 c/s 0s\nN:";
				}
			}
			Console.WriteLine("key_LockUp_PleyEnd");
			if(this.chkb[1].Checked){
				Console.WriteLine("# mode is Loop!");
				Console.WriteLine("key_SendDown_PlayStart "+ this.cmb[4].SelectedIndex);
				MusicStart();
			}
		}
		else
		{
			Console.WriteLine("! Warning:file does not exist");
		}
		syncs[chcous] = 2;
		playback=false;
	}

	private string strCou(string str)
	{
		string tmpStr = new string(str.ToCharArray().Distinct().ToArray());
		char tmp;
		StringBuilder ans = new StringBuilder("");
		int cou;
		for(int i=0;i<tmpStr.Length;i++)
		{
			tmp = tmpStr.Substring(i, 1).ToCharArray()[0];
			cou = str.Count(f => f == tmp);
			ans.Append(cou);
			ans.Append(tmp);
		}
		return ans.ToString();
	}
	private bool IsKeyLocked(System.Windows.Forms.Keys Key_Value)
	{
	// WindowsAPIで押下判定
		if(this.chkb[0].Checked == true)
		{
			bool Key_State = (GetKeyState((int)Key_Value) & 0x80) != 0;
			return Key_State;
		}
		else
		{
			return false;
		}
	}
}

class FormPianoHelp : Form
{
	/*--------------------------------------------------*/
	private TextBox[] txb;
	private Label[] lbl;
	private ComboBox[] cmb;
	private CheckBox[] chkb;
	private Button[] btn;
	private bool ResetFlag = false;

	public FormPianoHelp(){
		if(File.Exists(FormPiano.iconPath)){
			this.Icon = new System.Drawing.Icon(FormPiano.iconPath);
		}
		this.ClientSize = new Size(450, 420);
		this.FormBorderStyle = FormBorderStyle.FixedSingle;
		this.MaximizeBox = false;
		this.Text = "PianoPls - Help";

		this.Load += new EventHandler(this.FormPianoHelp_Load);
		this.Closed += new EventHandler(this.FormPianoHelp_Closed);

		Console.WriteLine("-* StartUp Set");
		/*--------------------------------------------------*/
		/* コントロールの作成 */
		/*--------------------------------------------------*/

		this.txb = new TextBox[2];

		for( int i = 0; i < this.txb.Length; i++)
		{
			this.txb[i] = new TextBox();
			this.Controls.Add(this.txb[i]);
		}

		this.txb[0].Multiline = true;
		this.txb[0].ReadOnly = true;
		this.txb[0].ScrollBars = ScrollBars.Vertical;
		this.txb[0].Font = new Font("ＭＳ ゴシック", 12);
		this.txb[0].SetBounds(1, 3, 452, 350, BoundsSpecified.All);
		this.txb[0].Text = "PianoPls "+ FormPiano.pianoVersion +"\r\n"
			+ "(binread "+ FormPiano.binreadVersion +")\r\n"
			+ "============================\r\n"
			+ "midiファイルを読み込む上での注意\r\n\r\n"
			+ "このプログラムはSMFの\r\n"
			+ "\"format0\"にしか対応していません\r\n"
			+ "また、\"Studio one\"の\r\n"
			+ "midiの書き出し方法にしか対応していません\r\n"
			+ "============================\r\n"
			+ "下の軽量化設定は一部機能が制限されます\r\n"
			+ "注意してご利用ください\r\n"
			+ "============================\r\n"
			+ "キー設定(変更不可)\r\n\r\n"
			+ "A:ド　(C)\r\n"
			+ "S:レ　(D)\tW:レ♭(D♭)\r\n"
			+ "D:ミ　(E)\tE:ミ♭(E♭)\r\n"
			+ "F:ファ(F)\t\r\n"
			+ "G:ソ　(G)\tT:ソ♭(G♭)\r\n"
			+ "H:ラ　(A)\tY:ラ♭(A♭)\r\n"
			+ "J:シ　(B)\tU:シ♭(B♭)\r\n"
			+ "K:ド＋(C+)\r\n\r\n"
			+ "Q:ピアノ/ドラム\r\n"
			+ "↑/↓:楽器変更\r\n"
			+ "→/←:オクターブ変更\r\n\r\n"
			+ "Shift:音を延ばす/止める\r\n"
			+ "----------------------------\r\n"
			+ "M/N:フレーム速度変更\r\n"
			+ "P:鍵盤色変更\r\n"
			+ "----------------------------\r\n"
			+ "0:曲再生スタート\r\n"
			+ "9:曲記録一時停止/スタート\r\n"
			+ "8:曲記録停止/スタート\r\n"
			+ "7:曲記録停止&データ破棄\r\n\r\n"
			+ "B:音同期(非使用推奨)\r\n"
			+ "============================\r\n"
			+ "本体画面説明\r\n\r\n"
			+ "鍵盤部分\r\n"
			+ "--マウスでも音を出す事が出来ます\r\n\r\n"
			+ "channel(コンボボックス)\r\n"
			+ "--変更する事で楽器の種類を変更できます\r\n"
			+ "wait(コンボボックス)\r\n"
			+ "--変更する事で全体のフレーム速度を変更できます\r\n"
			+ "waitの1つ右(コンボボックス)\r\n"
			+ "--変更する事で流す曲を変更できます\r\n"
			+ "waitの2つ右(コンボボックス)\r\n"
			+ "--変更する事で動作modeを変更できます\r\n"
			+ "Key(チェックボックス)\r\n"
			+ "--キーボードを使用するかを選択します\r\n"
			+ "Loop(チェックボックス)\r\n"
			+ "--曲を流した際にループするかを選択します\r\n"
			+ "AN/N(チェックボックス)\r\n"
			+ "--曲を流した際の\"AN:\"と\"N:\"の表示のmodeを選択します\r\n\r\n"
			+ "0/0fps\r\n"
			+ "--画面の現在のfps/画面で実行しうるfps最高値を表示\r\n\r\n"
			+ "曲を流した際の表示部分\r\n"
			+ "Char:0/0\r\n"
			+ "--曲の現在の進行度を表示\r\n"
			+ "0 n/s\r\n"
			+ "--曲の1秒間の文字読込速度を表示\r\n"
			+ "0s\r\n"
			+ "--曲の終了までの残り時間(不正確)を表示\r\n\r\n"
			+ "NA:\r\n"
			+ "--現在鳴っている音のデータを表示\r\n"
			+ "N:\r\n"
			+ "--現在鳴った瞬間の音のデータを表示\r\n"
			+ "----------------------------\r\n"
			+ "設定画面説明\r\n\r\n"
			+ "midi参照フォルダ(ボタン)\r\n"
			+ "--midi(txt)ファイルを参照する親フォルダを設定します\r\n"
			+ "検索ファイル(コンボボックス)\r\n"
			+ "--midi(txt)ファイルを検索する際のフィルター設定\r\n"
			+ "軽量化モード(コンボボックス)\r\n"
			+ "--PianoPlsを軽量化モードで実行する設定\r\n"
			+ "ノイズサイズ(テキストボックス)\r\n"
			+ "--midi変換の際のノイズをカットする時のカットする\r\n"
			+ "音の最大サイズを設定します[初期値:0.06]\r\n"
			+ "notSleep(チェックボックス)\r\n"
			+ "--PCが時間によりスリープ状態になっても実行するかの設定\r\n"
			+ "getFps(チェックボックス)\r\n"
			+ "--本体右上にfpsを表示するかの設定\r\n"
			+ "skFrame(チェックボックス)\r\n"
			+ "--プログラム実行時に1fに1回の処理を1fに2回に変更する\r\n"
			+ "secure(チェックボックス)\r\n"
			+ "--midi変換の際に1音ごとに休符を挟むかの設定\r\n"
			+ "noiseCut(チェックボックス)\r\n"
			+ "--上記の{ノイズサイズ}を使用するかの設定\r\n\r\n"
			+ "※ノイズカット系の動作は不正確です"
			+ "============================\r\n"
			+ "使用ファイル\r\n\r\n"
			+ "binread_pls.cs(.exe)\r\n"
			+ "--midiファイルをこのプログラム用に\r\n書き直すプログラム\r\n"
			+ "piano_pls.cs(.exe)\r\n"
			+ "--本体\r\n"
			+ "pinfl.ico\r\n"
			+ "--アイコンファイル\r\n"
			+ "Music.txt\r\n"
			+ "--Tmpファイル\r\n"
			+ "compile.bat\r\n"
			+ "--csファイルコンパイル用\r\n"
			+ "(binread_pls.cs存在時のみ存在)\r\n"
			+ "Setting.dat\r\n"
			+ "--設定ファイル\r\n"
			+ "(問題が発生した時は削除してみると良いかもしれません)\r\n"
			+ "============================\r\n"
			+ "製作　　:tromtub\r\n"
			+ "デバック:Kdr\r\n\r\n"
			+ "個人サイト:\r\n"
			+ "http://snow.deca.jp\r\n\r\n"
			+ "参考元:\r\n"
			+ "http://otktake.blogspot.com/2016/02/c.html\r\n\r\n"
			+ "Copyright © 2019-2022 tromtub";

		this.txb[1].SetBounds(241, 378, 70, 18, BoundsSpecified.All);
		this.txb[1].Text = FormPiano.binNoiseSize.ToString();
		this.txb[1].TextChanged+=new EventHandler(this.Setting_Changed);
		Console.WriteLine("-* TextBox Set");

		this.lbl = new Label[3];

		for( int i = 0; i < this.lbl.Length; i++)
		{
			this.lbl[i] = new Label();
			this.Controls.Add(this.lbl[i]);
		}
		this.lbl[0].SetBounds(101, 358, 70, 18, BoundsSpecified.All);
		this.lbl[0].Text = "検索ファイル";

		this.lbl[1].SetBounds(171, 358, 70, 18, BoundsSpecified.All);
		this.lbl[1].Text = "軽量化モード";

		this.lbl[2].SetBounds(241, 358, 70, 18, BoundsSpecified.All);
		this.lbl[2].Text = "ノイズサイズ";
		Console.WriteLine("-* Label Set");

		this.cmb = new ComboBox[2];
		for(int i = 0; i < this.cmb.Length; i++)
		{
			this.cmb[i] = new ComboBox();
			this.cmb[i].DropDownStyle = ComboBoxStyle.DropDownList;
			this.Controls.Add(this.cmb[i]);
		}

		for(int i = 0; i < FormPiano.midFilter.Length; i++)
		{
			this.cmb[0].Items.Add("\"" + FormPiano.midFilter[i] + "\"");
		}
		this.cmb[0].SetBounds(101, 376, 70, 20, BoundsSpecified.All);
		this.cmb[0].SelectedIndex = FormPiano.midFilterInd;
		this.cmb[0].SelectedIndexChanged+=new EventHandler(this.Setting_Changed);

		this.cmb[1].Items.Add("通常");
		this.cmb[1].Items.Add("弱軽量化");
		this.cmb[1].Items.Add("強軽量化");

		this.cmb[1].SetBounds(171, 376, 70, 20, BoundsSpecified.All);
		this.cmb[1].SelectedIndex = FormPiano.fasts;
		this.cmb[1].SelectedIndexChanged+=new EventHandler(this.Setting_Changed);
		Console.WriteLine("-* ComboBox Set");

		this.chkb = new CheckBox[5];
		for(int i = 0; i < this.chkb.Length; i++)
		{
			this.chkb[i] = new CheckBox();
			this.Controls.Add(this.chkb[i]);
		}
		this.chkb[0].SetBounds(311, 356, 70, 18, BoundsSpecified.All);
		this.chkb[0].Text = "notSleep";
		this.chkb[0].Checked = FormPiano.sleepPC;
		this.chkb[0].CheckedChanged += new EventHandler(this.Setting_Changed);

		this.chkb[1].SetBounds(311, 376, 70, 18, BoundsSpecified.All);
		this.chkb[1].Text = "getFps";
		this.chkb[1].Checked = FormPiano.fpsFlag;
		this.chkb[1].CheckedChanged += new EventHandler(this.Setting_Changed);

		this.chkb[2].SetBounds(311, 396, 70, 18, BoundsSpecified.All);
		this.chkb[2].Text = "skFrame";
		this.chkb[2].Checked = FormPiano.skipFrame;
		this.chkb[2].CheckedChanged += new EventHandler(this.Setting_Changed);

		this.chkb[3].SetBounds(381, 356, 70, 18, BoundsSpecified.All);
		this.chkb[3].Text = "secure";
		this.chkb[3].Checked = FormPiano.binSecure;
		this.chkb[3].CheckedChanged += new EventHandler(this.Setting_Changed);

		this.chkb[4].SetBounds(381, 376, 70, 18, BoundsSpecified.All);
		this.chkb[4].Text = "noiseCut";
		this.chkb[4].Checked = FormPiano.binNoiseCut;
		this.chkb[4].CheckedChanged += new EventHandler(this.Setting_Changed);

		this.btn = new Button[1];
		for(int i = 0; i < this.btn.Length; i++)
		{
			this.btn[i] = new Button();
			this.btn[i].Name = i.ToString();
			this.Controls.Add(this.btn[i]);
			this.btn[i].MouseDown += new MouseEventHandler(this.clickButtons);
		}
		this.btn[0].SetBounds(1, 356, 100, 40, BoundsSpecified.All);
		this.btn[0].Text = "midi参照フォルダ";
		this.btn[0].BackColor = Color.LightGray;
		Console.WriteLine("-* Button Set");

	}
	/*--------------------------------------------------*/
	/* Buttonを押した際の実行
	/*--------------------------------------------------*/
	private void clickButtons(object sender, MouseEventArgs e)
	{
		int key = int.Parse(((Button)sender).Name);

		if(key==0){
			FolderBrowserDialog fbd = new FolderBrowserDialog();
			fbd.Description = "midiを含むフォルダを指定してください。";
			fbd.SelectedPath = FormPiano.musicDirectory;
			fbd.ShowNewFolderButton = false;
			Console.WriteLine("-$ Folder Select Window Open");
			if (fbd.ShowDialog(this) == DialogResult.OK)
			{
				FormPiano.musicDirectory = fbd.SelectedPath;
				SettingWrite();
			}
			else
			{
				Console.WriteLine("-$ Cancel");
			}
		}
	}
	/*--------------------------------------------------*/
	/* コンボボックスの処理 */
	/*--------------------------------------------------*/
	private void Setting_Changed(object sender, System.EventArgs e)
	{
		FormPiano.midFilterInd = this.cmb[0].SelectedIndex;
		FormPiano.fasts = this.cmb[1].SelectedIndex;
		FormPiano.binNoiseSize = Convert.ToDouble(Regex.Replace(this.txb[1].Text,@"[^0-9\.]","").PadLeft(2, '0'));
		FormPiano.sleepPC = this.chkb[0].Checked;
		FormPiano.fpsFlag = this.chkb[1].Checked;
		FormPiano.skipFrame = this.chkb[2].Checked;
		FormPiano.binSecure = this.chkb[3].Checked;
		FormPiano.binNoiseCut = this.chkb[4].Checked;
		SettingWrite();
	}
	/*--------------------------------------------------*/
	/* Setting.datの書き込み
	/*--------------------------------------------------*/
	private void SettingWrite(){
		ResetFlag = true;
		Console.WriteLine("-@ Write \"Setting.dat\"");
		SettingFiles.Update();
		Console.WriteLine("-@ Close \"Setting.dat\"");
	}
	/*--------------------------------------------------*/
	/* HelpのOpenとClose
	/*--------------------------------------------------*/
	private void FormPianoHelp_Load(object sender, EventArgs e)
	{
		//this.SetDesktopBounds(-5, 0, Screen.PrimaryScreen.Bounds.Width+10, 256);
		//Console.WriteLine("* Window Set");
		
		Console.WriteLine("-! FormPianoHelp_Load");
	}
	private void FormPianoHelp_Closed(object sender, EventArgs e)
	{
		Console.WriteLine("-! FormPianoHelp_Closed");
		if(ResetFlag)
		{
			DialogResult result= MessageBox.Show("変更が検知されました\n再起動しますか？","PianoPls", MessageBoxButtons.YesNo);
			if (result == DialogResult.Yes)
			{
				ExecutionState.EnableSuspend();
				Console.WriteLine("* Display Sleep reset");
				Console.WriteLine("! PianoPls Restart");
				Application.Restart();
			}
			
		}
	}
}


public static class SettingFiles
{
	public static string[] settingData;

	public static void Update()
	{
		using (StreamWriter dest = new StreamWriter(FormPiano.settingPath,false))
		{
			dest.WriteLine(FormPiano.pianoVersion);
			dest.WriteLine(FormPiano.binreadVersion);
			dest.WriteLine(FormPiano.musicDirectory);
			dest.WriteLine(FormPiano.midFilterInd);
			dest.WriteLine(FormPiano.fasts);
			dest.WriteLine(FormPiano.binNoiseSize);
			dest.WriteLine(FormPiano.binSecure);
			dest.WriteLine(FormPiano.binNoiseCut);
			dest.WriteLine(FormPiano.sleepPC);
			dest.WriteLine(FormPiano.fpsFlag);
			dest.WriteLine(FormPiano.skipFrame);
		}
	}
	public static void Reading()
	{
			Console.WriteLine("@ SettingFile Load");
			List<string> listSd = new List<string>();
			string line;
			int wi = 0;
			StreamReader src = new StreamReader(FormPiano.settingPath, Encoding.GetEncoding("UTF-8"));
			while(src.EndOfStream == false)
			{
				line = src.ReadLine();
				listSd.Add(line);
				wi++;
			}
			settingData = listSd.ToArray();
			src.Close();
			Console.WriteLine("@ SettingFile Close");

			Console.WriteLine("# Setting.dat " + settingData[0]);
			if(FormPiano.pianoVersion!=settingData[0])
			{
				Console.WriteLine("! Wrong version");
			}
			else
			{
				if(FormPiano.binreadVersion=="NoData"){
					FormPiano.binreadVersion = settingData[1];
				}
				else if(settingData[1]!="NoData" && FormPiano.binreadVersion!=settingData[1]){
					FormPiano.newbinreadflag = true;
					Console.WriteLine("# Setting.dat binread.cs " + settingData[1]);
					Console.WriteLine("! Update binread version");
				}
				else if(settingData[1]=="NoData")
				{
					FormPiano.newbinreadflag = true;
					Console.WriteLine("! Update binread version");
				}
				FormPiano.musicDirectory = settingData[2];
				FormPiano.midFilterInd = int.Parse(settingData[3]);
				FormPiano.fasts = int.Parse(settingData[4]);
				FormPiano.binNoiseSize = Convert.ToDouble(settingData[5]);
				FormPiano.binSecure = Convert.ToBoolean(settingData[6]);
				FormPiano.binNoiseCut = Convert.ToBoolean(settingData[7]);
				FormPiano.sleepPC = Convert.ToBoolean(settingData[8]);
				FormPiano.fpsFlag = Convert.ToBoolean(settingData[9]);
				FormPiano.skipFrame = Convert.ToBoolean(settingData[10]);
			}
	}
}

public static class ExecutionState
{
	[DllImport("kernel32.dll")]
	static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

	[FlagsAttribute]
	public enum EXECUTION_STATE : uint
	{
		// スタンバイ状態にするのを防ぐ
		ES_SYSTEM_REQUIRED  = 0x00000001,
		// ディスプレイをオフにするのを防ぐ
		ES_DISPLAY_REQUIRED = 0x00000002,
		// 実行状態を維持する 
		ES_CONTINUOUS       = 0x80000000,
	}

	// スタンバイ防止
	public static EXECUTION_STATE DisableSuspend()
	{
		return SetThreadExecutionState(
			EXECUTION_STATE.ES_SYSTEM_REQUIRED | 
			EXECUTION_STATE.ES_CONTINUOUS);
	}
	// スタンバイ防止を解除
	public static EXECUTION_STATE EnableSuspend()
	{
		return SetThreadExecutionState(
			EXECUTION_STATE.ES_CONTINUOUS);
	}
}


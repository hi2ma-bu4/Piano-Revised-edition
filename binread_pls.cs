using System;
using System.IO;
using System.Linq;
using System.Text;

class BinaryCopy {
	public static string binreadVersion = "ver.1.86.12";

	static int Main(string[] cmds) {
		bool Secure = false; //必ず休符を挟む[t:確実?,f:正確?]
		bool NewLog = false; //ログ表示[f:高速]
		bool NoiseCut = false; //ノイズカット(短すぎる音を削除)
		double NoiseSize = 0.06; //ノイズサイズ[標:0.06-0.62]

		switch(cmds.Length)
		{
			case 0:
				string[] tmp = binreadVersion.Split('.');
				return int.Parse(tmp[1] + tmp[2].PadLeft(3, '0') + tmp[3].PadLeft(3, '0'));
			case 2:
				Secure = Convert.ToBoolean(cmds[1]);
				break;
			case 3:
				NoiseCut = Convert.ToBoolean(cmds[2]);
				goto case 2;
			case 4:
				NoiseSize = Convert.ToDouble(cmds[3]);
				goto case 3;
		}
		string srcName = cmds[0];
		string destName =@"Music.txt"; // コピー先のファイル名

		const int BUFSIZE = 1; // 1度に処理するサイズ
		byte[] buf = new byte[BUFSIZE]; // 読み込み用バッファ
		byte[] ZEROARRAY = new byte[BUFSIZE]; // 0埋め用

		int readSize; // Readメソッドで読み込んだバイト数
		bool midiFlag=true;

/*
		int[] noteonflag = new int[128];
		for(int i=0;i<128;i++){
			noteonflag[i] = 0;
		}
*/

		using (FileStream src = new FileStream(
			srcName, FileMode.Open, FileAccess.Read))
		//using (FileStream dest = new FileStream(
		//	destName, FileMode.Create, FileAccess.Write))
		using (StreamWriter dest = new StreamWriter(destName,false))
		{
			int i=0,j;
			int Mtrk=0;
			int noteTmp=0;
			int noteTall=0;
			int backTall=5;
			int miTall=0;
			bool rk=false;
			int bpm=0;
			string note="";
			byte[] tmpBit=new Byte[2];
			byte[] Ticks=new Byte[5];
			double noteSleep;
			bool Ncut;
			Ticks[0]=0;
			Ticks[1]=0;
			Ticks[2]=0;
			Ticks[3]=0;
			Ticks[4]=0;
			while (true) {
				Ncut = false;
				i++;
				try {
					readSize = src.Read(buf, 0, BUFSIZE); // 読み込み
				} catch {
					// 読み込みに失敗した場合
					Console.WriteLine("read error at " + src.Position);

					if (src.Length - src.Position < BUFSIZE) {
						readSize = (int)(src.Length - src.Position);
					} else {
						readSize = BUFSIZE;
					}
					src.Seek(readSize, SeekOrigin.Current);
					//dest.Write(ZEROARRAY, 0, readSize); // 0埋めで書き込み
					continue;
				}
				Ticks[4]=Ticks[3];
				Ticks[3]=Ticks[2];
				Ticks[2]=Ticks[1];
				Ticks[1]=Ticks[0];
				Ticks[0]=buf[0];
				if (readSize == 0){
					break; // コピー完了
				}
				if (i==1 && Encoding.ASCII.GetString(buf)=="M"){
					Console.WriteLine(Encoding.ASCII.GetString(buf));
				}
				else if(i==2 && Encoding.ASCII.GetString(buf)=="T"){
					Console.WriteLine(Encoding.ASCII.GetString(buf));
				}
				else if(i==3 && Encoding.ASCII.GetString(buf)=="h"){
					Console.WriteLine(Encoding.ASCII.GetString(buf));
				}
				else if(i==4 && Encoding.ASCII.GetString(buf)=="d"){
					Console.WriteLine(Encoding.ASCII.GetString(buf));
				}
				else if(i<=4 && midiFlag){
					midiFlag=false;
				}
				else if(!midiFlag){
					break;
				}
				if(i==13){
					tmpBit[1]=buf[0];
				}
				else if(i==14){
					tmpBit[0]=buf[0];
					bpm=BitConverter.ToInt16(tmpBit,0);
					Console.WriteLine(bpm.ToString());
				}
				if(Mtrk==0 && Encoding.ASCII.GetString(buf)=="M"){
					Mtrk++;
				}
				else if(Mtrk==1 && Encoding.ASCII.GetString(buf)=="T"){
					Mtrk++;
				}
				else if(Mtrk==2 && Encoding.ASCII.GetString(buf)=="r"){
					Mtrk++;
				}
				else if(Mtrk==3 && Encoding.ASCII.GetString(buf)=="k"){
					rk=false;
				}
				if(Ticks[0]==(byte)0x90 && note==""){
					note="0";
				}
				else if(Ticks[0]==(byte)0x80 && note==""){
					note="1";
				}
				else if(note!=""){
					//noteonflag[Ticks[0]] = int.Parse(note);
					noteTmp=(int)Ticks[0]%12;
					noteTall=(int)Ticks[0]/12;
					miTall=backTall-noteTall;
					if(miTall>=1){
						for(j=1;j<=miTall;j++){
							backTall--;
							dest.Write("Z");
						}
					}
					else if(miTall<=1){
						miTall*=-1;
						for(j=1;j<=miTall;j++){
							backTall++;
							dest.Write("X");
						}
					}
					//noteSleep = (128*Ticks[2]+Ticks[1])/(bpm*4*1.2);
					//noteSleep = (128*Ticks[2]+Ticks[1])/(bpm * 4);
					noteSleep = (128*Ticks[2]+Ticks[1])/(bpm * 4);
					if(NoiseCut){
						if(backTall < 1 || backTall > 8){
							Ncut = true;
						}
						else if(noteSleep!=0 && noteSleep <= NoiseSize){
							Ncut = true;
						}
					}
					if(NewLog){
						Console.WriteLine(noteTmp+","+(int)noteSleep);
					}
						//Console.WriteLine(noteTmp+","+(int)noteSleep+","+(int)Ticks[4]+","+(int)Ticks[3]+","+(int)Ticks[2]+","+(int)Ticks[1]);
					for(j=1;j<=(int)(noteSleep+0.5);j++){
						dest.Write("N");
					}
					if(Secure && note=="0"){
						dest.Write("N");
					}
					if(!Ncut){
						if(noteTmp==0)
						{
							if(note=="0"){
								dest.Write("A");
							}
							else
							{
								dest.Write("a");
							}
						}
						else if(noteTmp==1)
						{
							if(note=="0"){
								dest.Write("W");
							}
							else
							{
								dest.Write("w");
							}
						}
						else if(noteTmp==2)
						{
							if(note=="0"){
								dest.Write("S");
							}
							else
							{
								dest.Write("s");
							}
						}
						else if(noteTmp==3)
						{
							if(note=="0"){
								dest.Write("E");
							}
							else
							{
								dest.Write("e");
							}
						}
						else if(noteTmp==4)
						{
							if(note=="0"){
								dest.Write("D");
							}
							else
							{
								dest.Write("d");
							}
						}
						else if(noteTmp==5)
						{
							if(note=="0"){
								dest.Write("F");
							}
							else
							{
								dest.Write("f");
							}
						}
						else if(noteTmp==6)
						{
							if(note=="0"){
								dest.Write("T");
							}
							else
							{
								dest.Write("t");
							}
						}
						else if(noteTmp==7)
						{
							if(note=="0"){
								dest.Write("G");
							}
							else
							{
								dest.Write("g");
							}
						}
						else if(noteTmp==8)
						{
							if(note=="0"){
								dest.Write("Y");
							}
							else
							{
								dest.Write("y");
							}
						}
						else if(noteTmp==9)
						{
							if(note=="0"){
								dest.Write("H");
							}
							else
							{
								dest.Write("h");
							}
						}
						else if(noteTmp==10)
						{
							if(note=="0"){
								dest.Write("U");
							}
							else
							{
								dest.Write("u");
							}
						}
						else if(noteTmp==11)
						{
							if(note=="0"){
								dest.Write("J");
							}
							else
							{
								dest.Write("j");
							}
						}
					}
					note="";
				}
				else
				{
					note="";
				}
				if(rk){
					//dest.Write(buf, 0, readSize); // 書き込み
				}
				if(!rk && Encoding.ASCII.GetString(buf)=="`"){
					rk=true;
					Console.WriteLine(Encoding.ASCII.GetString(buf));
				}
			}
		}
		if(NewLog){
			Console.WriteLine("続行するには何かキーを押してください．．．");
			Console.ReadKey();
		}
		return 0;
	}
}

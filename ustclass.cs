using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace UST_MUSICXML_LIB
{
    /// <summary>
    /// USTファイルをMusicXMLにするクラス
    /// </summary>
    public class ustclass
    {
        private string APPNAME = "ust to musicxml library 2020";
        private List<notes> Notes=new List<notes>();
        private float tempo     = 100.0F; // テンポ
        private int divisions = 480;  // 四分音符の分解能
        private int beats     = 4;    // 拍子
        private int beatType  = 4;    // ○分の..
        private int useTie    = 1;    // 1=音長が小節区切りをまたぐ際等にタイを使う
        private bool init_one = false;
        private  float[] lengthList = new float[]{1920,    1680  ,   1440,   960  ,   840  ,     720   ,   480   ,  420,
                      360   ,  320    ,  240   , 210  ,   180   ,    160   ,   120   ,  105    ,  90     , 80  ,   60,
                      52.5F  ,  45     ,  30 ,    26.25F ,  22.5F   ,   15    ,   13.125F , 11.25F  ,7.5F };
        private string[] typeList   = new string[]{"whole", "halfDD",  "halfD" ,"half"    ,"quarterDD", "quarterD" ,"quarter" ,"eighthDD",
                      "eighthD", "quarterT" ,"eighth" ,"16thDD",  "16thD", "eighthT",  "16th",    "32ndDD",   "32ndD"   ,"16thT"  ,"32nd",
                      "64thDD"  ,"64thD"    ,"64th"   ,"128thDD" ,"128thD",    "128th",    "256thDD", "256thD", "256th" };
        private Regex lenght2_note_D_regex = new Regex(@"^(.+)?D$", RegexOptions.Compiled);

        private Regex lenght2_note_T_regex = new Regex(@"^(.+)?T$", RegexOptions.Compiled);
        private Regex lenght2_note_DPlus_regex = new Regex(@"^.+?(D+)$", RegexOptions.Compiled);
        #region 初期化(オーバーロード)
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="filename">USTファイルのファイル名</param>
        public ustclass(string filename)
        {
            this.ustclass_init(filename);
        }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="stream">USTファイルのストリーム</param>
        public ustclass(FileStream stream) 
        {
            this.ustclass_init(stream);

        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="stream">USTファイルのストリーム</param>
        public ustclass(Stream stream)
        {
            this.ustclass_init(stream);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="stream">USTファイルのストリームリーダー</param>
        public ustclass(StreamReader stream)
        {
            this.ustclass_init(stream);
        }
        private void ustclass_init(FileStream stream)
        {
            ustclass_init(new StreamReader(stream, Encoding.GetEncoding(932)));
        }
        private void ustclass_init(string fname)
        {
            using (FileStream hage = new FileStream(fname, FileMode.Open))
            {
                ustclass_init(hage);
            }
        }
        private void ustclass_init(Stream stream)
        {
            ustclass_init(new StreamReader(stream, Encoding.GetEncoding(932)));

        }
        #endregion
        private void ustclass_init(StreamReader stream)
        {
            
            string strkun = "";
            #region ヘッダの処理
            Regex matchkun_HEAD = new Regex(@"^\[#\d+\]", RegexOptions.Compiled);
            Regex matchkun_TNP = new Regex(@"^Tempo", RegexOptions.Compiled);

            while (true)
            {
                strkun = stream.ReadLine();
                if(matchkun_HEAD.IsMatch(strkun))
                {
                    break;
                }
                if (matchkun_TNP.IsMatch(strkun))
                {
                    strkun = strkun.Replace(" ", "");
                    string[] tempostr = strkun.Split('=');
                    tempo = float.Parse(tempostr[1]);

                }

            }
            #endregion

            int seq = 0;
            //Notes.Add(newnotes());
            //Regex nonkekun = new Regex(@"s/(\r|\n)+//g", RegexOptions.Compiled);
            string[] datakun;
            notes ndkun=newnotes();
            string leftnone;
            string rightnone;
            Regex dtseekkun = new Regex(@"^\[#\d+\]", RegexOptions.Compiled);
            while (!stream.EndOfStream)
            {
                strkun = stream.ReadLine();
                if (strkun == null)
                {
                    continue;
                }
                datakun = strkun.Split('=');
                if (datakun[0].Equals("Length"))
                {
                    //System.Console.WriteLine(quantize(102));
                    //ndkun = Notes[seq];
                    ndkun.duration = quantize(int.Parse(datakun[1]));
                    length2note(ref ndkun);
                    /*
                    System.Console.WriteLine(Notes[seq].type);
                    System.Console.WriteLine(Notes[seq].dot.ToString());
                    */
                }
                else if (datakun[0].Equals("Lyric"))
                {
                    //ndkun = Notes[seq];
                    ndkun.lyricText = datakun[1];
                    //Notes[seq] = ndkun;
                }else if (datakun[0].Equals("NoteNum"))
                {
                    //ndkun = Notes[seq];
                    ndkun.pitchStep = noteNum2step(datakun[1]);
                    ndkun.pitchOctave = noteNum2octave(datakun[1]);
                    ndkun.pitchAlter = noteNum2alter(datakun[1]);
                }else if (dtseekkun.IsMatch(datakun[0])){
                    Notes.Add(ndkun);
                    ndkun = newnotes();
                    seq++;
                    
                }
            }
            float measurelen = divisions * beats / (beatType / 4);
            uniqR(ref Notes);
            setTie(ref Notes,measurelen);
            setTuplet(ref Notes);

        }
        #region いろいろな合成
        /// <summary>
        /// 連続する休符を結合します。
        /// </summary>
        /// <param name="nkun">結合するnotes list</param>
        private static void uniqR(ref List<notes> nkun)
        {
            notes nkunbefore;
            for(int i=1;i < nkun.Count; i++)
            {
                redokun:
                if(nkun[i-1].lyricText.Equals("R") && nkun[i].lyricText.Equals("R"))
                {
                    nkunbefore = nkun[i - 1];
                    nkunbefore.duration = nkun[i - 1].duration + nkun[i].duration;
                    nkun[i - 1] = nkunbefore;
                    nkun.RemoveAt(i);
                    goto redokun;
                }
            }
        }
        /// <summary>
        /// 3連符をまとめる
        /// </summary>
        /// <param name="nkun">まとめるnotesのlist</param>
        private static void setTuplet(ref List<notes> nkun)
        {
            int tuplet = 0;
            notes nkun_edit1;
            notes nkun_edit2;
            notes nkun_edit3;
            for (int i=0;i < nkun.Count; i++)
            {
                if(!nkun[i].time_modification.Equals("0"))
                {
                    tuplet++;
                    if(tuplet == 3)
                    {
                        nkun_edit1 = nkun[i-2];
                        nkun_edit2 = nkun[i - 1];
                        nkun_edit3 = nkun[i];
                        nkun_edit1.time_modification = "begin";
                        nkun_edit2.time_modification = "continue";
                        nkun_edit3.time_modification = "end";
                        nkun[i - 2] = nkun_edit1;
                        nkun[i - 1] = nkun_edit2;
                        nkun[i] = nkun_edit3;
                        tuplet = 0;

                    }
                }
                else
                {
                    tuplet = 0;
                }
            }
        }
        /// <summary>
        /// 小節をまたぐ音符があれば分解してタイでつなぎます。
        /// </summary>
        /// <param name="nkun">つなぐnotesのlist</param>
        /// <param name="measureLength"></param>
        private void setTie(ref List<notes> nkun,float measureLength)
        {
            float len = 0;
            notes editnkun;
            notes editnewnkun;

            for (int i=0; i< nkun.Count; i++)
            {
                redokun:
                editnkun = nkun[i];
                if (len + nkun[i].duration > measureLength)
                {
                    editnewnkun = nkun[i];
                    if (!editnewnkun.lyricText.Equals("R"))
                    {
                        if (nkun[i].tie.Equals(""))
                        {
                            
                            editnkun.tie = "start";
                            editnewnkun.tie = "stop";
                            nkun[i] = editnkun;
                        }else if (nkun[i].tie.Equals("stop"))
                        {
                            editnkun.tie = "inter";
                            nkun[i] = editnkun;
                            editnewnkun.tie = "stop";
                        }
                    }
                    float dur0 = measureLength - len;
                    editnkun.duration = dur0;
                    length2note(ref editnkun);
                    nkun[i] = editnkun;
                    editnewnkun.duration = editnewnkun.duration - dur0;
                    length2note(ref editnewnkun);
                    if (!editnewnkun.lyricText.Equals("R"))
                    {
                        editnewnkun.lyricText = "ー";
                    }
                    nkun.Insert(i + 1, editnewnkun);
                    goto redokun;
                }
                else
                {
                    int l = 0;
                    for(l=0;l< lengthList.Length; l++)
                    {
                        if (nkun[i].duration >= lengthList[l]) break;
                    }
                    if(l < lengthList.Length && nkun[i].duration != lengthList[l])
                    {
                        editnkun = nkun[i];
                        editnewnkun = nkun[i];
                        if (editnkun.tie.Equals(""))
                        {
                            editnkun.tie = "start";
                            editnewnkun.tie = "stop";
                            nkun[i] = editnkun;
                        }else if (nkun[i].tie.Equals("stop"))
                        {
                            editnkun.tie = "inter";
                            editnewnkun.tie = "stop";
                            nkun[i] = editnkun;
                        }
                        float dur0 = lengthList[l];
                        editnkun = nkun[i];
                        editnkun.duration = dur0;
                        length2note(ref editnkun);
                        nkun[i] = editnkun;
                        editnewnkun.duration = editnewnkun.duration - dur0;
                        length2note(ref editnewnkun);
                        editnewnkun.lyricText = "ー";
                        nkun.Insert(i + 1, editnewnkun);
                        goto redokun;
                    }
                    else
                    {
                        len = (len + nkun[i].duration) % measureLength;
                    }

                }
            }
            if(len < measureLength)
            {
                editnewnkun = nkun[nkun.Count -1];
                editnewnkun.lyricText = "R";
                editnewnkun.duration = measureLength - len;
                length2note(ref editnewnkun);
                nkun.Add(editnewnkun);
            }
        }
        #endregion
        #region ノート to key名
        /// <summary>
        /// UTAU ustのノート番号をキー名に変換する
        /// </summary>
        /// <param name="note_num">ノード番号</param>
        /// <returns>キー名</returns>
        private static char noteNum2step(int note_num)
        {
            char[] keylist = new char[] { 'C','C','D','D','E','F','F','G','G','A','A','B'};
            return keylist[note_num % 12]; //noteNum=24 のとき C1
        }
        /// <summary>
        /// UTAU ustのノート番号をキー名に変換する
        /// </summary>
        /// <param name="note_num">ノード番号</param>
        /// <returns>キー名</returns>
        private static char noteNum2step(string note_num)
        {
            return noteNum2step(int.Parse(note_num));
        }
        /// <summary>
        /// UTAU ustのノート番号をオクターブ番号に変換する
        /// </summary>
        /// <param name="noteNum">ノート番号</param>
        /// <returns>オクターブ番号</returns>
        private static int noteNum2octave(string noteNum)
        {
            return noteNum2octave(int.Parse(noteNum));
        }
        /// <summary>
        /// UTAU ustのノート番号をオクターブ番号に変換する
        /// </summary>
        /// <param name="noteNum">ノート番号</param>
        /// <returns>オクターブ番号</returns>
        private static int noteNum2octave(int noteNum)
        {
            return (int)(noteNum / 12 - 1); //noteNum=24 のとき C1
        }
        /// <summary>
        /// UTAU ustのノート番号を＃に変換する
        /// </summary>
        /// <param name="noteNum">ノート番号</param>
        /// <returns> true = #あり, false = #なし</returns>
        private static bool noteNum2alter(int noteNum)
        {
            bool[] alterList = new bool[]
            {
                false,true,false,true,false,false,true,false,true,false,true,false
            };
            return alterList[noteNum % 12];
        }
        /// <summary>
        /// UTAU ustのノート番号を＃に変換する
        /// </summary>
        /// <param name="noteNum">ノート番号</param>
        /// <returns> true = #あり, false = #なし</returns>
        private static bool noteNum2alter(string noteNum)
        {
            return noteNum2alter(int.Parse(noteNum));
        }
        #endregion
        #region lenght2note
        /// <summary>
        /// type,dot,time_modificationを生成
        /// </summary>
        /// <param name="nkun">note君(書き換えられます。)</param>
        private void length2note(ref notes nkun)
        {
            float lenght = nkun.duration;
            string bufkun = "";
            for(int i=0;i < lengthList.Length; i++)
            {
                if(lenght >= lengthList[i])
                {
                    if (lenght2_note_D_regex.IsMatch(typeList[i]))
                    {
                        nkun.type = lenght2_note_D_regex.Match(typeList[i]).Value;
                        string tmp = typeList[i];
                        bufkun = lenght2_note_D_regex.Match(typeList[i]).Value;
                        tmp = Regex.Replace(tmp, @"^.+?(D+)$", bufkun.Replace(lenght2_note_D_regex.Match(typeList[i]).Value.Replace("D",""),""));

                        nkun.dot = tmp.Length;
                        nkun.time_modification = "0";
                    }else if (lenght2_note_T_regex.IsMatch(typeList[i])){
                        nkun.type = lenght2_note_T_regex.Match(typeList[i]).Value;
                        nkun.dot = 0;
                        nkun.time_modification = "1";

                    }
                    else
                    {
                        nkun.type = typeList[i];
                        nkun.dot = 0;
                        nkun.time_modification = "0";
                    }
                    return;
                }
            }
            nkun.type = "whole";
            nkun.dot = 0;
            nkun.time_modification = "0";
        }
        #endregion
        #region ノート君
        private notes newnotes()
        {
            return new notes
            {
                duration = 0,
                pitchStep = 'C',
                pitchOctave = 4,
                pitchAlter = false,
                type = "whole",
                lyricText = "",
                dot = 0,
                tie = ""
            };
        }
        #endregion
        private int quantize(int val)
        {
            return (((val + 5) / 10) * 10);
        }
        /// <summary>
        /// NoteをXMLに突っ込む
        /// </summary>
        /// <param name="xelem">挿入♂されるXElement君</param>
        /// <param name="nkun">突っ込むノート君</param>
        /// <param name="dotnext">判定用</param>
        /// <param name="tienext">判定用</param>
        private void insertNote(ref XElement xelem,notes nkun,ref bool dotnext,ref bool tienext)
        {
            string pitchalterkun;
            if (nkun.pitchAlter)
            {
                pitchalterkun = "1";
            }
            else pitchalterkun = "0";
            XElement childeditelement = new XElement("measure");

            if (init_one == false)
            {
                IEnumerable<XElement> address =
                     from el in xelem.Elements("measure")
                       where (string)el.Attribute("number") == "1"
                select el;
                foreach (XElement el in address)
                    childeditelement = el;
                childeditelement.Add(new XElement("pitch", new XElement("step", nkun.pitchStep.ToString())));

                childeditelement.Add(new XElement("octave", nkun.pitchOctave.ToString()), new XElement("alter", pitchalterkun));
                childeditelement.Add(new XElement("duration", ((int)nkun.duration).ToString()));
                childeditelement.Add(new XElement("type", nkun.type));
                childeditelement.Add(new XElement("voice", "1"));
                childeditelement.Add(new XElement("staff", "1"));


                init_one = true;
            }
            else
            {

                childeditelement = new XElement("measure", new XAttribute("number", xelem.Elements("measure").Count() + 1),
                new XElement("pitch", new XElement("step", nkun.pitchStep.ToString()),
                new XElement("octave", nkun.pitchOctave.ToString()), new XElement("alter", pitchalterkun)),
                new XElement("duration", ((int)nkun.duration).ToString()),
                new XElement("type", nkun.type),
                new XElement("voice", "1"),
                new XElement("staff", "1"));
            }
            if (dotnext)
            {
                childeditelement.Add(new XElement("dot"));
                dotnext = false;
            }
            if (tienext)
            {
                childeditelement.Add(new XElement("tie", new XAttribute("type", "start")));
                childeditelement.Add(new XElement("notations", new XElement("tied", new XAttribute("type", "start"))));

                tienext = false;
            }
            if (nkun.dot > 0)
            childeditelement.Add(new XElement("dot"));
            if(nkun.dot >= 2)
            {
                dotnext = true;
            }
            if (nkun.tie.Equals("inter"))
            {
                childeditelement.Add(new XElement("tie", new XAttribute("type", "stop")));
                childeditelement.Add(new XElement("notations",new XElement("tied",new XAttribute("type", "stop"))));
                tienext = true;
            }else if (!nkun.tie.Equals(""))
            {
                childeditelement.Add(new XElement("tie", new XAttribute("type", nkun.tie)));
                childeditelement.Add(new XElement("notations", new XElement("tied", new XAttribute("type", nkun.tie))));
            }
            if (!nkun.lyricText.Equals("R"))
            {
                childeditelement.Add(new XElement("lyric", new XAttribute("default-y", "-77"),new XElement("text",nkun.lyricText)));
            }
            else
            {
                childeditelement.Add(new XElement("rest"));
                /*childeditelement.Add(new XElement("pitch", new XElement("step", "A")));
                childeditelement.Add(new XElement("pitch", new XElement("octave", "4")));
                */
                childeditelement.Element("pitch").Element("step").Value = "A";
                childeditelement.Element("pitch").Element("octave").Value = "4";

            }
            if (!nkun.time_modification.Equals("0"))
            {
                childeditelement.Add(new XElement("time-modification"));
                childeditelement.Element("time-modification").Add(new XElement("actual-notes", "3"));
                childeditelement.Element("time-modification").Add(new XElement("normal-notes", "2"));
                if (nkun.time_modification.Equals("begin"))
                {
                    childeditelement.Add(new XElement("beam", new XAttribute("number", "1"), "begin"));
                    if (childeditelement.Element("notations") == null)
                    {
                        childeditelement.Add(new XElement("notations"));
                    }
                    childeditelement.Element("notations").Add(new XElement("tuplet", new XAttribute("type", "start"),new XAttribute("bracket","no")));
                    childeditelement.Element("notations").Add(new XElement("tuplet"));
                }else if (nkun.time_modification.Equals("continue"))
                {
                    childeditelement.Add(new XElement("beam", new XAttribute("number", "1"), "continue"));

                }else if (nkun.time_modification.Equals("end"))
                {
                    childeditelement.Add(new XElement("beam", new XAttribute("number", "1"), "end"));
                    if (childeditelement.Element("notations") == null)
                    {
                        childeditelement.Add(new XElement("notations"));
                    }
                    childeditelement.Element("notations").Add(new XElement("tuplet", new XAttribute("type", "stop")));
                    childeditelement.Element("notations").Add(new XElement("tuplet"));
                }

            }
            xelem.Add(childeditelement);



        }
        /// <summary>
        /// XMLに出力します。
        /// </summary>
        /// <param name="stream">出力先ストリーム</param>
        public void Write_XML(Stream stream)
        {

            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream basexml_res = assembly.GetManifestResourceStream("UST_MUSICXML_LIB.Resources.XML_BASE.txt");
            XDocument xml = XDocument.Load(basexml_res);
            XElement enc = xml.Element("score-partwise").Element("identification").Element("encoding");
            enc.Element("encoding-date").Value = DateTime.Now.ToString("yyyy-MM-dd");
            enc.Element("software").Value = APPNAME;
            float measurelen = divisions * beats / (beatType / 4);
            XElement measure = xml.Element("score-partwise").Element("part").Element("measure");
            measure.Element("direction").Element("sound").Attribute("tempo").Value = ((int)tempo).ToString();
            measure.Element("attributes").Element("divisions").Value = divisions.ToString();
            measure.Element("attributes").Element("time").Element("beats").Value = beats.ToString();
            measure.Element("attributes").Element("time").Element("beat-type").Value = beatType.ToString();
            XElement score = xml.Element("score-partwise").Element("part");
            init_one = false;
            bool tien = false;
            bool doen = false;
            foreach(notes nkun in Notes)
            {
                insertNote(ref score, nkun,ref doen,ref tien);
            }
            xml.Save(stream);

        }
    }
}

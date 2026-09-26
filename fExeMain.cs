#define Auto

using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml.Serialization;
using static ExeMain.fExeMain;
using static ExeMain.mLink;
using static OpenTK.Graphics.OpenGL.GL;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.LinkLabel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ExeMain
{
    public partial class fExeMain : Form
    {
        mLink mLNK;

        private System.Windows.Forms.Timer tm20;

        public uint[] tmtbl = new uint[] { 0, 0, 0, 0, 0 };

        public fExeMain()
        {
            InitializeComponent();

            ReadXml();

            comboBoxGate.SelectedIndex = InfXml.GateSel;
            numericUpDownAngle.Value = InfXml.numericUpDownAngle;
            comboBoxTarget.SelectedIndex = InfXml.Target;
            numericUpDownV1.Minimum = numericUpDownV2.Minimum = numericUpDownV3.Minimum = 0;
            numericUpDownV1.Maximum = numericUpDownV2.Maximum = numericUpDownV3.Maximum = 99;
            numericUpDownV1.Value = InfXml.numericUpDownV1;
            numericUpDownV2.Value = InfXml.numericUpDownV2;
            numericUpDownV3.Value = InfXml.numericUpDownV3;

            if ((int)numericUpDownV1.Value >= Program.bits) numericUpDownV1.Value = 0;
            if ((int)numericUpDownV2.Value >= Program.bits) numericUpDownV2.Value = 0;
            if ((int)numericUpDownV3.Value >= Program.bits) numericUpDownV3.Value = 0;
            numericUpDownV1.Maximum = numericUpDownV2.Maximum = numericUpDownV3.Maximum = Program.bits - 1;

            if (Program.bits <= 2)
            {
                numericUpDownV3.Value = 0;
                numericUpDownV3.Visible = false;
            }

            //---

            mLNK = new mLink();
            mLNK.Init();
            InitTimer20ms();
        }

        private void InitTimer20ms()
        {
            tm20 = new System.Windows.Forms.Timer();
            tm20.Interval = 20;
            tm20.Tick += sftim_Tick;
            tm20.Start();
        }

        private void sftim_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < tmtbl.Length; i++)
            {
                if (tmtbl[i] > 0) tmtbl[i]--;
            }

            int dead = mLNK.ReadInt((int)mLinkDef.bits);
            if (dead <= 0) this.Close();

            if (!bNgUp)
            {
                exeCOMstack();
                if (bCheck) checkLatest();
                if (bComplex && tmtbl[1] == 0)
                {
                    bComplex = false;
                    ComplexEdit(radioButtonChild.Checked);

                    using (var dlg = new fComplexNumber(sComlex))
                    {
                        if (!radioButtonChild.Checked)
                            dlg.Name = "Child complex number";
                        else
                            dlg.Name = "Child history";
                        dlg.ShowDialog(this);
                    }
                }
            }
        }

        bool bCheck = false;
        int CheckSts = 0;
        int kind = 0;
        List<Vector3d>[] v3LatestAll = new List<Vector3d>[2];
        uint edge = 0;

        /// <summary>
        /// X/Y/Z/H/Rn 指令発行で呼ぶ
        /// </summary>
        private void exeGate(int _kind)
        {
            kind = _kind;
            CheckSts = 0;
            bCheck = true;
        }

        /// <summary>
        /// 全ての子の velatest を読み込む
        /// </summary>
        /// <param name="no"></param>
        private void Read_v3Latest(int no)
        {
            Vector3d v3 = new Vector3d();

            v3LatestAll[no] = new List<Vector3d>();
            int adr = (int)mLinkDef.v3Latest;
            for (int i = 0; i < Program.bits; i++)
            {
                v3 = new Vector3d();
                v3.X = mLNK.ReadDouble(adr);
                v3.Y = mLNK.ReadDouble(adr + 8);
                v3.Z = mLNK.ReadDouble(adr + 16);
                adr += 24;
                v3LatestAll[no].Add(v3);
            }
        }

        int noAlt = 0;

        /// <summary>
        /// check v3Latest same 2 times
        /// </summary>
        private void checkLatest()
        {
            switch (CheckSts)
            {
                case 0: //--- same check start
                    noAlt = 0;
                    edge = 0;
                    Read_v3Latest(noAlt); noAlt = (noAlt + 1) & 1;
                    CheckSts = 1;
                    break;
                case 1: //--- Comparison
                    Read_v3Latest(noAlt); noAlt = (noAlt + 1) & 1;
                    edge <<= 1;
                    if (v3LatestAll[0].SequenceEqual(v3LatestAll[1])) edge |= 1;
                    //--- check on edge timing
                    if ((edge & 7) == 3)
                    {   // same 2 times
                        //--- ここで風向きを計算する ... XYZ合成
                        // ▼ 風向き計算（例：全ビットのXYZを合成）
                        Vector3d wind = new Vector3d(0, 0, 0);
                        for (int i = 0; i < Program.bits; i++)
                        {
                            wind.X += v3LatestAll[0][i].X;
                            wind.Y += v3LatestAll[0][i].Y;
                            wind.Z += v3LatestAll[0][i].Z;
                        }
                        
                        // 正規化（向きだけ欲しい場合）
                        // ▼ wind が「親が使うべき風向き（XYZ）」
                        double len = Math.Sqrt(wind.X * wind.X + wind.Y * wind.Y + wind.Z * wind.Z);
                        if (len > 0)
                        {
                            wind.X /= len;
                            wind.Y /= len;
                            wind.Z /= len;
                        }

                        // Rn の回転角度 angle を決める
                        double angle = 0.0;

                        switch (kind)
                        {   // 要因別
                            case 1: // X
                            case 2: // Y
                            case 3: // Z
                                //angle = Math.PI;
                                angle = 180.0;
                                break;
                            case 4: // H
                                //angle = Math.PI / 2.0;
                                angle = 90.0;
                                break;
                            case 5: // Rx(),Ry(),Rz()
                                angle = (double)InfXml.numericUpDownAngle;
                                break;
                            case 6: // Diaplay reset
                                angle = 0.0;
                                break;
                        }
                        
                        for (int i = 0; i < Program.bits; i++)
                        {
                            // 全ての子にRn()を送る
                            int adr = childBase[i];
                            mLNK.WriteDouble(adr + (int)ChildDetail.nAxis, wind.X);
                            mLNK.WriteDouble(adr + (int)ChildDetail.nAxis + 8, wind.Y);
                            mLNK.WriteDouble(adr + (int)ChildDetail.nAxis + 16, wind.Z);

                            mLNK.WriteDouble(adr + (int)ChildDetail.RnAngle, angle);
                            toCOMstack((i + 1) * 100 + 7);  // to chile no.=Tno command=7
                        }

                        bCheck = false;
                    }
                    break;
            }
        }

        private bool bShown = false;

        private void fExeMain_Shown(object sender, EventArgs e)
        {
            mLNK.WriteInt((int)mLinkDef.bits, Program.bits);

            toCOMstack(1);    // command stack no.1

            bShown = true;
        }

        private int[] comStack = new int[128];
        private int setno = 0;
        private int getno = 0;

        private void toCOMstack(int cmd)
        {
            comStack[setno] = cmd;
            setno += 1; setno &= comStack.Length - 1;
        }

        bool bNgUp = false;
        Action DownProcess = () =>
        {
            MessageBox.Show("--- main ➡ Failed to startup child.");
            Application.Exit();
        };
        Action CommandDown = () =>
        {
            MessageBox.Show("--- main ➡ Failed to command execution.");
            Application.Exit();
        };

        int cmdStage = 0;
        int cmd = 0;

        int stage = 0;
        int SetNum = 0;
        int cmdDetail = 0;

        System.Diagnostics.Process[] hProcess = new Process[5];

        private void exeCOMstack()
        {
            switch (cmdStage)
            {
                case 0: // lookup commsnd que.
                    if (getno == setno) break;

                    cmd = comStack[getno];
                    switch (cmd)
                    {
                        case 1: // child .exe go
                            string exeName = string.Empty;
#if Auto
                            List<string> DebugName = new List<string>()
                            {
                                @"C:\\Users\ToshioUramoto\Desktop\QuantumN\QuantumViewerN\Exe1\bin\Debug\Exe1.exe",
                                @"C:\\Users\ToshioUramoto\Desktop\QuantumN\QuantumViewerN\Exe2\bin\Debug\Exe2.exe",
                                @"C:\\Users\ToshioUramoto\Desktop\QuantumN\QuantumViewerN\Exe3\bin\Debug\Exe3.exe",
                                @"C:\\Users\ToshioUramoto\Desktop\QuantumN\QuantumViewerN\Exe4\bin\Debug\Exe4.exe",
                                @"C:\\Users\ToshioUramoto\Desktop\QuantumN\QuantumViewerN\Exe5\bin\Debug\Exe5.exe"
                            };
#endif
                            for (int i = 0; i < Program.bits; i++)
                            {
#if Auto
                                hProcess[i] = Process.Start(DebugName[i]);
#else
                                exeName = "Exe" + (i + 1).ToString("0");
                                //hProcess[i] = Process.Start(exeName);
#endif
                            }
                            break;
                        case 2: // 
                            break;

                    }

                    //test
                    //tmtbl[0] = 5000 / 20;
                    tmtbl[0] = 60000 / 20;
                    cmdStage = 1;
                    break;
                case 1: // child accept % comleted wait
                    switch (cmd)
                    {
                        case 1: // check child .exe all go
                            if (tmtbl[0] > 0)
                            {   // 5秒待ち
                                bool bNG = false;

                                for (int i = 0; i < Program.bits; i++)
                                {
                                    if (mLNK.ReadInt((int)mLinkDef.StartupEXE + 4 * i) != 1)
                                    {
                                        bNG = true;
                                        break;
                                    }
                                }

                                if (!bNG)
                                {   // all child 1 responce
                                    Point oya = this.Location;
                                    mLNK.WriteInt((int)mLinkDef.Left, oya.X);
                                    mLNK.WriteInt((int)mLinkDef.Top, oya.Y);
                                    Size sz = this.Size;
                                    mLNK.WriteInt((int)mLinkDef.SizeX, sz.Width);
                                    mLNK.WriteInt((int)mLinkDef.SizeY, sz.Height);
                                    cmd = 2;
                                }
                            }
                            else
                            {   // time out
                                bNgUp = true;
                                DownProcess();
                            }
                            break;
                        case 2: // 子EXE全部に対するRUN指示
                            for (int i = 0; i < Program.bits; i++)
                            {
                                // RUN指示
                                mLNK.WriteInt((int)mLinkDef.StartupEXE + 4 * i, 2);
                            }

                            getno += 1; getno &= comStack.Length - 1;
                            cmdStage = 2;

                            buttonLayout_Click(null, null);
                            break;
                    }
                    break;

                //------------------------------------------------------- 通常 command 処理

                case 2: //--- get command
                    if (getno == setno) break;

                    cmd = comStack[getno];
                    cmdDetail = cmd % 100;
                    SetNum = cmd / 100 - 1;
                    stage = 0;
                    cmdStage = 3;
                    break;
                case 3: //--- execute command
                    switch (stage)
                    {
                        case 0: //--- set command
                            mLNK.WriteInt(((int)mLinkDef.command + 4 * SetNum), cmdDetail);
                            //if (SetNum == 0 && (cmdDetail >= 1 && cmdDetail <= 6)) exeGate(cmdDetail);
                            if (cmdDetail >= 1 && cmdDetail <= 6) exeGate(cmdDetail);
                            tmtbl[0] = 3000 / 20;
                            stage = 1;
                            break;
                        case 1: //--- search responce
                            if (tmtbl[0] > 0)
                            {   // check responce time
                                if (mLNK.ReadInt(((int)mLinkDef.accept + 4 * SetNum)) == cmdDetail)
                                {   // exist responce
                                    // command off
                                    mLNK.WriteInt(((int)mLinkDef.command + 4 * SetNum), 0);
                                    stage = 2;
                                    break;
                                }
                            }
                            else
                            {   // time out
                                CommandDown();
                                stage = 3;
                            }
                            break;
                        case 2: //--- check responce
                            if (tmtbl[0] > 0)
                            {   // check responce off
                                if (mLNK.ReadInt((int)mLinkDef.accept + 4 * SetNum) == 0)
                                {
                                    stage = 0;
                                    getno += 1; getno &= comStack.Length - 1;
                                    cmdStage = 2;
                                    break;
                                }
                            }
                            else
                            {   // time out
                                CommandDown();
                                stage = 3;
                            }
                            break;
                        case 3: //--- error
                            break;
                    }
                    break;
            }
        }

        private void fExeMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            for (int i = 0; i < Program.bits; i++)
            {
#if Auto
                // 終了しなかった場合は強制終了する
                if (!hProcess[i].CloseMainWindow()) { hProcess[i].Kill(); }

                hProcess[i].Close();
                hProcess[i].Dispose();
#endif
            }

            WriteXml();
        }

        #region ProcessDialogKey =======================================================================================

        /// <summary>
        /// Key process
        /// </summary>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessDialogKey(Keys keyData)
        {
            Keys code = keyData & Keys.KeyCode;

            switch (code)
            {
                case Keys.Escape:
                    this.Close();
                    break;

                default:
                    return base.ProcessDialogKey(keyData);
            }

            return true;
        }

        #endregion ProcessDialogKey ====================================================================================

        #region OnPaint ================================================================================================

        /// <summary>
        /// GLControlの窓を□で囲む
        /// </summary>
        /// <param name="Thick"></param>
        /// <param name="col"></param>
        /// <param name="csgl"></param>
        /// <param name="pea"></param>
        public static void glRectangle(int Thick, Color col, GLControl gl, PaintEventArgs pea)
        {
            Graphics g = pea.Graphics;
            Pen p = new Pen(col, Thick);

            Size sz = gl.ClientSize;
            Point[] p4 = {
                            new Point(gl.Bounds.Location.X - Thick, gl.Bounds.Location.Y - Thick),
                            new Point(gl.Bounds.Location.X + sz.Width, gl.Bounds.Location.Y - Thick),
                            new Point(gl.Bounds.Location.X + sz.Width, gl.Bounds.Location.Y + sz.Height),
                            new Point(gl.Bounds.Location.X - Thick, gl.Bounds.Location.Y + sz.Height) };

            g.DrawPolygon(p, p4);

            p.Dispose();
        }

        /// <summary>
        /// Panel領域を□で囲む
        /// </summary>
        /// <param name="Thick"></param>
        /// <param name="col"></param>
        /// <param name="sz"></param>
        /// <param name="pt"></param>
        /// <param name="pea"></param>
        public static void panelRectangle(int Thick, Color col, Panel pnl, PaintEventArgs pea)
        {
            Graphics g = pea.Graphics;
            Pen p = new Pen(col, Thick);

            Size sz = pnl.ClientSize;
            Point[] p4 = {
                            new Point(pnl.Location.X - Thick, pnl.Location.Y - Thick),
                            new Point(pnl.Location.X + sz.Width, pnl.Location.Y - Thick),
                            new Point(pnl.Location.X + sz.Width, pnl.Location.Y + sz.Height),
                            new Point(pnl.Location.X - Thick, pnl.Location.Y + sz.Height) };

            g.DrawPolygon(p, p4);

            p.Dispose();
        }

        /// <summary>
        /// Group窓を□で囲む
        /// </summary>
        /// <param name="Thick"></param>
        /// <param name="col"></param>
        /// <param name="GB"></param>
        /// <param name="pea"></param>
        public static void GroupBoxRectangle(int Thick, Color col, GroupBox GB, PaintEventArgs pea)
        {
            Graphics g = pea.Graphics;
            Pen p = new Pen(col, Thick);

            Size sz = GB.ClientSize;
            Point[] p4 = {
                            new Point(GB.Location.X - Thick, GB.Location.Y - Thick),
                            new Point(GB.Location.X + sz.Width, GB.Location.Y - Thick),
                            new Point(GB.Location.X + sz.Width, GB.Location.Y + sz.Height),
                            new Point(GB.Location.X - Thick, GB.Location.Y + sz.Height) };

            g.DrawPolygon(p, p4);

            p.Dispose();
        }

        /// <summary>
        /// OnPaint
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            // GLControl窓を□で囲む
            //glRectangle(1, Color.DarkGreen, this.glControlMain, e);

            // Panel窓を□で囲む
            //panelRectangle(1, Color.DarkGreen, panelAlphaBeta, e);

            // GroupBoxR窓を□で囲む
            //GroupBoxRectangle(1, Color.Blue, this.groupBox_Initialization, e);

            // Call the OnPaint method of the base class.
            base.OnPaint(e);

            // フォームを□で囲む
            e.Graphics.DrawRectangle(Pens.DarkGreen, 0, 0, this.Width - 1, this.Height - 1);
        }

        #endregion OnPaint =============================================================================================

        #region InfXml ==================================================================================================

        /// <summary>
        /// class "AP .xml contents"
        /// </summary>
        public infXml InfXml = new infXml();

        /// <summary>
        /// Write ?.Xml
        /// </summary>
        public void WriteXml()
        {
            string exeName = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
            string pathFname = Directory.GetCurrentDirectory() + @"\" + exeName + ".xml";

            File.Delete(pathFname);

            XmlSerializer serializer = new XmlSerializer(typeof(infXml));

            using (FileStream fs = new FileStream(pathFname, FileMode.Create))
            {
                serializer.Serialize(fs, (object)InfXml);
            }
        }

        /// <summary>
        /// Read ?.Xml
        /// </summary>
        /// <returns></returns>
        public bool ReadXml()
        {
            string exeName = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
            string pathFname = Directory.GetCurrentDirectory() + @"\" + exeName + ".xml";

            if (!File.Exists(pathFname)) return false;

            XmlSerializer serializer = new XmlSerializer(typeof(infXml));

            using (FileStream fs = new FileStream(pathFname, FileMode.Open))
            {
                InfXml = (infXml)serializer.Deserialize(fs);
            }

            return true;
        }

        /// <summary>
        /// AP .xml contents
        /// </summary>
        public class infXml
        {
            /// <summary>
            /// Gate selection
            /// </summary>
            public int GateSel = 0;

            /// <summary>
            /// numericUpDownAngle.Value
            /// </summary>
            public decimal numericUpDownAngle = 0;

            /// <summary>
            /// Target selection
            /// </summary>
            public int Target = 0;

            /// <summary>
            /// Child view
            /// </summary>
            public decimal numericUpDownV1 = 0;
            public decimal numericUpDownV2 = 1;
            public decimal numericUpDownV3 = 2;
        }

        #endregion InfXml ===============================================================================================

        #region Child all Hide ==========================================================================================

        // Win32 API のインポート
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindow(IntPtr hWnd);

        // ShowWindow のコマンド
        private const int SW_HIDE = 0;  // 非表示
        private const int SW_SHOW = 5;  // 表示

        private void All_Child_Hide()
        {
            // プロセス取得
            Process[] processes = Process.GetProcesses(); 
            if (processes.Length == 0) return;

            // 末尾が９９迄の数値か検査
            for (int i = 0; i < processes.Length; i ++)
            {
                string sExeName = processes[i].ProcessName;

                if (sExeName.Length > 3 && sExeName.Length < 5)
                {
                    if (int.TryParse(sExeName.Substring(3), out int intV))
                    {
                        // メインウィンドウハンドル取得
                        IntPtr hWnd = processes[i].MainWindowHandle;
                        if (hWnd == IntPtr.Zero || !IsWindow(hWnd)) return;

                        ShowWindow(hWnd, SW_HIDE);
                    }
                }
            }
        }

        #endregion Child all Hide =======================================================================================

        #region form move ==============================================================================================

        bool _dragging = false;
        Point _start;

        private void Panel_MouseDown(object sender, MouseEventArgs e)
        {
            _dragging = true;
            _start = new Point(e.X, e.Y);
        }

        private void Panel_MouseUp(object sender, MouseEventArgs e)
        {
            _dragging = false;
        }

        private void Panel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragging)
            {
                int dx = e.X - _start.X;
                int dy = e.Y - _start.Y;

                this.Location = new Point(this.Left + dx, this.Top + dy);
            }
        }

        #endregion form move ===========================================================================================

        private void comboBoxGate_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = InfXml.GateSel = comboBoxGate.SelectedIndex;

            switch (idx)
            {
                case 0: // 空白
                    break;
                case 1: // X
                    break;
                case 2: // Y
                    break;
                case 3: // Z
                    break;
                case 4: // H
                    break;
                case 5: // Rx
                    break;
                case 6: // Ry
                    break;
                case 7: // Rz
                    break;
            }

            if ((idx >= 5) && (idx <= 7))
                numericUpDownAngle.Visible = true;
            else
                numericUpDownAngle.Visible = false;
        }

        private void numericUpDownAngle_ValueChanged(object sender, EventArgs e)
        {
            InfXml.numericUpDownAngle = numericUpDownAngle.Value;
        }

        private void comboBoxTarget_SelectedIndexChanged(object sender, EventArgs e)
        {
            InfXml.Target = comboBoxTarget.SelectedIndex;
        }

        private void numericUpDownV1_ValueChanged(object sender, EventArgs e)
        {
            InfXml.numericUpDownV1 = numericUpDownV1.Value;
        }

        private void numericUpDownV2_ValueChanged(object sender, EventArgs e)
        {
            InfXml.numericUpDownV2 = numericUpDownV2.Value;
        }

        private void numericUpDownV3_ValueChanged(object sender, EventArgs e)
        {
            InfXml.numericUpDownV3 = numericUpDownV3.Value;
        }

        List<int> childBase = new List<int>()
        {
            (int)ChildArea.Child1,
            (int)ChildArea.Child2,
            (int)ChildArea.Child3,
            (int)ChildArea.Child4,
            (int)ChildArea.Child5
        };

        private void buttonApply_Click(object sender, EventArgs e)
        {
            int Gno = comboBoxGate.SelectedIndex;
            if (Gno < 1)
            {
                MessageBox.Show("Please choose the right gate.");
                return;
            }

            int Tno = comboBoxTarget.SelectedIndex;
            if (Tno >= Program.bits)
            {
                MessageBox.Show("Please choose the right target.");
                return;
            }

            switch (Gno)
            {
                case 1: // X
                    toCOMstack((Tno + 1) * 100 + 1);    // to chile no.=Tno command=1
                    break;
                case 2: // Y
                    toCOMstack((Tno + 1) * 100 + 2);    // to chile no.=Tno command=2
                    break;
                case 3: // Z
                    toCOMstack((Tno + 1) * 100 + 3);    // to chile no.=Tno command=3
                    break;
                case 4: // H
                    toCOMstack((Tno + 1) * 100 + 4);    // to chile no.=Tno command=4
                    break;
                case 5: // Rx()
                case 6: // Ry()
                case 7: // Rz()
                    Gno -= 5;
                    int adr = childBase[Tno];
                    mLNK.WriteInt(adr + (int)ChildDetail.Gate, Gno);
                    mLNK.WriteFloat(adr + (int)ChildDetail.Angle, (float)numericUpDownAngle.Value);
                    toCOMstack((Tno + 1) * 100 + 5);    // to chile no.=Tno command=5
                    break;
            }

            mLNK.WriteInt((int)mLinkDef.targetChild, InfXml.Target);
        }

        private void buttonLayout_Click(object sender, EventArgs e)
        {
            // all child hide
            All_Child_Hide();

            // Layout decision
            List<int> cn = new List<int>();
            cn.Add((int)numericUpDownV1.Value);
            cn.Add((int)numericUpDownV2.Value);
            if (Program.bits > 2)
                cn.Add((int)numericUpDownV3.Value);
            // 重複を省く
            cn = cn.Distinct().ToList();

            List<int> Lw = new List<int>();
            List<int> Lh = new List<int>();
            for (int i = 0; i < cn.Count; i++)
            {
                Lw.Add(mLNK.ReadInt((int)mLinkDef.childSize + cn[i] * 8));
                Lh.Add(mLNK.ReadInt((int)mLinkDef.childSize + cn[i] * 8 + 4));
            }

            // 横/縦
            int wV = Lw.Sum() + cn.Count;
            int hV = this.Size.Height + Lh.Max() + 2;

            // screen 中央表示
            var ScreenArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            int LeftM = (ScreenArea.Width - wV) / 2;
            int Top = (ScreenArea.Height - hV) / 2;
            int Top2 = Top + this.Height;

            int Xbase = LeftM;
            List<int> cX = new List<int>();
            for (int i = 0; i < cn.Count; i++)
            {
                cX.Add(Xbase);
                Xbase += (Lw[i] + 1);
            }

            // 親 Location
            this.Location = new Point(LeftM, Top);

            // 子 Location = new Point(cX[i], Top2);
            for (int i = 0; i < cn.Count; i++)
            {
                mLNK.WriteInt((int)mLinkDef.orderLocation + cn[i] * 8, cX[i]);
                mLNK.WriteInt((int)mLinkDef.orderLocation + cn[i] * 8 + 4, Top2);
                toCOMstack((cn[i] + 1) * 100 + 99); // to chile no.n command=99
            }
        }

        private void buttonDisplayReset_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < Program.bits; i++)
            {
                toCOMstack((i + 1) * 100 + 6);  // to chile no.=all command=6
            }
        }

        /// <summary>
        /// 全ての子の複素数を取得
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonComplex_Click(object sender, EventArgs e)
        {
            if (bComplex) return;

            if (!radioButtonComplex.Checked)
            {
                toCOMstack((InfXml.Target + 1) * 100 + 97); // to chile All command=97
            }
            else
            {
                for (int i = 0; i < Program.bits; i++)
                {
                    toCOMstack((i + 1) * 100 + 98);         // to chile All command=98
                }
            }
            tmtbl[1] = 10;
            bComplex = true;
        }

        bool bComplex = false;
        public List<string> sComlex = new List<string>();

        private void ComplexEdit(bool bChild)
        {
            int adr = 0;
            sComlex = new List<string>();
            string sEdit = string.Empty;

            if (!bChild)
            {   // Complex
                for (int i = 0; i < Program.bits; i++)
                {
                    adr = (int)mLinkDef.complexNumber + i * 8 * 4;
                    double Ax0Re = mLNK.ReadDouble(adr + 0);
                    double Ax0Im = mLNK.ReadDouble(adr + 8);
                    double Ax1Re = mLNK.ReadDouble(adr + 16);
                    double Ax1Im = mLNK.ReadDouble(adr + 24);
                    //---
                    adr = (int)mLinkDef.v3Latest + i * 8 * 3;
                    double v3LatestX = mLNK.ReadDouble(adr + 0);
                    double v3LatestY = mLNK.ReadDouble(adr + 8);
                    double v3LatestZ = mLNK.ReadDouble(adr + 16);
                    //---
                    sEdit = " α" + i.ToString() + "0 ➡ " +
                             Ax0Re.ToString("F4") + " + " + Ax0Im.ToString("F4") + " * i";
                    sComlex.Add(sEdit);
                    sEdit = " α" + i.ToString() + "1 ➡ " +
                             Ax1Re.ToString("F4") + " + " + Ax1Im.ToString("F4") + " * i";
                    sComlex.Add(sEdit);
                    sEdit = "    " + "X = " + v3LatestX.ToString("F4") + ", " +
                                     "Y = " + v3LatestY.ToString("F4") + ", " +
                                     "Z = " + v3LatestZ.ToString("F4");
                    sComlex.Add(sEdit);
                }
            }
            else
            {   // Child histry
                adr = (int)mLinkDef.OPhistory;
                string sMark = string.Empty;
                int no = 1;

                for (int i = 0; i < 20; i++)
                {
                    // V3from ... double[3]
                    double V3fromX = mLNK.ReadDouble(adr + 0);
                    double V3fromY = mLNK.ReadDouble(adr + 8);
                    double V3fromZ = mLNK.ReadDouble(adr + 16);
                    if (V3fromX == 0 && V3fromY == 0 && V3fromZ == 0) break;
                    // V3to ... double[3]
                    double V3toX = mLNK.ReadDouble(adr + 24);
                    double V3toY= mLNK.ReadDouble(adr + 32);
                    double V3toZ = mLNK.ReadDouble(adr + 40);
                    // type ... int
                    int type = mLNK.ReadInt(adr + 48);

                    if (type < 7)
                    {
                        sMark = " ●(" + no.ToString() + ") ";
                        no += 1;
                    }
                    else sMark = " ▼";
                    // angleRad
                    double angleRad = mLNK.ReadDouble(adr + 52);
                    // Ax0Re ... double ... α(x0)Re
                    // Ax0Im ... double ... α(x0)Im
                    // Ax1Re ... double ... α(x1)Re
                    // Ax1Im ... double ... α(x1)Im
                    double Ax0Re = mLNK.ReadDouble(adr + 60);
                    double Ax0Im = mLNK.ReadDouble(adr + 68);
                    double Ax1Re = mLNK.ReadDouble(adr + 76);
                    double Ax1Im = mLNK.ReadDouble(adr + 84);
                    // θ,φ
                    double theta = mLNK.ReadDouble(adr + 92);
                    double phi = mLNK.ReadDouble(adr + 100);

                    adr += 108;
                    //---
                    sEdit = sMark + "fX = " + V3fromX.ToString("F4") + ", " +
                                    "fY = " + V3fromY.ToString("F4") + ", " +
                                    "fZ = " + V3fromZ.ToString("F4") + " / " +
                                    "tX = " + V3toX.ToString("F4") + ", " +
                                    "tY = " + V3toY.ToString("F4") + ", " +
                                    "tZ = " + V3toZ.ToString("F4");
                    sComlex.Add(sEdit);
                    //---
                    string sDeg = "";
                    if (type > 0) sDeg = "(" + (angleRad / 0.01745329252).ToString("F1") + ")";
                    switch (type)
                    {
                        case 0: // 0:H
                            sEdit = "  type = " + "H";
                            break;
                        case 1: // 1:Rx
                            sEdit = "  type = " + "Rx" + sDeg;
                            break;
                        case 2: // 2:Ry
                            sEdit = "  type = " + "Ry" + sDeg;
                            break;
                        case 3: // 3:Rz
                            sEdit = "  type = " + "Rz" + sDeg;
                            break;
                        case 7: // 7:Rn
                            sEdit = "  type = " + "Rn" + sDeg;
                            break;
                    }
                    sComlex.Add(sEdit);
                    //---
                    sEdit = "   α" + i.ToString() + "0 ➡ " +
                             Ax0Re.ToString("F4") + " + " + Ax0Im.ToString("F4") + " * i";
                    sComlex.Add(sEdit);
                    sEdit = "   α" + i.ToString() + "1 ➡ " +
                             Ax1Re.ToString("F4") + " + " + Ax1Im.ToString("F4") + " * i";
                    sComlex.Add(sEdit);
                    //---
                    sEdit = "  θ = " + theta.ToString("F4") + " rad / " +
                              "φ = " + phi.ToString("F4") + " rad";
                    sComlex.Add(sEdit);
                }
            }
        }

        //--- ↑フォームイベント
    }
}
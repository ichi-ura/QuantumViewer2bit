using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using static System.Windows.Forms.LinkLabel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace Exe1
{
    public partial class fExe1 : Form
    {
        mLink mLNK;

        private System.Windows.Forms.Timer tm20;

        public uint[] tmtbl = new uint[] { 0, 0, 0, 0, 0 };

        private int bits = 0;

        private int ReferNum = 0;

        int[] Ctop = new int[] { (int)ChildArea.Child1, (int)ChildArea.Child2, (int)ChildArea.Child3,
                                 (int)ChildArea.Child4, (int)ChildArea.Child5 };

        int childTop = 0;

        public fExe1()
        {
            InitializeComponent();

            mLNK = new mLink();
            mLNK.Init();

            // 実行ファイルのフルパスを取得
            string exePath = Assembly.GetExecutingAssembly().Location;
            // 実行ファイル名（拡張子含まない）を取得
            string exeName = Path.GetFileNameWithoutExtension(exePath);
            // 0～4
            ReferNum = int.Parse(exeName.Substring(exeName.Length - 1, 1)) - 1;
            // child top memory link $
            childTop = Ctop[ReferNum];

            InitTimer20ms();
        }

        private void InitTimer20ms()
        {
            ReadXml();

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

            if (iRun == 0)
                SyncMain();
            else if (iRun == 2)
                getCommand();
        }

        bool bShown = false;

        private void fExe1_Shown(object sender, EventArgs e)
        {
            // 量子ビット数
            bits = mLNK.ReadInt((int)mLinkDef.bits);

            // 子の画面サイズ情報
            int mAdr = (int)mLinkDef.childSize + (ReferNum * 4 * 2);
            mLNK.WriteInt(mAdr, this.Size.Width);
            mLNK.WriteInt(mAdr + 4, this.Size.Height);

            bShown = true;

            // set initial point(Z)
            //α[x0] = 0 + 0i
            //α[x1] = 1 + 0i
            this.Ax0Re = 1;
            this.Ax0Im = 0;
            this.Ax1Re = 0;
            this.Ax1Im = 0;
            pointSet(1, new Vector3d(0, 0, 1));
        }

        private void fExe1_FormClosed(object sender, FormClosedEventArgs e)
        {
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
                    mLNK.WriteInt((int)mLinkDef.bits, -1);
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
            glRectangle(1, Color.DarkGreen, this.glControlChild, e);

            // Panel窓を□で囲む
            //panelRectangle(1, Color.DarkGreen, panelAlphaBeta, e);

            // GroupBoxR窓を□で囲む
            //GroupBoxRectangle(1, Color.Blue, this.groupBoxAlpha, e);

            // Call the OnPaint method of the base class.
            base.OnPaint(e);

            // フォームを□で囲む
            e.Graphics.DrawRectangle(Pens.DarkGreen, 0, 0, this.Width - 1, this.Height - 1);
        }

        #endregion OnPaint =============================================================================================

        #region form move ==============================================================================================

        bool _dragging = false;
        Point _start;

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            _dragging = true;
            _start = new Point(e.X, e.Y);
        }

        private void Form_MouseUp(object sender, MouseEventArgs e)
        {
            _dragging = false;
        }

        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragging)
            {
                int dx = e.X - _start.X;
                int dy = e.Y - _start.Y;

                this.Location = new Point(this.Left + dx, this.Top + dy);
            }
        }

        #endregion form move ===========================================================================================

        #region GLControl Load/Paint ===================================================================================

        private void SetupProjection()
        {
            glControlChild.MakeCurrent();

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();

            float fovy = MathHelper.DegreesToRadians(45.0f);
            float aspect = glControlChild.Width / (float)glControlChild.Height;
            float zNear = 0.1f;
            float zFar = 100.0f;

            Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(fovy, aspect, zNear, zFar);
            GL.LoadMatrix(ref proj);
        }

        private void Child_Load(object sender, EventArgs e)
        {
            glControlChild.MakeCurrent();

            GL.Viewport(0, 0, glControlChild.Width, glControlChild.Height);
            GL.ClearColor(0f, 0f, 0f, 1f);

            GL.Enable(EnableCap.DepthTest); // 球の奥行きを正しく描くために必須
            SetupProjection();              // 投影行列（あなたの装置に必須）

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
        }

        private void Child_Paint(object sender, PaintEventArgs e)
        {
            if (!bShown) return;

            glControlChild.MakeCurrent();

            GL.Viewport(0, 0, glControlChild.Width, glControlChild.Height);

            GL.ClearColor(0.35f, 0.35f, 0.35f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // --- Projection ---
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            float fovy = (float)MathHelper.DegreesToRadians(45.0f);
            float aspect = glControlChild.Width / (float)glControlChild.Height;
            Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(fovy, aspect, 0.1f, 100f);
            InfXml.projection = proj;
            GL.LoadMatrix(ref proj);

            // --- ModelView ---
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            GL.Translate(0.0f, 0.0f, -5.0f);
            GL.Rotate(InfXml.rot_v, 1.0f, 0.0f, 0.0f);
            GL.Rotate(InfXml.rot_h, 0.0f, 1.0f, 0.0f);
            GL.Scale(InfXml.mag, InfXml.mag, InfXml.mag);

            GL.GetFloat(GetPName.ModelviewMatrix, out InfXml.model);

            float radius = 1.0f;

            // --- 球本体 ---
            int slices = 40;
            int stacks = 40;

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Color4(0.9216f, 0.9686, 1, 0.6f);            // ← α=0.6 の半透明球

            for (int i = 0; i < stacks; i++)
            {
                float lat0 = (float)(Math.PI * (-0.5 + (double)i / stacks));
                float z0 = (float)Math.Sin(lat0);
                float zr0 = (float)Math.Cos(lat0);

                float lat1 = (float)(Math.PI * (-0.5 + (double)(i + 1) / stacks));
                float z1 = (float)Math.Sin(lat1);
                float zr1 = (float)Math.Cos(lat1);

                GL.Begin(PrimitiveType.QuadStrip);

                for (int j = 0; j <= slices; j++)
                {
                    float lng = (float)(2.0 * Math.PI * j / slices);
                    float x = (float)Math.Cos(lng);
                    float y = (float)Math.Sin(lng);

                    GL.Vertex3(x * zr0 * radius, y * zr0 * radius, z0 * radius);
                    GL.Vertex3(x * zr1 * radius, y * zr1 * radius, z1 * radius);
                }

                GL.End();
            }

            GL.LineWidth(0.00001f); // 細くもならない
            drawLatitude();     // 緯度線
            drawLongitude();    // 経度線
            GL.LineWidth(1.0f);

            // --- XYZ 軸（細め・球貫通） ---
            float axisLen = radius * 1.2f;

            GL.LineWidth(2.0f);

            GL.Color3(1.0f, 0.0f, 0.0f);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(-axisLen, 0, 0);
            GL.Vertex3(axisLen, 0, 0);
            GL.End();

            GL.Color3(0.0f, 1.0f, 0.0f);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(0, -axisLen, 0);
            GL.Vertex3(0, axisLen, 0);
            GL.End();

            GL.Color3(0.0f, 0.0f, 1.0f);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(0, 0, -axisLen);
            GL.Vertex3(0, 0, axisLen);
            GL.End();

            //---

            OPhistoryDraw();
            pointDraw();

            glControlChild.SwapBuffers();

            // --- ここから GDI 描画 ---
            Graphics g = e.Graphics;

            DrawText(g, "+X", Project(pX), Color.OrangeRed, 14f);
            DrawText(g, "+Y", Project(pY), Color.LightGreen, 14f);
            DrawText(g, "+Z", Project(pZ), Color.Blue, 14f);

            DrawText(g, "-X", Project(nX), Color.OrangeRed, 12f);
            DrawText(g, "-Y", Project(nY), Color.LightGreen, 12f);
            DrawText(g, "-Z", Project(nZ), Color.Blue, 12f);
        }

        //--- Paint sub

        /// <summary>
        /// 緯度線の描画
        /// </summary>
        void drawLatitude()
        {
            // ---- 状態を保存 ----
            GL.PushAttrib(AttribMask.AllAttribBits);
            GL.PushMatrix();

            GL.Disable(EnableCap.Lighting);
            GL.Color3(1, 1, 1); // なぜか黒にしかならない

            int slices = 24;
            double r = 1.001;

            for (int i = 1; i < slices - 1; i++)
            {
                double phi = Math.PI * i / slices;
                double z = r * Math.Cos(phi);
                double rr = r * Math.Sin(phi);

                GL.Begin(PrimitiveType.LineLoop);
                for (int j = 0; j < slices; j++)
                {
                    double theta = 2.0 * Math.PI * j / slices;
                    double x = rr * Math.Cos(theta);
                    double y = rr * Math.Sin(theta);
                    GL.Vertex3(x, y, z);
                }
                GL.End();
            }

            // ---- 状態を元に戻す ----
            GL.PopMatrix();
            GL.PopAttrib();
        }

        /// <summary>
        /// 経度線の描画
        /// </summary>
        void drawLongitude()
        {
            // ---- 状態を保存 ----
            GL.PushAttrib(AttribMask.AllAttribBits);
            GL.PushMatrix();

            GL.Disable(EnableCap.Lighting);
            GL.Color3(1, 1, 1);  // なぜか黒にしかならない

            int slices = 24;
            double r = 1.001;

            for (int i = 0; i < slices; i++)
            {
                double theta = 2.0 * Math.PI * i / slices;

                GL.Begin(PrimitiveType.LineLoop);
                for (int j = 0; j < slices; j++)
                {
                    double phi = Math.PI * j / (slices - 1);

                    double x = r * Math.Sin(phi) * Math.Cos(theta);
                    double y = r * Math.Sin(phi) * Math.Sin(theta);
                    double z = r * Math.Cos(phi);

                    GL.Vertex3(x, y, z);
                }
                GL.End();
            }

            // ---- 状態を元に戻す ----
            GL.PopMatrix();
            GL.PopAttrib();
        }

        public void DrawText(Graphics g, string text, PointF pt, Color color, float size = 12f)
        {
            using (Font font = new Font("Consolas", size, FontStyle.Regular))
            using (SolidBrush br = new SolidBrush(color))
            {
                g.DrawString(text, font, br, pt);
            }
        }

        //---

        private Point _last;

        private void Child_MouseDown(object sender, MouseEventArgs e)
        {
            _last = e.Location;
        }

        private void Child_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int dx = e.X - _last.X;
                int dy = e.Y - _last.Y;

                // ★ 回転量を累積
                InfXml.rot_h += dx;
                InfXml.rot_v += dy;

                _last = e.Location;

                glControlChild.Invalidate();
            }
        }

        private void Child_MouseWheel(object sender, MouseEventArgs e)
        {
            if (!bShown) return;

            InfXml.mag += e.Delta * 0.001f; // 拡大率の更新

            glControlChild.Invalidate();
        }

        #endregion GLControl Load/Paint ================================================================================

        #region Sphere Axis Name =======================================================================================

        Vector3 pX = new Vector3(1.2f, 0f, 0f);     // +X
        Vector3 pY = new Vector3(0f, 1.2f, 0f);     // +Y
        Vector3 pZ = new Vector3(0f, 0f, 1.2f);     // +Z

        Vector3 nX = new Vector3(-1.2f, 0f, 0f);    // -X
        Vector3 nY = new Vector3(0f, -1.2f, 0f);    // -Y
        Vector3 nZ = new Vector3(0f, 0f, -1.2f);    // -Z

        public PointF Project(Vector3 obj)
        {
            Matrix4 mv = InfXml.model;
            Matrix4 pr = InfXml.projection;

            int w = glControlChild.Width;
            int h = glControlChild.Height;

            Vector4 v = new Vector4(obj, 1f);

            v = Vector4.Transform(v, mv);
            v = Vector4.Transform(v, pr);

            v /= v.W;

            float x = (1 + v.X) * w / 2;
            float y = (1 - v.Y) * h / 2;

            return new PointF(x, y);
        }

        #endregion Sphere Axis Name ====================================================================================

        //--- ↑フォームイベント

        Action<int> DownProcess = num =>
        {
            MessageBox.Show("--- child no.=" + (num + 1).ToString() + " ➡ Failed to ExeMain startup.");
            Application.Exit();
        };

        private int iRun = 0;

        int Sync = 0;

        /// <summary>
        ///  子、立ち上げ同期処理
        /// </summary>
        private void SyncMain()
        {
            switch (Sync)
            {
                case 0: //--- mark = 1
                    if (!bShown) return;

                    // 
                    mLNK.WriteInt(((int)mLinkDef.StartupEXE + 4 * ReferNum), 1);
                    //test
                    tmtbl[0] = 30000 / 20;
                    //tmtbl[0] = 5000 / 20;
                    tmtbl[1] = 500 / 20;

                    Sync = 1;
                    break;
                case 1: //--- check RUN 指示
                    if (tmtbl[0] == 0)
                    {   // time out
                        iRun = 1;
                        DownProcess(ReferNum + 1);
                        Sync = 3;
                    }
                    else
                    {   // check busy
                        if (tmtbl[1] == 0)
                        {   // refer time
                            // check RUN 指示
                            if (mLNK.ReadInt(((int)mLinkDef.StartupEXE + 4 * ReferNum)) == 2)
                            {   // Run request on
                                Sync = 2;
                            }
                            else
                                tmtbl[1] = 500 / 20;
                        }
                    }
                    break;
                case 2: //--- ok end

                    //koko イベントなど登録する
                    // form move enable
                    this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Form_MouseDown);
                    this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Form_MouseUp);
                    this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Form_MouseMove);

                    // GLControl mag/rotation
                    glControlChild.MouseDown += Child_MouseDown;
                    glControlChild.MouseMove += Child_MouseMove;
                    glControlChild.MouseWheel += Child_MouseWheel;
                    iRun = 2;
                    break;
                case 3: //--- ng end
                    break;
            }
        }

        int stage = 0;
        int cmd = 0;

        List<int> childBase = new List<int>()
        {
            (int)ChildArea.Child1,
            (int)ChildArea.Child2,
            (int)ChildArea.Child3,
            (int)ChildArea.Child4,
            (int)ChildArea.Child5
        };

        /// <summary>
        ///Search command By ExeMain.exe ... RUN interval 20ms
        /// </summary>
        private void getCommand()
        {
            switch (stage)
            {
                case 0: //--- search command
                    cmd = mLNK.ReadInt(((int)mLinkDef.command + 4 * ReferNum));
                    if (cmd != 0) stage = 1;
                    break;
                case 1: //--- analysis command
                    switch (cmd)
                    {
                        case 1: //--- X 初期化
                            this.Ax0Re = 0.707106;
                            this.Ax0Im = 0;
                            this.Ax1Re = 0.707106;
                            this.Ax1Im = 0;
                            // set initial point(X)
                            pointSet(1, new Vector3d(1, 0, 0));
                            break;
                        case 2: //--- Y 初期化
                            this.Ax0Re = 0.707106;
                            this.Ax0Im = 0;
                            this.Ax1Re = 0;
                            this.Ax1Im = 0.707106;
                            // set initial point(Y)
                            pointSet(1, new Vector3d(0, 1, 0));
                            break;
                        case 3: //--- Z 初期化
                            this.Ax0Re = 1;
                            this.Ax0Im = 0;
                            this.Ax1Re = 0;
                            this.Ax1Im = 0;
                            // set initial point(Z)
                            pointSet(1, new Vector3d(0, 0, 1));
                            break;
                        case 4: //--- H execute
                            v3Exe = exeRotation(0, 0);
                            setHistory(v3Latest, v3Exe, 0, 0, 0);
                            pointSet(0, v3Exe);
                            break;
                        case 5: //--- Rx()/Ry()/Rz() exexute
                            int adr = childBase[ReferNum];
                            // Gno ➡ 0:Rx / 1:Ry / 2:Rz
                            int Gno = mLNK.ReadInt(adr + (int)ChildDetail.Gate) + 1;
                            // Angle ➡ -360～+360
                            float Angle = mLNK.ReadFloat(adr + (int)ChildDetail.Angle);
                            v3Exe = exeRotation(Gno, Angle);
                            int splits = 0;
                            if (Gno > 0)
                            {
                                splits = Math.Abs((int)((float)Angle / 0.4f));
                                if (splits == 0) splits = 1;
                            }
                            setHistory(v3Latest, v3Exe, Gno, Angle, splits);
                            pointSet(0, v3Exe);
                            break;
                        case 6: //--- Display reset
                                // set initial point(Z)
                            this.Ax0Re = 1;
                            this.Ax0Im = 0;
                            this.Ax1Re = 0;
                            this.Ax1Im = 0;
                            pointSet(1, new Vector3d(0, 0, 1));
                            // history clear
                            OPhistory = new List<OPhis>();
                            break;
                        case 7: //--- Rn() execute
                            adr = childBase[ReferNum];
                            Vector3d nAxis = new Vector3d();
                            nAxis.X = mLNK.ReadDouble(adr + (int)ChildDetail.nAxis);
                            nAxis.Y = mLNK.ReadDouble(adr + (int)ChildDetail.nAxis + 8);
                            nAxis.Z = mLNK.ReadDouble(adr + (int)ChildDetail.nAxis + 16);
                            double RnAngle = mLNK.ReadDouble(adr + (int)ChildDetail.RnAngle);
                            Vector3d perV3 = Rn(v3Latest, nAxis, RnAngle * Math.PI / 180);
                            splits = Math.Abs((int)(RnAngle / 0.4));
                            if (splits == 0) splits = 1;
                            setHistory(v3Latest, perV3, 7, RnAngle, splits);
                            pointSet(0, perV3);
                            break;

                        case 97:    // send List<OPhis> OPhistory
                            adr = (int)mLinkDef.OPhistory;
                            int stIdx = 0; if (OPhistory.Count > 20) stIdx = OPhistory.Count - 20;

                            // clear
                            for (int i = 0; i < (108 * 20 / 4); i++) mLNK.WriteInt(adr + i * 4, 0);

                            int ct = 0;
                            int ctMax = OPhistory.Count;
                            OPhis h = new OPhis();

                            for (int i = stIdx; ; i++)
                            {
                                if (ct >= ctMax) break;

                                h = OPhistory[i];

                                // h.V3from ... double[3]
                                mLNK.WriteDouble(adr + 0, h.V3from.X);
                                mLNK.WriteDouble(adr + 8, h.V3from.Y);
                                mLNK.WriteDouble(adr + 16, h.V3from.Z);
                                // h.V3to ... double[3]
                                mLNK.WriteDouble(adr + 24, h.V3to.X);
                                mLNK.WriteDouble(adr + 32, h.V3to.Y);
                                mLNK.WriteDouble(adr + 40, h.V3to.Z);
                                // h.type ... int
                                mLNK.WriteInt(adr + 48, h.type);
                                // h.angleRad ... double
                                mLNK.WriteDouble(adr + 52, h.angleRad);
                                // h.Ax0Re ... double ... α(x0)Re
                                // h.Ax0Im ... double ... α(x0)Im
                                // h.Ax1Re ... double ... α(x1)Re
                                // h.Ax1Im ... double ... α(x1)Im
                                mLNK.WriteDouble(adr + 60, h.Ax0Re);
                                mLNK.WriteDouble(adr + 68, h.Ax0Im);
                                mLNK.WriteDouble(adr + 76, h.Ax1Re);
                                mLNK.WriteDouble(adr + 84, h.Ax1Im);
                                // θ, φ を求める
                                double theta = Math.Acos(h.V3to.Z);             // z = cosθ
                                double phi = Math.Atan2(h.V3to.Y, h.V3to.X);    // φ = atan2(y, x)
                                mLNK.WriteDouble(adr + 92, theta);
                                mLNK.WriteDouble(adr + 100, phi);

                                adr += 108; ct += 1;

                                if (ct >= 19) break;
                            }
                            break;
                        case 98:    // 現在のα[x0]/α[x1]を格納/ついでに座標も
                            adr = (int)mLinkDef.complexNumber + ReferNum * 8 * 4;
                            mLNK.WriteDouble(adr + 0, this.Ax0Re);
                            mLNK.WriteDouble(adr + 8, this.Ax0Im);
                            mLNK.WriteDouble(adr + 16, this.Ax1Re);
                            mLNK.WriteDouble(adr + 24, this.Ax1Im);
                            //---
                            adr = (int)mLinkDef.v3Latest + ReferNum * 8 * 3;
                            mLNK.WriteDouble(adr + 0, v3Latest.X);
                            mLNK.WriteDouble(adr + 8, v3Latest.Y);
                            mLNK.WriteDouble(adr + 16, v3Latest.Z);
                            break;
                        case 99:    // set Location
                            adr = (int)mLinkDef.orderLocation + (ReferNum * 4 * 2);
                            this.Location = new Point(mLNK.ReadInt(adr), mLNK.ReadInt(adr + 4));
                            this.Show();
                            break;
                    }
                    stage = 2;
                    break;
                case 2: //--- responce command
                    mLNK.WriteInt(((int)mLinkDef.accept + 4 * ReferNum), cmd);
                    stage = 3;
                    break;
                case 3: //--- finished command
                    if (mLNK.ReadInt(((int)mLinkDef.command + 4 * ReferNum)) == 0)
                    {   // command off
                        // init responce
                        mLNK.WriteInt(((int)mLinkDef.accept + 4 * ReferNum), 0);

                        stage = 0;
                    }
                    break;
            }
        }

        /// <summary>
        /// Rn()（任意軸回転）
        /// </summary>
        /// <param name="v3Latest:子の現在の向き"></param>
        /// <param name="nAxis:親から渡された wind（正規化済み）"></param>
        /// <param name="angle:親が決めた XY の角度(弧度)"></param>
        /// <returns></returns>
        private Vector3d Rn(Vector3d v3Latest, Vector3d nAxis, double angleRad)
        {
            // --- 軸の正規化（ローカル変数にする） ---
            double len = Math.Sqrt(nAxis.X * nAxis.X + nAxis.Y * nAxis.Y + nAxis.Z * nAxis.Z);
            if (len == 0) return v3Latest;

            Vector3d axis = new Vector3d(
                nAxis.X / len,
                nAxis.Y / len,
                nAxis.Z / len
            );

            // -----------------------------
            // 1) Blochベクトル（Rodrigues）
            // -----------------------------
            double cosA = Math.Cos(angleRad);
            double sinA = Math.Sin(angleRad);

            Vector3d cross = new Vector3d(
                axis.Y * v3Latest.Z - axis.Z * v3Latest.Y,
                axis.Z * v3Latest.X - axis.X * v3Latest.Z,
                axis.X * v3Latest.Y - axis.Y * v3Latest.X
            );

            double dot = axis.X * v3Latest.X + axis.Y * v3Latest.Y + axis.Z * v3Latest.Z;

            Vector3d blochRot = new Vector3d(
                v3Latest.X * cosA + cross.X * sinA + axis.X * dot * (1 - cosA),
                v3Latest.Y * cosA + cross.Y * sinA + axis.Y * dot * (1 - cosA),
                v3Latest.Z * cosA + cross.Z * sinA + axis.Z * dot * (1 - cosA)
            );

            // -----------------------------
            // 2) 量子状態 Ax0/Ax1 を SU(2) で回す
            // -----------------------------
            //if (mLNK.ReadInt((int)mLinkDef.targetChild) == ReferNum)
            {
                double nx = axis.X;
                double ny = axis.Y;
                double nz = axis.Z;

                double half = angleRad / 2.0;
                double c = Math.Cos(half);
                double s = Math.Sin(half);

                double a0r = this.Ax0Re;
                double a0i = this.Ax0Im;
                double a1r = this.Ax1Re;
                double a1i = this.Ax1Im;

                double u00r = c;
                double u00i = -nz * s;

                double u01r = -ny * s;
                double u01i = -nx * s;

                double u10r = ny * s;
                double u10i = -nx * s;

                double u11r = c;
                double u11i = nz * s;

                double new0r = u00r * a0r - u00i * a0i + u01r * a1r - u01i * a1i;
                double new0i = u00r * a0i + u00i * a0r + u01r * a1i + u01i * a1r;

                double new1r = u10r * a0r - u10i * a0i + u11r * a1r - u11i * a1i;
                double new1i = u10r * a0i + u10i * a0r + u11r * a1i + u11i * a1r;

                this.Ax0Re = new0r;
                this.Ax0Im = new0i;
                this.Ax1Re = new1r;
                this.Ax1Im = new1i;

                Normalize(ref this.Ax0Re, ref this.Ax0Im);
                Normalize(ref this.Ax1Re, ref this.Ax1Im);
            }

            // -----------------------------
            // 3) Blochベクトルの回転結果を返す
            // -----------------------------
            return blochRot;
        }

        /// <summary>
        /// 角度を −π〜π に正規化する
        /// </summary>
        double NormalizeAngle(double rad)
        {
            while (rad > Math.PI)
                rad -= 2.0 * Math.PI;

            while (rad <= -Math.PI)
                rad += 2.0 * Math.PI;

            return rad;
        }

        /// <summary>
        /// 複素数を正規化する（自前版）
        /// </summary>
        void Normalize(ref double re, ref double im)
        {
            double magnitude = Math.Sqrt(re * re + im * im);
            double arg = Math.Atan2(im, re);

            arg = NormalizeAngle(arg);

            re = magnitude * Math.Cos(arg);
            im = magnitude * Math.Sin(arg);
        }

        /// <summary>
        /// current Re/Im
        /// </summary>
        private double Ax0Re = 0;   // α[x0] Re ... αRe
        private double Ax0Im = 0;   // α[x0] Im ... αIm
        private double Ax1Re = 0;   // α[x1] Re ... βRe
        private double Ax1Im = 0;   // α[x1] Im ... βIm

        /// <summary>
        /// α[x0],α[x1] から Bloch 座標 XYZ を計算
        /// </summary>
        private Vector3d CalcXYZ()
        {
            double re = this.Ax0Re * this.Ax1Re + this.Ax0Im * this.Ax1Im;  // Re(α β*)
            double im = this.Ax0Re * this.Ax1Im - this.Ax0Im * this.Ax1Re;  // Im(α β*)

            Vector3d v3 = new Vector3d();
            v3.X = 2.0 * re;
            v3.Y = 2.0 * im;
            v3.Z = (this.Ax0Re * this.Ax0Re + this.Ax0Im * this.Ax0Im)
                 - (this.Ax1Re * this.Ax1Re + this.Ax1Im * this.Ax1Im);

            return v3;
        }

        private Vector3d RotateX(float Angle)
        {
            double rad = (double)Angle * Math.PI / 180.0;
            double c = Math.Cos(rad / 2.0);
            double s = Math.Sin(rad / 2.0);

            double newAx0Re = c * this.Ax0Re + s * this.Ax1Im;
            double newAx0Im = c * this.Ax0Im - s * this.Ax1Re;

            double newAx1Re = c * this.Ax1Re + s * this.Ax0Im;
            double newAx1Im = c * this.Ax1Im - s * this.Ax0Re;

            this.Ax0Re = newAx0Re;
            this.Ax0Im = newAx0Im;
            this.Ax1Re = newAx1Re;
            this.Ax1Im = newAx1Im;

            Normalize(ref this.Ax0Re, ref this.Ax0Im);
            Normalize(ref this.Ax1Re, ref this.Ax1Im);

            return CalcXYZ();
        }

        private Vector3d RotateY(float Angle)
        {
            double rad = (double)Angle * Math.PI / 180.0;
            double c = Math.Cos(rad / 2.0);
            double s = Math.Sin(rad / 2.0);

            double newAx0Re = c * this.Ax0Re - s * this.Ax1Re;
            double newAx0Im = c * this.Ax0Im - s * this.Ax1Im;

            double newAx1Re = s * this.Ax0Re + c * this.Ax1Re;
            double newAx1Im = s * this.Ax0Im + c * this.Ax1Im;

            this.Ax0Re = newAx0Re;
            this.Ax0Im = newAx0Im;
            this.Ax1Re = newAx1Re;
            this.Ax1Im = newAx1Im;

            Normalize(ref this.Ax0Re, ref this.Ax0Im);
            Normalize(ref this.Ax1Re, ref this.Ax1Im);

            return CalcXYZ();
        }

        private Vector3d RotateZ(float Angle)
        {
            double rad = (double)Angle * Math.PI / 180.0;
            double c = Math.Cos(rad / 2.0);
            double s = Math.Sin(rad / 2.0);

            double newAx0Re = c * this.Ax0Re + s * this.Ax0Im;
            double newAx0Im = c * this.Ax0Im - s * this.Ax0Re;

            double newAx1Re = c * this.Ax1Re - s * this.Ax1Im;
            double newAx1Im = c * this.Ax1Im + s * this.Ax1Re;

            this.Ax0Re = newAx0Re;
            this.Ax0Im = newAx0Im;
            this.Ax1Re = newAx1Re;
            this.Ax1Im = newAx1Im;

            Normalize(ref this.Ax0Re, ref this.Ax0Im);
            Normalize(ref this.Ax1Re, ref this.Ax1Im);

            return CalcXYZ();
        }

        private Vector3d exeRotation(int Gno, float Angle)
        {
            if (Gno == 0)
            {   // H
                double inv = 1.0 / Math.Sqrt(2.0);

                double newAx0Re = inv * (this.Ax0Re + this.Ax1Re);
                double newAx0Im = inv * (this.Ax0Im + this.Ax1Im);
                double newAx1Re = inv * (this.Ax0Re - this.Ax1Re);
                double newAx1Im = inv * (this.Ax0Im - this.Ax1Im);

                this.Ax0Re = newAx0Re;
                this.Ax0Im = newAx0Im;
                this.Ax1Re = newAx1Re;
                this.Ax1Im = newAx1Im;

                Normalize(ref this.Ax0Re, ref this.Ax0Im);
                Normalize(ref this.Ax1Re, ref this.Ax1Im);

                return CalcXYZ();
            }
            else
            {
                if (Gno == 1) return RotateX(Angle);
                if (Gno == 2) return RotateY(Angle);
                return RotateZ(Angle);
            }
        }

        private Vector3d[] RedBluePoint = new Vector3d[] { new Vector3d(0, 0, 0), new Vector3d(0, 0, 0) };

        private void pointSet(int type, Vector3d v3)
        {
            if (type == 0)
            {   // set RED
                RedBluePoint[0] = v3;
                RedBluePoint[1] = new Vector3d();
            }
            else
            {   // set BLUE
                RedBluePoint[0] = new Vector3d();
                RedBluePoint[1] = v3;
            }

            v3Latest = v3;

            int Adr = (int)mLinkDef.v3Latest + ReferNum * 24;
            mLNK.WriteDouble(Adr, v3Latest.X);
            mLNK.WriteDouble(Adr + 8, v3Latest.Y);
            mLNK.WriteDouble(Adr + 16, v3Latest.Z);

            labelCordinates.Text = v3Latest.X.ToString("F4") + ". " +
                                   v3Latest.Y.ToString("F4") + ". " +
                                   v3Latest.Z.ToString("F4");


            glControlChild.Invalidate();
        }

        private double GetAngleBetween(Vector3d from, Vector3d to, char axis)
        {
            // 回転軸ベクトル
            Vector3d axisVec =
                (axis == 'X') ? new Vector3d(1, 0, 0) :
                (axis == 'Y') ? new Vector3d(0, 1, 0) :
                                new Vector3d(0, 0, 1);

            // 軸方向成分を除去（軸に直交する平面へ射影）
            Vector3d f = from - axisVec * Vector3d.Dot(from, axisVec);
            Vector3d t = to - axisVec * Vector3d.Dot(to, axisVec);

            f.Normalize();
            t.Normalize();

            // 角度（絶対値）
            double dot = Vector3d.Dot(f, t);
            dot = Math.Max(-1.0, Math.Min(1.0, dot));   // 安全クランプ
            double angle = Math.Acos(dot) * 180.0 / Math.PI;

            // ★ 符号判定（クロス積 → 軸方向との向き）
            Vector3d cross = Vector3d.Cross(f, t);
            double sign = Vector3d.Dot(cross, axisVec);

            // sign が負なら角度を負にする
            if (sign < 0)
                angle = -angle;

            return angle;
        }

        public static Vector3d RotateAroundAxis(Vector3d v, Vector3d axis, double angleRad)
        {
            axis.Normalize();

            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);

            return v * cos
                 + Vector3d.Cross(axis, v) * sin
                 + axis * (Vector3d.Dot(axis, v) * (1 - cos));
        }

        public static List<Vector3d> CreateArcPoints(
            Vector3d start,
            char axis,
            double angleDeg,
            int divisions)
        {
            List<Vector3d> points = new List<Vector3d>();

            double angleRad = Math.PI * angleDeg / 180.0;
            double step = angleRad / divisions;

            Vector3d axisVec;

            if (axis == 'X') axisVec = new Vector3d(1, 0, 0);
            else if (axis == 'Y') axisVec = new Vector3d(0, 1, 0);
            else axisVec = new Vector3d(0, 0, 1);

            for (int i = 0; i <= divisions; i++)
            {
                double t = step * i;
                Vector3d rotated = RotateAroundAxis(start, axisVec, t);
                rotated.Normalize();
                points.Add(rotated);
            }

            return points;
        }

        /* 昔の
        private void OPhistoryDraw()
        {
            if (OPhistory.Count == 0) return;

            GL.PointSize(3f);

            foreach (var op in OPhistory)
            {
                Vector3d from = op.V3from;
                Vector3d to = op.V3to;

                // 回転軸
                Vector3d axis = Vector3d.Cross(from, to);

                // 軸が取れない → 直線描画
                if (axis.Length < 1e-12 || op.angleRad < 1e-12 || op.splits <= 0)
                {
                    GL.Color3(1f, 1f, 1f); // WHITE
                    GL.Begin(PrimitiveType.LineStrip);
                    GL.Vertex3(from);
                    GL.Vertex3(to);
                    GL.End();
                    continue;
                }

                axis.Normalize();
                int div = Math.Max(1, op.splits);

                double dot = Vector3d.Dot(from, to);
                dot = Math.Max(-1.0, Math.Min(1.0, dot));   // 安全クランプ
                double angle = Math.Acos(dot);              // from→to の実角度

                // 色
                if (op.type == 7)
                    GL.Color3(1f, 0.843f, 0f);  // GOLD
                else
                    GL.Color3(1f, 1f, 0f);      // YELLOW

                GL.Begin(PrimitiveType.LineStrip);

                for (int i = 1; i <= div; i++)
                {
                    double t = (double)i / div;
                    double th = angle * t;

                    Vector3d v =
                        from * Math.Cos(th)
                        + Vector3d.Cross(axis, from) * Math.Sin(th)
                        + axis * (Vector3d.Dot(axis, from) * (1 - Math.Cos(th)));

                    GL.Vertex3(v);
                }

                GL.End();
            }

            GL.PointSize(1f);
        }
        */

        private void OPhistoryDraw()
        {
            if (OPhistory.Count == 0) return;

            GL.PointSize(3f);

            foreach (var op in OPhistory)
            {
                Vector3d p1 = op.V3from;
                Vector3d p2 = op.V3to;

                if (op.type == 0)
                {   // H
                    GL.Color3(1f, 1f, 1f); // WHITE
                    GL.Begin(PrimitiveType.LineStrip);
                    GL.Vertex3(p1);
                    GL.Vertex3(p2);
                    GL.End();
                }
                else
                {   // Rx/Ry/Rz/Rn
                    int n = (int)(op.angleRad / 0.007d);
                    if (n < 25) n = 50;

                    Vector3[] arcVertices = GenerateGreatCircleArc((Vector3)p1, (Vector3)p2, n);

                    // 色
                    if (op.type == 7)
                        GL.Color3(1f, 0.843f, 0f);  // GOLD
                    else
                        GL.Color3(1f, 1f, 0f);      // YELLOW

                    GL.Begin(PrimitiveType.LineStrip);
                    foreach (var v3 in arcVertices) { GL.Vertex3(v3); }
                    GL.End();
                }
            }

            GL.PointSize(1f);
        }

        /// <summary>
        /// 球体上の2点を結ぶ大円弧の頂点列を生成
        /// </summary>
        private Vector3[] GenerateGreatCircleArc(Vector3 start, Vector3 end, int segments)
        {
            start.Normalize();
            end.Normalize();

            // 角度
            float angle = (float)Math.Acos(MathHelper.Clamp(Vector3.Dot(start, end), -1f, 1f));

            Vector3[] points = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                // 球面線形補間 (Slerp)
                points[i] = Slerp(start, end, t, angle);
            }
            return points;
        }

        /// <summary>
        /// 球面線形補間 (Slerp)
        /// </summary>
        private Vector3 Slerp(Vector3 a, Vector3 b, float t, float angle)
        {
            float sinTotal = (float)Math.Sin(angle);
            float ratioA = (float)Math.Sin((1 - t) * angle) / sinTotal;
            float ratioB = (float)Math.Sin(t * angle) / sinTotal;
            return (a * ratioA + b * ratioB).Normalized();
        }

        /// <summary>
        /// H/Rn 実行座標
        /// </summary>
        Vector3d v3Exe = new Vector3d();

        /// <summary>
        /// 最終座標
        /// </summary>
        Vector3d v3Latest = new Vector3d();

        void ptGL(Vector3d v3, float size, float r, float g, float b)
        {
            GL.PointSize(size);
            GL.Color3(r, g, b);
            GL.Begin(PrimitiveType.Points);
            GL.Vertex3(v3);
            GL.End();
        }

        private void pointDraw()
        {
            float r; float g; float b;

            // RED ➡ Salmon (250, 128, 114) : After Rn() or H
            if (RedBluePoint[0].X != 0 || RedBluePoint[0].Y != 0 || RedBluePoint[0].Z != 0)
            {
                r = 250F / 255F; g = 128F / 255F; b = 114F / 255F;
                ptGL(RedBluePoint[0], 12, r, g, b);
            }

            // BLUE ➡ DarkGreen (0, 100, 0) : After initialize
            if (RedBluePoint[1].X != 0 || RedBluePoint[1].Y != 0 || RedBluePoint[1].Z != 0)
            {
                r = 0; g = 100F / 255F; b = 0;
                ptGL(RedBluePoint[1], 12, r, g, b);
            }

            GL.PointSize(1f);
        }

        private class OPhis
        {
            public Vector3d V3from = new Vector3d();
            public Vector3d V3to = new Vector3d();
            public int type = 0;            // 0:H / 1:Rx / 2:Ry / 3:Rz / 7:Rn
            public double angleRad = 0;     // type=0の時は0 / 以外は弧度とする
            public int splits = 0;          // 描画する弧線分割数
            //---
            public double Ax0Re = 0;
            public double Ax0Im = 0;
            public double Ax1Re = 0;
            public double Ax1Im = 0;
        }

        List<OPhis> OPhistory = new List<OPhis>();

        /// <summary>
        /// 回転系の操作履歴を格納
        /// </summary>
        /// <param name="Latest"></param>
        /// <param name="Exe"></param>
        /// <param name="type"></param>
        /// <param name="angle:度"></param>
        /// <param name="splits"></param>
        private void setHistory(Vector3d Latest, Vector3d Exe, int type, double angle, int splits)
        {
            OPhis oh = new OPhis();
            oh.V3from = Latest; oh.V3to = Exe; oh.type = type; oh.splits = splits;
            oh.angleRad = angle * Math.PI / 180;
            //---
            oh.Ax0Re = this.Ax0Re; oh.Ax0Im = this.Ax0Im;
            oh.Ax1Re = this.Ax1Re; oh.Ax1Im = this.Ax1Im;

            OPhistory.Add(oh);
        }

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
            /// Sphere 拡大・回転
            /// </summary>
            public float mag = 2.0f;
            public int rot_h = 0;
            public int rot_v = 0;

            /// <summary>
            /// MODELVIEW 行列 ... for Sphere GLControl
            /// </summary>
            public Matrix4 model = new Matrix4();

            /// <summary>
            /// PROJECTION 行列 ... for Sphere GLControl
            /// </summary>
            public Matrix4 projection = new Matrix4();
        }

        #endregion InfXml ===============================================================================================
    }
}

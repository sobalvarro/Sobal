using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sobal
{
    public enum Theme
    {
        System,
        Dark,
        Light
    }

    public enum CornerType
    {
        Default,
        None,
        Round,
        RoundSmall
    }

    public partial class SForm : Form
    {
        private Timer maxButtonTimer;
        private Theme theme = Theme.System;
        private CornerType cornerType;
        private bool useDark;


        public Theme Theme
        {
            get
            {
                return theme;
            }
            set
            {
                if (value == Theme.System)
                {
                    useDark = IsWindowsInDarkMode();
                }
                else
                {
                    useDark = value == Theme.Dark;
                }


                if (useDark)
                {
                    BackColor = Color.FromArgb(20, 20, 20);
                    ForeColor = Color.White;
                    minButton.ForeColor = Color.Gainsboro;
                    maxButton.ForeColor = Color.Gainsboro;
                    closeButton.ForeColor = Color.Gainsboro;
                }
                else
                {
                    BackColor = Color.FromArgb(240, 240, 240);
                    ForeColor = Color.Black;
                    minButton.ForeColor = Color.DarkGray;
                    maxButton.ForeColor = Color.DarkGray;
                    closeButton.ForeColor = Color.DarkGray;
                }

                theme = value;
            }
        }


        public CornerType CornerType
        {
            get { return cornerType; }
            set { cornerType = value; }
        }


        public Color BorderColor { get; set; } = Color.Gray;








        public SForm()
        {
            InitializeComponent();
            Theme = theme;


        }

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(ref Point point);
        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hWnd, int attr, ref int attrValue, int attrSize);
        [DllImport("user32.dll")]
        static extern int SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private string ToBgr(Color c)
        {
            return $"{c.B:X2}{c.G:X2}{c.R:X2}";
        }
        protected override CreateParams CreateParams
        {
            get
            {

                CreateParams cp = base.CreateParams;

                cp.ClassStyle |= 0x00020000; return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            //When the form is created
            if (m.Msg == WM.WM_CREATE)
            {
                //This timer helps to set the back color of the button normal after mouse is not longer over the button
                maxButtonTimer = new Timer();
                maxButtonTimer.Interval = 1;
                maxButtonTimer.Tick += CheckIfCursorIsOnMaxButton;

                //Reposition buttons to the upper right corner
                closeButton.Left = Width - 16 - closeButton.Width;
                maxButton.Left = closeButton.Left - maxButton.Width;
                minButton.Left = maxButton.Left - minButton.Width;

                //Default button coolor, change the color by a shade when the cursor is over
                minButton.FlatAppearance.MouseOverBackColor = ChangeColor(BackColor, -10);

                //Change border color
                int border = int.Parse(ToBgr(BorderColor), System.Globalization.NumberStyles.HexNumber);
                DwmSetWindowAttribute(Handle, 34, ref border, 4);

                //Set corner shape, round, semi round or no corner
                int cornerShape = (int)cornerType;
                DwmSetWindowAttribute(Handle, DWMWA.WINDOW_CORNER_PREFERENCE, ref cornerShape, sizeof(uint));
                

                //Set the border thickness
                MARGINS margins = new MARGINS
                {
                    Left = 0,
                    Top = 0,
                    Right = 0,
                    //Ceck if user is on on Win Version >=11 and disable all aborders if not
                    Bottom = IsWin11orGreater() ? 1 : 0
                };



                //Apply Border
                DwmExtendFrameIntoClientArea(Handle, ref margins);



            }
            else if (m.Msg == WM.NCHITTEST)
            {
                base.WndProc(ref m);

                //Get Client mouse coordinates from screen coordinates
                Point point = new Point();
                GetCursorPos(ref point);
                Point clientPoint = PointToClient(point);
                Rectangle maxButtonRectangle = new Rectangle(maxButton.Left, maxButton.Top, maxButton.Width, maxButton.Height);


                if ((int)m.Result == 0x01/*HTCLIENT*/)
                {

                    //If the point is in maximize button then change the back color
                    if (maxButtonRectangle.Contains(clientPoint))
                    {
                        maxButton.BackColor = ChangeColor(BackColor, -10);
                        maxButtonTimer.Start();
                    }
                    //If cursor is near the top border
                    else if (clientPoint.Y <= 10)
                    {
                        //if cursor is near left corner, then resize in the upper left corner
                        if (clientPoint.X <= 10)
                            m.Result = (IntPtr)13/*HTTOPLEFT*/ ;
                        //If cursor is on the width of the form, then resize in the top border
                        else if (clientPoint.X < (Size.Width - 10))
                            m.Result = (IntPtr)12/*HTTOP*/ ;
                        //Else resize in the top right corner
                        else
                            m.Result = (IntPtr)14/*HTTOPRIGHT*/ ;
                    }
                    else if (clientPoint.Y <= (Size.Height - 10))
                    {
                        if (clientPoint.X <= 10)
                            m.Result = (IntPtr)10/*HTLEFT*/ ;
                        else if (clientPoint.X < (Size.Width - 10))
                            m.Result = (IntPtr)2/*HTCAPTION*/ ;
                        else
                            m.Result = (IntPtr)11/*HTRIGHT*/ ;
                    }
                    else
                    {
                        if (clientPoint.X <= 10)
                            m.Result = (IntPtr)16/*HTBOTTOMLEFT*/ ;
                        else if (clientPoint.X < (Size.Width - 10))
                            m.Result = (IntPtr)15/*HTBOTTOM*/ ;
                        else
                            m.Result = (IntPtr)17/*HTBOTTOMRIGHT*/ ;
                    }
                }
                return;
            }
            else if (m.Msg == WM.SIZE)
            {
                //Check if windows is Maximized, so change the padding
                if (m.WParam == WPARAM_WINDOWS_STATE.Maximized)
                {
                    Padding = new Padding(0, 8, 8, 0);
                }
                else
                {
                    Padding = new Padding(0, 0, 0, 0);
                }


                //If Windows is < 11 then draw manually the borders

                //Pen pen = new Pen(BorderColor);

                //Graphics graphics = CreateGraphics();
                //graphics.Clear(BackColor);
                //graphics.DrawRectangle(pen,0,0,Width-1,Height-1);
                //graphics.DrawLine(pen, 0, 0, Width - 1, 0);
                //graphics.DrawLine(pen, 0, 0, 0, Height - 1);
                //graphics.DrawLine(pen, Width - 1, 0, Width - 1, Height - 1);
                //graphics.DrawLine(pen, 0, Height - 1, Width - 1, Height - 1);
                return;
            }
            //else if(m.Msg == 0x0232)
            //{
            //    //Pen pen = new Pen(BorderColor);

            //    //Graphics graphics = CreateGraphics();
            //    //graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            //    //return;
            //}
            else if (m.Msg == WM.NCCALCSIZE && m.WParam.ToInt32() == 1)
            {


                return;
            }


            base.WndProc(ref m);
        }

        private void CheckIfCursorIsOnMaxButton(object sender, EventArgs e)
        {
            Point point = new Point();
            GetCursorPos(ref point);
            Point clientPoint = PointToClient(point);
            Rectangle maxButtonRectangle = new Rectangle(maxButton.Left, maxButton.Top, maxButton.Width, maxButton.Height);
            if (!maxButtonRectangle.Contains(clientPoint))
            {
                maxButton.BackColor = BackColor;
                maxButtonTimer.Stop();
            }
        }
        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void minButton_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private static Color ChangeColor(Color color, int amount)
        {
            // Use Math.Min and Math.Max for concise clamping
            int red = Math.Min(255, Math.Max(0, color.R + amount));
            int green = Math.Min(255, Math.Max(0, color.G + amount));
            int blue = Math.Min(255, Math.Max(0, color.B + amount));

            return Color.FromArgb(red, green, blue);
        }

        public static bool IsWindowsInDarkMode()
        {
            try
            {
                int res = (int)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", -1);
                return res == 0;
            }
            catch
            {
                return false;
            }
        }
        private bool IsWin11orGreater()
        {
            return false;

            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            int buildNo = Convert.ToInt16(registryKey.GetValue("CurrentBuild").ToString());
            return buildNo >= 22000;

        }

        private void SForm_Load(object sender, EventArgs e)
        {
            //Add four panels
            Panel ptop = new Panel();
            Panel pleft = new Panel();
            Panel pright = new Panel();
            Panel pbottom = new Panel();
            Controls.Add(ptop);
            Controls.Add(pleft);
            Controls.Add(pright);
            Controls.Add(pbottom);
            ptop.Location = new Point(0, 0); ptop.Width = Width; ptop.Height = 1; ptop.BackColor = BorderColor; ptop.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;ptop.BringToFront() ;
            pleft.Location = new Point(0, 0); pleft.Width = 1; pleft.Height = Height; pleft.BackColor = BorderColor; pleft.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            pright.Location = new Point(Width - 1, 0); pright.Width = 1; pright.Height = Height; pright.BackColor = BorderColor; pright.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;pright.BringToFront() ;
            pbottom.Location = new Point(0, Height - 1); pbottom.Width = Width; pbottom.Height = 1; pbottom.BackColor = BorderColor; pbottom.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        }
    }

}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sobal
{
    public class MaxButton : Button
    {
        protected override void WndProc(ref Message m)
        {
            //Check for the NCHITTEST message, this will invalidate the mouse events
            if (m.Msg == WM.NCHITTEST)
            {
                m.Result = (IntPtr)9;

                //Send message back to form so can catch the coordinates
                SendMessage(Parent.Handle, WM.NCHITTEST, m.WParam, m.LParam);
                return;

            }
            //Implement the message for the click
            else if (m.Msg == WM.WM_NCLBUTTONDOWN)
            {
                //Get the form
                Form form = FindForm();

                //Check the current windows state and maximize or restore
                if (form.WindowState == FormWindowState.Normal)
                {
                    Text = SForm.IsFontInstalled("Segoe Fluent Icons")?"\ue923":"\u2750";
                    //Text = "";
                    form.WindowState = FormWindowState.Maximized;
                }
                else
                {
                    Text = SForm.IsFontInstalled("Segoe Fluent Icons") ? "\ue922": "\u25a2";
                    //Text = "";
                    form.WindowState = FormWindowState.Normal;
                }

                return;
            }
            else
            {
                base.WndProc(ref m);
                return;
            }

        }

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    }
}


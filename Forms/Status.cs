// Utility to automatically download radio programmes, using a plugin framework for provider specific implementation.
// Copyright © 2007-2010 Matt Robinson
//
// This program is free software; you can redistribute it and/or modify it under the terms of the GNU General
// Public License as published by the Free Software Foundation; either version 2 of the License, or (at your
// option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public
// License for more details.
//
// You should have received a copy of the GNU General Public License along with this program; if not, write
// to the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

namespace RadioDld
{
    using System;
    using System.Drawing;
    using System.Threading;
    using System.Windows.Forms;

    internal partial class Status : Form
    {
        private Thread showThread;
        private TaskbarNotify tbarNotif;

        public Status()
        {
            this.InitializeComponent();
        }

        public new void Show()
        {
            this.showThread = new Thread(this.ShowFormThread);
            this.showThread.Start();
        }

        private void ShowFormThread()
        {
            if (OsUtils.WinSevenOrLater())
            {
                this.tbarNotif = new TaskbarNotify();
            }

            this.ShowDialog();
        }

        public string StatusText
        {
            get
            {
                return this.lblStatus.Text;
            }

            set
            {
                if (this.IsHandleCreated)
                {
                    this.Invoke((MethodInvoker)delegate { this.SetStatusText_FormThread(value); });
                }
                else
                {
                    this.SetStatusText_FormThread(value);
                }
            }
        }

        private void SetStatusText_FormThread(string text)
        {
            this.lblStatus.Text = text;
        }

        public bool ProgressBarMarquee
        {
            get
            {
                return this.prgProgress.Style == ProgressBarStyle.Marquee;
            }

            set
            {
                if (this.IsHandleCreated)
                {
                    this.Invoke((MethodInvoker)delegate { this.SetProgressBarMarquee_FormThread(value); });
                }
                else
                {
                    this.SetProgressBarMarquee_FormThread(value);
                }
            }
        }

        private void SetProgressBarMarquee_FormThread(bool marquee)
        {
            this.prgProgress.Style = marquee ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;

            if (OsUtils.WinSevenOrLater() & this.IsHandleCreated)
            {
                if (marquee)
                {
                    this.tbarNotif.SetProgressMarquee(this);
                }
                else
                {
                    this.tbarNotif.SetProgressNone(this);
                }
            }
        }

        public int ProgressBarMax
        {
            get
            {
                return this.prgProgress.Maximum;
            }

            set
            {
                if (this.IsHandleCreated)
                {
                    this.Invoke((MethodInvoker)delegate { this.SetProgressBarMax_FormThread(value); });
                }
                else
                {
                    this.SetProgressBarMax_FormThread(value);
                }
            }
        }

        private void SetProgressBarMax_FormThread(int value)
        {
            this.prgProgress.Maximum = value;
        }

        public int ProgressBarValue
        {
            get
            {
                return this.prgProgress.Value;
            }

            set
            {
                if (this.IsHandleCreated)
                {
                    this.Invoke((MethodInvoker)delegate { this.SetProgressBarValue_FormThread(value); });
                }
                else
                {
                    this.SetProgressBarValue_FormThread(value);
                }
            }
        }

        private void SetProgressBarValue_FormThread(int value)
        {
            this.prgProgress.Value = value;

            if (OsUtils.WinSevenOrLater() & this.IsHandleCreated)
            {
                this.tbarNotif.SetProgressValue(this, value, this.prgProgress.Maximum);
            }
        }

        public new void Hide()
        {
            if (this.IsHandleCreated)
            {
                this.Invoke((MethodInvoker)delegate { this.HideForm_FormThread(); });
            }
            else
            {
                this.HideForm_FormThread();
            }
        }

        private void HideForm_FormThread()
        {
            base.Hide();
        }

        private void Status_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            e.Cancel = true;
        }

        private void Status_Load(object sender, System.EventArgs e)
        {
            this.Font = SystemFonts.MessageBoxFont;
        }

        private void Status_Shown(object sender, System.EventArgs e)
        {
            if (OsUtils.WinSevenOrLater())
            {
                if (this.prgProgress.Style == ProgressBarStyle.Marquee)
                {
                    this.tbarNotif.SetProgressMarquee(this);
                }
                else
                {
                    if (this.prgProgress.Value != 0)
                    {
                        this.tbarNotif.SetProgressValue(this, this.prgProgress.Value, this.prgProgress.Maximum);
                    }
                }
            }
        }
    }
}

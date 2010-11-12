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
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Globalization;
    using System.Windows.Forms;

    // Parts of the code in this class are based on code from http://www.codeproject.com/cs/miscctrl/ListViewEmbeddedControls.asp
    internal class ExtListView : ListView
    {
        // List to store the EmbeddedProgress structures
        private List<EmbeddedProgress> embeddedControls = new List<EmbeddedProgress>();

        // Extra events
        public event ColumnClickEventHandler ColumnRightClick;

        public void AddProgressBar(ref ProgressBar progress, ListViewItem parentItem, int column)
        {
            this.AddProgressBar(ref progress, parentItem, column, DockStyle.Fill);
        }

        public void AddProgressBar(ref ProgressBar progress, ListViewItem parentItem, int column, DockStyle dstDock)
        {
            if (progress == null)
            {
                throw new ArgumentNullException("progress");
            }

            if (column >= Columns.Count)
            {
                throw new ArgumentOutOfRangeException("column");
            }

            EmbeddedProgress control = default(EmbeddedProgress);

            control.progress = progress;
            control.column = column;
            control.dock = dstDock;
            control.item = parentItem;

            this.embeddedControls.Add(control);

            // Add a Click event handler to select the ListView row when an embedded control is clicked
            progress.Click += this.embeddedControl_Click;

            this.Controls.Add(progress);
        }

        public void RemoveProgressBar(ref ProgressBar progressBar)
        {
            if (progressBar == null)
            {
                throw new ArgumentNullException("progressBar");
            }

            for (int process = 0; process <= this.embeddedControls.Count - 1; process++)
            {
                if (this.embeddedControls[process].progress.Equals(progressBar))
                {
                    progressBar.Click -= this.embeddedControl_Click;
                    this.Controls.Remove(progressBar);
                    this.embeddedControls.RemoveAt(process);
                    return;
                }
            }

            throw new ArgumentException("Progress bar not found!");
        }

        public ProgressBar GetProgressBar(ListViewItem parentItem, int column)
        {
            foreach (EmbeddedProgress control in this.embeddedControls)
            {
                if (control.item.Equals(parentItem) & control.column == column)
                {
                    return control.progress;
                }
            }

            return null;
        }

        public void RemoveAllControls()
        {
            for (int process = 0; process <= this.embeddedControls.Count - 1; process++)
            {
                EmbeddedProgress control = this.embeddedControls[process];

                control.progress.Visible = false;
                control.progress.Click -= this.embeddedControl_Click;
                this.Controls.Remove(control.progress);
            }

            this.embeddedControls.Clear();
        }

        public void ShowSortOnHeader(int column, SortOrder order)
        {
            IntPtr headersHwnd = NativeMethods.SendMessage(this.Handle, NativeMethods.LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);

            for (int processCols = 0; processCols <= this.Columns.Count; processCols++)
            {
                NativeMethods.HDITEM headerInfo = new NativeMethods.HDITEM();
                headerInfo.mask = NativeMethods.HDI_FORMAT;

                NativeMethods.SendMessage(headersHwnd, NativeMethods.HDM_GETITEM, (IntPtr)processCols, ref headerInfo);

                if (order != SortOrder.None && processCols == column)
                {
                    switch (order)
                    {
                        case SortOrder.Ascending:
                            headerInfo.fmt = headerInfo.fmt & ~NativeMethods.HDF_SORTDOWN;
                            headerInfo.fmt = headerInfo.fmt | NativeMethods.HDF_SORTUP;
                            break;
                        case SortOrder.Descending:
                            headerInfo.fmt = headerInfo.fmt & ~NativeMethods.HDF_SORTUP;
                            headerInfo.fmt = headerInfo.fmt | NativeMethods.HDF_SORTDOWN;
                            break;
                    }
                }
                else
                {
                    headerInfo.fmt = headerInfo.fmt & ~NativeMethods.HDF_SORTDOWN & ~NativeMethods.HDF_SORTUP;
                }

                NativeMethods.SendMessage(headersHwnd, NativeMethods.HDM_SETITEM, (IntPtr)processCols, ref headerInfo);
            }
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case NativeMethods.WM_CREATE:
                    if (OsUtils.WinXpOrLater())
                    {
                        // Set the theme of the control to "explorer", to give the 
                        // correct styling under Vista.  This has no effect under XP.
                        if (NativeMethods.SetWindowTheme(this.Handle, "explorer", null) != 0)
                        {
                            throw new Win32Exception();
                        }
                    }

                    // Remove the focus rectangle from the control (and as a side effect, all other controls on the
                    // form) if the last input event came from the mouse, or add them if it came from the keyboard.
                    NativeMethods.SendMessage(this.Handle, NativeMethods.WM_CHANGEUISTATE, this.MakeLParam(NativeMethods.UIS_INITIALIZE, NativeMethods.UISF_HIDEFOCUS), new IntPtr(0));
                    break;
                case NativeMethods.LVM_SETEXTENDEDLISTVIEWSTYLE:
                    if (OsUtils.WinXpOrLater())
                    {
                        int styles = (int)m.LParam;

                        if ((styles & NativeMethods.LVS_EX_DOUBLEBUFFER) != NativeMethods.LVS_EX_DOUBLEBUFFER)
                        {
                            styles = styles | NativeMethods.LVS_EX_DOUBLEBUFFER;
                            m.LParam = (IntPtr)styles;
                        }
                    }

                    break;
                case NativeMethods.WM_SETFOCUS:
                    // Remove the focus rectangle from the control (and as a side effect, all other controls on the
                    // form) if the last input event came from the mouse, or add them if it came from the keyboard.
                    NativeMethods.SendMessage(this.Handle, NativeMethods.WM_CHANGEUISTATE, this.MakeLParam(NativeMethods.UIS_INITIALIZE, NativeMethods.UISF_HIDEFOCUS), IntPtr.Zero);
                    break;
                case NativeMethods.WM_NOTIFY:
                    // Test to see if the notification was for a right-click in the header
                    if (((NativeMethods.NMHDR)m.GetLParam(typeof(NativeMethods.NMHDR))).code == NativeMethods.NM_RCLICK)
                    {
                        // Fire an event to indicate the click has occurred.  Set the column number
                        // to -1 for all clicks, as this information isn't currently required.
                        if (this.ColumnRightClick != null)
                        {
                            this.ColumnRightClick(this, new ColumnClickEventArgs(-1));
                        }
                    }

                    break;
                case NativeMethods.WM_PAINT:
                    if (View != View.Details)
                    {
                        break;
                    }

                    // Calculate the position of all embedded controls
                    foreach (EmbeddedProgress emcControl in this.embeddedControls)
                    {
                        Rectangle rect = this.GetSubItemBounds(emcControl.item, emcControl.column);

                        if (((this.HeaderStyle != ColumnHeaderStyle.None) & (rect.Top < this.Font.Height)) | (rect.Top + rect.Height) <= 0 | (rect.Top > this.ClientRectangle.Height))
                        {
                            // Control overlaps ColumnHeader, is off the top, or is off the bottom of the listview
                            emcControl.progress.Visible = false;
                            continue;
                        }
                        else
                        {
                            emcControl.progress.Visible = true;
                        }

                        switch (emcControl.dock)
                        {
                            case DockStyle.Fill:
                                break;
                            case DockStyle.Top:
                                rect.Height = emcControl.progress.Height;
                                break;
                            case DockStyle.Left:
                                rect.Width = emcControl.progress.Width;
                                break;
                            case DockStyle.Bottom:
                                rect.Offset(0, rect.Height - emcControl.progress.Height);
                                rect.Height = emcControl.progress.Height;
                                break;
                            case DockStyle.Right:
                                rect.Offset(rect.Width - emcControl.progress.Width, 0);
                                rect.Width = emcControl.progress.Width;
                                break;
                            case DockStyle.None:
                                rect.Size = emcControl.progress.Size;
                                break;
                        }

                        // Set embedded control's bounds
                        emcControl.progress.Bounds = rect;
                    }

                    break;
            }

            base.WndProc(ref m);
        }

        private int[] GetColumnOrder()
        {
            int[] order = new int[this.Columns.Count + 1];

            for (int process = 0; process <= this.Columns.Count - 1; process++)
            {
                order[process] = this.Columns[process].DisplayIndex;
            }

            return order;
        }

        private Rectangle GetSubItemBounds(ListViewItem listItem, int subItem)
        {
            Rectangle subItemRect = Rectangle.Empty;

            if (listItem == null)
            {
                throw new ArgumentNullException("listItem");
            }

            int[] order = this.GetColumnOrder();

            if (order == null)
            {
                // No Columns
                return subItemRect;
            }

            if (subItem >= order.Length)
            {
                throw new ArgumentOutOfRangeException("SubItem " + subItem.ToString(CultureInfo.InvariantCulture) + " out of range");
            }

            // Retrieve the bounds of the entire ListViewItem (all subitems)
            Rectangle bounds = listItem.GetBounds(ItemBoundsPortion.Entire);

            int subItemX = bounds.Left;
            ColumnHeader header = null;
            int process = 0;

            // Calculate the X position of the SubItem.
            for (process = 0; process <= order.Length - 1; process++)
            {
                header = this.Columns[order[process]];

                if (header.Index == subItem)
                {
                    break;
                }

                subItemX += header.Width;
            }

            subItemRect = new Rectangle(subItemX, bounds.Top, this.Columns[order[process]].Width, bounds.Height);

            return subItemRect;
        }

        private void embeddedControl_Click(object sender, EventArgs e)
        {
            // When a progress bar is clicked the ListViewItem holding it is selected
            foreach (EmbeddedProgress control in this.embeddedControls)
            {
                if (control.progress.Equals((ProgressBar)sender))
                {
                    this.SelectedItems.Clear();
                    control.item.Selected = true;
                }
            }
        }

        private IntPtr MakeLParam(int LoWord, int HiWord)
        {
            IntPtr IntPtrHiWord = new IntPtr(HiWord << 16);
            IntPtr IntPtrLoWord = new IntPtr(LoWord & 0xffff);

            return new IntPtr(IntPtrHiWord.ToInt32() | IntPtrLoWord.ToInt32());
        }

        // Data structure to store information about the controls
        private struct EmbeddedProgress
        {
            public ProgressBar progress;
            public int column;
            public DockStyle dock;
            public ListViewItem item;
        }
    }
}

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> Partial Class frmMain
#Region "Windows Form Designer generated code "
	<System.Diagnostics.DebuggerNonUserCode()> Public Sub New()
		MyBase.New()
		'This call is required by the Windows Form Designer.
		InitializeComponent()
	End Sub
	'Form overrides dispose to clean up the component list.
	<System.Diagnostics.DebuggerNonUserCode()> Protected Overloads Overrides Sub Dispose(ByVal Disposing As Boolean)
		If Disposing Then
			If Not components Is Nothing Then
				components.Dispose()
			End If
		End If
		MyBase.Dispose(Disposing)
	End Sub
	'Required by the Windows Form Designer
	Private components As System.ComponentModel.IContainer
	Public ToolTip1 As System.Windows.Forms.ToolTip
    Public WithEvents tmrCheckSub As System.Windows.Forms.Timer
    Public WithEvents tmrStartProcess As System.Windows.Forms.Timer
    Public WithEvents webDetails As System.Windows.Forms.WebBrowser
    Public WithEvents mnuFileExit As System.Windows.Forms.ToolStripMenuItem
    Public WithEvents File As System.Windows.Forms.ToolStripMenuItem
    Public WithEvents mnuToolsPrefs As System.Windows.Forms.ToolStripMenuItem
    Public WithEvents mnuTools As System.Windows.Forms.ToolStripMenuItem
    Public WithEvents mnuHelpAbout As System.Windows.Forms.ToolStripMenuItem
    Public WithEvents mnuHelp As System.Windows.Forms.ToolStripMenuItem
    Public WithEvents mnuMainMenu As System.Windows.Forms.MenuStrip
    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmMain))
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.tmrCheckSub = New System.Windows.Forms.Timer(Me.components)
        Me.tmrStartProcess = New System.Windows.Forms.Timer(Me.components)
        Me.webDetails = New System.Windows.Forms.WebBrowser
        Me.mnuMainMenu = New System.Windows.Forms.MenuStrip
        Me.File = New System.Windows.Forms.ToolStripMenuItem
        Me.mnuFileExit = New System.Windows.Forms.ToolStripMenuItem
        Me.mnuTools = New System.Windows.Forms.ToolStripMenuItem
        Me.mnuToolsPrefs = New System.Windows.Forms.ToolStripMenuItem
        Me.mnuHelp = New System.Windows.Forms.ToolStripMenuItem
        Me.mnuHelpAbout = New System.Windows.Forms.ToolStripMenuItem
        Me.tbrToolbar = New System.Windows.Forms.ToolStrip
        Me.tbtFindNew = New System.Windows.Forms.ToolStripButton
        Me.tbtSubscriptions = New System.Windows.Forms.ToolStripButton
        Me.tbtDownloads = New System.Windows.Forms.ToolStripButton
        Me.ToolStripSeparator1 = New System.Windows.Forms.ToolStripSeparator
        Me.tbtUp = New System.Windows.Forms.ToolStripButton
        Me.ToolStripSeparator2 = New System.Windows.Forms.ToolStripSeparator
        Me.tbtRefresh = New System.Windows.Forms.ToolStripButton
        Me.tbtCleanUp = New System.Windows.Forms.ToolStripButton
        Me.ttxSearch = New System.Windows.Forms.ToolStripTextBox
        Me.nicTrayIcon = New System.Windows.Forms.NotifyIcon(Me.components)
        Me.mnuTray = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.mnuTrayShow = New System.Windows.Forms.ToolStripMenuItem
        Me.ToolStripSeparator3 = New System.Windows.Forms.ToolStripSeparator
        Me.mnuTrayExit = New System.Windows.Forms.ToolStripMenuItem
        Me.lstNew = New System.Windows.Forms.ListView
        Me.imlListIcons = New System.Windows.Forms.ImageList(Me.components)
        Me.imlStations = New System.Windows.Forms.ImageList(Me.components)
        Me.staStatus = New System.Windows.Forms.StatusStrip
        Me.stlStatusText = New System.Windows.Forms.ToolStripStatusLabel
        Me.lstSubscribed = New System.Windows.Forms.ListView
        Me.lstDownloads = New System.Windows.Forms.ListView
        Me.mnuMainMenu.SuspendLayout()
        Me.tbrToolbar.SuspendLayout()
        Me.mnuTray.SuspendLayout()
        Me.staStatus.SuspendLayout()
        Me.SuspendLayout()
        '
        'tmrCheckSub
        '
        Me.tmrCheckSub.Enabled = True
        Me.tmrCheckSub.Interval = 60000
        '
        'tmrStartProcess
        '
        Me.tmrStartProcess.Enabled = True
        Me.tmrStartProcess.Interval = 2000
        '
        'webDetails
        '
        Me.webDetails.AllowWebBrowserDrop = False
        Me.webDetails.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.webDetails.IsWebBrowserContextMenuEnabled = False
        Me.webDetails.Location = New System.Drawing.Point(0, 55)
        Me.webDetails.Name = "webDetails"
        Me.webDetails.ScrollBarsEnabled = False
        Me.webDetails.Size = New System.Drawing.Size(210, 394)
        Me.webDetails.TabIndex = 2
        Me.webDetails.WebBrowserShortcutsEnabled = False
        '
        'mnuMainMenu
        '
        Me.mnuMainMenu.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.File, Me.mnuTools, Me.mnuHelp})
        Me.mnuMainMenu.Location = New System.Drawing.Point(0, 0)
        Me.mnuMainMenu.Name = "mnuMainMenu"
        Me.mnuMainMenu.RenderMode = System.Windows.Forms.ToolStripRenderMode.System
        Me.mnuMainMenu.Size = New System.Drawing.Size(757, 24)
        Me.mnuMainMenu.TabIndex = 10
        '
        'File
        '
        Me.File.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuFileExit})
        Me.File.Name = "File"
        Me.File.Size = New System.Drawing.Size(37, 20)
        Me.File.Text = "&File"
        '
        'mnuFileExit
        '
        Me.mnuFileExit.Name = "mnuFileExit"
        Me.mnuFileExit.Size = New System.Drawing.Size(92, 22)
        Me.mnuFileExit.Text = "E&xit"
        '
        'mnuTools
        '
        Me.mnuTools.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuToolsPrefs})
        Me.mnuTools.Name = "mnuTools"
        Me.mnuTools.Size = New System.Drawing.Size(48, 20)
        Me.mnuTools.Text = "&Tools"
        '
        'mnuToolsPrefs
        '
        Me.mnuToolsPrefs.Name = "mnuToolsPrefs"
        Me.mnuToolsPrefs.Size = New System.Drawing.Size(135, 22)
        Me.mnuToolsPrefs.Text = "&Preferences"
        '
        'mnuHelp
        '
        Me.mnuHelp.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuHelpAbout})
        Me.mnuHelp.Name = "mnuHelp"
        Me.mnuHelp.Size = New System.Drawing.Size(44, 20)
        Me.mnuHelp.Text = "&Help"
        '
        'mnuHelpAbout
        '
        Me.mnuHelpAbout.Name = "mnuHelpAbout"
        Me.mnuHelpAbout.Size = New System.Drawing.Size(107, 22)
        Me.mnuHelpAbout.Text = "&About"
        '
        'tbrToolbar
        '
        Me.tbrToolbar.ImageScalingSize = New System.Drawing.Size(24, 24)
        Me.tbrToolbar.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.tbtFindNew, Me.tbtSubscriptions, Me.tbtDownloads, Me.ToolStripSeparator1, Me.tbtUp, Me.ToolStripSeparator2, Me.tbtRefresh, Me.tbtCleanUp, Me.ttxSearch})
        Me.tbrToolbar.Location = New System.Drawing.Point(0, 24)
        Me.tbrToolbar.Name = "tbrToolbar"
        Me.tbrToolbar.RenderMode = System.Windows.Forms.ToolStripRenderMode.System
        Me.tbrToolbar.Size = New System.Drawing.Size(757, 31)
        Me.tbrToolbar.TabIndex = 11
        '
        'tbtFindNew
        '
        Me.tbtFindNew.Checked = True
        Me.tbtFindNew.CheckState = System.Windows.Forms.CheckState.Checked
        Me.tbtFindNew.Image = CType(resources.GetObject("tbtFindNew.Image"), System.Drawing.Image)
        Me.tbtFindNew.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.tbtFindNew.Name = "tbtFindNew"
        Me.tbtFindNew.Size = New System.Drawing.Size(85, 28)
        Me.tbtFindNew.Text = "Find New"
        '
        'tbtSubscriptions
        '
        Me.tbtSubscriptions.Image = CType(resources.GetObject("tbtSubscriptions.Image"), System.Drawing.Image)
        Me.tbtSubscriptions.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.tbtSubscriptions.Name = "tbtSubscriptions"
        Me.tbtSubscriptions.Size = New System.Drawing.Size(106, 28)
        Me.tbtSubscriptions.Text = "Subscriptions"
        '
        'tbtDownloads
        '
        Me.tbtDownloads.Image = CType(resources.GetObject("tbtDownloads.Image"), System.Drawing.Image)
        Me.tbtDownloads.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.tbtDownloads.Name = "tbtDownloads"
        Me.tbtDownloads.Size = New System.Drawing.Size(94, 28)
        Me.tbtDownloads.Text = "Downloads"
        '
        'ToolStripSeparator1
        '
        Me.ToolStripSeparator1.Name = "ToolStripSeparator1"
        Me.ToolStripSeparator1.Size = New System.Drawing.Size(6, 31)
        '
        'tbtUp
        '
        Me.tbtUp.Image = CType(resources.GetObject("tbtUp.Image"), System.Drawing.Image)
        Me.tbtUp.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.tbtUp.Name = "tbtUp"
        Me.tbtUp.Size = New System.Drawing.Size(50, 28)
        Me.tbtUp.Text = "Up"
        '
        'ToolStripSeparator2
        '
        Me.ToolStripSeparator2.Name = "ToolStripSeparator2"
        Me.ToolStripSeparator2.Size = New System.Drawing.Size(6, 31)
        '
        'tbtRefresh
        '
        Me.tbtRefresh.Image = CType(resources.GetObject("tbtRefresh.Image"), System.Drawing.Image)
        Me.tbtRefresh.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.tbtRefresh.Name = "tbtRefresh"
        Me.tbtRefresh.Size = New System.Drawing.Size(74, 28)
        Me.tbtRefresh.Text = "Refresh"
        '
        'tbtCleanUp
        '
        Me.tbtCleanUp.Image = CType(resources.GetObject("tbtCleanUp.Image"), System.Drawing.Image)
        Me.tbtCleanUp.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.tbtCleanUp.Name = "tbtCleanUp"
        Me.tbtCleanUp.Size = New System.Drawing.Size(83, 28)
        Me.tbtCleanUp.Text = "Clean Up"
        '
        'ttxSearch
        '
        Me.ttxSearch.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right
        Me.ttxSearch.Name = "ttxSearch"
        Me.ttxSearch.Size = New System.Drawing.Size(100, 31)
        Me.ttxSearch.Text = "Search..."
        '
        'nicTrayIcon
        '
        Me.nicTrayIcon.ContextMenuStrip = Me.mnuTray
        Me.nicTrayIcon.Visible = True
        '
        'mnuTray
        '
        Me.mnuTray.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuTrayShow, Me.ToolStripSeparator3, Me.mnuTrayExit})
        Me.mnuTray.Name = "ContextMenuStrip1"
        Me.mnuTray.Size = New System.Drawing.Size(204, 54)
        '
        'mnuTrayShow
        '
        Me.mnuTrayShow.Name = "mnuTrayShow"
        Me.mnuTrayShow.Size = New System.Drawing.Size(203, 22)
        Me.mnuTrayShow.Text = "&Show Radio Downloader"
        '
        'ToolStripSeparator3
        '
        Me.ToolStripSeparator3.Name = "ToolStripSeparator3"
        Me.ToolStripSeparator3.Size = New System.Drawing.Size(200, 6)
        '
        'mnuTrayExit
        '
        Me.mnuTrayExit.Name = "mnuTrayExit"
        Me.mnuTrayExit.Size = New System.Drawing.Size(203, 22)
        Me.mnuTrayExit.Text = "E&xit"
        '
        'lstNew
        '
        Me.lstNew.Activation = System.Windows.Forms.ItemActivation.TwoClick
        Me.lstNew.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lstNew.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.lstNew.Location = New System.Drawing.Point(210, 55)
        Me.lstNew.MultiSelect = False
        Me.lstNew.Name = "lstNew"
        Me.lstNew.Size = New System.Drawing.Size(547, 110)
        Me.lstNew.TabIndex = 12
        Me.lstNew.UseCompatibleStateImageBehavior = False
        '
        'imlListIcons
        '
        Me.imlListIcons.ImageStream = CType(resources.GetObject("imlListIcons.ImageStream"), System.Windows.Forms.ImageListStreamer)
        Me.imlListIcons.TransparentColor = System.Drawing.Color.Transparent
        Me.imlListIcons.Images.SetKeyName(0, "downloading")
        Me.imlListIcons.Images.SetKeyName(1, "paused")
        Me.imlListIcons.Images.SetKeyName(2, "converting")
        Me.imlListIcons.Images.SetKeyName(3, "downloaded")
        Me.imlListIcons.Images.SetKeyName(4, "new")
        Me.imlListIcons.Images.SetKeyName(5, "subscribed")
        Me.imlListIcons.Images.SetKeyName(6, "error")
        '
        'imlStations
        '
        Me.imlStations.ImageStream = CType(resources.GetObject("imlStations.ImageStream"), System.Windows.Forms.ImageListStreamer)
        Me.imlStations.TransparentColor = System.Drawing.Color.Transparent
        Me.imlStations.Images.SetKeyName(0, "default")
        '
        'staStatus
        '
        Me.staStatus.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.stlStatusText})
        Me.staStatus.Location = New System.Drawing.Point(0, 449)
        Me.staStatus.Name = "staStatus"
        Me.staStatus.Size = New System.Drawing.Size(757, 22)
        Me.staStatus.TabIndex = 13
        '
        'stlStatusText
        '
        Me.stlStatusText.Name = "stlStatusText"
        Me.stlStatusText.Size = New System.Drawing.Size(64, 17)
        Me.stlStatusText.Text = "Status Text"
        '
        'lstSubscribed
        '
        Me.lstSubscribed.Activation = System.Windows.Forms.ItemActivation.TwoClick
        Me.lstSubscribed.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lstSubscribed.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.lstSubscribed.Location = New System.Drawing.Point(210, 197)
        Me.lstSubscribed.MultiSelect = False
        Me.lstSubscribed.Name = "lstSubscribed"
        Me.lstSubscribed.Size = New System.Drawing.Size(547, 110)
        Me.lstSubscribed.TabIndex = 14
        Me.lstSubscribed.UseCompatibleStateImageBehavior = False
        Me.lstSubscribed.View = System.Windows.Forms.View.Details
        '
        'lstDownloads
        '
        Me.lstDownloads.Activation = System.Windows.Forms.ItemActivation.TwoClick
        Me.lstDownloads.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lstDownloads.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.lstDownloads.Location = New System.Drawing.Point(216, 336)
        Me.lstDownloads.MultiSelect = False
        Me.lstDownloads.Name = "lstDownloads"
        Me.lstDownloads.Size = New System.Drawing.Size(547, 110)
        Me.lstDownloads.TabIndex = 15
        Me.lstDownloads.UseCompatibleStateImageBehavior = False
        Me.lstDownloads.View = System.Windows.Forms.View.Details
        '
        'frmMain
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.SystemColors.Control
        Me.ClientSize = New System.Drawing.Size(757, 471)
        Me.Controls.Add(Me.lstDownloads)
        Me.Controls.Add(Me.lstSubscribed)
        Me.Controls.Add(Me.staStatus)
        Me.Controls.Add(Me.tbrToolbar)
        Me.Controls.Add(Me.lstNew)
        Me.Controls.Add(Me.webDetails)
        Me.Controls.Add(Me.mnuMainMenu)
        Me.Cursor = System.Windows.Forms.Cursors.Default
        Me.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Location = New System.Drawing.Point(11, 37)
        Me.MinimumSize = New System.Drawing.Size(400, 300)
        Me.Name = "frmMain"
        Me.RightToLeft = System.Windows.Forms.RightToLeft.No
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Radio Downloader"
        Me.mnuMainMenu.ResumeLayout(False)
        Me.mnuMainMenu.PerformLayout()
        Me.tbrToolbar.ResumeLayout(False)
        Me.tbrToolbar.PerformLayout()
        Me.mnuTray.ResumeLayout(False)
        Me.staStatus.ResumeLayout(False)
        Me.staStatus.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents tbrToolbar As System.Windows.Forms.ToolStrip
    Friend WithEvents tbtFindNew As System.Windows.Forms.ToolStripButton
    Friend WithEvents tbtSubscriptions As System.Windows.Forms.ToolStripButton
    Friend WithEvents tbtDownloads As System.Windows.Forms.ToolStripButton
    Friend WithEvents ToolStripSeparator1 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents tbtUp As System.Windows.Forms.ToolStripButton
    Friend WithEvents ToolStripSeparator2 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents tbtRefresh As System.Windows.Forms.ToolStripButton
    Friend WithEvents tbtCleanUp As System.Windows.Forms.ToolStripButton
    Friend WithEvents nicTrayIcon As System.Windows.Forms.NotifyIcon
    Friend WithEvents mnuTray As System.Windows.Forms.ContextMenuStrip
    Friend WithEvents mnuTrayShow As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripSeparator3 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents mnuTrayExit As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ttxSearch As System.Windows.Forms.ToolStripTextBox
    Friend WithEvents lstNew As System.Windows.Forms.ListView
    Friend WithEvents imlListIcons As System.Windows.Forms.ImageList
    Friend WithEvents imlStations As System.Windows.Forms.ImageList
    Friend WithEvents staStatus As System.Windows.Forms.StatusStrip
    Friend WithEvents stlStatusText As System.Windows.Forms.ToolStripStatusLabel
    Friend WithEvents lstSubscribed As System.Windows.Forms.ListView
    Friend WithEvents lstDownloads As System.Windows.Forms.ListView
#End Region
End Class
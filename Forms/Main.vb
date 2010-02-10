' Utility to automatically download radio programmes, using a plugin framework for provider specific implementation.
' Copyright � 2007-2010 Matt Robinson
'
' This program is free software; you can redistribute it and/or modify it under the terms of the GNU General
' Public License as published by the Free Software Foundation; either version 2 of the License, or (at your
' option) any later version.
'
' This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
' implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public
' License for more details.
'
' You should have received a copy of the GNU General Public License along with this program; if not, write
' to the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Windows.Forms.VisualStyles

Friend Class Main
    Inherits System.Windows.Forms.Form

    Public Enum ErrorStatus
        NoChange
        Normal
        [Error]
    End Enum

    Private Enum MainTab
        FindProgramme
        Favourites
        Subscriptions
        Downloads
    End Enum

    Private Enum View
        FindNewChooseProvider
        FindNewProviderForm
        ProgEpisodes
        Favourites
        Subscriptions
        Downloads
    End Enum

    Private Structure ViewStore
        Dim Tab As MainTab
        Dim View As View
        Dim ViewData As Object
    End Structure

    Private Structure FindNewViewData
        Dim ProviderID As Guid
        Dim View As Object
    End Structure

    Private Class ListComparer
        Implements IComparer

        Public Enum ListType
            Subscription
            Download
        End Enum

        Private dataInstance As Data
        Private compareType As ListType

        Public Sub New(ByVal compareType As ListType)
            dataInstance = Data.GetInstance
            Me.compareType = compareType
        End Sub

        Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements System.Collections.IComparer.Compare
            Select Case compareType
                Case ListType.Subscription
                    Return dataInstance.CompareSubscriptions(CInt(CType(x, ListViewItem).Name), CInt(CType(y, ListViewItem).Name))
                Case ListType.Download
                    Return dataInstance.CompareDownloads(CInt(CType(x, ListViewItem).Name), CInt(CType(y, ListViewItem).Name))
                Case Else
                    Throw New ArgumentException("compareType not a valid value", "compareType")
            End Select
        End Function
    End Class

    Private viwBackData(-1) As ViewStore
    Private viwFwdData(-1) As ViewStore

    Private WithEvents clsProgData As Data
    Private clsDoDBUpdate As UpdateDB
    Private clsUpdate As UpdateCheck

    Private Delegate Sub clsProgData_ProviderAdded_Delegate(ByVal providerId As Guid)
    Private Delegate Sub clsProgData_Programme_Delegate(ByVal progid As Integer)
    Private Delegate Sub clsProgData_Episode_Delegate(ByVal epid As Integer)
    Private Delegate Sub clsProgData_DownloadProgress_Delegate(ByVal epid As Integer, ByVal percent As Integer, ByVal statusText As String, ByVal icon As IRadioProvider.ProgressIcon)

    Private Const downloadProgCol As Integer = 3

    Public Sub SetTrayStatus(ByVal booActive As Boolean, Optional ByVal ErrorStatus As ErrorStatus = ErrorStatus.NoChange)
        Dim booErrorStatus As Boolean

        If ErrorStatus = Main.ErrorStatus.Error Then
            booErrorStatus = True
        ElseIf ErrorStatus = Main.ErrorStatus.Normal Then
            booErrorStatus = False
        End If

        If booErrorStatus = True Then
            nicTrayIcon.Icon = My.Resources.icon_error
            nicTrayIcon.Text = Me.Text + ": Error"
        Else
            If booActive = True Then
                nicTrayIcon.Icon = My.Resources.icon_working
                nicTrayIcon.Text = Me.Text + ": Downloading"
            Else
                nicTrayIcon.Icon = My.Resources.icon_main
                nicTrayIcon.Text = Me.Text
            End If
        End If
    End Sub

    Private Sub Main_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Me.KeyDown
        Select Case e.KeyCode
            Case Keys.F1
                e.Handled = True
                Call mnuHelpShowHelp_Click(sender, e)
            Case Keys.Delete
                If tbtDelete.Visible Then
                    e.Handled = True
                    Call tbtDelete_Click()
                ElseIf tbtCancel.Visible Then
                    e.Handled = True
                    Call tbtCancel_Click()
                End If
            Case Keys.Back
                If Me.ActiveControl.GetType IsNot GetType(TextBox) Then
                    If e.Shift Then
                        If tbtForward.Enabled Then
                            e.Handled = True
                            Call tbtForward_Click(sender, e)
                        End If
                    Else
                        If tbtBack.Enabled Then
                            e.Handled = True
                            Call tbtBack_Click(sender, e)
                        End If
                    End If
                End If
            Case Keys.BrowserBack
                If tbtBack.Enabled Then
                    e.Handled = True
                    Call tbtBack_Click(sender, e)
                End If
            Case Keys.BrowserForward
                If tbtForward.Enabled Then
                    e.Handled = True
                    Call tbtForward_Click(sender, e)
                End If
        End Select
    End Sub

    Private Sub Main_Load(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles MyBase.Load
        ' If this is the first run of a new version of the application, then upgrade the settings from the old version.
        If My.Settings.UpgradeSettings Then
            My.Settings.Upgrade()
            My.Settings.UpgradeSettings = False
            My.Settings.Save()
        End If

        ' Make sure that the temp and application data folders exist
        Directory.CreateDirectory(Path.Combine(System.IO.Path.GetTempPath, "RadioDownloader"))
        Directory.CreateDirectory(FileUtils.GetAppDataFolder)

        ' Make sure that the database exists.  If not, then copy across the empty database from the program's folder.
        Dim fileExits As New IO.FileInfo(Path.Combine(FileUtils.GetAppDataFolder(), "store.db"))

        If fileExits.Exists = False Then
            Try
                IO.File.Copy(Path.Combine(My.Application.Info.DirectoryPath, "store.db"), Path.Combine(FileUtils.GetAppDataFolder(), "store.db"))
            Catch expFileNotFound As FileNotFoundException
                Call MsgBox("The Radio Downloader template database was not found at '" + Path.Combine(My.Application.Info.DirectoryPath, "store.db") + "'." + vbCrLf + "Try repairing the Radio Downloader installation, or uninstalling Radio Downloader and then installing the latest version from the NerdoftheHerd website.", MsgBoxStyle.Critical)
                Me.Close()
                Me.Dispose()
                Exit Sub
            End Try
        Else
            ' As the database already exists, copy the specimen database across from the program folder
            ' and then make sure that the current db's structure matches it.
            Try
                IO.File.Copy(Path.Combine(My.Application.Info.DirectoryPath, "store.db"), Path.Combine(FileUtils.GetAppDataFolder(), "spec-store.db"), True)
            Catch expFileNotFound As FileNotFoundException
                Call MsgBox("The Radio Downloader template database was not found at '" + Path.Combine(My.Application.Info.DirectoryPath, "store.db") + "'." + vbCrLf + "Try repairing the Radio Downloader installation, or uninstalling Radio Downloader and then installing the latest version from the NerdoftheHerd website.", MsgBoxStyle.Critical)
                Me.Close()
                Me.Dispose()
                Exit Sub
            End Try

            clsDoDBUpdate = New UpdateDB(Path.Combine(FileUtils.GetAppDataFolder(), "spec-store.db"), Path.Combine(FileUtils.GetAppDataFolder(), "store.db"))
            Call clsDoDBUpdate.UpdateStructure()
        End If

        imlListIcons.Images.Add("downloading", My.Resources.list_downloading)
        imlListIcons.Images.Add("waiting", My.Resources.list_waiting)
        imlListIcons.Images.Add("converting", My.Resources.list_converting)
        imlListIcons.Images.Add("downloaded_new", My.Resources.list_downloaded_new)
        imlListIcons.Images.Add("downloaded", My.Resources.list_downloaded)
        imlListIcons.Images.Add("subscribed", My.Resources.list_subscribed)
        imlListIcons.Images.Add("error", My.Resources.list_error)

        imlProviders.Images.Add("default", My.Resources.provider_default)

        imlToolbar.Images.Add("choose_programme", My.Resources.toolbar_choose_programme)
        imlToolbar.Images.Add("clean_up", My.Resources.toolbar_clean_up)
        imlToolbar.Images.Add("current_episodes", My.Resources.toolbar_current_episodes)
        imlToolbar.Images.Add("delete", My.Resources.toolbar_delete)
        imlToolbar.Images.Add("download", My.Resources.toolbar_download)
        imlToolbar.Images.Add("help", My.Resources.toolbar_help)
        imlToolbar.Images.Add("options", My.Resources.toolbar_options)
        imlToolbar.Images.Add("play", My.Resources.toolbar_play)
        imlToolbar.Images.Add("report_error", My.Resources.toolbar_report_error)
        imlToolbar.Images.Add("retry", My.Resources.toolbar_retry)
        imlToolbar.Images.Add("subscribe", My.Resources.toolbar_subscribe)
        imlToolbar.Images.Add("unsubscribe", My.Resources.toolbar_unsubscribe)

        tbrToolbar.ImageList = imlToolbar
        tbrHelp.ImageList = imlToolbar
        lstProviders.LargeImageList = imlProviders
        lstSubscribed.SmallImageList = imlListIcons
        lstDownloads.SmallImageList = imlListIcons

        Call lstEpisodes.Columns.Add("Date", CInt(0.179 * lstEpisodes.Width))
        Call lstEpisodes.Columns.Add("Episode Name", CInt(0.786 * lstEpisodes.Width))
        Call lstSubscribed.Columns.Add("Programme Name", CInt(0.482 * lstSubscribed.Width))
        Call lstSubscribed.Columns.Add("Last Download", CInt(0.179 * lstSubscribed.Width))
        Call lstSubscribed.Columns.Add("Provider", CInt(0.304 * lstSubscribed.Width))
        Call lstDownloads.Columns.Add("Episode Name", CInt(0.426 * lstDownloads.Width))
        Call lstDownloads.Columns.Add("Date", CInt(0.14 * lstDownloads.Width))
        Call lstDownloads.Columns.Add("Status", CInt(0.22 * lstDownloads.Width))
        Call lstDownloads.Columns.Add("Progress", CInt(0.179 * lstDownloads.Width))

        Call SetView(MainTab.FindProgramme, View.FindNewChooseProvider, Nothing)

        clsProgData = Data.GetInstance
        Call clsProgData.InitProviderList()
        Call clsProgData.InitSubscriptionList()
        lstSubscribed.ListViewItemSorter = New ListComparer(ListComparer.ListType.Subscription)
        Call clsProgData.InitDownloadList()
        lstDownloads.ListViewItemSorter = New ListComparer(ListComparer.ListType.Download)

        ' Set up and then show the system tray icon
        Call SetTrayStatus(False)
        nicTrayIcon.Visible = True

        clsUpdate = New UpdateCheck("http://www.nerdoftheherd.com/tools/radiodld/latestversion.txt?reqver=" + My.Application.Info.Version.ToString)

        picSideBarBorder.Width = 2

        lstProviders.Dock = DockStyle.Fill
        pnlPluginSpace.Dock = DockStyle.Fill
        lstEpisodes.Dock = DockStyle.Fill
        lstSubscribed.Dock = DockStyle.Fill
        lstDownloads.Dock = DockStyle.Fill

        ttxSearch.Visible = False

        Me.Font = SystemFonts.MessageBoxFont
        lblSideMainTitle.Font = New Font(Me.Font.FontFamily, CSng(Me.Font.SizeInPoints * 1.16), Me.Font.Style, GraphicsUnit.Point)

        ' Scale the max size of the sidebar image for values other than 96 dpi, as it is specified in pixels
        Dim graphicsForDpi As Graphics = Me.CreateGraphics()
        picSidebarImg.MaximumSize = New Size(CInt(picSidebarImg.MaximumSize.Width * (graphicsForDpi.DpiX / 96)), CInt(picSidebarImg.MaximumSize.Height * (graphicsForDpi.DpiY / 96)))

        tblToolbars.Height = tbrToolbar.Height
        tbrToolbar.SetWholeDropDown(tbtOptionsMenu)
        tbrHelp.SetWholeDropDown(tbtHelpMenu)
        tbrHelp.Width = tbtHelpMenu.Rectangle.Width

        If Me.WindowState <> FormWindowState.Minimized Then
            tblToolbars.ColumnStyles(0) = New ColumnStyle(SizeType.Absolute, tblToolbars.Width - (tbtHelpMenu.Rectangle.Width + tbrHelp.Margin.Right))
            tblToolbars.ColumnStyles(1) = New ColumnStyle(SizeType.Absolute, tbtHelpMenu.Rectangle.Width + tbrHelp.Margin.Right)
        End If

        tmrCheckSub.Enabled = True
        clsProgData.StartDownload()
        tmrCheckForUpdates.Enabled = True
    End Sub

    Private Sub Main_FormClosing(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        If eventArgs.CloseReason = CloseReason.UserClosing Then
            Call TrayAnimate(Me, True)
            Me.Visible = False
            eventArgs.Cancel = True

            If My.Settings.ShownTrayBalloon = False Then
                nicTrayIcon.BalloonTipIcon = ToolTipIcon.Info
                nicTrayIcon.BalloonTipText = "Radio Downloader will continue to run in the background, so that it can download your subscriptions as soon as they become available." + vbCrLf + "Click here to hide this message in future."
                nicTrayIcon.BalloonTipTitle = "Radio Downloader is Still Running"
                nicTrayIcon.ShowBalloonTip(30000)
            End If
        End If
    End Sub

    Private Sub lstProviders_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lstProviders.SelectedIndexChanged
        If lstProviders.SelectedItems.Count > 0 Then
            Dim pluginId As Guid = New Guid(lstProviders.SelectedItems(0).Name)
            ShowProviderInfo(pluginId)
        Else
            Call SetViewDefaults()
        End If
    End Sub

    Private Sub ShowProviderInfo(ByVal providerId As Guid)
        Dim info As Data.ProviderData = clsProgData.FetchProviderData(providerId)
        Call SetSideBar(info.name, info.description, Nothing)

        If viwBackData(viwBackData.GetUpperBound(0)).View = View.FindNewChooseProvider Then
            SetToolbarButtons("ChooseProgramme")
        End If
    End Sub

    Private Sub lstProviders_ItemActivate(ByVal sender As Object, ByVal e As System.EventArgs) Handles lstProviders.ItemActivate
        ' Occasionally the event gets fired when there isn't an item selected
        If lstProviders.SelectedItems.Count = 0 Then
            Exit Sub
        End If

        Call tbtChooseProgramme_Click()
    End Sub

    Private Sub lstEpisodes_ItemCheck(ByVal sender As Object, ByVal e As System.Windows.Forms.ItemCheckEventArgs) Handles lstEpisodes.ItemCheck
        clsProgData.EpisodeSetAutoDownload(CInt(lstEpisodes.Items(e.Index).Name), e.NewValue = CheckState.Checked)
    End Sub

    Private Sub ShowEpisodeInfo(ByVal epid As Integer)
        Dim progInfo As Data.ProgrammeData = clsProgData.FetchProgrammeData(CInt(viwBackData(viwBackData.GetUpperBound(0)).ViewData))
        Dim epInfo As Data.EpisodeData = clsProgData.FetchEpisodeData(epid)
        Dim infoText As String = ""

        If epInfo.description IsNot Nothing Then
            infoText += epInfo.description + Environment.NewLine + Environment.NewLine
        End If

        infoText += "Date: " + epInfo.episodeDate.ToString("ddd dd/MMM/yy HH:mm", CultureInfo.CurrentCulture)
        infoText += ReadableDuration(epInfo.duration)

        Call SetSideBar(epInfo.name, infoText, clsProgData.FetchEpisodeImage(epid))

        If progInfo.subscribed Then
            Call SetToolbarButtons("Download,Unsubscribe")
        Else
            If progInfo.singleEpisode Then
                Call SetToolbarButtons("Download")
            Else
                Call SetToolbarButtons("Download,Subscribe")
            End If
        End If
    End Sub

    Private Sub lstEpisodes_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles lstEpisodes.SelectedIndexChanged
        If lstEpisodes.SelectedItems.Count > 0 Then
            Dim epid As Integer = CInt(lstEpisodes.SelectedItems(0).Name)
            Call ShowEpisodeInfo(epid)
        Else
            Call SetViewDefaults() ' Revert back to programme info in sidebar
        End If
    End Sub

    Private Sub lstSubscribed_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lstSubscribed.SelectedIndexChanged
        If lstSubscribed.SelectedItems.Count > 0 Then
            Dim progid As Integer = CInt(lstSubscribed.SelectedItems(0).Name)

            clsProgData.UpdateProgInfoIfRequired(progid)
            Call ShowSubscriptionInfo(progid)
        Else
            Call SetViewDefaults() ' Revert back to subscribed items view default sidebar and toolbar
        End If
    End Sub

    Private Sub ShowSubscriptionInfo(ByVal progid As Integer)
        Dim info As Data.SubscriptionData = clsProgData.FetchSubscriptionData(progid)

        Call SetSideBar(info.name, info.description, clsProgData.FetchProgrammeImage(progid))
        Call SetToolbarButtons("Unsubscribe,CurrentEps")
    End Sub

    Private Sub lstDownloads_ItemActivate(ByVal sender As Object, ByVal e As System.EventArgs) Handles lstDownloads.ItemActivate
        ' Occasionally the event gets fired when there isn't an item selected
        If lstDownloads.SelectedItems.Count = 0 Then
            Exit Sub
        End If

        Call tbtPlay_Click()
    End Sub

    Private Sub lstDownloads_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lstDownloads.SelectedIndexChanged
        If lstDownloads.SelectedItems.Count > 0 Then
            Call ShowDownloadInfo(CInt(lstDownloads.SelectedItems(0).Name))
        Else
            Call SetViewDefaults() ' Revert back to downloads view default sidebar and toolbar
        End If
    End Sub

    Private Sub ShowDownloadInfo(ByVal epid As Integer)
        Dim info As Data.DownloadData = clsProgData.FetchDownloadData(epid)

        Dim actionString As String
        Dim infoText As String = ""

        If info.description IsNot Nothing Then
            infoText += info.description + Environment.NewLine + Environment.NewLine
        End If

        infoText += "Date: " + info.episodeDate.ToString("ddd dd/MMM/yy HH:mm", CultureInfo.CurrentCulture)
        infoText += ReadableDuration(info.duration)

        Select Case info.status
            Case Data.DownloadStatus.Downloaded
                If File.Exists(info.downloadPath) Then
                    actionString = "Play,Delete"
                Else
                    actionString = "Delete"
                End If

                infoText += Environment.NewLine + "Play count: " + info.playCount.ToString
            Case Data.DownloadStatus.Errored
                Dim errorName As String = ""
                Dim errorDetails As String = info.errorDetails

                actionString = "Retry,Cancel"

                Select Case info.errorType
                    Case IRadioProvider.ErrorType.LocalProblem
                        errorName = "Local problem"
                    Case IRadioProvider.ErrorType.ShorterThanExpected
                        errorName = "Shorter than expected"
                    Case IRadioProvider.ErrorType.NotAvailable
                        errorName = "Not available"
                    Case IRadioProvider.ErrorType.NotAvailableInLocation
                        errorName = "Not available in your location"
                    Case IRadioProvider.ErrorType.NetworkProblem
                        errorName = "Network problem"
                    Case IRadioProvider.ErrorType.UnknownError
                        errorName = "Unknown error"
                        errorDetails = "An unknown error occurred when trying to download this programme.  Press the 'Report Error' button on the toolbar to send a report of this error back to NerdoftheHerd, so that it can be fixed."
                        actionString = "Retry,Cancel,ReportError"
                End Select

                infoText += Environment.NewLine + Environment.NewLine + "Error: " + errorName

                If errorDetails <> "" Then
                    infoText += Environment.NewLine + Environment.NewLine + errorDetails
                End If
            Case Else
                actionString = "Cancel"
        End Select

        Call SetSideBar(info.name, infoText, clsProgData.FetchEpisodeImage(epid))
        Call SetToolbarButtons(actionString)
    End Sub

    Private Function ReadableDuration(ByVal duration As Integer) As String
        Dim readable As String = ""

        If duration <> 0 Then
            readable += Environment.NewLine + "Duration: "

            Dim mins As Integer = duration \ 60
            Dim hours As Integer = mins \ 60
            mins = mins Mod 60

            If hours > 0 Then
                readable += CStr(hours) + "hr" + Plural(hours)
            End If

            If hours > 0 And mins > 0 Then
                readable += " "
            End If

            If mins > 0 Then
                readable += CStr(mins) + "min"
            End If
        End If

        Return readable
    End Function

    Private Sub SetSideBar(ByVal strTitle As String, ByVal strDescription As String, ByVal bmpPicture As Bitmap)
        lblSideMainTitle.Text = strTitle

        txtSideDescript.Text = strDescription

        ' Make sure the scrollbars update correctly
        txtSideDescript.ScrollBars = RichTextBoxScrollBars.None
        txtSideDescript.ScrollBars = RichTextBoxScrollBars.Both

        If bmpPicture IsNot Nothing Then
            If bmpPicture.Width > picSidebarImg.MaximumSize.Width Or bmpPicture.Height > picSidebarImg.MaximumSize.Height Then
                Dim intNewWidth As Integer
                Dim intNewHeight As Integer

                If bmpPicture.Width > bmpPicture.Height Then
                    intNewWidth = picSidebarImg.MaximumSize.Width
                    intNewHeight = CInt((intNewWidth / bmpPicture.Width) * bmpPicture.Height)
                Else
                    intNewHeight = picSidebarImg.MaximumSize.Height
                    intNewWidth = CInt((intNewHeight / bmpPicture.Height) * bmpPicture.Width)
                End If

                Dim bmpOrigImg As Bitmap = bmpPicture
                bmpPicture = New Bitmap(intNewWidth, intNewHeight)
                Dim graGraphics As Graphics

                graGraphics = Graphics.FromImage(bmpPicture)
                graGraphics.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic

                graGraphics.DrawImage(bmpOrigImg, 0, 0, intNewWidth, intNewHeight)

                bmpOrigImg.Dispose()
            End If

            picSidebarImg.Image = bmpPicture
            picSidebarImg.Visible = True
        Else
            picSidebarImg.Visible = False
        End If
    End Sub

    Private Sub SetToolbarButtons(ByVal buttons As String)
        Dim splitButtons() As String = Split(buttons, ",")

        tbtChooseProgramme.Visible = False
        tbtDownload.Visible = False
        tbtSubscribe.Visible = False
        tbtUnsubscribe.Visible = False
        tbtCurrentEps.Visible = False
        tbtPlay.Visible = False
        tbtCancel.Visible = False
        tbtDelete.Visible = False
        tbtRetry.Visible = False
        tbtReportError.Visible = False
        tbtCleanUp.Visible = False

        For Each loopButtons As String In splitButtons
            Select Case loopButtons
                Case "ChooseProgramme"
                    tbtChooseProgramme.Visible = True
                Case "Download"
                    tbtDownload.Visible = True
                Case "Subscribe"
                    tbtSubscribe.Visible = True
                Case "Unsubscribe"
                    tbtUnsubscribe.Visible = True
                Case "CurrentEps"
                    tbtCurrentEps.Visible = True
                Case "Play"
                    tbtPlay.Visible = True
                Case "Delete"
                    tbtDelete.Visible = True
                Case "Cancel"
                    tbtCancel.Visible = True
                Case "Retry"
                    tbtRetry.Visible = True
                Case "ReportError"
                    tbtReportError.Visible = True
                Case "CleanUp"
                    tbtCleanUp.Visible = True
            End Select
        Next
    End Sub

    Public Sub mnuTrayShow_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles mnuTrayShow.Click
        If Me.Visible = False Then
            Call TrayAnimate(Me, False)
            Me.Visible = True
        End If

        If Me.WindowState = FormWindowState.Minimized Then
            Me.WindowState = FormWindowState.Normal
        End If

        Me.Activate()
    End Sub

    Public Sub mnuTrayExit_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuTrayExit.Click
        Me.Close()
        Me.Dispose()

        Try
            Directory.Delete(Path.Combine(System.IO.Path.GetTempPath, "RadioDownloader"), True)
        Catch exp As IOException
            ' Ignore an IOException - this just means that a file in the temp folder is still in use.
        End Try
    End Sub

    Private Sub nicTrayIcon_BalloonTipClicked(ByVal sender As Object, ByVal e As System.EventArgs) Handles nicTrayIcon.BalloonTipClicked
        My.Settings.ShownTrayBalloon = True
    End Sub

    Private Sub nicTrayIcon_MouseDoubleClick(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles nicTrayIcon.MouseDoubleClick
        Call mnuTrayShow_Click(sender, e)
    End Sub

    Private Sub tmrCheckSub_Tick(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles tmrCheckSub.Tick
        Call clsProgData.CheckSubscriptions()
    End Sub

    Private Sub tbtFindNew_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tbtFindNew.Click
        Call SetView(MainTab.FindProgramme, View.FindNewChooseProvider, Nothing)
    End Sub

    Private Sub tbtSubscriptions_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tbtSubscriptions.Click
        Call SetView(MainTab.Subscriptions, View.Subscriptions, Nothing)
    End Sub

    Private Sub tbtDownloads_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tbtDownloads.Click
        Call SetView(MainTab.Downloads, View.Downloads, Nothing)
    End Sub

    Private Sub clsProgData_ProviderAdded(ByVal providerId As System.Guid) Handles clsProgData.ProviderAdded
        If Me.InvokeRequired Then
            ' Events will sometimes be fired on a different thread to the ui
            Me.BeginInvoke(New clsProgData_ProviderAdded_Delegate(AddressOf clsProgData_ProviderAdded), providerId)
            Return
        End If

        Dim info As Data.ProviderData = clsProgData.FetchProviderData(providerId)

        Dim addItem As New ListViewItem
        addItem.Name = providerId.ToString
        addItem.Text = info.name

        If info.icon IsNot Nothing Then
            imlProviders.Images.Add(providerId.ToString, info.icon)
            addItem.ImageKey = providerId.ToString
        Else
            addItem.ImageKey = "default"
        End If

        lstProviders.Items.Add(addItem)

        ' Hide the 'No providers' provider options menu item
        If mnuOptionsProviderOptsNoProvs.Visible = True Then
            mnuOptionsProviderOptsNoProvs.Visible = False
        End If

        Dim addMenuItem As New MenuItem(info.name + " Provider")

        If info.showOptionsHandler IsNot Nothing Then
            AddHandler addMenuItem.Click, info.showOptionsHandler
        Else
            addMenuItem.Enabled = False
        End If

        mnuOptionsProviderOpts.MenuItems.Add(addMenuItem)

        If viwBackData(viwBackData.GetUpperBound(0)).View = View.FindNewChooseProvider Then
            If lstProviders.SelectedItems.Count = 0 Then
                ' Update the displayed statistics
                SetViewDefaults()
            End If
        End If
    End Sub

    Private Sub clsProgData_ProgrammeUpdated(ByVal progid As Integer) Handles clsProgData.ProgrammeUpdated
        If Me.InvokeRequired Then
            ' Events will sometimes be fired on a different thread to the ui
            Me.BeginInvoke(New clsProgData_Programme_Delegate(AddressOf clsProgData_ProgrammeUpdated), progid)
            Return
        End If

        If viwBackData(viwBackData.GetUpperBound(0)).View = View.ProgEpisodes Then
            If CInt(viwBackData(viwBackData.GetUpperBound(0)).ViewData) = progid Then
                If lstEpisodes.SelectedItems.Count = 0 Then
                    ' Update the displayed programme information
                    Call ShowProgrammeInfo(progid)
                Else
                    ' Update the displayed episode information (in case the subscription status has changed)
                    Dim epid As Integer = CInt(lstEpisodes.SelectedItems(0).Name)
                    Call ShowEpisodeInfo(epid)
                End If
            End If
        End If
    End Sub

    Private Sub ShowProgrammeInfo(ByVal progid As Integer)
        Dim progInfo As Data.ProgrammeData = clsProgData.FetchProgrammeData(CInt(viwBackData(viwBackData.GetUpperBound(0)).ViewData))

        If progInfo.subscribed Then
            Call SetToolbarButtons("Unsubscribe")
        Else
            If progInfo.singleEpisode Then
                Call SetToolbarButtons("")
            Else
                Call SetToolbarButtons("Subscribe")
            End If
        End If

        Call SetSideBar(progInfo.name, progInfo.description, clsProgData.FetchProgrammeImage(progid))
    End Sub

    Private Sub EpisodeListItem(ByVal epid As Integer, ByVal info As Data.EpisodeData, ByRef item As ListViewItem)
        item.Name = epid.ToString
        item.Text = info.episodeDate.ToShortDateString
        item.SubItems(1).Text = info.name
        item.Checked = info.autoDownload
    End Sub

    Private Sub clsProgData_EpisodeAdded(ByVal epid As Integer) Handles clsProgData.EpisodeAdded
        If Me.InvokeRequired Then
            ' Events will sometimes be fired on a different thread to the ui
            Me.BeginInvoke(New clsProgData_Episode_Delegate(AddressOf clsProgData_EpisodeAdded), epid)
            Return
        End If

        Dim info As Data.EpisodeData = clsProgData.FetchEpisodeData(epid)

        Dim addItem As New ListViewItem
        addItem.SubItems.Add("")

        EpisodeListItem(epid, info, addItem)

        RemoveHandler lstEpisodes.ItemCheck, AddressOf lstEpisodes_ItemCheck
        lstEpisodes.Items.Add(addItem)
        AddHandler lstEpisodes.ItemCheck, AddressOf lstEpisodes_ItemCheck
    End Sub

    Private Sub SubscriptionListItem(ByVal progid As Integer, ByVal info As Data.SubscriptionData, ByRef item As ListViewItem)
        item.Name = progid.ToString
        item.Text = info.name

        If info.latestDownload = Nothing Then
            item.SubItems(1).Text = "Never"
        Else
            item.SubItems(1).Text = info.latestDownload.ToShortDateString
        End If

        item.SubItems(2).Text = info.providerName
        item.ImageKey = "subscribed"
    End Sub

    Private Sub clsProgData_SubscriptionAdded(ByVal progid As Integer) Handles clsProgData.SubscriptionAdded
        If Me.InvokeRequired Then
            ' Events will sometimes be fired on a different thread to the ui
            Me.BeginInvoke(New clsProgData_Programme_Delegate(AddressOf clsProgData_SubscriptionAdded), progid)
            Return
        End If

        Dim info As Data.SubscriptionData = clsProgData.FetchSubscriptionData(progid)

        Dim addItem As New ListViewItem
        addItem.SubItems.Add("")
        addItem.SubItems.Add("")

        SubscriptionListItem(progid, info, addItem)
        lstSubscribed.Items.Add(addItem)

        If viwBackData(viwBackData.GetUpperBound(0)).View = View.Subscriptions Then
            If lstSubscribed.SelectedItems.Count = 0 Then
                ' Update the displayed statistics
                SetViewDefaults()
            End If
        End If
    End Sub

    Private Sub clsProgData_SubscriptionUpdated(ByVal progid As Integer) Handles clsProgData.SubscriptionUpdated
        If Me.InvokeRequired Then
            ' Events will sometimes be fired on a different thread to the ui
            Me.BeginInvoke(New clsProgData_Programme_Delegate(AddressOf clsProgData_SubscriptionUpdated), progid)
            Return
        End If

        Dim info As Data.SubscriptionData = clsProgData.FetchSubscriptionData(progid)
        Dim item As ListViewItem = lstSubscribed.Items(progid.ToString)

        SubscriptionListItem(progid, info, item)

        If viwBackData(viwBackData.GetUpperBound(0)).View = View.Subscriptions Then
            If lstSubscribed.Items(progid.ToString).Selected Then
                ShowSubscriptionInfo(progid)
            ElseIf lstSubscribed.SelectedItems.Count = 0 Then
                ' Update the displayed statistics
                SetViewDefaults()
            End If
        End If
    End Sub

    Private Sub clsProgData_SubscriptionRemoved(ByVal progid As Integer) Handles clsProgData.SubscriptionRemoved
        If Me.InvokeRequired Then
            ' Events will sometimes be fired on a different thread to the ui
            Me.BeginInvoke(New clsProgData_Programme_Delegate(AddressOf clsProgData_SubscriptionRemoved), progid)
            Return
        End If

        If viwBackData(viwBackData.GetUpperBound(0)).View = View.Subscriptions Then
            If lstSubscribed.SelectedItems.Count = 0 Then
                ' Update the displayed statistics
                SetViewDefaults()
            End If
        End If

        lstSubscribed.Items(progid.ToString).Remove()
    End Sub

    Private Sub DownloadListItem(ByVal epid As Integer, ByVal info As Data.DownloadData, ByRef item As ListViewItem)
        item.Name = epid.ToString
        item.Text = info.name

        item.SubItems(1).Text = info.episodeDate.ToShortDateString

        Select Case info.status
            Case Data.DownloadStatus.Waiting
                item.SubItems(2).Text = "Waiting"
                item.ImageKey = "waiting"
            Case Data.DownloadStatus.Downloaded
                If info.playCount = 0 Then
                    item.SubItems(2).Text = "Newly Downloaded"
                    item.ImageKey = "downloaded_new"
                Else
                    item.SubItems(2).Text = "Downloaded"
                    item.ImageKey = "downloaded"
                End If
            Case Data.DownloadStatus.Errored
                item.SubItems(2).Text = "Error"
                item.ImageKey = "error"
        End Select
    End Sub

    Private Sub clsProgData_DownloadAdded(ByVal epid As Integer) Handles clsProgData.DownloadAdded
        If Me.InvokeRequired Then
            ' Events will sometimes be fired on a different thread to the ui
            Me.BeginInvoke(New clsProgData_Episode_Delegate(AddressOf clsProgData_DownloadAdded), epid)
            Return
        End If

        Dim info As Data.DownloadData = clsProgData.FetchDownloadData(epid)

        Dim addItem As New ListViewItem
        addItem.SubItems.Add("")
        addItem.SubItems.Add("")
        addItem.SubItems.Add("")

        DownloadListItem(epid, info, addItem)
        lstDownloads.Items.Add(addItem)

        If viwBackData(viwBackData.GetUpperBound(0)).View = View.Downloads Then
            If lstDownloads.SelectedItems.Count = 0 Then
                ' Update the displayed statistics
                SetViewDefaults()
            End If
        End If
    End Sub

    Private Sub clsProgData_DownloadProgress(ByVal epid As Integer, ByVal percent As Integer, ByVal statusText As String, ByVal icon As IRadioProvider.ProgressIcon) Handles clsProgData.DownloadProgress
        If Me.InvokeRequired Then
            ' Events will sometimes be fired on a different thread to the ui
            Me.BeginInvoke(New clsProgData_DownloadProgress_Delegate(AddressOf clsProgData_DownloadProgress), New Object() {epid, percent, statusText, icon})
            Return
        End If

        Dim item As ListViewItem = lstDownloads.Items(CStr(epid))

        item.SubItems(2).Text = statusText
        prgDldProg.Value = percent

        If lstDownloads.Controls.Count = 0 Then
            lstDownloads.AddProgressBar(prgDldProg, item, downloadProgCol)
        End If

        Select Case icon
            Case IRadioProvider.ProgressIcon.Downloading
                item.ImageKey = "downloading"
            Case IRadioProvider.ProgressIcon.Converting
                item.ImageKey = "converting"
        End Select

        'Call SetTrayStatus(True)
    End Sub

    Private Sub clsProgData_DownloadRemoved(ByVal epid As Integer) Handles clsProgData.DownloadRemoved
        If Me.InvokeRequired Then
            ' Events will sometimes be fired on a different thread to the ui
            Me.BeginInvoke(New clsProgData_Episode_Delegate(AddressOf clsProgData_DownloadRemoved), epid)
            Return
        End If

        If viwBackData(viwBackData.GetUpperBound(0)).View = View.Downloads Then
            If lstDownloads.SelectedItems.Count = 0 Then
                ' Update the displayed statistics
                SetViewDefaults()
            End If
        End If

        Dim item As ListViewItem = lstDownloads.Items(epid.ToString)

        If lstDownloads.GetProgressBar(item, downloadProgCol) IsNot Nothing Then
            lstDownloads.RemoveProgressBar(prgDldProg)
        End If

        item.Remove()
    End Sub

    Private Sub clsProgData_DownloadUpdated(ByVal epid As Integer) Handles clsProgData.DownloadUpdated
        If Me.InvokeRequired Then
            ' Events will sometimes be fired on a different thread to the ui
            Me.BeginInvoke(New clsProgData_Episode_Delegate(AddressOf clsProgData_DownloadUpdated), epid)
            Return
        End If

        Dim info As Data.DownloadData = clsProgData.FetchDownloadData(epid)
        Dim item As ListViewItem = lstDownloads.Items(epid.ToString)

        DownloadListItem(epid, info, item)

        If lstDownloads.GetProgressBar(item, downloadProgCol) IsNot Nothing Then
            lstDownloads.RemoveProgressBar(prgDldProg)
        End If

        If viwBackData(viwBackData.GetUpperBound(0)).View = View.Downloads Then
            If lstDownloads.Items(epid.ToString).Selected Then
                ShowDownloadInfo(epid)
            ElseIf lstDownloads.SelectedItems.Count = 0 Then
                ' Update the displayed statistics
                SetViewDefaults()
            End If
        End If
    End Sub

    Private Sub clsProgData_FindNewViewChange(ByVal objView As Object) Handles clsProgData.FindNewViewChange
        Dim ChangedView As ViewStore = viwBackData(viwBackData.GetUpperBound(0))
        Dim FindViewData As FindNewViewData = DirectCast(ChangedView.ViewData, FindNewViewData)

        FindViewData.View = objView
        ChangedView.ViewData = FindViewData

        Call StoreView(ChangedView)
        Call UpdateNavCtrlState()
    End Sub

    Private Sub clsProgData_FoundNew(ByVal intProgID As Integer) Handles clsProgData.FoundNew
        Call SetView(MainTab.FindProgramme, View.ProgEpisodes, intProgID)
    End Sub

    Private Sub Main_Shown(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Shown
        For Each commandLineArg As String In Environment.GetCommandLineArgs
            If commandLineArg.ToUpperInvariant = "/HIDEMAINWINDOW" Then
                Call TrayAnimate(Me, True)
                Me.Visible = False
            End If
        Next
    End Sub

    Private Sub tmrCheckForUpdates_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tmrCheckForUpdates.Tick
        If clsUpdate.IsUpdateAvailable Then
            If My.Settings.LastUpdatePrompt.AddDays(7) < Now Then
                My.Settings.LastUpdatePrompt = Now
                My.Settings.Save() ' Save the last prompt time in case of unexpected termination

                If MsgBox("A new version of Radio Downloader is available." + Environment.NewLine + "Would you like to visit the website to download it now?", MsgBoxStyle.YesNo Or MsgBoxStyle.Question, "Radio Downloader") = MsgBoxResult.Yes Then
                    Process.Start("http://www.nerdoftheherd.com/tools/radiodld/")
                End If
            End If
        End If
    End Sub

    Private Sub tbtSubscribe_Click()
        Dim progid As Integer = CInt(viwBackData(viwBackData.GetUpperBound(0)).ViewData)

        If clsProgData.AddSubscription(progid) Then
            Call SetView(MainTab.Subscriptions, View.Subscriptions, Nothing)
        Else
            Call MsgBox("You are already subscribed to this programme!", MsgBoxStyle.Exclamation)
        End If
    End Sub

    Private Sub tbtUnsubscribe_Click()
        Dim progid As Integer

        Select Case viwBackData(viwBackData.GetUpperBound(0)).View
            Case View.ProgEpisodes
                progid = CInt(viwBackData(viwBackData.GetUpperBound(0)).ViewData)
            Case View.Subscriptions
                progid = CInt(lstSubscribed.SelectedItems(0).Name)
        End Select

        If MsgBox("Are you sure you would like to stop having new episodes of this programme downloaded automatically?", MsgBoxStyle.Question Or MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
            Call clsProgData.RemoveSubscription(progid)
        End If
    End Sub

    Private Sub tbtCancel_Click()
        Dim epid As Integer = CInt(lstDownloads.SelectedItems(0).Name)

        If MsgBox("Are you sure that you would like to stop downloading this programme?", MsgBoxStyle.Question Or MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
            clsProgData.DownloadRemove(epid)
        End If
    End Sub

    Private Sub tbtPlay_Click()
        Dim epid As Integer = CInt(lstDownloads.SelectedItems(0).Name)
        Dim info As Data.DownloadData = clsProgData.FetchDownloadData(epid)

        If info.status = Data.DownloadStatus.Downloaded Then
            If File.Exists(info.downloadPath) Then
                Process.Start(info.downloadPath)

                ' Bump the play count of this item up by one
                clsProgData.DownloadBumpPlayCount(epid)
            End If
        End If
    End Sub

    Private Sub tbtDelete_Click()
        Dim epid As Integer = CInt(lstDownloads.SelectedItems(0).Name)
        Dim info As Data.DownloadData = clsProgData.FetchDownloadData(epid)

        Dim fileExists As Boolean = File.Exists(info.downloadPath)
        Dim delQuestion As String = "Are you sure that you would like to delete this episode"

        If fileExists Then
            delQuestion += " and the associated audio file"
        End If

        If MsgBox(delQuestion + "?", MsgBoxStyle.Question Or MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
            If fileExists Then
                Try
                    File.Delete(info.downloadPath)
                Catch ioExp As IOException
                    If MsgBox("There was a problem deleting the audio file for this episode, as the file is in use by another application." + Environment.NewLine + Environment.NewLine + "Would you like to delete the episode from the list anyway?", MsgBoxStyle.Exclamation Or MsgBoxStyle.YesNo) = MsgBoxResult.No Then
                        Exit Sub
                    End If
                End Try
            End If

            clsProgData.DownloadRemove(epid)
        End If
    End Sub

    Private Sub tbtRetry_Click()
        Call clsProgData.ResetDownload(CInt(lstDownloads.SelectedItems(0).Name))
    End Sub

    Private Sub tbtDownload_Click()
        Dim epid As Integer = CInt(lstEpisodes.SelectedItems(0).Name)

        If clsProgData.AddDownload(epid) Then
            Call SetView(MainTab.Downloads, View.Downloads, Nothing)
        Else
            Call MsgBox("This episode is already in the download list!", MsgBoxStyle.Exclamation)
        End If
    End Sub

    Private Sub tbtCurrentEps_Click()
        Dim progid As Integer = CInt(lstSubscribed.SelectedItems(0).Name)
        Call SetView(MainTab.Subscriptions, View.ProgEpisodes, progid)
    End Sub

    Private Sub tbtReportError_Click()
        Dim episodeID As Integer = CInt(lstDownloads.SelectedItems(0).Name)
        clsProgData.DownloadReportError(episodeID)
    End Sub

    Private Sub tbtChooseProgramme_Click()
        Dim viewData As FindNewViewData
        viewData.ProviderID = New Guid(lstProviders.SelectedItems(0).Name)
        viewData.View = Nothing

        Call SetView(MainTab.FindProgramme, View.FindNewProviderForm, viewData)
    End Sub

    Private Sub mnuOptionsShowOpts_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles mnuOptionsShowOpts.Click
        Call Preferences.ShowDialog()
    End Sub

    Private Sub mnuOptionsExit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles mnuOptionsExit.Click
        Call mnuTrayExit_Click(mnuTrayExit, e)
    End Sub

    Private Sub mnuHelpAbout_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles mnuHelpAbout.Click
        Call About.ShowDialog()
    End Sub

    Private Sub mnuHelpShowHelp_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles mnuHelpShowHelp.Click
        Process.Start("http://www.nerdoftheherd.com/tools/radiodld/help/")
    End Sub

    Private Sub mnuHelpReportBug_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles mnuHelpReportBug.Click
        Process.Start("http://www.nerdoftheherd.com/tools/radiodld/bug_report.php")
    End Sub

    Private Sub tbtCleanUp_Click()
        Call CleanUp.ShowDialog()

        'Call clsProgData.UpdateDlList(lstDownloads)
    End Sub

    Private Sub StoreView(ByVal ViewData As ViewStore)
        ReDim Preserve viwBackData(viwBackData.GetUpperBound(0) + 1)
        viwBackData(viwBackData.GetUpperBound(0)) = ViewData

        If viwFwdData.GetUpperBound(0) > -1 Then
            ReDim viwFwdData(-1)
        End If
    End Sub

    Private Sub SetView(ByVal Tab As MainTab, ByVal View As View, ByVal ViewData As Object)
        Dim ViewDataStore As ViewStore

        ViewDataStore.Tab = Tab
        ViewDataStore.View = View
        ViewDataStore.ViewData = ViewData

        Call StoreView(ViewDataStore)
        Call PerformViewChanges(ViewDataStore)
    End Sub

    Private Sub UpdateNavCtrlState()
        tbtBack.Enabled = viwBackData.GetUpperBound(0) > 0
        tbtForward.Enabled = viwFwdData.GetUpperBound(0) > -1
    End Sub

    Private Sub PerformViewChanges(ByVal ViewData As ViewStore)
        Call UpdateNavCtrlState()

        tbtFindNew.Checked = False
        tbtFavourites.Checked = False
        tbtSubscriptions.Checked = False
        tbtDownloads.Checked = False

        Select Case ViewData.Tab
            Case MainTab.FindProgramme
                tbtFindNew.Checked = True
            Case MainTab.Favourites
                tbtFavourites.Checked = True
            Case MainTab.Subscriptions
                tbtSubscriptions.Checked = True
            Case MainTab.Downloads
                tbtDownloads.Checked = True
        End Select

        SetViewDefaults(ViewData)

        ' Set the focus to a control which does not show it, to prevent the toolbar momentarily showing focus
        lblSideMainTitle.Focus()

        lstProviders.Visible = False
        pnlPluginSpace.Visible = False
        lstEpisodes.Visible = False
        lstSubscribed.Visible = False
        lstDownloads.Visible = False

        Select Case ViewData.View
            Case View.FindNewChooseProvider
                lstProviders.Visible = True
                lstProviders.Focus()

                If lstProviders.SelectedItems.Count > 0 Then
                    ShowProviderInfo(New Guid(lstProviders.SelectedItems(0).Name))
                End If
            Case View.FindNewProviderForm
                Dim FindViewData As FindNewViewData = DirectCast(ViewData.ViewData, FindNewViewData)

                pnlPluginSpace.Visible = True
                pnlPluginSpace.Controls.Clear()
                pnlPluginSpace.Controls.Add(clsProgData.GetFindNewPanel(FindViewData.ProviderID, FindViewData.View))
                pnlPluginSpace.Controls(0).Dock = DockStyle.Fill
                pnlPluginSpace.Controls(0).Focus()
            Case View.ProgEpisodes
                lstEpisodes.Visible = True
                clsProgData.CancelEpisodeListing()
                lstEpisodes.Items.Clear() ' Clear before DoEvents so that old items don't flash up on screen
                Application.DoEvents() ' Give any queued BeginInvoke calls a chance to be processed
                lstEpisodes.Items.Clear()
                clsProgData.InitEpisodeList(CInt(ViewData.ViewData))
            Case View.Subscriptions
                lstSubscribed.Visible = True
                lstSubscribed.Focus()

                If lstSubscribed.SelectedItems.Count > 0 Then
                    ShowSubscriptionInfo(CInt(lstSubscribed.SelectedItems(0).Name))
                End If
            Case View.Downloads
                lstDownloads.Visible = True
                lstDownloads.Focus()

                If lstDownloads.SelectedItems.Count > 0 Then
                    ShowDownloadInfo(CInt(lstDownloads.SelectedItems(0).Name))
                End If
        End Select
    End Sub

    Private Sub SetViewDefaults()
        SetViewDefaults(viwBackData(viwBackData.GetUpperBound(0)))
    End Sub

    Private Sub SetViewDefaults(ByVal ViewData As ViewStore)
        Select Case ViewData.View
            Case View.FindNewChooseProvider
                Call SetToolbarButtons("")
                Call SetSideBar(CStr(lstProviders.Items.Count) + " provider" + Plural(lstProviders.Items.Count), "", Nothing)
            Case View.FindNewProviderForm
                Dim FindViewData As FindNewViewData = DirectCast(ViewData.ViewData, FindNewViewData)
                Call SetToolbarButtons("")
                Call ShowProviderInfo(FindViewData.ProviderID)
            Case View.ProgEpisodes
                Dim progid As Integer = CInt(ViewData.ViewData)
                Call ShowProgrammeInfo(progid)
            Case View.Subscriptions
                Call SetToolbarButtons("")
                Call SetSideBar(CStr(lstSubscribed.Items.Count) + " subscription" + Plural(lstSubscribed.Items.Count), "", Nothing)
            Case View.Downloads
                Call SetToolbarButtons("CleanUp")

                Dim description As String = ""
                Dim newCount As Integer = clsProgData.CountDownloadsNew
                Dim errorCount As Integer = clsProgData.CountDownloadsErrored

                If newCount > 0 Then
                    description += "Newly downloaded: " + CStr(newCount) + Environment.NewLine
                End If

                If errorCount > 0 Then
                    description += "Errored: " + CStr(errorCount)
                End If

                Call SetSideBar(CStr(lstDownloads.Items.Count) + " download" + Plural(lstDownloads.Items.Count), description, Nothing)
        End Select
    End Sub

    Private Function Plural(ByVal intNumber As Integer) As String
        If intNumber = 1 Then
            Return ""
        Else
            Return "s"
        End If
    End Function

    Private Sub tbtBack_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tbtBack.Click
        ReDim Preserve viwFwdData(viwFwdData.GetUpperBound(0) + 1)
        viwFwdData(viwFwdData.GetUpperBound(0)) = viwBackData(viwBackData.GetUpperBound(0))
        ReDim Preserve viwBackData(viwBackData.GetUpperBound(0) - 1)

        Call PerformViewChanges(viwBackData(viwBackData.GetUpperBound(0)))
    End Sub

    Private Sub tbtForward_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tbtForward.Click
        ReDim Preserve viwBackData(viwBackData.GetUpperBound(0) + 1)
        viwBackData(viwBackData.GetUpperBound(0)) = viwFwdData(viwFwdData.GetUpperBound(0))
        ReDim Preserve viwFwdData(viwFwdData.GetUpperBound(0) - 1)

        Call PerformViewChanges(viwBackData(viwBackData.GetUpperBound(0)))
    End Sub

    Private Sub tbrToolbar_ButtonClick(ByVal sender As System.Object, ByVal e As System.Windows.Forms.ToolBarButtonClickEventArgs) Handles tbrToolbar.ButtonClick
        Select Case e.Button.Name
            Case "tbtChooseProgramme"
                Call tbtChooseProgramme_Click()
            Case "tbtDownload"
                Call tbtDownload_Click()
            Case "tbtSubscribe"
                Call tbtSubscribe_Click()
            Case "tbtUnsubscribe"
                Call tbtUnsubscribe_Click()
            Case "tbtCurrentEps"
                Call tbtCurrentEps_Click()
            Case "tbtCancel"
                Call tbtCancel_Click()
            Case "tbtPlay"
                Call tbtPlay_Click()
            Case "tbtDelete"
                Call tbtDelete_Click()
            Case "tbtRetry"
                Call tbtRetry_Click()
            Case "tbtReportError"
                Call tbtReportError_Click()
            Case "tbtCleanUp"
                Call tbtCleanUp_Click()
        End Select
    End Sub

    Private Sub tblToolbars_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles tblToolbars.Resize
        If Me.WindowState <> FormWindowState.Minimized Then
            tblToolbars.ColumnStyles(0) = New ColumnStyle(SizeType.Absolute, tblToolbars.Width - (tbtHelpMenu.Rectangle.Width + tbrHelp.Margin.Right))
            tblToolbars.ColumnStyles(1) = New ColumnStyle(SizeType.Absolute, tbtHelpMenu.Rectangle.Width + tbrHelp.Margin.Right)

            If VisualStyleRenderer.IsSupported Then
                ' Visual styles are enabled, so draw the correct background behind the toolbars

                Dim bmpBackground As New Bitmap(tblToolbars.Width, tblToolbars.Height)
                Dim graGraphics As Graphics = Graphics.FromImage(bmpBackground)

                Try
                    Dim vsrRebar As New VisualStyleRenderer("Rebar", 0, 0)
                    vsrRebar.DrawBackground(graGraphics, New Rectangle(0, 0, tblToolbars.Width, tblToolbars.Height))
                    tblToolbars.BackgroundImage = bmpBackground
                Catch expArgument As ArgumentException
                    ' The 'Rebar' background image style did not exist, so don't try to draw it.
                End Try
            End If
        End If
    End Sub

    Private Sub txtSideDescript_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtSideDescript.GotFocus
        lblSideMainTitle.Focus()
    End Sub

    Private Sub txtSideDescript_LinkClicked(ByVal sender As Object, ByVal e As System.Windows.Forms.LinkClickedEventArgs) Handles txtSideDescript.LinkClicked
        Process.Start(e.LinkText)
    End Sub

    Private Sub txtSideDescript_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtSideDescript.Resize
        txtSideDescript.Refresh() ' Make sure the scrollbars update correctly
    End Sub

    Private Sub picSideBarBorder_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles picSideBarBorder.Paint
        e.Graphics.DrawLine(New Pen(Color.FromArgb(255, 167, 186, 197)), 0, 0, 0, picSideBarBorder.Height)
    End Sub
End Class

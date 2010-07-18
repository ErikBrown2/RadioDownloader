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

Imports System.IO

Friend Class Preferences
    Inherits System.Windows.Forms.Form

    Private cancelClose As Boolean
	
	Private Sub cmdChangeFolder_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles cmdChangeFolder.Click
        Dim BrowseDialog As New FolderBrowserDialog
        BrowseDialog.SelectedPath = txtSaveIn.Text
        BrowseDialog.Description = "Choose the folder to save downloaded programmes in:"

        If BrowseDialog.ShowDialog = Windows.Forms.DialogResult.OK Then
            txtSaveIn.Text = BrowseDialog.SelectedPath
        End If
	End Sub
	
	Private Sub cmdOK_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles cmdOK.Click
        If txtFileNameFormat.Text = String.Empty Then
            MsgBox("Please enter a value for the downloaded programme file name format.", MsgBoxStyle.Exclamation)
            txtFileNameFormat.Focus()
            cancelClose = True
            Exit Sub
        End If

        My.Settings.RunOnStartup = uxRunOnStartup.Checked
        My.Settings.SaveFolder = txtSaveIn.Text
        My.Settings.FileNameFormat = txtFileNameFormat.Text
        My.Settings.RunAfterCommand = txtRunAfter.Text

        If OsUtils.WinSevenOrLater Then
            My.Settings.CloseToSystray = uxCloseToSystray.Checked
        End If

        OsUtils.ApplyRunOnStartup()
        My.Settings.Save()
    End Sub

    Private Sub Preferences_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        If cancelClose Then
            ' Prevent the form from being automatically closed if it failed validation
            e.Cancel = True
            cancelClose = False
        End If
    End Sub

    Private Sub Preferences_Load(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles MyBase.Load
        Me.Font = SystemFonts.MessageBoxFont

        uxRunOnStartup.Checked = My.Settings.RunOnStartup

        If OsUtils.WinSevenOrLater Then
            uxCloseToSystray.Checked = My.Settings.CloseToSystray
        Else
            uxCloseToSystray.Checked = True
            uxCloseToSystray.Enabled = False
        End If

        Try
            txtSaveIn.Text = FileUtils.GetSaveFolder()
        Catch dirNotFoundExp As DirectoryNotFoundException
            txtSaveIn.Text = My.Settings.SaveFolder
        End Try

        txtFileNameFormat.Text = My.Settings.FileNameFormat
        txtRunAfter.Text = My.Settings.RunAfterCommand
    End Sub

    Private Sub txtFileNameFormat_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtFileNameFormat.TextChanged
        lblFilenameFormatResult.Text = "Result: " + FileUtils.CreateSaveFileName(txtFileNameFormat.Text, "Programme Name", "Episode Name", Now) + ".mp3"
    End Sub

    Private Sub cmdReset_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdReset.Click
        If MsgBox("Are you sure that you would like to reset all of your settings?", MsgBoxStyle.YesNo Or MsgBoxStyle.Question) = MsgBoxResult.Yes Then
            My.Settings.RunOnStartup = CBool(My.Settings.Properties.Item("RunOnStartup").DefaultValue)
            My.Settings.CloseToSystray = CBool(My.Settings.Properties.Item("CloseToSystray").DefaultValue)
            My.Settings.SaveFolder = My.Settings.Properties.Item("SaveFolder").DefaultValue.ToString
            My.Settings.FileNameFormat = My.Settings.Properties.Item("FileNameFormat").DefaultValue.ToString
            My.Settings.RunAfterCommand = My.Settings.Properties.Item("RunAfterCommand").DefaultValue.ToString

            OsUtils.ApplyRunOnStartup()
            My.Settings.Save()

            Me.Close()
        End If
    End Sub
End Class
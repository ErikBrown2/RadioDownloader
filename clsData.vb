' Utility to automatically download radio programmes, using a plugin framework for provider specific implementation.
' Copyright � 2007  www.nerdoftheherd.com
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
Imports System.Threading
Imports System.Data.SQLite
Imports System.Text.RegularExpressions

Friend Class clsData
    Public Enum Statuses
        Waiting
        Downloaded
        Errored
    End Enum

    Private Const strSqlDateFormat As String = "yyyy-MM-dd HH:mm"

    Private sqlConnection As SQLiteConnection
    Private clsPluginsInst As clsPlugins

    Private clsCurDldProgData As clsDldProgData
    Private thrDownloadThread As Thread
    Private WithEvents DownloadPluginInst As IRadioProvider

    Public Event AddStationToList(ByRef strStationName As String, ByRef strStationId As String, ByRef strStationType As String, ByVal booVisible As Boolean)
    Public Event AddProgramToList(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal strProgramName As String)
    Public Event Progress(ByVal clsCurDldProgData As clsDldProgData, ByVal intPercent As Integer, ByVal strStatusText As String, ByVal Icon As IRadioProvider.ProgressIcon)
    Public Event DldError(ByVal clsCurDldProgData As clsDldProgData, ByVal errType As IRadioProvider.ErrorType, ByVal strErrorDetails As String)
    Public Event Finished(ByVal clsCurDldProgData As clsDldProgData)

    Public Sub New()
        MyBase.New()

        sqlConnection = New SQLiteConnection("Data Source=" + GetAppDataFolder() + "\store.db;Version=3;New=False")
        sqlConnection.Open()

        clsPluginsInst = New clsPlugins(My.Application.Info.DirectoryPath)
    End Sub

    Protected Overrides Sub Finalize()
        sqlConnection.Close()
        Call AbortDownloadThread()
        MyBase.Finalize()
    End Sub

    Public Sub SetErrored(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal dteProgramDate As Date, ByVal errType As IRadioProvider.ErrorType, ByVal strErrorDetails As String)
        Dim sqlCommand As New SQLiteCommand("UPDATE tblDownloads SET Status=" + CStr(Statuses.Errored) + ", ErrorTime=""" + Now.ToString(strSqlDateFormat) + """, ErrorType=" + CStr(CInt(errType)) + ", ErrorDetails=""" + strErrorDetails.Replace("""", """""") + """ WHERE type=""" & strProgramType & """ and Station=""" + strStationID + """ AND ID=""" & strProgramID & """ AND date=""" + dteProgramDate.ToString(strSqlDateFormat) + """", sqlConnection)
        sqlCommand.ExecuteNonQuery()
    End Sub

    Public Sub SetDownloaded(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal dteProgramDate As Date, ByVal strDownloadPath As String)
        Dim sqlCommand As New SQLiteCommand("UPDATE tblDownloads SET Status=""" + CStr(Statuses.Downloaded) + """, Path=""" + strDownloadPath + """ WHERE type=""" & strProgramType & """ and Station=""" + strStationID + """ AND ID=""" & strProgramID & """ AND date=""" + dteProgramDate.ToString(strSqlDateFormat) + """", sqlConnection)
        sqlCommand.ExecuteNonQuery()
    End Sub

    Public Sub FindAndDownload()
        Const lngMaxErrors As Integer = 2

        If thrDownloadThread Is Nothing Then
            Dim sqlCommand As New SQLiteCommand("select status, type, station, id, date from tblDownloads where status=" + CStr(Statuses.Waiting) + " or (status=" + CStr(Statuses.Errored) + " and ErrorCount<" + lngMaxErrors.ToString + ") order by date", sqlConnection)
            Dim sqlReader As SQLiteDataReader = sqlCommand.ExecuteReader

            With sqlReader
                While thrDownloadThread Is Nothing
                    If sqlReader.Read() Then
                        If clsPluginsInst.PluginExists(.GetString(.GetOrdinal("Type"))) Then
                            If IsProgramStillAvailable(.GetString(.GetOrdinal("Type")), .GetString(.GetOrdinal("Station")), .GetString(.GetOrdinal("ID")), .GetDateTime(.GetOrdinal("Date"))) = False Then
                                Call RemoveDownload(.GetString(.GetOrdinal("Type")), .GetString(.GetOrdinal("Station")), .GetString(.GetOrdinal("ID")), .GetDateTime(.GetOrdinal("Date")))
                            Else
                                If .GetInt32(.GetOrdinal("Status")) = Statuses.Errored Then
                                    Call ResetDownload(.GetString(.GetOrdinal("Type")), .GetString(.GetOrdinal("Station")), .GetString(.GetOrdinal("ID")), .GetDateTime(.GetOrdinal("Date")), True)
                                End If

                                clsCurDldProgData = New clsDldProgData
                                clsCurDldProgData.ProgramType = .GetString(.GetOrdinal("Type"))
                                clsCurDldProgData.StationID = .GetString(.GetOrdinal("Station"))
                                clsCurDldProgData.ProgramID = .GetString(.GetOrdinal("ID"))
                                clsCurDldProgData.ProgramDate = .GetDateTime(.GetOrdinal("Date"))
                                clsCurDldProgData.ProgramDuration = ProgramDuration(.GetString(.GetOrdinal("Type")), .GetString(.GetOrdinal("Station")), .GetString(.GetOrdinal("ID")), .GetDateTime(.GetOrdinal("Date")))
                                clsCurDldProgData.DownloadUrl = ProgramDldUrl(.GetString(.GetOrdinal("Type")), .GetString(.GetOrdinal("Station")), .GetString(.GetOrdinal("ID")), .GetDateTime(.GetOrdinal("Date")))
                                clsCurDldProgData.FinalName = FindFreeSaveFileName(My.Settings.FileNameFormat, ProgramTitle(.GetString(.GetOrdinal("Type")), .GetString(.GetOrdinal("Station")), .GetString(.GetOrdinal("ID")), .GetDateTime(.GetOrdinal("Date"))), "mp3", .GetDateTime(.GetOrdinal("Date")), GetSaveFolder())
                                clsCurDldProgData.BandwidthLimit = My.Settings.BandwidthLimit

                                thrDownloadThread = New Thread(AddressOf DownloadProgThread)
                                thrDownloadThread.Start()
                            End If
                        End If
                    Else
                        Exit While
                    End If
                End While

                sqlReader.Close()
            End With
        End If
    End Sub

    Public Sub DownloadProgThread()
        DownloadPluginInst = clsPluginsInst.GetPluginInstance(clsCurDldProgData.ProgramType)
        DownloadPluginInst.DownloadProgram(clsCurDldProgData.StationID, clsCurDldProgData.ProgramID, clsCurDldProgData.ProgramDate, clsCurDldProgData.ProgramDuration, clsCurDldProgData.DownloadUrl, clsCurDldProgData.FinalName, clsCurDldProgData.BandwidthLimit)
    End Sub

    Public Function GetDownloadPath(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal dteProgramDate As Date) As String
        Dim sqlCommand As New SQLiteCommand("SELECT path FROM tblDownloads WHERE type=""" + strProgramType + """ and Station=""" + strStationID + """ AND ID=""" + strProgramID + """ AND date=""" + dteProgramDate.ToString(strSqlDateFormat) + """", sqlConnection)
        Dim sqlReader As SQLiteDataReader = sqlCommand.ExecuteReader

        sqlReader.Read()
        GetDownloadPath = sqlReader.GetString(sqlReader.GetOrdinal("Path"))

        sqlReader.Close()
    End Function

    Public Function DownloadStatus(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal dteProgramDate As Date) As Statuses
        Dim sqlCommand As New SQLiteCommand("SELECT status FROM tblDownloads WHERE type=""" + strProgramType + """ and Station=""" + strStationID + """ AND ID=""" + strProgramID + """ AND date=""" + dteProgramDate.ToString(strSqlDateFormat) + """", sqlConnection)
        Dim sqlReader As SQLiteDataReader = sqlCommand.ExecuteReader

        sqlReader.Read()
        DownloadStatus = DirectCast(sqlReader.GetInt32(sqlReader.GetOrdinal("Status")), Statuses)

        sqlReader.Close()
    End Function

    Public Function ProgramDuration(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal dteProgramDate As Date) As Integer
        Dim sqlCommand As New SQLiteCommand("SELECT duration FROM tblInfo WHERE type=""" + strProgramType + """ and Station=""" + strStationID + """ AND ID=""" & strProgramID + """ AND date=""" + dteProgramDate.ToString(strSqlDateFormat) + """", sqlConnection)
        Dim sqlReader As SQLiteDataReader = sqlCommand.ExecuteReader

        sqlReader.Read()
        ProgramDuration = sqlReader.GetInt32(sqlReader.GetOrdinal("Duration"))

        sqlReader.Close()
    End Function

    Public Function ProgramTitle(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal dteProgramDate As Date) As String
        Dim sqlCommand As New SQLiteCommand("SELECT name FROM tblInfo WHERE type=""" & strProgramType & """ and Station=""" + strStationID + """ AND ID=""" & strProgramID & """ AND date=""" + dteProgramDate.ToString(strSqlDateFormat) + """", sqlConnection)
        Dim sqlReader As SQLiteDataReader = sqlCommand.ExecuteReader

        sqlReader.Read()
        ProgramTitle = sqlReader.GetString(sqlReader.GetOrdinal("Name"))

        sqlReader.Close()
    End Function

    Public Function ProgramDescription(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal dteProgramDate As Date) As String
        Dim sqlCommand As New SQLiteCommand("SELECT description FROM tblInfo WHERE type=""" & strProgramType & """ and Station=""" + strStationID + """ AND ID=""" & strProgramID & """ AND date=""" + dteProgramDate.ToString(strSqlDateFormat) + """", sqlConnection)
        Dim sqlReader As SQLiteDataReader = sqlCommand.ExecuteReader

        sqlReader.Read()
        ProgramDescription = sqlReader.GetString(sqlReader.GetOrdinal("description"))

        sqlReader.Close()
    End Function

    Public Function ProgramDetails(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal dteProgramDate As Date) As String
        ProgramDetails = ProgramDescription(strProgramType, strStationID, strProgramID, dteProgramDate) + vbCrLf + vbCrLf
        ProgramDetails += "Date: " + dteProgramDate.ToString("ddd dd/MMM/yy HH:mm") + vbCrLf
        ProgramDetails += "Duration: "

        Dim lngDuration As Integer = ProgramDuration(strProgramType, strStationID, strProgramID, dteProgramDate)
        Dim lngHours As Integer = lngDuration \ 60
        Dim lngMins As Integer = lngDuration Mod 60

        If lngHours > 0 Then
            ProgramDetails += CStr(lngHours) + "hr"
            If lngHours > 1 Then
                ProgramDetails += "s"
            End If
        End If
        If lngHours > 0 And lngMins > 0 Then
            ProgramDetails += " "
        End If
        If lngMins > 0 Then
            ProgramDetails += CStr(lngMins) + "min"
        End If
    End Function

    Public Function ProgramImage(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal dteProgramDate As Date) As Bitmap
        Dim sqlCommand As New SQLiteCommand("SELECT image FROM tblInfo WHERE type=""" & strProgramType & """ and Station=""" + strStationID + """ AND ID=""" & strProgramID & """ AND date=""" + dteProgramDate.ToString(strSqlDateFormat) + """", sqlConnection)
        Dim sqlReader As SQLiteDataReader = sqlCommand.ExecuteReader

        sqlReader.Read()

        Dim objBlob As Object = sqlReader.GetValue(sqlReader.GetOrdinal("image")) 

        If IsDBNull(objBlob) = False Then
            Dim stmStream As New MemoryStream(CType(objBlob, Byte()))
            ProgramImage = New Bitmap(stmStream)
        Else
            ProgramImage = Nothing
        End If

        sqlReader.Close()
    End Function

    Private Function ProgramDldUrl(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal dteProgramDate As Date) As String
        Dim sqlCommand As New SQLiteCommand("SELECT downloadurl FROM tblInfo WHERE type=""" & strProgramType & """ and Station=""" + strStationID + """ AND ID=""" & strProgramID & """ AND date=""" + dteProgramDate.ToString(strSqlDateFormat) + """", sqlConnection)
        Dim sqlReader As SQLiteDataReader = sqlCommand.ExecuteReader

        sqlReader.Read()
        ProgramDldUrl = sqlReader.GetString(sqlReader.GetOrdinal("DownloadUrl"))

        sqlReader.Close()
    End Function

    Public Sub UpdateDlList(ByRef lstListview As ExtListView, ByRef prgProgressBar As ProgressBar)
        Dim comCommand As New SQLiteCommand("select Type,Station,ID,Date,Status,PlayCount from tblDownloads order by Date desc", sqlConnection)
        Dim sqlReader As SQLiteDataReader = comCommand.ExecuteReader()

        lstListview.RemoveAllControls()

        Dim lstItem As ListViewItem
        Dim booErrorStatus As Boolean = False
        Dim intExistingPos As Integer = 0

        Do While sqlReader.Read()
            Dim strUniqueId As String = sqlReader.GetDateTime(sqlReader.GetOrdinal("Date")).ToString & "||" + sqlReader.GetString(sqlReader.GetOrdinal("ID")) + "||" + sqlReader.GetString(sqlReader.GetOrdinal("Station")) + "||" + sqlReader.GetString(sqlReader.GetOrdinal("Type"))

            If lstListview.Items.Count - 1 < intExistingPos Then
                lstItem = lstListview.Items.Add(strUniqueId, "", 0)
            Else
                While lstListview.Items.ContainsKey(strUniqueId) And CStr(lstListview.Items(intExistingPos).Name) <> strUniqueId
                    lstListview.Items.RemoveAt(intExistingPos)
                End While

                If CStr(lstListview.Items(intExistingPos).Name) = strUniqueId Then
                    lstItem = lstListview.Items(intExistingPos)
                Else
                    lstItem = lstListview.Items.Insert(intExistingPos, strUniqueId, "", 0)
                End If
            End If

            intExistingPos += 1

            lstItem.SubItems.Clear()
            lstItem.Name = strUniqueId
            lstItem.Text = ProgramTitle(sqlReader.GetString(sqlReader.GetOrdinal("Type")), sqlReader.GetString(sqlReader.GetOrdinal("Station")), sqlReader.GetString(sqlReader.GetOrdinal("ID")), sqlReader.GetDateTime(sqlReader.GetOrdinal("Date"))) '+ " " + strUniqueId

            lstItem.SubItems.Add(sqlReader.GetDateTime(sqlReader.GetOrdinal("Date")).ToShortDateString())

            Select Case sqlReader.GetInt32(sqlReader.GetOrdinal("Status"))
                Case Statuses.Waiting
                    lstItem.SubItems.Add("Waiting")
                    lstItem.ImageKey = "waiting"
                Case Statuses.Downloaded
                    If sqlReader.GetInt32(sqlReader.GetOrdinal("PlayCount")) > 0 Then
                        lstItem.SubItems.Add("Downloaded")
                        lstItem.ImageKey = "downloaded"
                    Else
                        lstItem.SubItems.Add("Newly Downloaded")
                        lstItem.ImageKey = "downloaded_new"
                    End If
                Case Statuses.Errored
                    lstItem.SubItems.Add("Error")
                    lstItem.ImageKey = "error"
                    booErrorStatus = True
            End Select

            lstItem.SubItems.Add("")
        Loop

        If booErrorStatus Then
            frmMain.SetTrayStatus(False, frmMain.ErrorStatus.Error)
        Else
            frmMain.SetTrayStatus(False, frmMain.ErrorStatus.Normal)
        End If

        sqlReader.Close()
    End Sub

    Public Sub UpdateSubscrList(ByRef lstListview As ListView)
        Dim lstAdd As ListViewItem

        Dim sqlCommand As New SQLiteCommand("select type, station, id from tblSubscribed", sqlConnection)
        Dim sqlReader As SQLiteDataReader = sqlCommand.ExecuteReader

        lstListview.Items.Clear()

        With sqlReader
            Do While .Read()
                Call GetLatest(.GetString(.GetOrdinal("Type")), .GetString(.GetOrdinal("Station")), .GetString(.GetOrdinal("ID")))

                lstAdd = New ListViewItem

                Dim strDynamicTitle As String
                strDynamicTitle = ProgramTitle(.GetString(.GetOrdinal("Type")), .GetString(.GetOrdinal("Station")), .GetString(.GetOrdinal("ID")), LatestDate(.GetString(.GetOrdinal("Type")), .GetString(.GetOrdinal("Station")), .GetString(.GetOrdinal("ID"))))

                If ProviderDynamicSubscriptionName(.GetString(.GetOrdinal("Type"))) Then
                    lstAdd.Text = strDynamicTitle
                Else
                    lstAdd.Text = SubscriptionName(.GetString(.GetOrdinal("Type")), .GetString(.GetOrdinal("Station")), .GetString(.GetOrdinal("ID")))

                    If lstAdd.Text = "" Then
                        lstAdd.Text = strDynamicTitle
                    End If
                End If

                lstAdd.SubItems.Add(StationName(.GetString(.GetOrdinal("Type")), .GetString(.GetOrdinal("Station"))))
                lstAdd.SubItems.Add(ProviderName(.GetString(.GetOrdinal("Type"))))
                lstAdd.Tag = .GetString(.GetOrdinal("Type")) + "||" + .GetString(.GetOrdinal("Station")) + "||" + .GetString(.GetOrdinal("ID"))
                lstAdd.ImageKey = "subscribed"

                lstListview.Items.Add(lstAdd)
            Loop
        End With

        sqlReader.Close()
    End Sub

    Private Function SubscriptionName(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String) As String
        Dim sqlCommand As New SQLiteCommand("SELECT SubscriptionName FROM tblSubscribed WHERE type=""" & strProgramType & """ and Station=""" + strStationID + """ AND ID=""" & strProgramID & """", sqlConnection)
        Dim sqlReader As SQLiteDataReader = sqlCommand.ExecuteReader

        sqlReader.Read()

        If IsDBNull(sqlReader.GetValue(sqlReader.GetOrdinal("SubscriptionName"))) Then
            SubscriptionName = ""
        Else
            SubscriptionName = sqlReader.GetString(sqlReader.GetOrdinal("SubscriptionName"))
        End If

        sqlReader.Close()
    End Function

    Public Sub CheckSubscriptions(ByRef lstList As ExtListView, ByRef tmrTimer As System.Windows.Forms.Timer, ByRef prgDldProg As ProgressBar)
        Dim sqlCommand As New SQLiteCommand("select type, station, id from tblSubscribed", sqlConnection)
        Dim sqlReader As SQLiteDataReader = sqlCommand.ExecuteReader

        With sqlReader
            Do While .Read()
                Call GetLatest(.GetString(.GetOrdinal("Type")), .GetString(sqlReader.GetOrdinal("Station")), .GetString(.GetOrdinal("ID")))

                Dim sqlComCheckDld As New SQLiteCommand("select id from tblDownloads where type=""" + .GetString(.GetOrdinal("Type")) + """ and Station=""" + .GetString(.GetOrdinal("Station")) + """ and ID=""" + .GetString(.GetOrdinal("ID")) + """ and Date=""" + LatestDate(.GetString(.GetOrdinal("Type")), .GetString(.GetOrdinal("Station")), .GetString(.GetOrdinal("ID"))).ToString(strSqlDateFormat) + """", sqlConnection)
                Dim sqlRdrCheckDld As SQLiteDataReader = sqlComCheckDld.ExecuteReader

                If sqlRdrCheckDld.Read() = False Then
                    Call AddDownload(.GetString(.GetOrdinal("Type")), .GetString(sqlReader.GetOrdinal("Station")), .GetString(.GetOrdinal("ID")))
                    Call UpdateDlList(lstList, prgDldProg)
                    tmrTimer.Enabled = True
                End If

                sqlRdrCheckDld.Close()
            Loop
        End With

        sqlReader.Close()
    End Sub

    Public Function AddDownload(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String) As Boolean
        Call GetLatest(strProgramType, strStationID, strProgramID)

        Dim sqlCommand As New SQLiteCommand("SELECT id FROM tblDownloads WHERE type=""" + strProgramType + """ and Station=""" + strStationID + """ AND ID=""" & strProgramID + """ AND date=""" + LatestDate(strProgramType, strStationID, strProgramID).ToString(strSqlDateFormat) + """", sqlConnection)
        Dim sqlReader As SQLiteDataReader = sqlCommand.ExecuteReader

        If sqlReader.Read() Then
            Return False
        End If

        sqlReader.Close()

        sqlCommand = New SQLiteCommand("INSERT INTO tblDownloads (Type, Station, ID, Date, Status) VALUES (""" + strProgramType + """, """ + strStationID + """, """ + strProgramID + """, """ + LatestDate(strProgramType, strStationID, strProgramID).ToString(strSqlDateFormat) + """, " + CStr(Statuses.Waiting) + ")", sqlConnection)
        Call sqlCommand.ExecuteNonQuery()

        Return True
    End Function

    Public Function AddSubscription(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal strSubscriptionName As String) As Boolean
        Dim sqlCommand As New SQLiteCommand("INSERT INTO tblSubscribed (Type, Station, ID, SubscriptionName) VALUES (""" + strProgramType + """, """ + strStationID + """, """ + strProgramID + """, """ + strSubscriptionName.Replace("""", """""") + """)", sqlConnection)

        Try
            Call sqlCommand.ExecuteNonQuery()
        Catch e As SQLiteException
            ' Probably trying to create duplicate in database, ie trying to subscribe twice!
            Return False
        End Try

        Return True
    End Function

    Public Sub RemoveSubscription(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String)
        Dim sqlCommand As New SQLiteCommand("DELETE FROM tblSubscribed WHERE type=""" & strProgramType & """ and Station=""" + strStationID + """ AND ID=""" & strProgramID & """", sqlConnection)
        Call sqlCommand.ExecuteNonQuery()
    End Sub

    Public Sub GetLatest(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String)
        If HaveLatest(strProgramType, strStationID, strProgramID) = False Then
            Call StoreLatestInfo(strProgramType, strStationID, strProgramID)
        End If
    End Sub

    Public Function LatestDate(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String) As DateTime
        Dim sqlCommand As New SQLiteCommand("SELECT date FROM tblInfo WHERE type=""" + strProgramType + """  and Station=""" + strStationID + """ and ID=""" & strProgramID & """ ORDER BY Date DESC", sqlConnection)
        Dim sqlReader As SQLiteDataReader = sqlCommand.ExecuteReader

        With sqlReader
            If .Read = False Then
                Return Nothing
            End If

            LatestDate = .GetDateTime(.GetOrdinal("Date"))
        End With

        sqlReader.Close()
    End Function

    Private Function HaveLatest(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String) As Boolean
        Dim dteLatest As Date
        dteLatest = LatestDate(strProgramType, strStationID, strProgramID)

        If dteLatest = Nothing Then
            Return False
        End If

        If clsPluginsInst.PluginExists(strProgramType) Then
            Dim ThisInstance As IRadioProvider
            ThisInstance = clsPluginsInst.GetPluginInstance(strProgramType)
            Return ThisInstance.CouldBeNewEpisode(strStationID, strProgramID, dteLatest) = False
        Else
            ' As we can't check if we have the latest, just assume that we do for the moment
            Return True
        End If
    End Function

    Private Sub StoreLatestInfo(ByVal strProgramType As String, ByVal strStationID As String, ByRef strProgramID As String)
        If clsPluginsInst.PluginExists(strProgramType) = False Then
            ' The plugin type for this program is unavailable, so no point trying to call it
            Exit Sub
        End If

        Dim sqlCommand As SQLiteCommand
        Dim sqlReader As SQLiteDataReader
        Dim dteLastAttempt As Date = Nothing
        Dim ThisInstance As IRadioProvider

        ThisInstance = clsPluginsInst.GetPluginInstance(strProgramType)

        sqlCommand = New SQLiteCommand("SELECT LastTry FROM tblLastFetch WHERE type=""" + strProgramType + """ and Station=""" + strStationID + """ and ID=""" & strProgramID & """", sqlConnection)
        sqlReader = sqlCommand.ExecuteReader

        If sqlReader.Read Then
            dteLastAttempt = sqlReader.GetDateTime(sqlReader.GetOrdinal("LastTry"))
        End If

        Dim ProgInfo As IRadioProvider.ProgramInfo
        ProgInfo = ThisInstance.GetLatestProgramInfo(strStationID, strProgramID, LatestDate(strProgramType, strStationID, strProgramID), dteLastAttempt)

        If ProgInfo.Result = IRadioProvider.ProgInfoResult.Success Then
            sqlCommand = New SQLiteCommand("SELECT id FROM tblInfo WHERE type=""" + strProgramType + """ and Station=""" + strStationID + """ and ID=""" & strProgramID & """ AND Date=""" + ProgInfo.ProgramDate.ToString(strSqlDateFormat) + """", sqlConnection)
            sqlReader = sqlCommand.ExecuteReader

            If sqlReader.Read = False Then
                sqlCommand = New SQLiteCommand("INSERT INTO tblInfo (Type, Station, ID, Date, Name, Description, DownloadUrl, Image, Duration) VALUES (""" + strProgramType + """, """ + strStationID + """, """ + strProgramID + """, """ + ProgInfo.ProgramDate.ToString(strSqlDateFormat) + """, """ + ProgInfo.ProgramName.Replace("""", """""") + """, """ + ProgInfo.ProgramDescription.Replace("""", """""") + """, """ + ProgInfo.ProgramDldUrl.Replace("""", """""") + """, @image, " + CStr(ProgInfo.ProgramDuration) + ")", sqlConnection)
                Dim sqlImage As SQLiteParameter

                If ProgInfo.Image Is Nothing = False Then
                    ' Convert the image into a byte array
                    Dim mstImage As New MemoryStream()
                    ProgInfo.Image.Save(mstImage, Imaging.ImageFormat.Bmp)
                    Dim bteImage(CInt(mstImage.Length - 1)) As Byte
                    mstImage.Position = 0
                    mstImage.Read(bteImage, 0, CInt(mstImage.Length))

                    ' Add the image as a parameter
                    sqlImage = New SQLiteParameter("@image", bteImage)
                Else
                    sqlImage = New SQLiteParameter("@image", Nothing)
                End If


                sqlCommand.Parameters.Add(sqlImage)
                sqlCommand.ExecuteNonQuery()
            End If

            sqlReader.Close()
        End If

        If ProgInfo.Result <> IRadioProvider.ProgInfoResult.Skipped And ProgInfo.Result <> IRadioProvider.ProgInfoResult.TempError Then
            ' Remove the previous record of when we tried to download info about this program (if it exists)
            sqlCommand = New SQLiteCommand("DELETE FROM tblLastFetch WHERE type=""" + strProgramType + """ and Station=""" + strStationID + """ and ID=""" & strProgramID & """", sqlConnection)
            Call sqlCommand.ExecuteNonQuery()

            ' Record when we tried to get information about this program
            sqlCommand = New SQLiteCommand("INSERT INTO tblLastFetch (Type, Station, ID, LastTry) VALUES (""" + strProgramType + """, """ + strStationID + """, """ & strProgramID & """, """ + Now.ToString(strSqlDateFormat) + """)", sqlConnection)
            Call sqlCommand.ExecuteNonQuery()
        End If
    End Sub

    Public Sub ResetDownload(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal dteProgramDate As Date, ByVal booAuto As Boolean)
        Dim sqlCommand As New SQLiteCommand("update tblDownloads set status=" + CStr(Statuses.Waiting) + " where type=""" + strProgramType + """ and Station=""" + strStationID + """ and id=""" + strProgramID + """ and date=""" + dteProgramDate.ToString(strSqlDateFormat) + """", sqlConnection)
        sqlCommand.ExecuteNonQuery()

        If booAuto Then
            sqlCommand = New SQLiteCommand("update tblDownloads set ErrorCount=ErrorCount+1 where type=""" + strProgramType + """ and Station=""" + strStationID + """ and id=""" + strProgramID + """ and date=""" + dteProgramDate.ToString(strSqlDateFormat) + """", sqlConnection)
            sqlCommand.ExecuteNonQuery()
        Else
            sqlCommand = New SQLiteCommand("update tblDownloads set ErrorCount=0 where type=""" + strProgramType + """ and Station=""" + strStationID + """ and id=""" + strProgramID + """ and date=""" + dteProgramDate.ToString(strSqlDateFormat) + """", sqlConnection)
            sqlCommand.ExecuteNonQuery()
        End If
    End Sub

    Public Sub RemoveDownload(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal dteProgramDate As Date)
        Dim sqlCommand As New SQLiteCommand("DELETE FROM tblDownloads WHERE type=""" + strProgramType + """ and Station=""" + strStationID + """ AND ID=""" + strProgramID + """ AND Date=""" + dteProgramDate.ToString(strSqlDateFormat) + """", sqlConnection)
        Call sqlCommand.ExecuteNonQuery()
    End Sub

    Public Sub StartListingProgrammes(ByVal strProgramType As String, ByVal strStationID As String)
        If clsPluginsInst.PluginExists(strProgramType) = False Then
            ' Plugin doesn't exist at the moment, so we can't list the station
            Exit Sub
        End If

        Dim ThisInstance As IRadioProvider
        ThisInstance = clsPluginsInst.GetPluginInstance(strProgramType)

        Dim Programs() As IRadioProvider.ProgramListItem
        Programs = ThisInstance.ListProgramIDs(strStationID)

        For Each SingleProg As IRadioProvider.ProgramListItem In Programs
            Dim strProgTitle As String
            strProgTitle = SingleProg.ProgramName
            RaiseEvent AddProgramToList(strProgramType, SingleProg.StationID, SingleProg.ProgramID, strProgTitle)
        Next
    End Sub

    Public Function IsProgramStillAvailable(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal dteProgramDate As Date) As Boolean
        Dim booIsLatestProg As Boolean

        Call GetLatest(strProgramType, strStationID, strProgramID)
        booIsLatestProg = (dteProgramDate = LatestDate(strProgramType, strStationID, strProgramID))

        If clsPluginsInst.PluginExists(strProgramType) = False Then
            ' Guess that the program is still available, as otherwise if the plugin is unavailable for a 
            ' short time, downloads will be deleted that should be downloaded when it becomes available again.
            Return True
        End If

        Dim ThisInstance As IRadioProvider
        ThisInstance = clsPluginsInst.GetPluginInstance(strProgramType)

        Return ThisInstance.IsStillAvailable(strStationID, strProgramID, dteProgramDate, booIsLatestProg)
    End Function

    Public Function StationName(ByVal strProgramType As String, ByVal strStationID As String) As String
        If clsPluginsInst.PluginExists(strProgramType) = False Then
            Return ""
        End If

        Dim ThisInstance As IRadioProvider
        ThisInstance = clsPluginsInst.GetPluginInstance(strProgramType)

        Return ThisInstance.ReturnStations.Item(strStationID).StationName
    End Function

    Public Function ProviderName(ByVal strProviderID As String) As String
        If clsPluginsInst.PluginExists(strProviderID) = False Then
            Return ""
        End If

        Dim ThisInstance As IRadioProvider
        ThisInstance = clsPluginsInst.GetPluginInstance(strProviderID)

        Return ThisInstance.ProviderName
    End Function

    Public Function ProviderDynamicSubscriptionName(ByVal strProviderID As String) As Boolean
        If clsPluginsInst.PluginExists(strProviderID) = False Then
            ' Just choose an option, as there is no way of finding out when the plugin is not available
            Return True
        End If

        Dim ThisInstance As IRadioProvider
        ThisInstance = clsPluginsInst.GetPluginInstance(strProviderID)

        Return ThisInstance.DynamicSubscriptionName
    End Function

    Public Sub IncreasePlayCount(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal dteProgramDate As Date)
        Dim sqlCommand As New SQLiteCommand("update tblDownloads set PlayCount=PlayCount+1 where type=""" + strProgramType + """ and Station=""" + strStationID + """ and id=""" + strProgramID + """ and date=""" + dteProgramDate.ToString(strSqlDateFormat) + """", sqlConnection)
        sqlCommand.ExecuteNonQuery()
    End Sub

    Public Function PlayCount(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal dteProgramDate As Date) As Integer
        Dim sqlCommand As New SQLiteCommand("SELECT PlayCount FROM tblDownloads WHERE type=""" + strProgramType + """ and Station=""" + strStationID + """ and id=""" + strProgramID + """ and date=""" + dteProgramDate.ToString(strSqlDateFormat) + """", sqlConnection)
        Dim sqlReader As SQLiteDataReader = sqlCommand.ExecuteReader

        With sqlReader
            If .Read Then
                PlayCount = .GetInt32(.GetOrdinal("PlayCount"))
            End If
        End With

        sqlReader.Close()
    End Function

    Public Function ErrorType(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal dteProgramDate As Date) As IRadioProvider.ErrorType
        Dim sqlCommand As New SQLiteCommand("SELECT ErrorType FROM tblDownloads WHERE type=""" + strProgramType + """ and Station=""" + strStationID + """ and id=""" + strProgramID + """ and date=""" + dteProgramDate.ToString(strSqlDateFormat) + """", sqlConnection)
        Dim sqlReader As SQLiteDataReader = sqlCommand.ExecuteReader

        sqlReader.Read()

        If IsDBNull(sqlReader.GetValue(sqlReader.GetOrdinal("ErrorType"))) Then
            ErrorType = IRadioProvider.ErrorType.UnknownError
        Else
            ErrorType = CType(sqlReader.GetInt32(sqlReader.GetOrdinal("ErrorType")), IRadioProvider.ErrorType)
        End If

        sqlReader.Close()
    End Function

    Public Function ErrorDetails(ByVal strProgramType As String, ByVal strStationID As String, ByVal strProgramID As String, ByVal dteProgramDate As Date) As String
        Dim sqlCommand As New SQLiteCommand("SELECT ErrorDetails FROM tblDownloads WHERE type=""" + strProgramType + """ and Station=""" + strStationID + """ and id=""" + strProgramID + """ and date=""" + dteProgramDate.ToString(strSqlDateFormat) + """", sqlConnection)
        Dim sqlReader As SQLiteDataReader = sqlCommand.ExecuteReader

        sqlReader.Read()

        If IsDBNull(sqlReader.GetValue(sqlReader.GetOrdinal("ErrorDetails"))) Then
            ErrorDetails = ""
        Else
            ErrorDetails = sqlReader.GetString(sqlReader.GetOrdinal("ErrorDetails"))
        End If

        sqlReader.Close()
    End Function

    Private Sub DownloadPluginInst_DldError(ByVal errType As IRadioProvider.ErrorType, ByVal strErrorDetails As String) Handles DownloadPluginInst.DldError
        RaiseEvent DldError(clsCurDldProgData, errType, strErrorDetails)
        thrDownloadThread = Nothing
        clsCurDldProgData = Nothing
    End Sub

    Private Sub DownloadPluginInst_Finished() Handles DownloadPluginInst.Finished
        RaiseEvent Finished(clsCurDldProgData)
        thrDownloadThread = Nothing
        clsCurDldProgData = Nothing
    End Sub

    Private Sub DownloadPluginInst_Progress(ByVal intPercent As Integer, ByVal strStatusText As String, ByVal Icon As IRadioProvider.ProgressIcon) Handles DownloadPluginInst.Progress
        RaiseEvent Progress(clsCurDldProgData, intPercent, strStatusText, Icon)
    End Sub

    Public Sub AbortDownloadThread()
        If thrDownloadThread Is Nothing = False Then
            thrDownloadThread.Abort()
            thrDownloadThread = Nothing
        End If
    End Sub

    Public Function GetCurrentDownloadInfo() As clsDldProgData
        Return clsCurDldProgData
    End Function

    Public Sub StartListingStations(ByVal booFullList As Boolean)
        Dim strPluginIDs() As String
        strPluginIDs = clsPluginsInst.GetPluginIdList

        Dim ThisInstance As IRadioProvider

        For Each strPluginID As String In strPluginIDs
            ThisInstance = clsPluginsInst.GetPluginInstance(strPluginID)

            For Each NewStation As IRadioProvider.StationInfo In ThisInstance.ReturnStations.SortedValues
                If booFullList = False Then
                    If IsStationVisible(strPluginID, NewStation.StationUniqueID, NewStation.VisibleByDefault) = False Then
                        Continue For
                    End If
                End If

                RaiseEvent AddStationToList(NewStation.StationName, NewStation.StationUniqueID, ThisInstance.ProviderUniqueID, IsStationVisible(strPluginID, NewStation.StationUniqueID, NewStation.VisibleByDefault))
            Next
        Next
    End Sub

    Private Function IsStationVisible(ByVal strPluginID As String, ByVal strStationID As String, ByVal booDefault As Boolean) As Boolean
        Dim sqlCommand As New SQLiteCommand("select Visible from tblStationVisibility where ProviderID=""" + strPluginID + """ and StationID=""" + strStationID + """", sqlConnection)
        Dim sqlReader As SQLiteDataReader = sqlCommand.ExecuteReader

        If sqlReader.Read() Then
            If sqlReader.GetBoolean(sqlReader.GetOrdinal("Visible")) Then
                IsStationVisible = True
            Else
                IsStationVisible = False
            End If
        Else
            If booDefault Then
                IsStationVisible = True
            Else
                IsStationVisible = False
            End If
        End If

        sqlReader.Close()
    End Function

    Public Sub SetStationVisibility(ByVal strStationType As String, ByVal strStationID As String, ByVal booVisible As Boolean)
        Dim sqlCommand As New SQLiteCommand("select Visible from tblStationVisibility where ProviderID=""" + strStationType + """ and StationID=""" + strStationID + """", sqlConnection)
        Dim sqlReader As SQLiteDataReader = sqlCommand.ExecuteReader

        Dim strVisible As String
        If booVisible Then
            strVisible = "1"
        Else
            strVisible = "0"
        End If


        If sqlReader.Read Then
            sqlCommand = New SQLiteCommand("update tblStationVisibility set Visible=" + strVisible + " where ProviderID=""" + strStationType + """ and StationID=""" + strStationID + """", sqlConnection)
            sqlCommand.ExecuteNonQuery()
        Else
            sqlCommand = New SQLiteCommand("insert into tblStationVisibility (ProviderID, StationID, Visible) VALUES (""" + strStationType + """, """ + strStationID + """, " + strVisible + ")", sqlConnection)
            Call sqlCommand.ExecuteNonQuery()
        End If

        sqlReader.Close()
    End Sub
End Class
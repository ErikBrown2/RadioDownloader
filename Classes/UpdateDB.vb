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

Option Explicit On
Option Strict On

Imports System.Collections.Generic
Imports System.Data.SQLite
Imports System.Text.RegularExpressions

Friend Class UpdateDB
    Private specConn As SQLiteConnection
    Private updateConn As SQLiteConnection

    Public Sub New(ByVal specDbPath As String, ByVal updateDbPath As String)
        MyBase.New()

        specConn = New SQLiteConnection("Data Source=" + specDbPath + ";Version=3;New=False")
        specConn.Open()

        updateConn = New SQLiteConnection("Data Source=" + updateDbPath + ";Version=3;New=False")
        updateConn.Open()
    End Sub

    Public Function UpdateStructure() As Boolean
        Dim specCommand As New SQLiteCommand("SELECT name, sql FROM sqlite_master WHERE type='table'", specConn)
        Dim specReader As SQLiteDataReader = specCommand.ExecuteReader

        Dim updateCommand As SQLiteCommand
        Dim updateReader As SQLiteDataReader

        With specReader
            While .Read()
                updateCommand = New SQLiteCommand("SELECT sql FROM sqlite_master WHERE type='table' AND name='" + .GetString(.GetOrdinal("name")).Replace("'", "''") + "'", updateConn)
                updateReader = updateCommand.ExecuteReader

                If updateReader.Read Then
                    ' The table exists in the database already, so check that it has the correct structure.
                    If .GetString(.GetOrdinal("sql")) <> updateReader.GetString(updateReader.GetOrdinal("sql")) Then
                        ' The structure of the table doesn't match, so update it

                        ' Store a list of common column names for transferring the data
                        Dim columnNames As String = ColNames(.GetString(.GetOrdinal("name")))

                        updateReader.Close()

                        ' Start a transaction, so we can roll back if there is an error
                        updateCommand = New SQLiteCommand("BEGIN TRANSACTION", updateConn)
                        updateCommand.ExecuteNonQuery()

                        Try
                            ' Rename the existing table to table_name_old
                            updateCommand = New SQLiteCommand("ALTER TABLE [" + .GetString(.GetOrdinal("name")) + "] RENAME TO [" + .GetString(.GetOrdinal("name")) + "_old]", updateConn)
                            updateCommand.ExecuteNonQuery()

                            ' Create the new table with the correct structure
                            updateCommand = New SQLiteCommand(.GetString(.GetOrdinal("sql")), updateConn)
                            updateCommand.ExecuteNonQuery()

                            ' Copy across the data (discarding rows which violate any new constraints)
                            If columnNames <> "" Then
                                updateCommand = New SQLiteCommand("INSERT OR IGNORE INTO [" + .GetString(.GetOrdinal("name")) + "] (" + columnNames + ") SELECT " + columnNames + " FROM [" + .GetString(.GetOrdinal("name")) + "_old]", updateConn)
                                updateCommand.ExecuteNonQuery()
                            End If

                            ' Delete the old table
                            updateCommand = New SQLiteCommand("DROP TABLE [" + .GetString(.GetOrdinal("name")) + "_old]", updateConn)
                            updateCommand.ExecuteNonQuery()

                            ' Commit the transaction
                            updateCommand = New SQLiteCommand("COMMIT TRANSACTION", updateConn)
                            updateCommand.ExecuteNonQuery()

                        Catch sqliteExp As SQLiteException
                            ' Roll back the transaction, to try and stop the database being corrupted
                            updateCommand = New SQLiteCommand("ROLLBACK TRANSACTION", updateConn)
                            updateCommand.ExecuteNonQuery()

                            Throw
                        End Try
                    End If
                Else
                    ' Create the table
                    updateCommand = New SQLiteCommand(.GetString(.GetOrdinal("sql")), updateConn)
                    updateCommand.ExecuteNonQuery()
                End If

                updateReader.Close()
            End While
        End With

        specReader.Close()

        Return True
    End Function

    Private Function ColNames(ByVal tableName As String) As String
        Dim fromCols As List(Of String) = ListTableColumns(updateConn, tableName)
        Dim toCols As List(Of String) = ListTableColumns(specConn, tableName)
        Dim resultCols As New List(Of String)

        ' Store an intersect of the from and to columns in resultCols
        For Each fromCol As String In fromCols
            If toCols.Contains(fromCol) Then
                resultCols.Add(fromCol)
            End If
        Next

        If resultCols.Count > 0 Then
            Return "[" + Join(resultCols.ToArray, "], [") + "]"
        Else
            Return ""
        End If
    End Function

    Private Function ListTableColumns(ByVal connection As SQLiteConnection, ByVal tableName As String) As List(Of String)
        Dim returnList As New List(Of String)

        Dim restrictionValues() As String = {Nothing, Nothing, tableName, Nothing}
        Dim columns As DataTable = connection.GetSchema(SQLite.SQLiteMetaDataCollectionNames.Columns, restrictionValues)

        For Each columnRow As DataRow In columns.Rows
            returnList.Add(columnRow.Item("COLUMN_NAME").ToString)
        Next

        Return returnList
    End Function

    Protected Overrides Sub Finalize()
        specConn.Close()
        updateConn.Close()

        MyBase.Finalize()
    End Sub
End Class

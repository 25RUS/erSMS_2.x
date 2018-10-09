﻿Imports System.IO.Ports
Imports System.Data.SQLite

Public Class Form1

#Region "variables"
    Dim SP As New SerialPort
    Dim separators As String = " "
    Dim commands As String = Command()
    Dim args() As String = commands.Split(separators.ToCharArray)
#End Region

#Region "DB presets"
    Dim connectionString As String = "Data Source={0};Version=3;"
    Dim DB As String = IO.Directory.GetCurrentDirectory & "\Plugins\erSMS_Resources\settings.db"
    'Dim SQL As String
    Dim m_dbConn As SQLiteConnection
    Dim m_sqlCmd As SQLiteCommand
#End Region

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        If args(0) = "Setting" Then
            Me.Show()
            AddCOM()
            Try
                'считывание настроек
                connectionString = String.Format(connectionString, DB)
                If Not My.Computer.FileSystem.FileExists(DB) Then
                    Try
                        'создать БД с настройками по умолчанию
                        SQLiteConnection.CreateFile(DB)
                        DBSend("CREATE TABLE IF NOT EXISTS phones (number TEXT, gate TEXT)")
                        DBSend("CREATE TABLE IF NOT EXISTS modems (com TEXT, name TEXT)")
                        DBSend("CREATE TABLE IF NOT EXISTS killthemall (killme TEXT)")
                        Dim killthemall() As String = ({"", ""})
                        For i = 0 To killthemall.Count - 1
                            DBSend("INSERT INTO killthemall ('" & killthemall(i) & "')")
                        Next
                    Catch ex As Exception
                        MsgBox(ex.Message)
                    End Try
                ElseIf My.Computer.FileSystem.FileExists(DB) = True Then
                    'просто считать настройки с существующей БД
                    DBPhoneRead()
                    ModemRead()
                End If
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
            Timer1.Start()
        ElseIf args(0) = "Push" Then
            Me.Hide()
            'Push +79940001234 text
            'sms(args(2...x))
            Me.Close()
        ElseIf args(0) = "PushToGate" Then
            Me.Hide()
            'Push +79940001234 GATE0 text
            'sms(args(2...x))
            Me.Close()
        ElseIf args.Count >= 1 And args(0) <> "Setting" Then
            Me.Hide()
            'text
            ' sms(0...x)
            Dim message As String = Nothing
            For i = 0 To args.Count - 1
                message = message + args(i) & " "
            Next
            Connect()
            Try
                Dim number As DataTable = New DataTable()
                Dim adapter0 As SQLiteDataAdapter = New SQLiteDataAdapter("SELECT * FROM phones", m_dbConn)
                adapter0.Fill(number)
                For i = 0 To number.Rows.Count - 1
                    Dim gate As DataTable = New DataTable()
                    Dim adapter1 As SQLiteDataAdapter = New SQLiteDataAdapter("SELECT * FROM modems WHERE name = " & number.Rows(i).Item(1), m_dbConn)
                    adapter1.Fill(gate)
                    SMS(number.Rows(i).Item(0), gate.Rows(0).Item(0), message)
                Next
            Catch ex As Exception

            End Try
            Me.Close()
        ElseIf args.Count = 0 Then
            End
        End If

    End Sub

    '****************************SYSTEM************************************************
    'подключение к базе
    Public Sub Connect()
        Try
            m_dbConn = New SQLiteConnection()
            m_sqlCmd = New SQLiteCommand()
            m_dbConn = New SQLiteConnection(connectionString)
            m_dbConn.Open()
        Catch ex As Exception
            MessageBox.Show("Error: " + ex.Message)
        End Try
    End Sub

    'запись в базу
    Public Sub DBSend(ByVal sqlQuery As String)
        Try
            Connect()
            m_sqlCmd.Connection = m_dbConn
            m_sqlCmd.CommandText = sqlQuery
            m_sqlCmd.ExecuteNonQuery()
        Catch ex As Exception
            MessageBox.Show("Error: " + ex.Message)
        End Try
    End Sub

    'чтение базы телефонов
    Private Sub DBPhoneRead()
        DataGridView1.Rows.Clear()
        Dim dTable As DataTable = New DataTable()
        Connect()
        Try
            Dim adapter As SQLiteDataAdapter = New SQLiteDataAdapter("SELECT * FROM phones", m_dbConn)
            adapter.Fill(dTable)
            If dTable.Rows.Count > 0 Then
                For i As Integer = 0 To dTable.Rows.Count - 1
                    DataGridView1.Rows.Add(dTable.Rows(i).ItemArray)
                Next
            End If
        Catch ex As Exception
            MessageBox.Show("Error: " + ex.Message)
        End Try
    End Sub

    'чтение базы модемов
    Private Sub ModemRead()
        Connect()
        Try
            Dim modem As DataTable = New DataTable()
            Dim adapter As SQLiteDataAdapter = New SQLiteDataAdapter("SELECT * FROM modems", m_dbConn)
            adapter.Fill(modem)
            ComboBox3.Items.Clear()
            For i = 0 To modem.Rows.Count - 1
                ComboBox3.Items.Add(modem.Rows(i).Item(1))
            Next
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub

    Private Sub SMS(ByVal TargetPhone As String, ByVal TargetPort As String, ByVal Message As String)

        'преобразование телефона к PDU
        Dim tel, tel1, tel2, tel3 As String
        Dim tellong As Integer
        tel = TargetPhone
        tel1 = tel.Replace("+", "") 'откидывает + заменой на ""
        tellong = Len(tel1)
        If tellong Mod 2 Then 'проверка на чётность с добавлением F
            tel2 = tel1 & "F"
        Else
            tel2 = tel1
        End If
        'намешиваем символы в номере  
        Dim rez As String
        rez = ""
        Dim i As Byte
        For i = 1 To Len(tel2) Step 2
            rez = rez & Mid(tel2, i + 1, 1) & Mid(tel2, i, 1)
        Next i
        tel3 = rez

        '######################преобразование текста в UCS-2######################
        Dim text0() As Byte = System.Text.Encoding.BigEndianUnicode.GetBytes(Message) '(text)
        Dim text1 As String = BitConverter.ToString(text0).Replace("-", "")
        Dim textlong As String = Len(text1) 'определяем длину фразы
        Dim textlongHEX As String = Hex(textlong) 'перегоняем длину сообщения в HEX
        Dim l As String = 26 + textlong 'два 0 спереди уже откинуто
        Dim l1 = l / 2
        Dim MSG As String = "0001000B91" & tel3 & "0008" & textlongHEX & text1
        Try
            Dim comport As String = TargetPort
            Dim SP As New SerialPort()
            SP.PortName = comport
            SP.BaudRate = 115200
            SP.Parity = Parity.None
            SP.StopBits = StopBits.One
            SP.DataBits = 8
            SP.Handshake = Handshake.RequestToSend
            SP.DtrEnable = True
            SP.RtsEnable = True
            SP.Open()
            SP.WriteLine("AT" & Chr(13) & vbCrLf)
            Threading.Thread.Sleep(1000)
            SP.WriteLine("AT+CMGF=0" & Chr(13) & vbCrLf)
            Threading.Thread.Sleep(1000)
            SP.WriteLine("AT+CMGS=" & l1 & vbCrLf)
            Threading.Thread.Sleep(1000)
            SP.WriteLine(MSG & Chr(26) & vbCrLf)
            Threading.Thread.Sleep(1000)
            Dim sp_result As String = SP.ReadExisting()
            ' If Message = "тестовое сообщение" Then
            'TextBox4.Text = sp_result
            'TextBox4.Text = TextBox4.Text.Replace(New Char() {vbLf, vbCr}, "").TrimEnd(New Char() {vbLf, vbCr})
            'End If
            SP.Close()
            'логгирование
            Dim D As Date = Now
            My.Computer.FileSystem.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\CLM\Plugins\erSMS_Resources\erSMS_log.txt", D & " Начата отправка СМС c текстом:" & Message & " на номер: " & tel & vbNewLine & sp_result & vbNewLine, True)
        Catch ex As Exception
            Dim D As Date = Now
            My.Computer.FileSystem.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\CLM\Plugins\erSMS_Resources\erSMS_log.txt", D & " СМС c текстом:" & Message & " не была послана на: " & tel & vbNewLine & ex.Message & vbNewLine, True)
        End Try
    End Sub


    'open port
    Private Sub OpenPort(ByVal port As String)
        Try
            SP.PortName = port
            SP.BaudRate = 115200
            SP.Parity = Parity.None
            SP.StopBits = StopBits.One
            SP.DataBits = 8
            SP.Handshake = Handshake.RequestToSend
            SP.DtrEnable = True
            SP.RtsEnable = True
            SP.Open()
        Catch ex As Exception
            'MsgBox(ex.Message)
        End Try
    End Sub

    Public Sub LogMrg()
        'удаление пустых строк из лога
        Try
            Dim textS As String = IO.File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\CLM\Plugins\erSMS_Resources\erSMS_log.txt")
            Dim textE = textS.Replace(New Char() {vbLf, vbCr}, "").TrimEnd(New Char() {vbLf, vbCr})
            My.Computer.FileSystem.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\CLM\Plugins\erSMS_Resources\erSMS_log.txt", textE & vbNewLine, False)
        Catch ex As Exception
        End Try
    End Sub

    'добавление портов
    Private Sub AddCOM()
        'COM searching
        Try
            ComboBox1.Items.Clear()
            Dim ports() As String = SerialPort.GetPortNames()
            Dim port As String
            For Each port In ports
                ComboBox1.Items.Add(port)
            Next port
        Catch ex As Exception
            ' Dim D As Date = Now
            ' My.Computer.FileSystem.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\CLM\Plugins\erSMS_Resources\erSMS_log.txt", D & ": PortsAdd_ERROR: " & ex.Message & vbNewLine, True)
        End Try
        'ComboBox1.Items.Add("COM2") 'debug
    End Sub
    '*********************************************************GUI************************************************

    'ввод только цифр и плюсиков в поле ввода телефона
    Private Sub TextBox1_KeyPress(ByVal sender As System.Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles TextBox1.KeyPress
        'If Not Char.IsDigit(e.KeyChar) Then e.Handled = True
        If Not Char.IsDigit(e.KeyChar) And e.KeyChar <> "+" And e.KeyChar <> vbBack Then e.Handled = True
    End Sub

    'добавление записи в БД телефонов
    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        If ComboBox3.SelectedItem = "" Then
            MsgBox("Выберите шлюз для отправки на номер " & TextBox1.Text)
        ElseIf TextBox1.Text = "" Then
            MsgBox("Введите номер телефона!")
        Else
            DataGridView1.Rows.Clear()
            DBSend("INSERT INTO phones (number, gate) VALUES ('" & TextBox1.Text & "', '" & ComboBox3.SelectedItem & "')")
            DBPhoneRead()
        End If
    End Sub

    'добавление модемов
    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        Try
            If TextBox2.Text <> "" And ComboBox1.SelectedItem <> "" Then
                DBSend("INSERT INTO modems (com,  name) VALUES ('" & ComboBox1.SelectedItem & "', '" & TextBox2.Text & "')")
                ModemRead()
                TextBox2.Text = ""
            Else
                MsgBox("Не должно быть пустых полей!")
            End If
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
        GateListUpdate()
    End Sub

    'удаление одной записи из базы тел-модем
    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        DBSend("DELETE FROM phones WHERE number='" & DataGridView1.CurrentRow.Cells(0).Value.ToString() & "'")
        DBPhoneRead()
    End Sub

    'очистка базы тел-модем
    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        DBSend("DELETE FROM phones")
        DBPhoneRead()
    End Sub

    'мониторинг модемов
    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        Timer1.Interval = 10000
        GateListUpdate()
    End Sub

    'обновление таблицы шлюзов
    Private Sub GateListUpdate()
        AddCOM()
        Dim modem As DataTable = New DataTable()
        'Connect()
        ModemRead()
        Dim OpSos As String = ""
        Dim SignalInfo As String = ""
        Dim name As String = ""
        Dim com As String = ""
        Try
            Dim adapter As SQLiteDataAdapter = New SQLiteDataAdapter("SELECT * FROM modems", m_dbConn)
            adapter.Fill(modem)
            DataGridView2.Rows.Clear()
            For i = 0 To modem.Rows.Count - 1
                OpenPort(modem.Rows(i).Item(0))
                OpSos = GetOperator()
                SP.Close()
                OpenPort(modem.Rows(i).Item(0))
                SignalInfo = Signal()
                SP.Close()
                name = modem.Rows(i).Item(1)
                com = modem.Rows(i).Item(0)
                Dim sigarr() As String = SignalInfo.Split(" ")
                DataGridView2.Rows.Add(name, com, OpSos, sigarr(0))
                DataGridView2.Rows(i).Cells(4).Value = New Bitmap(IO.Directory.GetCurrentDirectory & "\Plugins\erSMS_Resources\Signal\" & sigarr(1))
            Next
        Catch ex As Exception
        End Try
    End Sub
    'получить имя сети
    Private Function GetOperator()
        Dim oper As String
        Dim GSMOperator As String

        Try
            SP.WriteLine("AT+COPS?" & vbCrLf)
            Threading.Thread.Sleep(500)
            oper = SP.ReadExisting()
            oper = oper.Replace(New Char() {vbLf, vbCr}, "").TrimEnd(New Char() {vbLf, vbCr})
            'Dim D As Date = Now
            'My.Computer.FileSystem.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\CLM\Plugins\erSMS_Resources\erSMS_log.txt", D & " GetOperator: " & oper & vbNewLine, True)
            Dim separator As String = "," 'Chr(132)
            Dim oper0() As String = oper.Split(separator.ToCharArray)
            Dim oper1 As String = oper0(2).Replace("""", "")
            'SP.Close()
            If oper1 = "" Then
                GSMOperator = "No service"
            ElseIf oper1 = "25099" Then
                GSMOperator = "Beeline"
            ElseIf oper1 = "25002" Then
                GSMOperator = "MegaFon"
            ElseIf oper1 = "25020" Then
                GSMOperator = "TELE2"
            ElseIf oper1 = "25001" Then
                GSMOperator = "MTS"
                'ElseIf oper1 = "" Then
                '    GSMOperator = "Orange"
            Else
                GSMOperator = oper1
            End If
        Catch ex As Exception
            GSMOperator = "No Service"
        End Try
        Return GSMOperator
    End Function

    'палки связи
    Public Function Signal()
        Dim SignalLevel As String = ""
        Dim ModemAnswer As String = ""
        Dim ModemArr() As String
        Dim SignalInfo As String = ""
        Dim img As String = ""
        Try
            SP.WriteLine("AT+CSQ" & vbCrLf)
            Threading.Thread.Sleep(500)
            ModemAnswer = SP.ReadExisting()
        Catch ex As Exception
            'Dim D As Date = Now
            'My.Computer.FileSystem.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\CLM\Plugins\erSMS_Resources\erSMS_log.txt", D & ": Signal_ERROR: " & ex.Message & vbNewLine, True)
        End Try

    
        Try
            ModemAnswer = ModemAnswer.Replace(New Char() {vbLf, vbCr}, "").TrimEnd(New Char() {vbLf, vbCr})
            ModemArr = ModemAnswer.Split(vbLf & vbCr & " ".ToCharArray)

            For m = 0 To ModemArr.Count - 1
                SignalLevel = ModemArr(ModemArr.Count - 2)
                SignalLevel = SignalLevel.Replace("+CSQ:", "")
                Exit For
            Next
            Dim SignalLevel0 As Integer = CInt(SignalLevel)
            Dim SignalLevel1 As Integer = -113 + (SignalLevel0 * 2)
            'Label3.Text = SignalLevel1
            If SignalLevel1 = Nothing Then
                img = "0.png"
                'Label3.ForeColor = Color.Red
            ElseIf SignalLevel1 < -110 Then
                img = "0.png"
                'Label3.ForeColor = Color.Red
            ElseIf SignalLevel1 < -105 Then
                img = "20.png"
                'Label3.ForeColor = Color.Orange
            ElseIf SignalLevel1 < -95 Then
                img = "40.png"
                'Label3.ForeColor = Color.YellowGreen
            ElseIf SignalLevel1 < -85 Then
                img = "60.png"
                'Label3.ForeColor = Color.YellowGreen
            ElseIf SignalLevel1 < -75 Then
                img = "80.png"
                'Label3.ForeColor = Color.Green
            ElseIf SignalLevel1 < 0 Then
                img = "100.png"
                'Label3.ForeColor = Color.Green
            End If
            If SignalLevel1 = Nothing Then
                SignalInfo = "No signal"
            Else
                SignalInfo = SignalLevel1
            End If
        Catch ex As Exception
            SignalInfo = "-"
            img = "0.png"
        End Try
        Dim SigOut As String = SignalInfo & " " & img
        Return SigOut
    End Function

    'удалить все шлюзы
    Private Sub Button6_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button6.Click
   
        Dim response As MsgBoxResult
        Dim style As MsgBoxStyle = MsgBoxStyle.DefaultButton2 Or _
           MsgBoxStyle.Critical Or MsgBoxStyle.YesNo
        ' Display message.
        response = MsgBox("Будет очищена база шлюзов, продолжить?", style, "Очистка списка шлюзов")
        If response = MsgBoxResult.Yes Then   ' User chose Yes.
            DBSend("DELETE FROM modems")
            GateListUpdate()
        Else
            ' Perform some other action.
        End If
    End Sub

    'удалить один шлюз
    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        DBSend("DELETE FROM modems WHERE name='" & DataGridView2.CurrentRow.Cells(0).Value.ToString() & "'")
        GateListUpdate()
    End Sub
End Class
Imports System.IO.Ports

Public Class Form1
    'variables
    Dim SP As New SerialPort

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load


        'предустановки
        Label2.ForeColor = Color.Red

        'COM searching
        Try
            ComboBox1.Items.Clear()
            Dim ports() As String = SerialPort.GetPortNames()
            Dim port As String
            For Each port In ports
                ComboBox1.Items.Add(port)
            Next port
        Catch ex As Exception
            Dim D As Date = Now
            ' My.Computer.FileSystem.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\CLM\Plugins\erSMS_Resources\erSMS_log.txt", D & ": PortsAdd_ERROR: " & ex.Message & vbNewLine, True)
        End Try

    End Sub

    'test modem
    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        Try
            OpenPort()
            TextBox4.Clear()
            SP.WriteLine("ATI" & vbCrLf)
            Threading.Thread.Sleep(1000)
            TextBox4.Text = SP.ReadExisting()
            TextBox4.Text = TextBox4.Text.Replace(New Char() {vbLf, vbCr}, "").TrimEnd(New Char() {vbLf, vbCr})
            SP.Close()
        Catch ex As Exception
            TextBox4.Text = ex.Message
            Dim D As Date = Now
            Exit Sub
        End Try
    End Sub

    'open port
    Private Sub OpenPort()
        Try
            SP.PortName = Label2.Text ' временно
            SP.BaudRate = ComboBox2.SelectedItem
            SP.Parity = Parity.None
            SP.StopBits = StopBits.One
            SP.DataBits = 8
            SP.Handshake = Handshake.RequestToSend
            SP.DtrEnable = True
            SP.RtsEnable = True
            SP.Open()
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub

    'remove one phone number
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        ListBox1.Items.Remove(ListBox1.SelectedItem)
    End Sub

    'setup COM to label
    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox1.SelectedIndexChanged
        Label2.Text = ComboBox1.SelectedItem
        Label2.ForeColor = Color.Green
        'write to db
    End Sub
End Class

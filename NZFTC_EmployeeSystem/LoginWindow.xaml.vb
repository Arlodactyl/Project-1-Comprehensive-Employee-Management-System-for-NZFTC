Imports System.Windows

Namespace NZFTC_EmployeeSystem

    Public Class LoginWindow

        ' Constructor - runs when window is created
        Public Sub New()
            InitializeComponent()
        End Sub

        ' Login button click event
        Private Sub LoginButton_Click(sender As Object, e As RoutedEventArgs)
            ' Get the values from text boxes
            Dim email As String = EmailTextBox.Text.Trim()
            Dim password As String = PasswordBox.Password

            ' Hide any previous error messages
            ErrorBorder.Visibility = Visibility.Collapsed

            ' Validate inputs
            If String.IsNullOrEmpty(email) Then
                ShowError("Please enter your email address")
                Return
            End If

            If String.IsNullOrEmpty(password) Then
                ShowError("Please enter your password")
                Return
            End If

            ' Check login credentials (hardcoded for now - we'll connect to database later)
            If email = "admin@nzftc.co.nz" AndAlso password = "admin123" Then
                ' Admin login successful
                MessageBox.Show("Admin Login Successful!" & vbCrLf & vbCrLf & "Next step: Build Admin Dashboard",
                               "Login Success",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information)

                ' TODO: Open Admin Dashboard window here
                ' Dim adminDashboard As New AdminDashboardWindow()
                ' adminDashboard.Show()
                ' Me.Close()

            ElseIf email = "employee@nzftc.co.nz" AndAlso password = "emp123" Then
                ' Employee login successful
                MessageBox.Show("Employee Login Successful!" & vbCrLf & vbCrLf & "Next step: Build Employee Dashboard",
                               "Login Success",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information)

                ' TODO: Open Employee Dashboard window here
                ' Dim empDashboard As New EmployeeDashboardWindow()
                ' empDashboard.Show()
                ' Me.Close()

            Else
                ' Login failed
                ShowError("Invalid email or password. Please try again.")
            End If

        End Sub

        ' Helper method to show error messages
        Private Sub ShowError(message As String)
            ErrorMessage.Text = message
            ErrorBorder.Visibility = Visibility.Visible
        End Sub

    End Class

End Namespace
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using Microsoft.EntityFrameworkCore;
using NZFTC_EmployeeSystem.Views;
using System.IO;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// Interaction logic for Contact_AbousUsPage.xaml
    /// </summary>
    public partial class Contact_AboutUsPage : Page
    {
        private readonly User _currentUser; // Getting current User
        public Contact_AboutUsPage(User currentUser)
        {

            _currentUser = currentUser;

            InitializeComponent();

            if (_currentUser.Role != "Admin")
            {
                EditModeButton.Visibility = Visibility.Collapsed;
                SaveModeButton.Visibility = Visibility.Collapsed;
            }
        }

        private void EditMode(object sender, RoutedEventArgs e)
        {
            // Hiding Edit Button and Showing Save Button
            EditModeButton.Visibility = Visibility.Collapsed;
            SaveModeButton.Visibility = Visibility.Visible;

            // Enabling Edit Mode
            ToggleEditMode(true);
        }

        private void SaveMode(object sender, RoutedEventArgs e)
        {
            // Save the new values from textboxes into textblocks
            TitleText.Text = TitleEdit.Text;
            ContextText.Text = ContextEdit.Text;
            SupportEmailDisplay.Text = SupportEmailEditBox.Text;
            AdminEmailDisplay.Text = SupportAdminEditlBox.Text;
            SupportNumberDisplay.Text = SupportNumberEditBox.Text;
            CompanyNameDisplay.Text = CompanyNameEdit.Text;
            AddressDisplay.Text = AddressDisplayEdit.Text;
            BusinessDisplay.Text = BusinessEdit.Text;
            EmailDisplay.Text = EmailEdit.Text;
            ContactDisplay.Text = ContactEdit.Text;

            // Hiding Save Button and Showing Edit Button
            SaveModeButton.Visibility = Visibility.Collapsed;
            EditModeButton.Visibility = Visibility.Visible;

            // Disabling Edit Mode
            ToggleEditMode(false);
        }

        private void ToggleEditMode(bool isEditing)
        {
            // For each section its toggle between visible textblock and collapsed textbox
            TitleText.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            TitleEdit.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
            TitleEdit.Text = TitleText.Text;

            ContextText.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            ContextEdit.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
            ContextEdit.Text = ContextText.Text;

            SupportEmailDisplay.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            SupportEmailEditBox.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
            SupportEmailEditBox.Text = SupportEmailDisplay.Text;

            AdminEmailDisplay.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            SupportAdminEditlBox.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
            SupportAdminEditlBox.Text = AdminEmailDisplay.Text;

            SupportNumberDisplay.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            SupportNumberEditBox.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
            SupportNumberEditBox.Text = SupportNumberDisplay.Text;

            CompanyNameDisplay.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            CompanyNameEdit.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
            CompanyNameEdit.Text = CompanyNameDisplay.Text;

            AddressDisplay.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            AddressDisplayEdit.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
            AddressDisplayEdit.Text = AddressDisplay.Text;

            BusinessDisplay.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            BusinessEdit.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
            BusinessEdit.Text = BusinessDisplay.Text;

            EmailDisplay.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            EmailEdit.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
            EmailEdit.Text = EmailDisplay.Text;

            ContactDisplay.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            ContactEdit.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
            ContactEdit.Text = ContactDisplay.Text;
        }
    }
}

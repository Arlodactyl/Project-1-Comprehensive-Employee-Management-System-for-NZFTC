using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for Contact_AboutUsPage.xaml
    /// Allows admins to edit company contact information
    /// Includes validation for phone numbers
    /// </summary>
    public partial class Contact_AboutUsPage : Page
    {
        private readonly User _currentUser;

        public Contact_AboutUsPage(User currentUser)
        {
            _currentUser = currentUser;
            InitializeComponent();

            // Only show edit buttons for admin users
            if (_currentUser.Role != "Admin")
            {
                EditModeButton.Visibility = Visibility.Collapsed;
                SaveModeButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Enables edit mode for admin users
        /// </summary>
        private void EditMode(object sender, RoutedEventArgs e)
        {
            // Hiding Edit Button and Showing Save Button
            EditModeButton.Visibility = Visibility.Collapsed;
            SaveModeButton.Visibility = Visibility.Visible;

            // Enabling Edit Mode
            ToggleEditMode(true);
        }

        /// <summary>
        /// Validates phone numbers to ensure they contain only numbers, hyphens, and spaces
        /// </summary>
        private bool ValidatePhoneNumber(string phoneNumber)
        {
            // Allow empty phone numbers
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return true;

            // Regex pattern: only digits, spaces, hyphens, and parentheses
            // This allows formats like: 0800-123-456, (03) 123 4567, 021 123 4567, etc.
            return Regex.IsMatch(phoneNumber, @"^[\d\s\-\(\)]+$");
        }

        /// <summary>
        /// Saves changes and validates input
        /// </summary>
        private void SaveMode(object sender, RoutedEventArgs e)
        {
            // Validate phone numbers before saving
            if (!ValidatePhoneNumber(SupportNumberEditBox.Text))
            {
                MessageBox.Show(
                    "Support Number can only contain numbers, spaces, hyphens, and parentheses.\nLetters and special characters are not allowed.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            if (!ValidatePhoneNumber(ContactEdit.Text))
            {
                MessageBox.Show(
                    "Contact Number can only contain numbers, spaces, hyphens, and parentheses.\nLetters and special characters are not allowed.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

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

            // Show success message
            MessageBox.Show(
                "Contact information updated successfully!",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        /// <summary>
        /// Toggles between view mode and edit mode
        /// </summary>
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

        /// <summary>
        /// Shows help information for the Contact/About page
        /// </summary>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string helpMessage = "Contact / About Us Help\n\n" +
                "This page displays company contact information and support details.\n\n";

            if (_currentUser.Role == "Admin")
            {
                helpMessage += "Admin Features:\n" +
                    "- Click 'Edit' to modify company information\n" +
                    "- Update announcements, contact details, and company info\n" +
                    "- Phone numbers can only contain numbers, spaces, hyphens, and parentheses\n" +
                    "- Click 'Save' when finished to update the information\n\n";
            }

            helpMessage += "Contact Information:\n" +
                "- Support Email: For general employee support\n" +
                "- Admin Email: For administrative inquiries\n" +
                "- Support Number: Phone support line\n\n" +
                "Company Information:\n" +
                "- View company details and contact information\n" +
                "- All staff can view this information";

            MessageBox.Show(
                helpMessage,
                "Contact/About Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
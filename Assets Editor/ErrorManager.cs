using System;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Assets_Editor;

public static class ErrorManager {
    public static void ShowInfo(string message) {
        MessageBox.Show(
            message,
            "Information",
            (MessageBoxButtons)MessageBoxButton.OK,
            (MessageBoxIcon)MessageBoxImage.Information
        );
    }
    public static void ShowWarning(string message) {
        MessageBox.Show(
            message,
            "Warning",
            (MessageBoxButtons)MessageBoxButton.OK,
            (MessageBoxIcon)MessageBoxImage.Warning
        );
    }
    public static void ShowError(string message) {
        MessageBox.Show(
            message,
            "Error",
            (MessageBoxButtons)MessageBoxButton.OK,
            (MessageBoxIcon)MessageBoxImage.Error
        );
    }

    public static void ShowException(Exception ex) {
        // to do: an option to copy this
        ShowError(ex.ToString());
    }
}

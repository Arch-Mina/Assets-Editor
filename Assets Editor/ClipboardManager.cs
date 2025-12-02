using MaterialDesignThemes.Wpf;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Assets_Editor;

/// <summary>
/// Manages the clipboard safely. Prevents crashes and resolves issues with clipboard accessibility.
/// </summary>
public class ClipboardManager {
    public static void CopyText(string text, string thingName, Snackbar subscriber, long seconds = 2) {
        TryCopyAsync(text, thingName, subscriber, 10, seconds);
    }

    private static async void TryCopyAsync(string text, string thingName, Snackbar subscriber, int remainingRetries, long seconds) {
        bool success = false;
        string? exceptionMessage = null;
        int delayMs = 100;

        // STA thread is required for clipboard
        await Task.Run(() => {
            Thread staThread = new(() => {
                try {
                    Clipboard.SetText(text);
                    success = true;
                } catch (Exception e) {
                    exceptionMessage = e.Message;
                }
            });
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
        });

        if (success) {
            Application.Current.Dispatcher.Invoke(() => {
                subscriber.MessageQueue?.Enqueue(
                    $"{thingName} copied to clipboard.",
                    null, null, null, false, true, TimeSpan.FromSeconds(seconds)
                );
            });
        } else if (remainingRetries > 0) {
            // Reschedule itself after a short delay
            await Task.Delay(delayMs);
            TryCopyAsync(text, thingName, subscriber, remainingRetries - 1, seconds);
        } else {
            Application.Current.Dispatcher.Invoke(() => {
                subscriber.MessageQueue?.Enqueue(
                    exceptionMessage ?? "Clipboard unavailable.",
                    null, null, null, false, true, TimeSpan.FromSeconds(seconds)
                );
            });
        }
    }
}

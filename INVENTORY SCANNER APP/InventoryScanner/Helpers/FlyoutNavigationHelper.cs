using System.ComponentModel;

namespace InventoryScanner.Helpers
{
    /// <summary>
    /// Synchronizes page startup work with Shell flyout animation state.
    /// </summary>
    public static class FlyoutNavigationHelper
    {
        public static Task WaitForFlyoutToCloseAsync(CancellationToken cancellationToken = default)
        {
            var shell = Shell.Current;
            if (shell == null || !shell.FlyoutIsPresented)
                return Task.CompletedTask;

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            PropertyChangedEventHandler? handler = null;

            handler = (_, e) =>
            {
                if (e.PropertyName != nameof(Shell.FlyoutIsPresented))
                    return;

                if (!shell.FlyoutIsPresented)
                {
                    shell.PropertyChanged -= handler;
                    tcs.TrySetResult();
                }
            };

            shell.PropertyChanged += handler;

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    shell.PropertyChanged -= handler;
                    tcs.TrySetCanceled(cancellationToken);
                });
            }

            return tcs.Task;
        }
    }
}

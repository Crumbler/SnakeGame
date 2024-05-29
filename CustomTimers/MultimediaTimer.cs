using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace CustomTimers
{
    /// <summary>
    /// A timer based on the multimedia timer API with 1ms precision.
    /// </summary>
    public class MultimediaTimer : IDisposable
    {
        private const int EventTypeSingle = 0;
        private const int EventTypePeriodic = 1;

        private bool disposed = false;
        private int interval, resolution;
        private volatile uint timerId;

        // Hold the timer callback to prevent garbage collection.
        private readonly MultimediaTimerCallback Callback;

        public MultimediaTimer()
        {
            Callback = new MultimediaTimerCallback(TimerCallbackMethod);
            Resolution = 5;
            Interval = 10;
        }

        ~MultimediaTimer()
        {
            Dispose(false);
        }

        /// <summary>
        /// The period of the timer in milliseconds.
        /// </summary>
        public int Interval
        {
            get
            {
                return interval;
            }
            set
            {
                CheckDisposed();

                ArgumentOutOfRangeException.ThrowIfNegative(value);

                interval = value;
                if (Resolution > Interval)
                {
                    Resolution = value;
                }
            }
        }

        /// <summary>
        /// The resolution of the timer in milliseconds. The minimum resolution is 0, meaning highest possible resolution.
        /// </summary>
        public int Resolution
        {
            get
            {
                return resolution;
            }
            set
            {
                CheckDisposed();

                ArgumentOutOfRangeException.ThrowIfNegative(value);

                resolution = value;
            }
        }

        /// <summary>
        /// Gets whether the timer has been started yet.
        /// </summary>
        public bool IsRunning => timerId != 0;

        public static Task Delay(int millisecondsDelay, CancellationToken token = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(millisecondsDelay);

            if (millisecondsDelay == 0)
            {
                return Task.CompletedTask;
            }

            token.ThrowIfCancellationRequested();

            // allocate an object to hold the callback in the async state.
            var state = new object();
            var completionSource = new TaskCompletionSource(state);
            MultimediaTimerCallback callback = (uint id, uint msg, ref uint uCtx, uint rsv1, uint rsv2) =>
            {
                // Note we don't need to kill the timer for one-off events.
                completionSource.SetResult();
            };

            state = callback;
            uint userCtx = 0;
            var timerId = NativeMethods.TimeSetEvent((uint)millisecondsDelay, 0, callback, ref userCtx, EventTypeSingle);
            if (timerId == 0)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }

            return completionSource.Task;
        }

        public void Start()
        {
            CheckDisposed();

            if (IsRunning)
            {
                throw new InvalidOperationException("Timer is already running");
            }

            // Event type = 0, one off event
            // Event type = 1, periodic event
            uint userCtx = 0;
            timerId = NativeMethods.TimeSetEvent((uint)Interval, (uint)Resolution, Callback, ref userCtx, EventTypePeriodic);
            if (timerId == 0)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }
        }

        public void Stop()
        {
            CheckDisposed();

            if (!IsRunning)
            {
                throw new InvalidOperationException("Timer has not been started");
            }

            StopInternal();
        }

        private void StopInternal()
        {
            NativeMethods.TimeKillEvent(timerId);
            timerId = 0;
        }

        public event EventHandler? Elapsed;

        public void Dispose()
        {
            Dispose(true);
        }

        private void TimerCallbackMethod(uint id, uint msg, ref uint userCtx, uint rsv1, uint rsv2)
        {
            Elapsed?.Invoke(this, EventArgs.Empty);
        }

        private void CheckDisposed()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
            { 
                return;
            }

            disposed = true;
            if (IsRunning)
            {
                StopInternal();
            }

            if (disposing)
            {
                Elapsed = null;
                GC.SuppressFinalize(this);
            }
        }
    }

    internal delegate void MultimediaTimerCallback(uint id, uint msg, ref uint userCtx, uint rsv1, uint rsv2);

    internal static partial class NativeMethods
    {
        [LibraryImport("winmm.dll", EntryPoint = "timeSetEvent", SetLastError = true)]
        internal static partial uint TimeSetEvent(uint msDelay, uint msResolution, MultimediaTimerCallback callback, ref uint userCtx, uint eventType);

        [LibraryImport("winmm.dll", EntryPoint = "timeKillEvent", SetLastError = true)]
        internal static partial void TimeKillEvent(uint uTimerId);
    }
}

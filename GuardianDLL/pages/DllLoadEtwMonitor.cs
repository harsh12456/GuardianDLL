using System;
using System.Threading;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

namespace GuardianDLL
{
    public class DllLoadEtwMonitor : IDisposable
    {
        private TraceEventSession _session;
        private Thread _etwThread;
        private bool _running = false;

        public delegate void DllLoadedHandler(string dllPath, string processName, int processId, bool isSuspicious, bool isMalicious);
        public event DllLoadedHandler OnDllLoaded;

        private readonly Func<string, bool> _isSuspiciousHeuristic;
        private readonly Func<string, bool> _hasValidSignature;

        public DllLoadEtwMonitor(Func<string, bool> isSuspiciousHeuristic, Func<string, bool> hasValidSignature)
        {
            _isSuspiciousHeuristic = isSuspiciousHeuristic;
            _hasValidSignature = hasValidSignature;
        }

        public void Start()
        {
            if (_running) return;
            _running = true;
            _etwThread = new Thread(() =>
            {
                string sessionName = $"GuardianDLL_ETW_Session_{System.Diagnostics.Process.GetCurrentProcess().Id}";

                // Clean up any existing session with the same name
                foreach (var activeName in TraceEventSession.GetActiveSessionNames())
                {
                    if (string.Equals(activeName, sessionName, StringComparison.OrdinalIgnoreCase))
                    {
                        using (var cleanupSession = new TraceEventSession(sessionName))
                        {
                            cleanupSession.Stop();
                        }
                        break;
                    }
                }

                using (_session = new TraceEventSession(sessionName))
                {
                    _session.EnableKernelProvider(KernelTraceEventParser.Keywords.ImageLoad);

                    _session.Source.Kernel.ImageLoad += data =>
                    {
                        string dllPath = data.FileName;
                        int processId = data.ProcessID;
                        string processName = data.ProcessName;

                        bool isSuspicious = _isSuspiciousHeuristic?.Invoke(dllPath) ?? false;
                        bool isMalicious = isSuspicious && !(_hasValidSignature?.Invoke(dllPath) ?? true);

                        OnDllLoaded?.Invoke(dllPath, processName, processId, isSuspicious, isMalicious);
                    };

                    _session.Source.Process();
                }
            });
            _etwThread.IsBackground = true;
            _etwThread.Start();
        }

        public void Dispose()
        {
            _running = false;
            _session?.Dispose();
            if (_etwThread != null && _etwThread.IsAlive)
                _etwThread.Join(1000);
        }
    }
}
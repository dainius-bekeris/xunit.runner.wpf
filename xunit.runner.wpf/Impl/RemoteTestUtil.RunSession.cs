﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using xunit.runner.data;
using xunit.runner.wpf.ViewModel;

namespace xunit.runner.wpf.Impl
{
    internal partial class RemoteTestUtil 
    {
        private sealed class RunSession : ITestRunSession
        {
            private readonly Task _task;
            private event EventHandler<TestResultDataEventArgs> _testFinished;
            private event EventHandler _sessionFinished;

            internal RunSession(Connection connection, Dispatcher dispatcher, CancellationToken cancellationToken)
            {
                _task = BackgroundProducer<TestResultData>.Go(connection, dispatcher, r => r.ReadTestResultData(), OnFinished, cancellationToken);
            }

            private void OnFinished(List<TestResultData> list)
            {
                Debug.Assert(!_task.IsCompleted);
                if (list == null)
                {
                    _sessionFinished?.Invoke(this, EventArgs.Empty);
                    return;
                }

                foreach (var cur in list)
                {
                    _testFinished?.Invoke(this, new wpf.TestResultDataEventArgs(cur));
                }
            }

            #region ITestRunSession

            Task ITestSession.Task => _task;

            event EventHandler<TestResultDataEventArgs> ITestRunSession.TestFinished
            {
                add { _testFinished += value; }
                remove { _testFinished -= value; }
            }

            event EventHandler ITestRunSession.SessionFinished
            {
                add { _sessionFinished += value; }
                remove { _sessionFinished -= value; }
            }

            #endregion
        }
    }
}

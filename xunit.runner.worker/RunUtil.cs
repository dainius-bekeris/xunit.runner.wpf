﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xunit.runner.data;
using Xunit;
using Xunit.Abstractions;

namespace xunit.runner.worker
{
    internal sealed class RunUtil
    {
        private class TestRunVisitor : TestMessageVisitor<ITestAssemblyFinished>
        {
            private readonly BinaryWriter _writer;
            private bool _continue = true;

            public TestRunVisitor(BinaryWriter writer)
            {
                _writer = writer;
            }

            private void Process(string displayName, TestState state)
            {
                Console.WriteLine($"{state} - {displayName}");
                var result = new TestResultData(displayName, state);

                try
                {
                    result.WriteTo(_writer);
                }
                catch (Exception ex)
                {
                    // This happens during a rude shutdown from the client.
                    Console.Error.WriteLine(ex.Message);
                    _continue = false;
                }
            }

            protected override bool Visit(ITestFailed testFailed)
            {
                Process(testFailed.TestCase.DisplayName, TestState.Failed);
                return _continue;
            }

            protected override bool Visit(ITestPassed testPassed)
            {
                Process(testPassed.TestCase.DisplayName, TestState.Passed);
                return _continue;
            }

            protected override bool Visit(ITestSkipped testSkipped)
            {
                Process(testSkipped.TestCase.DisplayName, TestState.Skipped);
                return _continue;
            }
        }

        internal static void Go(string assemblyPath, Stream stream)
        {
            using (AssemblyHelper.SubscribeResolve())
            using (var xunit = new XunitFrontController(
                assemblyFileName: assemblyPath,
                useAppDomain: false,
                shadowCopy: false,
                diagnosticMessageSink: new MessageVisitor()))
            using (var writer = new BinaryWriter(stream, Constants.Encoding, leaveOpen: true))
            using (var testRunVisitor = new TestRunVisitor(writer))
            {
                xunit.RunAll(testRunVisitor, TestFrameworkOptions.ForDiscovery(), TestFrameworkOptions.ForExecution());
                testRunVisitor.Finished.WaitOne();
            }
        }
    }
}

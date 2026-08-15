using System;
using System.Threading;

namespace Test_Project_1.TestHelpers
{
    /// <summary>
    /// Runs a test body on a dedicated STA thread. This is needed because MSTest's
    /// [assembly: Parallelize] runs tests on non-STA worker threads by default, but
    /// some tests instantiate WinForms/WPF controls (e.g. FormMain, which hosts an
    /// ElementHost/WPF control) that require the calling thread to be STA.
    /// Any exception (including assertion failures) thrown inside the action is
    /// captured and re-thrown on the calling thread so MSTest reports it normally.
    /// </summary>
    public static class StaTestRunner
    {
        public static void Run(Action action)
        {
            Exception caught = null;

            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    caught = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (caught != null)
            {
                throw caught;
            }
        }
    }
}


namespace Test.Psi.Study.x64
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Psi;
    using TBD.Psi.StudyComponents;

    [TestClass]
    public class StateTrackerTest
    {
        [TestMethod]
        [Timeout(10000)]
        public void TestRunning()
        {
            using (var p = Pipeline.Create())
            {
                var clock = Generators.Range(p, 0, 10, TimeSpan.FromMilliseconds(100));

                var stateTracker = new StateTracker(p, "testing");

                clock.Process<int, string>((m, e, o) =>
                {
                    if (m == 2)
                    {
                        o.Post("Since-2", e.OriginatingTime);
                    }
                }).PipeTo(stateTracker);

                int counter = 0;
                stateTracker.Do(m =>
                {
                    switch (counter)
                    {
                        case 0:
                            Assert.AreEqual(m.Item1, "testing");
                            Assert.AreEqual(200, m.Item2.TotalMilliseconds, 10);
                            break;
                        case 1:
                            Assert.AreEqual(m.Item1, "Since-2");
                            Assert.AreEqual(700, m.Item2.TotalMilliseconds, 10);
                            break;
                    }
                    counter++;
                });
                p.Run();
            }
        }
    }
}

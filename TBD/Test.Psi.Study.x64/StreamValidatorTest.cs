
namespace Test.Psi.Study.x64
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Psi;
    using TBD.Psi.StudyComponents;
    
    [TestClass]
    public class StreamValidatorTest
    {
        [TestMethod]
        [Timeout(10000)]
        public void TestRunning()
        {
            var completed = false;
            using(var p = Pipeline.Create())
            {
                var s1 = Generators.Range(p, 1, 1, TimeSpan.FromSeconds(1));
                var s2 = Generators.Once(p, 2);

                var validator = new StreamValidator(p);
                validator.AddStream(s1);
                validator.AddStream(s2);

                validator.Do(m =>
                {
                    completed = true;
                });

                p.Run();
            }
            Assert.IsTrue(completed);
        }


        [TestMethod]
        [Timeout(60000)]
        public void TestDelay()
        {
            var completed = false;
            using (var p = Pipeline.Create())
            {
                var s1 = Generators.Range(p, 1, 2, TimeSpan.FromSeconds(1));
                var s2 = Generators.Once(p, 2);

                var validator = new StreamValidator(p);
                validator.AddStream(s1);
                validator.AddStream(s2.Delay(TimeSpan.FromSeconds(0.5)));

                DateTime s2Time = DateTime.MinValue;
                s2.Do((m,e) => s2Time = e.OriginatingTime);

                validator.Do((m,e) =>
                {
                    Assert.AreEqual(0.5, (p.GetCurrentTime() - p.StartTime).TotalSeconds, 0.01);
                    Assert.AreEqual(m.Ticks, s2Time.Ticks);
                    Assert.AreEqual(e.OriginatingTime.Ticks, s2Time.Ticks);
                    completed = true;
                });

                p.Run();
            }
            Assert.IsTrue(completed);
        }
    }
}

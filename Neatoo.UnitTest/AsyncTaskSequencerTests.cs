//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Neatoo.Core;
//using System;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Neatoo.UnitTest
//{
//    [TestClass]
//    public class AsyncTaskSequencerTests
//    {

//        [TestMethod]
//        public async Task AsyncTaskSequencer_AsyncForks()
//        {
//            var sequencer = new AsyncTaskSequencer();

//            bool completedA = false;
//            bool completedB = false;
//            bool completedC = false;

//            Func<Task, Task> funcA = async (t) =>
//            {
//                await Task.Delay(5);
//                Assert.IsFalse(completedA);
//                Assert.IsFalse(completedB);
//                Assert.IsFalse(completedC);
//                completedA = true;
//            };

//            Func<Task, Task> funcB = async (t) =>
//            {
//                await Task.Delay(10);
//                Assert.IsTrue(completedA);
//                Assert.IsFalse(completedB);
//                Assert.IsFalse(completedC);
//                completedB = true;
//            };

//            Func<Task, Task> funcC = async (t) =>
//            {
//                await Task.Delay(15);
//                Assert.IsTrue(completedA);
//                Assert.IsTrue(completedB);
//                Assert.IsFalse(completedC);
//                completedC = true;
//            };

//            sequencer.AddTask(funcA);
//            sequencer.AddTask(funcB);
//            sequencer.AddTask(funcC);

//            await sequencer.AllDone;

//            Assert.IsTrue(completedA);
//            Assert.IsTrue(completedB);
//            Assert.IsTrue(completedC);
//        }



//        [TestMethod]
//        public void AsyncTaskSequencer_NoAsyncForks()
//        {
//            var sequencer = new AsyncTaskSequencer();

//            bool completedA = false;
//            bool completedB = false;
//            bool completedC = false;

//            Func<Task, Task> funcA = (t) =>
//            {
//                Assert.IsFalse(completedA);
//                Assert.IsFalse(completedB);
//                Assert.IsFalse(completedC);
//                completedA = true;
//                return Task.CompletedTask;
//            };

//            Func<Task, Task> funcB = (t) =>
//            {
//                Assert.IsTrue(completedA);
//                Assert.IsFalse(completedB);
//                Assert.IsFalse(completedC);
//                completedB = true;
//                return Task.CompletedTask;
//            };

//            Func<Task, Task> funcC = (t) =>
//            {
//                Assert.IsTrue(completedA);
//                Assert.IsTrue(completedB);
//                Assert.IsFalse(completedC);
//                completedC = true;
//                return Task.CompletedTask;
//            };

//            sequencer.AddTask(funcA);
//            sequencer.AddTask(funcB);
//            sequencer.AddTask(funcC);

//            // Since there were no async forks
//            // should not have to await
//            // (In ValidateBase this means don't have to await AllRulesDone)
//            Assert.IsTrue(completedA);
//            Assert.IsTrue(completedB);
//            Assert.IsTrue(completedC);
//        }



//        [TestMethod]
//        public async Task AsyncTaskSequencer_AsyncForkException()
//        {
//            var sequencer = new AsyncTaskSequencer();

//            bool completedA = false;
//            bool completedB = false;
//            bool completedC = false;
//            bool completedD = false;

//            Exception exception = new Exception("Test");

//            Func<Task, Task> funcA = async (t) =>
//            {
//                await Task.Delay(5);
//                Assert.IsFalse(completedA);
//                Assert.IsFalse(completedB);
//                Assert.IsFalse(completedC);
//                completedA = true;
//            };

//            Func<Task, Task> funcB = async (t) =>
//            {
//                await Task.Delay(5);
//                Assert.IsTrue(completedA);
//                Assert.IsFalse(completedB);
//                Assert.IsFalse(completedC);
//                completedB = true;
//                throw exception;
//            };

//            Func<Task, Task> funcC = async (t) =>
//            {
//                await Task.Delay(5);
//                Assert.IsTrue(completedA);
//                Assert.IsTrue(completedB);
//                Assert.IsFalse(completedC);
//                completedC = true;
//            };

//            Func<Task, Task> funcD = async (t) =>
//            {
//                await Task.Delay(5);
//                Assert.IsTrue(completedA);
//                Assert.IsTrue(completedB);
//                Assert.IsFalse(completedC);
//                completedD = true;
//            };

//            sequencer.AddTask(funcA);
//            sequencer.AddTask(funcB);
//            sequencer.AddTask(funcC);
//            sequencer.AddTask(funcD, true);

//            try
//            {
//                await sequencer.AllDone;
//            }
//            catch (AggregateException ex)
//            {
//                Assert.AreSame(ex.InnerExceptions.Single(), exception);
//            }

//            Assert.IsTrue(completedA);
//            Assert.IsTrue(completedB);
//            Assert.IsFalse(completedC);
//            Assert.IsTrue(completedD);
//        }


//        [TestMethod]
//        public async Task AsyncTaskSequencer_NonAwaitedAsyncFork()
//        {

//            // This is the core issue
//            // We update a property thru the setter
//            // that has async rules
//            // The setter does not await the async rules

//            var sequencer = new AsyncTaskSequencer();

//            bool completedA = false;
//            bool completedB = false;
//            bool completedC = false;

//            Func<Task, Task> funcC = async (t) =>
//            {
//                await Task.Delay(5);
//                Assert.IsTrue(completedA);
//                Assert.IsTrue(completedB);
//                Assert.IsFalse(completedC);
//                completedC = true;
//            };

//            Func<Task, Task> funcB = async (t) =>
//            {
//                await Task.Delay(5);
//                sequencer.AddTask(funcC);
//                Assert.IsTrue(completedA);
//                Assert.IsFalse(completedB);
//                Assert.IsFalse(completedC);
//                completedB = true;
//            };

//            Action funcA = () =>
//            {
//                sequencer.AddTask(funcB); // Not awaited - This is the core difficulty
//                Assert.IsFalse(completedA);
//                Assert.IsFalse(completedB);
//                Assert.IsFalse(completedC);
//                completedA = true;
//            };

//            funcA();

//            Assert.IsTrue(completedA);
//            Assert.IsFalse(completedB);
//            Assert.IsFalse(completedC);

//            await sequencer.AllDone;

//            Assert.IsTrue(completedA);
//            Assert.IsTrue(completedB);
//            Assert.IsTrue(completedC);
//        }


//        [TestMethod]
//        public void AsyncTaskSequencer_NoAsyncForks_KeepTask()
//        {
//            var sequencer = new AsyncTaskSequencer();

//            bool completedA = false;
//            bool completedB = false;
//            bool completedC = false;

//            Func<Task, Task> funcA = (t) =>
//            {
//                Assert.IsFalse(completedA);
//                Assert.IsFalse(completedB);
//                Assert.IsFalse(completedC);
//                completedA = true;
//                return Task.CompletedTask;
//            };

//            Func<Task, Task> funcB = (t) =>
//            {
//                Assert.IsTrue(completedA);
//                Assert.IsFalse(completedB);
//                Assert.IsFalse(completedC);
//                completedB = true;
//                return Task.CompletedTask;
//            };

//            Func<Task, Task> funcC = (t) =>
//            {
//                Assert.IsTrue(completedA);
//                Assert.IsTrue(completedB);
//                Assert.IsFalse(completedC);
//                completedC = true;
//                return Task.CompletedTask;
//            };

//            Assert.IsTrue(sequencer.AddTask(funcA).IsCompleted);
//            Assert.IsTrue(sequencer.AddTask(funcB).IsCompleted);
//            Assert.IsTrue(sequencer.AddTask(funcC).IsCompleted);

//            // Since there were no async forks
//            // should not have to await
//            // (In ValidateBase this means don't have to await AllRulesDone)
//            Assert.IsTrue(completedA);
//            Assert.IsTrue(completedB);
//            Assert.IsTrue(completedC);
//        }


//        [TestMethod]
//        public async Task AsyncTaskSequencer_AsyncForks_KeepTask()
//        {
//            var sequencer = new AsyncTaskSequencer();

//            bool completedA = false;
//            bool completedB = false;
//            bool completedC = false;

//            Func<Task, Task> funcA = async (t) =>
//            {
//                await Task.Delay(5);
//                Assert.IsFalse(completedA);
//                Assert.IsFalse(completedB);
//                Assert.IsFalse(completedC);
//                completedA = true;
//            };

//            Func<Task, Task> funcB = async (t) =>
//            {
//                await Task.Delay(10);
//                Assert.IsTrue(completedA);
//                Assert.IsFalse(completedB);
//                Assert.IsFalse(completedC);
//                completedB = true;
//            };

//            Func<Task, Task> funcC = async (t) =>
//            {
//                await Task.Delay(15);
//                Assert.IsTrue(completedA);
//                Assert.IsTrue(completedB);
//                Assert.IsFalse(completedC);
//                completedC = true;
//            };

//            sequencer.AddTask(funcA);
//            sequencer.AddTask(funcB);
//            sequencer.AddTask(funcC);

//            await sequencer.AllDone;

//            Assert.IsTrue(completedA);
//            Assert.IsTrue(completedB);
//            Assert.IsTrue(completedC);
//        }


//        [TestMethod]
//        public async Task AsyncTaskSequencer_NestedAddsAsyncMethod() {

//            asyncTaskSequencer.AddTask((t) => FunctionA());

//            await asyncTaskSequencer.AllDone;

//        }

//        private AsyncTaskSequencer asyncTaskSequencer = new AsyncTaskSequencer();
//        private int count = 0;

//        private async Task FunctionA()
//        {
//            var myCount = count;
//            if (count < 5)
//            {
//                count++;
//                asyncTaskSequencer.AddTask((t) => FunctionA());
//            }
//            await Task.Delay(5);
//            Debug.WriteLine($"FunctionA {myCount}");
//        }

//        [TestMethod]
//        public async Task AsyncTaskSequencer_Hangs()
//        {

//            asyncTaskSequencer.AddTask((t) => Hangs());

//            await asyncTaskSequencer.AllDone;

//        }


//        private async Task Hangs()
//        {
//            await Task.Delay(5);

//            if (count == 0)
//            {
//                count++;
//                asyncTaskSequencer.AddTask((t) => Hangs());
//            }

//            await Task.CompletedTask;
//        }
//    }
//}
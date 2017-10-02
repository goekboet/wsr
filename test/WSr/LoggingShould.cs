using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static WSr.LogFunctions;

namespace WSr.Tests
{
    [TestClass]
    public class LoggingShould
    {
        [TestMethod]
        public void AddManyAContext()
        {
            var log = new string[1];

            Action<string> logger = s => log[0] = s;
            var a = AddContext("a", logger);
            var b = AddContext("b", a);
            b("c");

            Assert.AreEqual("a/b/c", log[0], $"e: >abc< a: >{log[0]}<");
        }
    }
}
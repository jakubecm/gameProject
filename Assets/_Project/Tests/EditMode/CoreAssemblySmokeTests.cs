using NUnit.Framework;
using ColdShutdown.Core;

namespace ColdShutdown.Tests.EditMode
{
    public class CoreAssemblySmokeTests
    {
        [Test]
        public void CoreAssembly_IsReferencedAndLoaded()
        {
            Assert.AreEqual("ColdShutdown.Core", CoreInfo.AssemblyName);
            Assert.AreEqual("ColdShutdown.Core", typeof(CoreInfo).Assembly.GetName().Name);
        }
    }
}

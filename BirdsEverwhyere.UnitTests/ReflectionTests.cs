using NUnit.Framework;
using BirdsEverywhere;

namespace BirdsEverwhyere.UnitTests
{
    public class ReflectionTest
    {
        [SetUp]
        public void Setup()
        {
        }

        public class TestClass
        {
            public string name { get; set; } = null;
            public int?  age { get; set; } = 21;

            public TestClass() { }

            public TestClass(string name)
            {
                this.name = name;
            }
        }

        [Test]
        public void nameIsNull()
        {
            TestClass test = new TestClass();
            Assert.That(test.name == null);
        }
        [Test]
        public void nameIsNotNull()
        {
            TestClass test = new TestClass("Peter");
            Assert.That(test.name != null);
        }
        [Test]
        public void ageIsNotNull()
        {
            TestClass test = new TestClass();
            Assert.That(test.age != null);
        }

        [Test]
        public void Reflection_SrcOverwritesTargetNameIfNull()
        {
            TestClass src = new TestClass("Anna");
            TestClass tgt = new TestClass();
            src.CopyProperties(tgt);
            Assert.That(tgt.name == "Anna");
        }

        [Test]
        public void Reflection_SrcDoesntOverwriteNotNullProperty()
        {
            TestClass src = new TestClass("Anna");
            TestClass tgt = new TestClass("Peter");
            src.CopyProperties(tgt);
            Assert.That(tgt.name == "Peter");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;

namespace OpenSmc.Ifrs17.Domain.Test
{
    public class TestDataTest
    {
        private readonly TestData testData;

        public TestDataTest()
        {
            testData = new TestData();
            testData.InitializeAsync();
        }

        [Fact]
        public void TestPreviousPartition()
        {
            testData.PreviousPeriodPartition.Id.Should().NotBe(null);
        }
    }
}

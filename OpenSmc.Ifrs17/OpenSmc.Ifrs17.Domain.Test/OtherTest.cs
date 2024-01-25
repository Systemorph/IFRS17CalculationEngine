using FluentAssertions;
using OpenSmc.Ifrs17.Domain.Utils;

namespace OpenSmc.Ifrs17.Domain.Test;


public class OtherTests
{
    [Fact]
    public void TestsCollection()
    {
        Enumerable.Repeat(1e6 / 12, 12)
            .NewBusinessInterestAccretion(new[]
            {
                1.00407412378365
            }, 12, 0)
            .Should().BeApproximately(26881.4607, 0.001);


        Enumerable.Repeat(1e6 / 12, 12)
            .NewBusinessInterestAccretion(new[]
            {
                1.0
            }, 12, 0)
            .Should().BeApproximately(0, 0.001);



        new[]
        {
            0.5, 0.4, 0.3, 0.2, 0.1
        }.Prune(1e-5).SequenceEqual(new[]
        {
            0.5, 0.4, 0.3, 0.2, 0.1
        }).Should().BeTrue();


        new[]
        {
            0.5, 0.4, 0.3, 0.2, 0.1
        }.Prune(0.2).SequenceEqual(new[]
        {
            0.5, 0.4, 0.3, 0.2
        }).Should().BeTrue();


        new[]
        {
            0.5, 0.4, 0.3, 0.2, 0.1
        }.Prune(0.3).SequenceEqual(new[]
        {
            0.5, 0.4, 0.3
        }).Should().BeTrue();


        new[]
        {
            0.5, 0.4, 0.3, 0.2, 0.1
        }.PruneButFirst(0.3).SequenceEqual(new[]
        {
            0.5, 0.4, 0.3, 0.0
        }).Should().BeTrue();


        new[]
        {
            0.5, 0.2, 0.0, 0.0, 0.0
        }.PruneButFirst(0.3).SequenceEqual(new[]
        {
            0.5, 0.0
        }).Should().BeTrue();


        new[]
        {
            0.5, 0.0, 0.0, 0.0, 0.0
        }.PruneButFirst(0.3).SequenceEqual(new[]
        {
            0.5, 0.0
        }).Should().BeTrue();
    }
}




using Everywhere.AI;

namespace Everywhere.Core.Tests.AI;

[TestFixture]
public class TokenBudgetTests
{
    [Test]
    public void Allocate_EmptyInput_ShouldReturnEmptyArray()
    {
        var result = TokenBudget.Allocate([], totalBudget: 1000);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Allocate_ZeroBudget_ShouldReturnAllZeros()
    {
        int[] desired = [100, 200, 300];
        var result = TokenBudget.Allocate(desired, totalBudget: 0);

        Assert.That(result, Is.EqualTo(new[] { 0, 0, 0 }));
    }

    [Test]
    public void Allocate_BudgetExceedsTotal_ShouldGiveExactDesired()
    {
        int[] desired = [50, 100, 150];
        var result = TokenBudget.Allocate(desired, totalBudget: 10000, minTokensPerItem: 0);

        Assert.That(result, Is.EqualTo(new[] { 50, 100, 150 }));
    }

    [Test]
    public void Allocate_AllItemsFitInMinimum_ShouldGiveExactDesired()
    {
        int[] desired = [50, 100, 150];
        var result = TokenBudget.Allocate(desired, totalBudget: 1000, minTokensPerItem: 200);

        Assert.That(result, Is.EqualTo(new[] { 50, 100, 150 }));
    }

    [Test]
    public void Allocate_NeverExceedsBudget()
    {
        int[] desired = [500, 800, 1200, 3000];
        var totalBudget = 2000;

        var result = TokenBudget.Allocate(desired, totalBudget, minTokensPerItem: 100);

        Assert.That(result.Sum(), Is.LessThanOrEqualTo(totalBudget));
    }

    [Test]
    public void Allocate_NeverExceedsDesired()
    {
        int[] desired = [100, 200, 300, 400];
        var result = TokenBudget.Allocate(desired, totalBudget: 5000, minTokensPerItem: 50);

        for (var i = 0; i < desired.Length; i++)
        {
            Assert.That(result[i], Is.LessThanOrEqualTo(desired[i]),
                $"Item {i}: allocated {result[i]} > desired {desired[i]}");
        }
    }

    [Test]
    public void Allocate_MinimumGuarantee_ShouldRespectMinPerItem()
    {
        int[] desired = [500, 500, 500];
        var result = TokenBudget.Allocate(desired, totalBudget: 900, minTokensPerItem: 200);

        // Each item should get at least min(desired, minPerItem) = 200
        for (var i = 0; i < desired.Length; i++)
        {
            Assert.That(result[i], Is.GreaterThanOrEqualTo(200),
                $"Item {i} should get at least 200 tokens");
        }
    }

    [Test]
    public void Allocate_MaxPerItem_ShouldCapSingleItem()
    {
        int[] desired = [10000, 100, 100];
        var result = TokenBudget.Allocate(desired, totalBudget: 5000, minTokensPerItem: 0, maxTokensPerItem: 2000);

        Assert.That(result[0], Is.LessThanOrEqualTo(2000));
    }

    [Test]
    public void Allocate_DefaultMaxPerItem_ShouldBeHalfBudget()
    {
        int[] desired = [10000, 100];
        var totalBudget = 2000;
        var result = TokenBudget.Allocate(desired, totalBudget, minTokensPerItem: 0);

        // Default max = totalBudget / 2 = 1000
        Assert.That(result[0], Is.LessThanOrEqualTo(1000));
    }

    [Test]
    public void Allocate_ProportionalDistribution_LargerItemsGetMore()
    {
        int[] desired = [1000, 2000, 3000];
        var result = TokenBudget.Allocate(desired, totalBudget: 3000, minTokensPerItem: 0, maxTokensPerItem: 3000);

        // Larger items should get proportionally more
        Assert.That(result[2], Is.GreaterThan(result[0]));
        Assert.That(result[1], Is.GreaterThan(result[0]));
    }

    [Test]
    public void Allocate_SingleItem_ShouldGetUpToMaxOrDesired()
    {
        int[] desired = [500];
        var result = TokenBudget.Allocate(desired, totalBudget: 1000, minTokensPerItem: 0);

        Assert.That(result[0], Is.EqualTo(500));
    }

    [Test]
    public void Allocate_SingleLargeItem_ShouldBeCapped()
    {
        int[] desired = [5000];
        var result = TokenBudget.Allocate(desired, totalBudget: 1000, minTokensPerItem: 0, maxTokensPerItem: 800);

        Assert.That(result[0], Is.LessThanOrEqualTo(800));
    }

    [Test]
    public void Allocate_ShortItemsSurplusFlowsToLonger()
    {
        // Item 0 needs only 50, which is below minPerItem=200
        // Its unused quota should flow to item 1
        int[] desired = [50, 2000];
        var result = TokenBudget.Allocate(desired, totalBudget: 1000, minTokensPerItem: 200, maxTokensPerItem: 1000);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(result[0], Is.EqualTo(50)); // Gets exact desired (< min)
        Assert.That(result[1], Is.GreaterThan(200)); // Gets more than minimum
    }

    [Test]
    public void Allocate_NegativeBudget_ShouldReturnAllZeros()
    {
        int[] desired = [100, 200];
        var result = TokenBudget.Allocate(desired, totalBudget: -1);

        Assert.That(result, Is.EqualTo(new[] { 0, 0 }));
    }

    [Test]
    public void Allocate_ManyItems_ShouldDistributeEfficiently()
    {
        var desired = Enumerable.Range(1, 20).Select(i => i * 100).ToArray();
        var totalBudget = 5000;

        var result = TokenBudget.Allocate(desired, totalBudget, minTokensPerItem: 50);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(result.Sum(), Is.LessThanOrEqualTo(totalBudget));
        Assert.That(result.Length, Is.EqualTo(desired.Length));
        // All allocations should be non-negative
        Assert.That(result.All(a => a >= 0), Is.True);
    }
}

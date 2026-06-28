using Everywhere.Chat;
using Everywhere.StrategyEngine;
using Everywhere.StrategyEngine.Conditions;
using NSubstitute;

namespace Everywhere.Core.Tests.StrategyEngine;

[TestFixture]
public class ConditionsTests
{
    private static StrategyContext CreateEmptyContext() => new()
    {
        Attachments = Array.Empty<ChatAttachment>()
    };

    [TestFixture]
    public class TrueConditionTests
    {
        [Test]
        public void Evaluate_ShouldAlwaysReturnTrue()
        {
            var condition = TrueCondition.Shared;
            Assert.That(condition.Evaluate(CreateEmptyContext()), Is.True);
        }

        [Test]
        public void Shared_ShouldReturnSameInstance()
        {
            var first = TrueCondition.Shared;
            var second = TrueCondition.Shared;
            Assert.That(first, Is.SameAs(second));
        }
    }

    [TestFixture]
    public class FalseConditionTests
    {
        [Test]
        public void Evaluate_ShouldAlwaysReturnFalse()
        {
            var condition = FalseCondition.Shared;
            Assert.That(condition.Evaluate(CreateEmptyContext()), Is.False);
        }

        [Test]
        public void Shared_ShouldReturnSameInstance()
        {
            var first = FalseCondition.Shared;
            var second = FalseCondition.Shared;
            Assert.That(first, Is.SameAs(second));
        }
    }

    [TestFixture]
    public class NotConditionTests
    {
        [Test]
        public void Evaluate_ShouldNegateTrue()
        {
            var condition = new NotCondition { Inner = TrueCondition.Shared };
            Assert.That(condition.Evaluate(CreateEmptyContext()), Is.False);
        }

        [Test]
        public void Evaluate_ShouldNegateFalse()
        {
            var condition = new NotCondition { Inner = FalseCondition.Shared };
            Assert.That(condition.Evaluate(CreateEmptyContext()), Is.True);
        }

        [Test]
        public void Evaluate_ShouldNegateDynamicCondition()
        {
            var mockCondition = Substitute.For<IStrategyCondition>();
            mockCondition.Evaluate(Arg.Any<StrategyContext>()).Returns(true);

            var notCondition = new NotCondition { Inner = mockCondition };
            Assert.That(notCondition.Evaluate(CreateEmptyContext()), Is.False);
        }
    }

    [TestFixture]
    public class CompositeConditionTests
    {
        [Test]
        public void And_AllTrue_ShouldReturnTrue()
        {
            var condition = CompositeCondition.And(
                TrueCondition.Shared,
                TrueCondition.Shared,
                TrueCondition.Shared
            );

            Assert.That(condition.Evaluate(CreateEmptyContext()), Is.True);
        }

        [Test]
        public void And_OneFalse_ShouldReturnFalse()
        {
            var condition = CompositeCondition.And(
                TrueCondition.Shared,
                FalseCondition.Shared,
                TrueCondition.Shared
            );

            Assert.That(condition.Evaluate(CreateEmptyContext()), Is.False);
        }

        [Test]
        public void And_AllFalse_ShouldReturnFalse()
        {
            var condition = CompositeCondition.And(
                FalseCondition.Shared,
                FalseCondition.Shared
            );

            Assert.That(condition.Evaluate(CreateEmptyContext()), Is.False);
        }

        [Test]
        public void Or_AllFalse_ShouldReturnFalse()
        {
            var condition = CompositeCondition.Or(
                FalseCondition.Shared,
                FalseCondition.Shared,
                FalseCondition.Shared
            );

            Assert.That(condition.Evaluate(CreateEmptyContext()), Is.False);
        }

        [Test]
        public void Or_OneTrue_ShouldReturnTrue()
        {
            var condition = CompositeCondition.Or(
                FalseCondition.Shared,
                TrueCondition.Shared,
                FalseCondition.Shared
            );

            Assert.That(condition.Evaluate(CreateEmptyContext()), Is.True);
        }

        [Test]
        public void Or_AllTrue_ShouldReturnTrue()
        {
            var condition = CompositeCondition.Or(
                TrueCondition.Shared,
                TrueCondition.Shared
            );

            Assert.That(condition.Evaluate(CreateEmptyContext()), Is.True);
        }

        [Test]
        public void EmptyConditions_ShouldReturnTrue()
        {
            var condition = new CompositeCondition
            {
                Logic = CompositeLogic.And,
                Conditions = Array.Empty<IStrategyCondition>()
            };

            Assert.That(condition.Evaluate(CreateEmptyContext()), Is.True);
        }

        [Test]
        public void And_ShortCircuits_ShouldNotEvaluateAfterFalse()
        {
            var secondCondition = Substitute.For<IStrategyCondition>();

            var condition = CompositeCondition.And(
                FalseCondition.Shared,
                secondCondition
            );

            condition.Evaluate(CreateEmptyContext());

            // ZLinq All short-circuits, so second should not be called
            secondCondition.DidNotReceive().Evaluate(Arg.Any<StrategyContext>());
        }

        [Test]
        public void Or_ShortCircuits_ShouldNotEvaluateAfterTrue()
        {
            var secondCondition = Substitute.For<IStrategyCondition>();

            var condition = CompositeCondition.Or(
                TrueCondition.Shared,
                secondCondition
            );

            condition.Evaluate(CreateEmptyContext());

            // ZLinq Any short-circuits, so second should not be called
            secondCondition.DidNotReceive().Evaluate(Arg.Any<StrategyContext>());
        }

        [Test]
        public void NestedComposite_ShouldEvaluateCorrectly()
        {
            // (True AND True) OR (True AND False)
            var condition = CompositeCondition.Or(
                CompositeCondition.And(TrueCondition.Shared, TrueCondition.Shared),
                CompositeCondition.And(TrueCondition.Shared, FalseCondition.Shared)
            );

            Assert.That(condition.Evaluate(CreateEmptyContext()), Is.True);
        }

        [Test]
        public void NestedComposite_AllGroupsFalse_ShouldReturnFalse()
        {
            // (True AND False) OR (False AND True)
            var condition = CompositeCondition.Or(
                CompositeCondition.And(TrueCondition.Shared, FalseCondition.Shared),
                CompositeCondition.And(FalseCondition.Shared, TrueCondition.Shared)
            );

            Assert.That(condition.Evaluate(CreateEmptyContext()), Is.False);
        }
    }

    [TestFixture]
    public class GroupedConditionTests
    {
        [Test]
        public void Evaluate_AnyGroupMatches_ShouldReturnTrue()
        {
            var condition = new GroupedCondition
            {
                Groups =
                [
                    new IStrategyCondition[] { FalseCondition.Shared, TrueCondition.Shared }, // AND: false
                    new IStrategyCondition[] { TrueCondition.Shared, TrueCondition.Shared }   // AND: true
                ]
            };

            Assert.That(condition.Evaluate(CreateEmptyContext()), Is.True);
        }

        [Test]
        public void Evaluate_NoGroupMatches_ShouldReturnFalse()
        {
            var condition = new GroupedCondition
            {
                Groups =
                [
                    new IStrategyCondition[] { FalseCondition.Shared, TrueCondition.Shared }, // AND: false
                    new IStrategyCondition[] { TrueCondition.Shared, FalseCondition.Shared }  // AND: false
                ]
            };

            Assert.That(condition.Evaluate(CreateEmptyContext()), Is.False);
        }

        [Test]
        public void Evaluate_EmptyGroups_ShouldReturnFalse()
        {
            var condition = new GroupedCondition
            {
                Groups = Array.Empty<IReadOnlyList<IStrategyCondition>>()
            };

            Assert.That(condition.Evaluate(CreateEmptyContext()), Is.False);
        }

        [Test]
        public void Evaluate_EmptyGroup_ShouldMatchAsTrue()
        {
            // An empty group has no conditions to fail, so AND of nothing is true
            var condition = new GroupedCondition
            {
                Groups =
                [
                    Array.Empty<IStrategyCondition>() // Empty group: vacuously true
                ]
            };

            Assert.That(condition.Evaluate(CreateEmptyContext()), Is.True);
        }

        [Test]
        public void Evaluate_SingleGroupAllTrue_ShouldReturnTrue()
        {
            var condition = new GroupedCondition
            {
                Groups =
                [
                    new IStrategyCondition[] { TrueCondition.Shared, TrueCondition.Shared, TrueCondition.Shared }
                ]
            };

            Assert.That(condition.Evaluate(CreateEmptyContext()), Is.True);
        }
    }
}

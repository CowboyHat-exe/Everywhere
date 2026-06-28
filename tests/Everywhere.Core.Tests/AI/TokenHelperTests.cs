using System.Text;
using Everywhere.AI;

namespace Everywhere.Core.Tests.AI;

[TestFixture]
public class TokenHelperTests
{
    [Test]
    public void EstimateTokenCount_NullOrEmpty_ShouldReturnZero()
    {
        Assert.That(TokenHelper.EstimateTokenCount(""), Is.EqualTo(0));
        Assert.That(TokenHelper.EstimateTokenCount(null!), Is.EqualTo(0));
    }

    [Test]
    public void EstimateTokenCount_SimpleText_ShouldReturnPositive()
    {
        var count = TokenHelper.EstimateTokenCount("Hello, world!");
        Assert.That(count, Is.GreaterThan(0));
    }

    [Test]
    public void EstimateTokenCount_LongerText_ShouldBeMoreTokens()
    {
        var shortCount = TokenHelper.EstimateTokenCount("Hi");
        var longCount = TokenHelper.EstimateTokenCount("This is a much longer sentence with many more words and tokens.");
        Assert.That(longCount, Is.GreaterThan(shortCount));
    }

    [Test]
    public void Omit_Null_ShouldReturnNull()
    {
        var result = TokenHelper.Omit(null);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Omit_Empty_ShouldReturnEmpty()
    {
        var result = TokenHelper.Omit("");
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void Omit_ShortText_ShouldReturnUnchanged()
    {
        const string text = "Short text";
        var result = TokenHelper.Omit(text, maxTokenCount: 100);
        Assert.That(result, Is.EqualTo(text));
    }

    [Test]
    public void Omit_LongText_Middle_ShouldTruncateMiddle()
    {
        var text = new string('a', 50000); // Very long text
        var result = TokenHelper.Omit(text, maxTokenCount: 100, position: TokenHelper.OmitPosition.Middle);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(result.Length, Is.LessThan(text.Length));
        Assert.That(result, Does.Contain("omitted"));
        // Should start and end with content from original
        Assert.That(result, Does.StartWith("a"));
        Assert.That(result, Does.EndWith("a"));
    }

    [Test]
    public void Omit_LongText_Start_ShouldTruncateStart()
    {
        var text = string.Concat(Enumerable.Repeat("beginning ", 2000)) + "END_MARKER";
        var result = TokenHelper.Omit(text, maxTokenCount: 100, position: TokenHelper.OmitPosition.Start);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(result.Length, Is.LessThan(text.Length));
        Assert.That(result, Does.Contain("omitted"));
        Assert.That(result, Does.EndWith("END_MARKER"));
    }

    [Test]
    public void Omit_LongText_End_ShouldTruncateEnd()
    {
        var text = "START_MARKER" + string.Concat(Enumerable.Repeat(" trailing", 2000));
        var result = TokenHelper.Omit(text, maxTokenCount: 100, position: TokenHelper.OmitPosition.End);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(result.Length, Is.LessThan(text.Length));
        Assert.That(result, Does.Contain("omitted"));
        Assert.That(result, Does.StartWith("START_MARKER"));
    }

    [Test]
    public void Omit_CustomOmitText_ShouldUseProvidedText()
    {
        var text = new string('x', 50000);
        var result = TokenHelper.Omit(text, maxTokenCount: 100, omitText: "---CUT---");

        Assert.That(result, Does.Contain("---CUT---"));
    }

    [Test]
    public void Omit_OmitTextWithPlaceholders_ShouldFormatCorrectly()
    {
        var text = new string('x', 50000);
        var result = TokenHelper.Omit(text, maxTokenCount: 100, omitText: "[{0} chars, {1} tokens omitted]");

        using var _ = Assert.EnterMultipleScope();
        Assert.That(result, Does.Contain("chars"));
        Assert.That(result, Does.Contain("tokens omitted"));
        Assert.That(result, Does.Not.Contain("{0}"));
        Assert.That(result, Does.Not.Contain("{1}"));
    }

    [Test]
    public void OmitTo_NullOrEmpty_ShouldAppendAndReturnZero()
    {
        var sb = new StringBuilder();
        var tokens = TokenHelper.OmitTo(null, sb);

        Assert.That(tokens, Is.EqualTo(0));
    }

    [Test]
    public void OmitTo_ShortText_ShouldAppendFullText()
    {
        var sb = new StringBuilder();
        const string text = "Short text";
        var tokens = TokenHelper.OmitTo(text, sb, maxTokenCount: 1000);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(sb.ToString(), Is.EqualTo(text));
        Assert.That(tokens, Is.GreaterThan(0));
    }

    [Test]
    public void OmitTo_LongText_ShouldTruncateAndAppend()
    {
        var sb = new StringBuilder();
        var text = new string('z', 50000);
        var tokens = TokenHelper.OmitTo(text, sb, maxTokenCount: 100);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(sb.Length, Is.LessThan(text.Length));
        Assert.That(sb.ToString(), Does.Contain("omitted"));
        Assert.That(tokens, Is.EqualTo(100));
    }

    [Test]
    public void OmitTo_Middle_ShouldPreserveStartAndEnd()
    {
        var sb = new StringBuilder();
        var text = "START" + new string('m', 50000) + "END";
        TokenHelper.OmitTo(text, sb, maxTokenCount: 100, position: TokenHelper.OmitPosition.Middle);

        var result = sb.ToString();
        Assert.That(result, Does.StartWith("START"));
    }
}

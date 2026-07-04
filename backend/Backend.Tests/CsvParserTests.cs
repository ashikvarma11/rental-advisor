using RentalAdvisor.Backend.Services;
using Xunit;

namespace Backend.Tests;

public class CsvParserTests
{
    [Fact]
    public void ParseLine_PlainCommaSeparated_ReturnsFields()
    {
        var result = CsvParser.ParseLine("a,b,c");
        Assert.Equal(new List<string> { "a", "b", "c" }, result);
    }

    [Fact]
    public void ParseLine_QuotedFieldWithComma_KeepsCommaInField()
    {
        var result = CsvParser.ParseLine("a,\"b,c\",d");
        Assert.Equal(new List<string> { "a", "b,c", "d" }, result);
    }

    [Fact]
    public void ParseLine_EscapedQuoteInsideQuotedField_UnescapesToSingleQuote()
    {
        var result = CsvParser.ParseLine("a,\"say \"\"hi\"\"\",c");
        Assert.Equal(new List<string> { "a", "say \"hi\"", "c" }, result);
    }

    [Fact]
    public void ParseLine_EmptyFields_ReturnsEmptyStrings()
    {
        var result = CsvParser.ParseLine("a,,c");
        Assert.Equal(new List<string> { "a", "", "c" }, result);
    }

    [Fact]
    public void ParseLine_TrailingEmptyField_IncludesFinalEmptyField()
    {
        var result = CsvParser.ParseLine("a,b,");
        Assert.Equal(new List<string> { "a", "b", "" }, result);
    }
}

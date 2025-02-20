using JoinRpg.Services.Impl.Search;

namespace JoinRpg.Services.Impl.Test;

public class SearchKeywordsResolverTest
{
    private static readonly string[] KeysForPerfectMath =
    {
  "%контакты",
  "контакты",
};

    private void Verify(string searchString, int? expectedId, bool exactMatchFlag)
    {
        (var id, var isPerfectMatch) = SearchKeywordsResolver.TryGetId(searchString, KeysForPerfectMath);

        id.ShouldBe(expectedId, $"ExpectedId was wrong for {searchString}");
        isPerfectMatch.ShouldBe(exactMatchFlag, $"isPerfectMathc was wrong for {searchString}");
    }

    [Fact]
    public void VerifyEmptyString()
    {
        Verify(
          "",
          expectedId: null,
          exactMatchFlag: false);
    }

    [Fact]
    public void VerifyBareNumber()
    {
        Verify(
          "123",
          expectedId: 123,
          exactMatchFlag: false);
    }

    [Fact]
    public void VerifyBareNumberWithSpaces()
    {
        Verify(
          " 123 ",
          expectedId: 123,
          exactMatchFlag: false);
    }

    [Fact]
    public void VerifyKeyword()
    {
        Verify(
          "%контакты123",
          expectedId: 123,
          exactMatchFlag: true);
    }

    [Fact]
    public void VerifyKeywordWrongCase()
    {
        Verify(
          "КОнТаКтЫ321",
          expectedId: 321,
          exactMatchFlag: true);
    }

    [Fact]
    public void VerifySpaceDelimeter()
    {
        Verify(
          "%контакты 123",
          expectedId: null,
          exactMatchFlag: false);
    }
}

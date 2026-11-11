using PersonModel;
using Xunit;

namespace CsvParser.Tests;

public class PersonTests
{
    [Fact]
    public void Equal_WhenAllValuesMatch()
    {
        var a = new Person(1, "Ann", "ann@example.com", 30);
        var b = new Person(1, "Ann", "ann@example.com", 30);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void NotEqual_WhenAnyValueDiffers()
    {
        var a = new Person(1, "Ann", "ann@example.com", 30);

        Assert.NotEqual(a, a with { Name = "Bob" });
        Assert.NotEqual(a, a with { Age = 31 });
    }

    [Fact]
    public void NotEqual_WhenOnlyIdDiffers()
    {
        var a = new Person(1, "Ann", "ann@example.com", 30);

        Assert.NotEqual(a, a with { Id = 2 });
    }

    [Fact]
    public void NotEqual_WhenOnlyEmailDiffers()
    {
        var a = new Person(1, "Ann", "ann@example.com", 30);

        Assert.NotEqual(a, a with { Email = "other@example.com" });
    }

    [Fact]
    public void With_ChangesOneProperty_AndLeavesOriginalUnchanged()
    {
        var original = new Person(1, "Ann", "ann@example.com", 30);

        var copy = original with { Name = "Bob" };

        Assert.Equal(new Person(1, "Bob", "ann@example.com", 30), copy);
        Assert.Equal(new Person(1, "Ann", "ann@example.com", 30), original);
    }

    [Fact]
    public void Deconstruct_ReturnsIdNameEmailAndAgeInOrder()
    {
        var person = new Person(1, "Ann", "ann@example.com", 30);

        var (id, name, email, age) = person;

        Assert.Equal(1, id);
        Assert.Equal("Ann", name);
        Assert.Equal("ann@example.com", email);
        Assert.Equal(30, age);
    }

    [Fact]
    public void Id_DefaultsToZero()
    {
        var person = new Person(default, "Ann", "ann@example.com", 30);

        Assert.Equal(0, person.Id);
    }

    [Fact]
    public void ToString_ListsAllProperties()
    {
        var person = new Person(0, "Ann", "ann@example.com", 30);

        Assert.Equal("Person { Id = 0, Name = Ann, Email = ann@example.com, Age = 30 }", person.ToString());
    }
}

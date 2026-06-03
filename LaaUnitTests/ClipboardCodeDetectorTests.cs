using LocalAIAssistant.Core.Coco;

namespace LaaUnitTests;

public class ClipboardCodeDetectorTests
{
    // ── Null / empty ──────────────────────────────────────────────────────────

    [Fact]
    public void IsCode_ReturnsFalse_WhenInputIsNull()
    {
        Assert.False(ClipboardCodeDetector.IsCode(null));
    }

    [Fact]
    public void IsCode_ReturnsFalse_WhenInputIsEmpty()
    {
        Assert.False(ClipboardCodeDetector.IsCode(string.Empty));
        Assert.False(ClipboardCodeDetector.IsCode("   "));
    }

    // ── Strong structural tokens ──────────────────────────────────────────────

    [Theory]
    [InlineData("int Foo() { return 42; }")]
    [InlineData("if (x > 0) { y++; }")]
    [InlineData("public class MyClass { }")]
    public void IsCode_ReturnsTrue_WhenContainsOpenBrace(string input)
    {
        Assert.True(ClipboardCodeDetector.IsCode(input));
    }

    [Theory]
    [InlineData("x => x * 2")]
    [InlineData("items.Where(item => item.IsActive)")]
    [InlineData("Func<int, int> square = x => x * x;")]
    public void IsCode_ReturnsTrue_WhenContainsArrowOperator(string input)
    {
        Assert.True(ClipboardCodeDetector.IsCode(input));
    }

    // ── Strong keyword indicators ─────────────────────────────────────────────

    [Theory]
    [InlineData("class MyClass")]
    [InlineData("public class Foo")]
    [InlineData("abstract class Bar")]
    public void IsCode_ReturnsTrue_WhenContainsClassKeyword(string input)
    {
        Assert.True(ClipboardCodeDetector.IsCode(input));
    }

    [Theory]
    [InlineData("void DoSomething()")]
    [InlineData("public void Run()")]
    [InlineData("async void HandleAsync()")]
    public void IsCode_ReturnsTrue_WhenContainsVoidKeyword(string input)
    {
        Assert.True(ClipboardCodeDetector.IsCode(input));
    }

    [Fact]
    public void IsCode_ReturnsTrue_WhenContainsDefKeyword()
    {
        Assert.True(ClipboardCodeDetector.IsCode("def my_function(x, y):"));
    }

    [Fact]
    public void IsCode_ReturnsTrue_WhenContainsFunctionKeyword()
    {
        Assert.True(ClipboardCodeDetector.IsCode("function sayHello() {}"));
    }

    [Fact]
    public void IsCode_ReturnsTrue_WhenContainsNamespaceKeyword()
    {
        Assert.True(ClipboardCodeDetector.IsCode("namespace MyApp.Services;"));
    }

    [Theory]
    [InlineData("public string Name { get; set; }")]
    [InlineData("private readonly ILogger _logger;")]
    [InlineData("protected override void OnCreate(Bundle b)")]
    public void IsCode_ReturnsTrue_WhenContainsAccessModifier(string input)
    {
        Assert.True(ClipboardCodeDetector.IsCode(input));
    }

    [Theory]
    [InlineData("import React from 'react';")]
    [InlineData("import { useState } from 'react';")]
    public void IsCode_ReturnsTrue_WhenContainsImportKeyword(string input)
    {
        Assert.True(ClipboardCodeDetector.IsCode(input));
    }

    [Theory]
    [InlineData("async Task<string> FetchAsync()")]
    [InlineData("public async Task RunAsync()")]
    public void IsCode_ReturnsTrue_WhenContainsAsyncKeyword(string input)
    {
        Assert.True(ClipboardCodeDetector.IsCode(input));
    }

    // ── Parenthesis + semicolon combination ───────────────────────────────────

    [Fact]
    public void IsCode_ReturnsTrue_WhenContainsParenthesisAndSemicolon()
    {
        Assert.True(ClipboardCodeDetector.IsCode("Console.WriteLine(\"hello\"); x = 5;"));
    }

    // ── Two soft keywords ─────────────────────────────────────────────────────

    [Fact]
    public void IsCode_ReturnsTrue_WhenContainsTwoSoftKeywords()
    {
        Assert.True(ClipboardCodeDetector.IsCode("var x = 5; return x;"));
    }

    [Fact]
    public void IsCode_ReturnsTrue_WhenContainsVarAndLet()
    {
        Assert.True(ClipboardCodeDetector.IsCode("var total = 0; let count = items.length;"));
    }

    // ── Plain English — should NOT be classified as code ─────────────────────

    [Fact]
    public void IsCode_ReturnsFalse_ForPlainEnglishText()
    {
        Assert.False(ClipboardCodeDetector.IsCode("Hello world, this is a note about something."));
    }

    [Fact]
    public void IsCode_ReturnsFalse_ForSentenceWithParenthesis()
    {
        Assert.False(ClipboardCodeDetector.IsCode("She arrived early (before 8am) and left at noon."));
    }

    [Fact]
    public void IsCode_ReturnsFalse_ForShoppingList()
    {
        Assert.False(ClipboardCodeDetector.IsCode("Buy milk, eggs, and bread."));
    }

    [Fact]
    public void IsCode_ReturnsFalse_ForOnlyOneSoftKeyword()
    {
        Assert.False(ClipboardCodeDetector.IsCode("Remember to return the library books."));
    }
}

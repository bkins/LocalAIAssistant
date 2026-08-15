using System;
using System.Collections.Generic;

namespace LocalAIAssistant.CognitivePlatform.DTOs;

public sealed record MealDto
{
    public string             Id         { get; init; } = string.Empty;
    public string             MealType   { get; init; } = "Unspecified";
    public DateTimeOffset     ConsumedAt { get; init; }
    public List<FoodEntryDto> Foods      { get; init; } = new();
    public string?            Notes      { get; init; }
    public string?            Source     { get; init; }
}

public sealed record FoodEntryDto
{
    public string               Name        { get; init; } = string.Empty;
    public double?              Quantity    { get; init; }
    public string?              Unit        { get; init; }
    public string?              Preparation { get; init; }
    public string?              Brand       { get; init; }
    public List<string>?        Additions   { get; init; }
    public NutritionalInfoDto?  Nutrition   { get; init; }
}

public sealed record NutritionalInfoDto
{
    public double? Calories     { get; init; }
    public double? ProteinGrams { get; init; }
    public double? CarbsGrams   { get; init; }
    public double? FatGrams     { get; init; }
    public double? FiberGrams   { get; init; }
}

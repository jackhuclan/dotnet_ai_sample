namespace dotnet_ai_agent_sample;

internal interface IRecipeService
{
    Task<string> GetRecipeAsync(ComplexObject complexObject, string recipeName);
    Task<bool> SaveRecipeAsync(ComplexObject complexObject, string recipeName, string recipeInstructions);
}

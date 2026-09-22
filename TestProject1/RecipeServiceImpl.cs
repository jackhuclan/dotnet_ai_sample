namespace dotnet_ai_agent_sample;

internal class RecipeServiceImpl : IRecipeService
{
    Task<string> IRecipeService.GetRecipeAsync(ComplexObject complexObject, string recipeName)
    {
        return Task.FromResult("Recipe content for " + recipeName);
    }

    Task<bool> IRecipeService.SaveRecipeAsync(ComplexObject complexObject, string recipeName, string recipeInstructions)
    {
        return Task.FromResult(true);
    }
}

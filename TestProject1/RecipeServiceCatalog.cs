using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace dotnet_ai_agent_sample;

internal class RecipeServiceCatalog : IRecipeService, IAITools
{
    private readonly IRecipeService recipeService;

    [ActivatorUtilitiesConstructor]
    public RecipeServiceCatalog(IRecipeService recipeService)
    {
        this.recipeService = recipeService;
    }

    [Description("Retrieves a recipe by its name.")]
    public Task<string> GetRecipeAsync([Description("The complex object containing recipe details.")] ComplexObject complexObject,
        [Description("The name of the recipe to retrieve.")] string recipeName)
    {
        return recipeService.GetRecipeAsync(complexObject, recipeName);
    }

    public IReadOnlyList<AIFunction> AsAITools() =>
    [
        AIFunctionFactory.Create(GetRecipeAsync, "get_recipe"),
        AIFunctionFactory.Create(SaveRecipeAsync,"save_recipe")
    ];

    [Description("Saves a recipe with the specified instructions.")]
    public Task<bool> SaveRecipeAsync([Description("The complex object containing recipe details.")] ComplexObject complexObject,
        [Description("The name of the recipe to save.")] string recipeName,
        [Description("The instructions for the recipe.")] string recipeInstructions)
    {
        return recipeService.SaveRecipeAsync(complexObject, recipeName, recipeInstructions);
    }
}

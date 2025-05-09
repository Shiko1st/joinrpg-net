using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain;

public static class OrderingExtensions
{
    public static IReadOnlyList<Character> GetOrderedCharacters(this CharacterGroup characterGroup) => characterGroup.GetCharactersContainer().OrderedItems;

    public static VirtualOrderContainer<Character> GetCharactersContainer(this CharacterGroup characterGroup)
      => VirtualOrderContainerFacade.Create(characterGroup.Characters, characterGroup.ChildCharactersOrdering);

    public static IReadOnlyList<CharacterGroup> GetOrderedChildGroups(this CharacterGroup characterGroup)
      => characterGroup.GetCharacterGroupsContainer().OrderedItems;

    public static VirtualOrderContainer<CharacterGroup> GetCharacterGroupsContainer(this CharacterGroup characterGroup)
      => VirtualOrderContainerFacade.Create(characterGroup.ChildGroups, characterGroup.ChildGroupsOrdering);

    public static IReadOnlyList<PlotElement> GetOrderedPlots(this Character character, IReadOnlyCollection<PlotElement> elements)
      => character.GetCharacterPlotContainer(elements).OrderedItems;

    public static IReadOnlyList<PlotElementTexts> GetOrderedPlots(this Character character, IReadOnlyCollection<PlotElementTexts> elements)
  => character.GetCharacterPlotContainer(elements).OrderedItems;

    public static VirtualOrderContainer<PlotElement> GetCharacterPlotContainer(this Character character,
      IReadOnlyCollection<PlotElement> plots) => VirtualOrderContainerFacade.Create(plots.OrderBy(pe => pe.PlotFolderId), character.PlotElementOrderData, preserveOrder: true);

    public static VirtualOrderContainer<PlotElementTexts> GetCharacterPlotContainer(this Character character,
  IReadOnlyCollection<PlotElementTexts> plots) => VirtualOrderContainerFacade.Create(plots, character.PlotElementOrderData, preserveOrder: true);

    public static IReadOnlyList<ProjectFieldDropdownValue> GetOrderedValues(this ProjectField field)
      => field.GetFieldValuesContainer().OrderedItems;

    public static VirtualOrderContainer<ProjectFieldDropdownValue> GetFieldValuesContainer(
      this ProjectField field)
      => VirtualOrderContainerFacade.Create(field.DropdownValues, field.ValuesOrdering);

    public static IReadOnlyList<ProjectField> GetOrderedFields(this Project field)
      => field.GetFieldsContainer().OrderedItems;

    public static VirtualOrderContainer<ProjectField> GetFieldsContainer(
      this Project field)
      => VirtualOrderContainerFacade.Create(field.ProjectFields, field.Details.FieldsOrdering);

    public static VirtualOrderContainer<PlotFolder> GetPlotFoldersContainer(this Project field)
        => VirtualOrderContainerFacade.Create(field.PlotFolders, field.Details.PlotFoldersOrdering);

    public static IReadOnlyList<PlotFolder> GetOrderedPlotFolders(this Project field) => field.GetPlotFoldersContainer().OrderedItems;

    public static VirtualOrderContainer<PlotElement> GetPlotElementsContainer(this PlotFolder field)
        => VirtualOrderContainerFacade.Create(field.Elements, field.ElementsOrdering);
}

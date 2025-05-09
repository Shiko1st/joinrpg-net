using JoinRpg.Data.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.ProjectCommon;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.AdminTools;

public record class AdminHotRoleViewModel(CharacterLinkSlimViewModel CharacterLink, string ProjectName, MarkupString CharacterDesc, MarkupString ProjectDesc)
{
    public AdminHotRoleViewModel(CharacterWithProject c)
        : this(new CharacterLinkSlimViewModel(c, true), c.ProjectName,
            c.CharacterDesc.TakeWords(50).ToHtmlString(), c.ProjectDesc.TakeWords(50).ToHtmlString())
    {

    }
}

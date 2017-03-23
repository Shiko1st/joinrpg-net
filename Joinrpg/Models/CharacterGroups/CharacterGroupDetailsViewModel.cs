﻿using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.Web.Models.CharacterGroups
{
  public enum GroupNavigationPage
  {
    None,
    Home,
    Roles,
    ClaimsActive,
    ClaimsDiscussing,
    ClaimsDirect,
    Characters,
    Report,
    Forums,
  }

  public class CharacterGroupDetailsViewModel : CharacterGroupWithDescViewModel
  {
    public GroupNavigationPage Page { get; }

    public CharacterGroupDetailsViewModel(CharacterGroup group, int? currentUser, GroupNavigationPage page) : base(group)
    {
      Page = page;
      HasMasterAccess = group.HasMasterAccess(currentUser);
      ShowEditControls = group.HasEditRolesAccess(currentUser);
      IsSpecial = group.IsSpecial;
      AvaiableDirectSlots = group.HaveDirectSlots ? group.AvaiableDirectSlots : 0;
      IsAcceptingClaims = group.IsAcceptingClaims();
      ActiveClaimsCount = group.Claims.Count(c => c.IsActive);
      IsRootGroup = group.IsRoot;
    }

    public bool HasMasterAccess { get;  }
    public bool ShowEditControls { get;  }
    public bool IsSpecial { get; }
    public int AvaiableDirectSlots { get; }
    public int ActiveClaimsCount { get; }
    public bool IsAcceptingClaims { get; }
    public bool IsRootGroup { get; }
  }
}
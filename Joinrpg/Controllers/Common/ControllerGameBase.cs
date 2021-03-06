using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.Exporters;

namespace JoinRpg.Web.Controllers.Common
{
  [JoinRpgExceptionHandler]
  public abstract class ControllerGameBase : ControllerBase
  {
    [ProvidesContext, NotNull]
    protected IProjectService ProjectService { get; }
    [ProvidesContext, NotNull]
    public IProjectRepository ProjectRepository { get; }

    protected ControllerGameBase(
        IProjectRepository projectRepository,
        IProjectService projectService,
        IUserRepository userRepository) : base(userRepository)
        {
      if (projectRepository == null) throw new ArgumentNullException(nameof(projectRepository));
      ProjectRepository = projectRepository;
      ProjectService = projectService;
    }

    protected ActionResult NoAccesToProjectView(Project project)
    {
      return View("ErrorNoAccessToProject", new ErrorNoAccessToProjectViewModel(project));
    }


    protected override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        var projectIdValue = ValueProvider.GetValue("projectid");
        if (projectIdValue != null)
        {
            var projectIdRawValue = projectIdValue.RawValue;
            var projectId = projectIdRawValue.GetType().IsArray
                ? int.Parse(((string[]) projectIdRawValue)[0])
                : int.Parse((string) projectIdRawValue);

                filterContext.HttpContext.Items[HttpContextItemHelpers.ProjectidKey] = projectIdValue;

                var project = ProjectRepository.GetProjectWithDetailsAsync(projectId).Result;
            RegisterProjectMenu(project);
        }

        base.OnActionExecuting(filterContext);
    }

        private void RegisterProjectMenu(Project project)
        {
            ViewBag.ProjectId = project.ProjectId;

            var acl = project.ProjectAcls.FirstOrDefault(a => a.UserId == CurrentUserIdOrDefault);

            MenuViewModelBase menuModel;
            if (acl != null)
            {
                menuModel = new MasterMenuViewModel()
                {
                    AccessToProject = acl,
                    CheckInModuleEnabled = project.Details.EnableCheckInModule,
                    ShowSchedule = project.Details.ScheduleSettings != null,
                };
            }
            else
            {
                var claims = project.Claims.OfUserActive(CurrentUserIdOrDefault).Select(c => new ClaimShortListItemViewModel(c)).ToArray();
                menuModel = new PlayerMenuViewModel()
                {
                    Claims = claims,
                    PlotPublished = project.Details.PublishPlot,
                };

                if (claims.Any(c => c.IsApproved))
                {
                    menuModel.ShowSchedule = project.Details.ScheduleSettings != null && project.Details.ScheduleSettings.RoomField.CanPlayerView;
                }
                else
                {
                    menuModel.ShowSchedule = project.Details.ScheduleSettings != null && project.Details.ScheduleSettings.RoomField.IsPublic;
                }
            }
            menuModel.ProjectId = project.ProjectId;
            menuModel.ProjectName = project.ProjectName;
            //TODO[GroupsLoad]. If we not loaded groups already, that's slow
            menuModel.BigGroups = project.RootGroup.ChildGroups.Where(
                cg => !cg.IsSpecial && cg.IsActive && cg.IsVisible(CurrentUserIdOrDefault))
              .Select(cg => new CharacterGroupLinkViewModel(cg)).ToList();
            menuModel.IsAcceptingClaims = project.IsAcceptingClaims;
            menuModel.IsActive = project.Active;
            menuModel.RootGroupId = project.RootGroup.CharacterGroupId;
            menuModel.EnableAccommodation = project.Details.EnableAccommodation;
            menuModel.IsAdmin = IsCurrentUserAdmin();

            if (acl != null)
            {
                ViewBag.MasterMenu = menuModel;
            }
            else
            {
                ViewBag.PlayerMenu = menuModel;
            }
        }

      protected IReadOnlyDictionary<int, string> GetCustomFieldValuesFromPost() =>
          GetDynamicValuesFromPost(FieldValueViewModel.HtmlIdPrefix);

        [Obsolete]
    protected async Task<Project> GetProjectFromList(int projectId, IEnumerable<IProjectEntity> folders)
    {
      return folders.FirstOrDefault()?.Project ?? await ProjectRepository.GetProjectAsync(projectId);
    }


        protected ActionResult RedirectToIndex(Project project)
        {
            return RedirectToAction("Index", "GameGroups", new { project.ProjectId, area = "" });
        }

    protected ActionResult RedirectToIndex(int projectId, int characterGroupId, [AspMvcAction] string action = "Index")
    {
      return RedirectToAction(action, "GameGroups", new {projectId, characterGroupId, area = ""});
    }

    protected async Task<ActionResult> RedirectToProject(int projectId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      return project == null ? HttpNotFound() : RedirectToIndex(project);
    }

    protected static ExportType? GetExportTypeByName(string export) => ExportTypeNameParserHelper.ToExportType(export);


    protected async Task<FileContentResult> ReturnExportResult(string fileName, IExportGenerator generator)
    {
      return File(await generator.Generate(), generator.ContentType,
        Path.ChangeExtension(fileName.ToSafeFileName(), generator.FileExtension));
    }
  }
}

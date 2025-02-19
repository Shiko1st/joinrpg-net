using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.ProjectCommon.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;
[Route("/webapi/project/[action]")]
[Authorize]
public class ProjectCreateController(ILogger<ProjectCreateController> logger) : ControllerBase
{
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Create([FromBody] ProjectCreateViewModel model, [FromServices] ICreateProjectService createProjectService)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        try
        {
            var request = CreateProjectRequest.Create(new ProjectName(model.ProjectName),
                (ProjectTypeDto)model.ProjectType,
                model.CopyFromProjectId,
                (ProjectCopySettingsDto)model.CopySettings
                );
            var result = await createProjectService.CreateProject(request);

            return result switch
            {
                FaildToCreateProjectResult failed => Problem(title: "Произошла ошибка при обработке запроса", detail: failed.Message, statusCode: 500),
                PartiallySuccessCreateProjectResult partially => Ok(new ProjectCreateResultViewModel(partially.ProjectId, partially.Message)),
                SuccessCreateProjectResult successCreateProjectResult => Ok(new ProjectCreateResultViewModel(successCreateProjectResult.ProjectId, Error: null)),
                _ => Ok(result)
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error creating project");
            return Problem(title: "Произошла ошибка при обработке запроса", detail: exception.Message, statusCode: 500);
        }
    }
}

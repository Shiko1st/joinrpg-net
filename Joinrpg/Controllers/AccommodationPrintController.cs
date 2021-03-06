using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Accommodation;
using JoinRpg.Web.Filter;

namespace JoinRpg.Web.Controllers
{
    [MasterAuthorize()]
    public class AccommodationPrintController : Common.ControllerGameBase
    {
        private IAccommodationRepository AccommodationRepository { get; }
        private IUriService UriService { get; }

        private IExportDataService ExportDataService { get; }

        public AccommodationPrintController(
            IProjectRepository projectRepository,
            IProjectService projectService,
            IExportDataService exportDataService,
            IUriService uriService,
            IAccommodationRepository accommodationRepository,
            IUserRepository userRepository) : base(projectRepository,
                projectService,
                userRepository)
        {
            UriService = uriService;
            AccommodationRepository = accommodationRepository;
            ExportDataService = exportDataService;
        }

        [HttpGet]
        public async Task<ActionResult> MainReport(int projectId, string export)
        {

            var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId)
                .ConfigureAwait(false);

            if (project == null) return HttpNotFound();
            if (!project.Details.EnableAccommodation) return RedirectToAction("Edit", "Game");

            var accommodations =
                (await AccommodationRepository.GetClaimAccommodationReport(projectId)).Select(row => new AccomodationReportListItemViewModel
                {
                    ProjectId = project.ProjectId,
                    ClaimId = row.ClaimId,
                    AccomodationType = row.AccomodationType,
                    RoomName = row.RoomName,
                    DisplayName = row.User.GetDisplayName(),
                    FullName = row.User.FullName,
                    Phone = row.User.Extra?.PhoneNumber,
                });
            
            var exportType = GetExportTypeByName(export);

            if (exportType == null)
            {

                return HttpNotFound();
            }
            else
            {
                var generator = ExportDataService.GetGenerator(exportType.Value, accommodations, new AccomodationReportExporter(UriService));

                return await ReturnExportResult(project.ProjectName + ": " + "Отчет по расселению", generator);
            }
        }

    }
}

using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models.Plot;

public enum PlotStatus
{
    [Display(Name = "В работе")]
    InWork,
    [Display(Name = "Закончен")]
    Completed,
    [Display(Name = "Удален")]
    Deleted,
    [Display(Name = "Есть новая версия")]
    HasNewVersion,
}

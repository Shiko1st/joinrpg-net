using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Models.Subscribe;

public interface ISubscribeTarget : ILinkable
{
    public string Name { get; }
}

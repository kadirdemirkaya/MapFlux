namespace MapFlux
{
    public interface IMapper 
    {
        TDestination Map<TSource, TDestination>(TSource source);
        void CreateMap<TProfile>() where TProfile : Profile, new();
    }
}

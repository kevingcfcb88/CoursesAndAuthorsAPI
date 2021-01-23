namespace CourseLibrary.API.Services
{
    public interface IPropertyChecker
    {
        bool TypeHasProperties<T>(string fields);
    }
}
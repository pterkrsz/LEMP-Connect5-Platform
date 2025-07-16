namespace LEMP.Application.Interfaces;

public interface IDataPointService
{
    Task WriteAsync<T>(T point);
}
